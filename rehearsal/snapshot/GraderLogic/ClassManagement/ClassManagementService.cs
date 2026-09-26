using GraderLogic.Data;
using Microsoft.EntityFrameworkCore;

namespace GraderLogic.ClassManagement;

public class ClassManagementService(GraderDbContext dbContext) : IClassManagementService
{
    public async Task<IReadOnlyList<SchoolYearSummary>> GetSchoolYearsAsync(CancellationToken cancellationToken) =>
        await dbContext.SchoolYears
            // Names like 2025/26 sort chronologically.
            .OrderByDescending(schoolYear => schoolYear.Name)
            .Select(schoolYear => new SchoolYearSummary(schoolYear.Id, schoolYear.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClassSummary>> GetClassesAsync(int schoolYearId, CancellationToken cancellationToken)
    {
        await EnsureSchoolYearExistsAsync(schoolYearId, cancellationToken);

        return await dbContext.SchoolClasses
            .Where(schoolClass => schoolClass.SchoolYearId == schoolYearId)
            .OrderBy(schoolClass => schoolClass.Name)
            .Select(schoolClass => new ClassSummary(schoolClass.Id, schoolClass.Name, schoolClass.Students.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClassDetails> GetClassAsync(int classId, CancellationToken cancellationToken) =>
        await dbContext.SchoolClasses
            .Where(schoolClass => schoolClass.Id == classId)
            .Select(schoolClass => new ClassDetails(
                schoolClass.Id,
                schoolClass.Name,
                schoolClass.SchoolYearId,
                schoolClass.SchoolYear.Name,
                schoolClass.Students
                    .OrderBy(student => student.LastName)
                    .ThenBy(student => student.FirstName)
                    .ThenBy(student => student.Id)
                    .Select(student => new ClassStudent(student.Id, student.FirstName, student.LastName))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw ClassNotFound(classId);

    public async Task<ClassDetails> CreateClassAsync(int schoolYearId, SaveClassRequest request, CancellationToken cancellationToken)
    {
        await EnsureSchoolYearExistsAsync(schoolYearId, cancellationToken);
        await EnsureNameIsFreeAsync(schoolYearId, request.Name, classId: null, cancellationToken);
        if (request.Students.FirstOrDefault(student => student.Id is not null) is { } studentWithId)
        {
            throw UnknownStudent(studentWithId.Id!.Value);
        }

        var schoolClass = new SchoolClass
        {
            Name = request.Name,
            SchoolYearId = schoolYearId,
            Students = [.. request.Students.Select(student => new Student { FirstName = student.FirstName, LastName = student.LastName })],
        };
        dbContext.SchoolClasses.Add(schoolClass);
        await dbContext.SaveChangesAndTranslateErrorsAsync(cancellationToken);

        return await GetClassAsync(schoolClass.Id, cancellationToken);
    }

    public async Task<ClassDetails> SaveClassAsync(int classId, SaveClassRequest request, CancellationToken cancellationToken)
    {
        var schoolClass = await dbContext.SchoolClasses
            .Include(schoolClass => schoolClass.Students)
            .SingleOrDefaultAsync(schoolClass => schoolClass.Id == classId, cancellationToken)
            ?? throw ClassNotFound(classId);
        await EnsureNameIsFreeAsync(schoolClass.SchoolYearId, request.Name, classId, cancellationToken);

        var existingStudents = schoolClass.Students.ToDictionary(student => student.Id);
        var requestedIds = new HashSet<int>();
        foreach (var id in request.Students.Where(student => student.Id is not null).Select(student => student.Id!.Value))
        {
            if (!existingStudents.ContainsKey(id))
            {
                throw UnknownStudent(id);
            }

            if (!requestedIds.Add(id))
            {
                throw new BusinessRuleException("duplicate-student", $"Student {id} appears more than once.");
            }
        }

        var removedStudents = schoolClass.Students.Where(student => !requestedIds.Contains(student.Id)).ToList();
        var removedIds = removedStudents.Select(student => student.Id).ToList();
        if (await dbContext.ExamResults.AnyAsync(result => removedIds.Contains(result.StudentId), cancellationToken))
        {
            throw new ConflictException(
                "student-has-results",
                "Students with exam results cannot be removed from the class. Delete their exam results first.");
        }

        schoolClass.Name = request.Name;
        dbContext.Students.RemoveRange(removedStudents);
        foreach (var student in request.Students)
        {
            if (student.Id is { } id)
            {
                existingStudents[id].FirstName = student.FirstName;
                existingStudents[id].LastName = student.LastName;
            }
            else
            {
                schoolClass.Students.Add(new Student { FirstName = student.FirstName, LastName = student.LastName });
            }
        }

        await dbContext.SaveChangesAndTranslateErrorsAsync(cancellationToken);

        return await GetClassAsync(classId, cancellationToken);
    }

    public async Task DeleteClassAsync(int classId, CancellationToken cancellationToken)
    {
        var schoolClass = await dbContext.SchoolClasses
            .Include(schoolClass => schoolClass.Students)
            .SingleOrDefaultAsync(schoolClass => schoolClass.Id == classId, cancellationToken)
            ?? throw ClassNotFound(classId);
        if (await dbContext.Exams.AnyAsync(exam => exam.SchoolClassId == classId, cancellationToken))
        {
            throw new ConflictException("class-has-exams", "A class with exams cannot be deleted. Delete its exams first.");
        }

        dbContext.Students.RemoveRange(schoolClass.Students);
        dbContext.SchoolClasses.Remove(schoolClass);
        await dbContext.SaveChangesAndTranslateErrorsAsync(cancellationToken);
    }

    private async Task EnsureSchoolYearExistsAsync(int schoolYearId, CancellationToken cancellationToken)
    {
        if (!await dbContext.SchoolYears.AnyAsync(schoolYear => schoolYear.Id == schoolYearId, cancellationToken))
        {
            throw new NotFoundException("school-year-not-found", $"School year {schoolYearId} does not exist.");
        }
    }

    private async Task EnsureNameIsFreeAsync(int schoolYearId, string name, int? classId, CancellationToken cancellationToken)
    {
        if (await dbContext.SchoolClasses.AnyAsync(
            schoolClass => schoolClass.SchoolYearId == schoolYearId && schoolClass.Name == name && schoolClass.Id != classId,
            cancellationToken))
        {
            throw new ConflictException("class-name-taken", $"The school year already has a class named '{name}'.");
        }
    }

    private static NotFoundException ClassNotFound(int classId) => new("class-not-found", $"Class {classId} does not exist.");

    private static BusinessRuleException UnknownStudent(int studentId) =>
        new("unknown-student", $"Student {studentId} is not a student of this class.");
}
