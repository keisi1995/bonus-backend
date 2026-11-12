using BonusTest.Application.Common;
using BonusTest.Domain.Entities;

namespace BonusTest.Application.Bonuses;

public interface IBonusRepository
{
    /// <summary>Listado filtrado y paginado en la base de datos. Excluye los eliminados.</summary>
    Task<PagedResponse<BonusListItemDto>> GetPagedAsync(BonusFilterRequest filter, CancellationToken cancellationToken);

    Task<Employee?> FindEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken);

    Task<IReadOnlyList<EmployeeProjectHours>> GetProjectHoursAsync(
        string departmentCode, int employeeNumber, CancellationToken cancellationToken);

    Task<bool> IsActiveReasonAsync(string reasonCode, CancellationToken cancellationToken);

    /// <summary>Indica si el empleado ya tiene en el periodo un bono no anulado y no eliminado.</summary>
    Task<bool> HasActiveBonusAsync(
        string departmentCode, int employeeNumber, string period, CancellationToken cancellationToken);

    /// <summary>Obtiene un bono no eliminado, con seguimiento de cambios para modificarlo.</summary>
    Task<Bonus?> FindAsync(int bonusId, CancellationToken cancellationToken);

    void Add(Bonus bonus);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
