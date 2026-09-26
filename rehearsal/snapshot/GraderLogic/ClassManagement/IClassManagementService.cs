namespace GraderLogic.ClassManagement;

/// <summary>
/// Use cases for managing school classes and their students.
/// </summary>
public interface IClassManagementService
{
    /// <summary>
    /// Returns all school years, newest first.
    /// </summary>
    Task<IReadOnlyList<SchoolYearSummary>> GetSchoolYearsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the classes of a school year, sorted by name.
    /// </summary>
    /// <exception cref="NotFoundException">The school year does not exist.</exception>
    Task<IReadOnlyList<ClassSummary>> GetClassesAsync(int schoolYearId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a class incl. its students.
    /// </summary>
    /// <exception cref="NotFoundException">The class does not exist.</exception>
    Task<ClassDetails> GetClassAsync(int classId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a class incl. its students.
    /// </summary>
    /// <exception cref="NotFoundException">The school year does not exist.</exception>
    /// <exception cref="BusinessRuleException">A student has an id.</exception>
    /// <exception cref="ConflictException">The name is already taken in the school year.</exception>
    Task<ClassDetails> CreateClassAsync(int schoolYearId, SaveClassRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a class incl. the complete list of its students: adds, updates and removes students.
    /// </summary>
    /// <exception cref="NotFoundException">The class does not exist.</exception>
    /// <exception cref="BusinessRuleException">A student id is not a student of the class or appears twice.</exception>
    /// <exception cref="ConflictException">The name is already taken, or a removed student has exam results.</exception>
    Task<ClassDetails> SaveClassAsync(int classId, SaveClassRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a class incl. its students.
    /// </summary>
    /// <exception cref="NotFoundException">The class does not exist.</exception>
    /// <exception cref="ConflictException">The class has exams.</exception>
    Task DeleteClassAsync(int classId, CancellationToken cancellationToken);
}
