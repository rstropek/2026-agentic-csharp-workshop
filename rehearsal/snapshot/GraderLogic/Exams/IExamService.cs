namespace GraderLogic.Exams;

/// <summary>
/// Use cases for managing the exams of a class and their results.
/// </summary>
public interface IExamService
{
    /// <summary>
    /// Returns all courses, sorted by name.
    /// </summary>
    Task<IReadOnlyList<CourseSummary>> GetCoursesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the exams of a class, sorted by date.
    /// </summary>
    /// <exception cref="NotFoundException">The class does not exist.</exception>
    Task<IReadOnlyList<ExamSummary>> GetExamsAsync(int classId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns an exam incl. the results of all students of the class.
    /// </summary>
    /// <exception cref="NotFoundException">The exam does not exist.</exception>
    Task<ExamDetails> GetExamAsync(int examId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an exam of a class incl. its results.
    /// </summary>
    /// <exception cref="NotFoundException">The class does not exist.</exception>
    /// <exception cref="BusinessRuleException">Unknown course, or a result for a student who is not in the class or appears twice.</exception>
    /// <exception cref="ConflictException">The class already has an exam in the course on that date.</exception>
    Task<ExamDetails> CreateExamAsync(int classId, SaveExamRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Saves an exam incl. the complete list of its results: adds, updates and removes results.
    /// The class of an exam cannot be changed.
    /// </summary>
    /// <exception cref="NotFoundException">The exam does not exist.</exception>
    /// <exception cref="BusinessRuleException">Unknown course, or a result for a student who is not in the class or appears twice.</exception>
    /// <exception cref="ConflictException">The class already has another exam in the course on that date.</exception>
    Task<ExamDetails> SaveExamAsync(int examId, SaveExamRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an exam incl. its results.
    /// </summary>
    /// <exception cref="NotFoundException">The exam does not exist.</exception>
    Task DeleteExamAsync(int examId, CancellationToken cancellationToken);
}
