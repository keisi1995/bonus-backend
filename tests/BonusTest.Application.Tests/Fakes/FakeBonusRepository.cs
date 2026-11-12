using BonusTest.Application.Bonuses;
using BonusTest.Application.Common;
using BonusTest.Domain.Entities;

namespace BonusTest.Application.Tests.Fakes;

/// <summary>
/// Repositorio en memoria: cada prueba configura lo que devuelve y luego revisa
/// qué se agregó y cuántas veces se guardó.
/// </summary>
public class FakeBonusRepository : IBonusRepository
{
    public Employee? Employee { get; set; }
    public Bonus? Bonus { get; set; }
    public List<EmployeeProjectHours> ProjectHours { get; set; } = [];
    public bool ReasonIsActive { get; set; } = true;
    public bool HasActiveBonus { get; set; }

    public List<Bonus> Added { get; } = [];
    public int SaveChangesCount { get; private set; }

    public Task<PagedResponse<BonusListItemDto>> GetPagedAsync(BonusFilterRequest filter, CancellationToken cancellationToken) =>
        throw new NotSupportedException("El listado se prueba contra la base de datos, no aquí.");

    public Task<Employee?> FindEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken) =>
        Task.FromResult(Employee is not null
                        && Employee.DepartmentCode == departmentCode
                        && Employee.EmployeeNumber == employeeNumber
            ? Employee
            : null);

    public Task<IReadOnlyList<EmployeeProjectHours>> GetProjectHoursAsync(
        string departmentCode, int employeeNumber, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EmployeeProjectHours>>(ProjectHours);

    public Task<bool> IsActiveReasonAsync(string reasonCode, CancellationToken cancellationToken) =>
        Task.FromResult(ReasonIsActive);

    public Task<bool> HasActiveBonusAsync(
        string departmentCode, int employeeNumber, string period, CancellationToken cancellationToken) =>
        Task.FromResult(HasActiveBonus);

    public Task<Bonus?> FindAsync(int bonusId, CancellationToken cancellationToken) =>
        Task.FromResult(Bonus);

    public void Add(Bonus bonus) => Added.Add(bonus);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCount++;
        return Task.CompletedTask;
    }
}
