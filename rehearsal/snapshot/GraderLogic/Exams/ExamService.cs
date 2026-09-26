using GraderLogic.Data;
using Microsoft.EntityFrameworkCore;

namespace GraderLogic.Exams;

public class ExamService(GraderDbContext dbContext) : IExamService
{
    public async Task<IReadOnlyList<CourseSummary>> GetCoursesAsync(CancellationToken cancellationToken) =>
        await dbContext.Courses
            .OrderBy(course => course.Name)
            .Select(course => new CourseSummary(course.Id, course.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ExamSummary>> GetExamsAsync(int classId, CancellationToken cancellationToken)
    {
        await EnsureClassExistsAsync(classId, cancellationToken);

        return await dbContext.Exams
            .Where(exam => exam.SchoolClassId == classId)
            .OrderBy(exam => exam.ExamDate)
            .ThenBy(exam => exam.Course.Name)
            .Select(exam => new ExamSummary(exam.Id, exam.ExamDate, exam.CourseId, exam.Course.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<ExamDetails> GetExamAsync(int examId, CancellationToken cancellationToken)
    {
        var exam = await dbContext.Exams
            .Where(exam => exam.Id == examId)
            .Select(exam => new ExamDetails(
                exam.Id,
                exam.ExamDate,
                exam.CourseId,
                exam.Course.Name,
                exam.SchoolClassId,
                exam.SchoolClass.Name,
                exam.SchoolClass.SchoolYearId,
                exam.SchoolClass.SchoolYear.Name,
                Array.Empty<ExamStudentResult>()))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw ExamNotFound(examId);

        // Separate query: a subquery per student inside a nested collection projection needs SQL APPLY, which SQLite lacks.
        var results = await dbContext.Students
            .Where(student => student.SchoolClassId == exam.ClassId)
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .ThenBy(student => student.Id)
            .Select(student => new ExamStudentResult(
                student.Id,
                student.FirstName,
                student.LastName,
                student.ExamResults
                    .Where(result => result.ExamId == examId)
                    .Select(result => (double?)result.Percentage)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return exam with { Results = results };
    }

    public async Task<ExamDetails> CreateExamAsync(int classId, SaveExamRequest request, CancellationToken cancellationToken)
    {
        await EnsureClassExistsAsync(classId, cancellationToken);
        var percentages = await ValidateAsync(classId, examId: null, request, cancellationToken);

        var exam = new Exam
        {
            ExamDate = request.ExamDate,
            CourseId = request.CourseId,
            SchoolClassId = classId,
            ExamResults = [.. percentages.Select(entry => new ExamResult { StudentId = entry.Key, Percentage = entry.Value })],
        };
        dbContext.Exams.Add(exam);
        await dbContext.SaveChangesAndTranslateErrorsAsync(cancellationToken);

        return await GetExamAsync(exam.Id, cancellationToken);
    }

    public async Task<ExamDetails> SaveExamAsync(int examId, SaveExamRequest request, CancellationToken cancellationToken)
    {
        var exam = await dbContext.Exams
            .Include(exam => exam.ExamResults)
            .SingleOrDefaultAsync(exam => exam.Id == examId, cancellationToken)
            ?? throw ExamNotFound(examId);
        var percentages = await ValidateAsync(exam.SchoolClassId, examId, request, cancellationToken);

        exam.ExamDate = request.ExamDate;
        exam.CourseId = request.CourseId;
        dbContext.ExamResults.RemoveRange([.. exam.ExamResults.Where(result => !percentages.ContainsKey(result.StudentId))]);
        var existingResults = exam.ExamResults.ToDictionary(result => result.StudentId);
        foreach (var (studentId, percentage) in percentages)
        {
            if (existingResults.TryGetValue(studentId, out var result))
            {
                result.Percentage = percentage;
            }
            else
            {
                exam.ExamResults.Add(new ExamResult { StudentId = studentId, Percentage = percentage });
            }
        }

        await dbContext.SaveChangesAndTranslateErrorsAsync(cancellationToken);

        return await GetExamAsync(examId, cancellationToken);
    }

    public async Task DeleteExamAsync(int examId, CancellationToken cancellationToken)
    {
        var exam = await dbContext.Exams
            .Include(exam => exam.ExamResults)
            .SingleOrDefaultAsync(exam => exam.Id == examId, cancellationToken)
            ?? throw ExamNotFound(examId);

        dbContext.ExamResults.RemoveRange(exam.ExamResults);
        dbContext.Exams.Remove(exam);
        await dbContext.SaveChangesAndTranslateErrorsAsync(cancellationToken);
    }

    /// <summary>
    /// Checks the business rules for creating (<paramref name="examId"/> is <see langword="null"/>) or saving an exam.
    /// </summary>
    /// <returns>The percentage per student id for all results that are not <see langword="null"/>.</returns>
    private async Task<Dictionary<int, double>> ValidateAsync(
        int classId, int? examId, SaveExamRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Courses.AnyAsync(course => course.Id == request.CourseId, cancellationToken))
        {
            throw new BusinessRuleException("unknown-course", $"Course {request.CourseId} does not exist.");
        }

        var studentIds = await dbContext.Students
            .Where(student => student.SchoolClassId == classId)
            .Select(student => student.Id)
            .ToHashSetAsync(cancellationToken);
        var requestedIds = new HashSet<int>();
        foreach (var result in request.Results)
        {
            if (!studentIds.Contains(result.StudentId))
            {
                throw new BusinessRuleException("student-not-in-class", $"Student {result.StudentId} is not a student of the exam's class.");
            }

            if (!requestedIds.Add(result.StudentId))
            {
                throw new BusinessRuleException("duplicate-student", $"Student {result.StudentId} has more than one result.");
            }
        }

        await EnsureExamIsUniqueAsync(classId, examId, request, cancellationToken);

        return request.Results
            .Where(result => result.Percentage is not null)
            .ToDictionary(result => result.StudentId, result => result.Percentage!.Value);
    }

    private async Task EnsureClassExistsAsync(int classId, CancellationToken cancellationToken)
    {
        if (!await dbContext.SchoolClasses.AnyAsync(schoolClass => schoolClass.Id == classId, cancellationToken))
        {
            throw new NotFoundException("class-not-found", $"Class {classId} does not exist.");
        }
    }

    private async Task EnsureExamIsUniqueAsync(int classId, int? examId, SaveExamRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.Exams.AnyAsync(
            exam => exam.SchoolClassId == classId && exam.CourseId == request.CourseId && exam.ExamDate == request.ExamDate
                && exam.Id != examId,
            cancellationToken))
        {
            throw new ConflictException("exam-exists", "The class already has an exam in this course on this date.");
        }
    }

    private static NotFoundException ExamNotFound(int examId) => new("exam-not-found", $"Exam {examId} does not exist.");
}
