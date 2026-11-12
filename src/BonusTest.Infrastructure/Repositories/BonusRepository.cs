using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using BonusTest.Application.Bonuses;
using BonusTest.Application.Common;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;
using BonusTest.Infrastructure.Persistence;

namespace BonusTest.Infrastructure.Repositories;

public class BonusRepository(AppDbContext context) : IBonusRepository
{
    // Errores de SQL Server por violación de índice único / UNIQUE constraint.
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public async Task<PagedResponse<BonusListItemDto>> GetPagedAsync(BonusFilterRequest filter, CancellationToken cancellationToken)
    {
        var bonuses = context.Bonuses.AsNoTracking().Where(b => !b.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.DepartmentCode))
            bonuses = bonuses.Where(b => b.DepartmentCode == filter.DepartmentCode.Trim());
        if (filter.EmployeeNumber is not null)
            bonuses = bonuses.Where(b => b.EmployeeNumber == filter.EmployeeNumber);
        if (!string.IsNullOrWhiteSpace(filter.Period))
            bonuses = bonuses.Where(b => b.Period == filter.Period.Trim());
        if (!string.IsNullOrWhiteSpace(filter.BonusStateCode))
            bonuses = bonuses.Where(b => b.BonusStateCode == filter.BonusStateCode.Trim());

        var totalRecords = await bonuses.CountAsync(cancellationToken);

        // Se traduce a un solo SELECT con JOINs + OFFSET/FETCH: la paginación ocurre en SQL Server.
        var data = await (
                from b in bonuses
                join e in context.Employees
                    on new { b.DepartmentCode, b.EmployeeNumber } equals new { e.DepartmentCode, e.EmployeeNumber }
                join d in context.Departments on b.DepartmentCode equals d.DepartmentCode
                join s in context.States on b.BonusStateCode equals s.StateCode
                join t in context.BonusTypes on b.BonusTypeId equals t.BonusTypeId
                join r in context.BonusReasons on b.ReasonCode equals r.ReasonCode
                orderby b.BonusId descending
                select new BonusListItemDto(
                    b.BonusId, b.BonusStateCode, s.StateDescription, b.Period,
                    b.DepartmentCode, d.DepartmentName, b.EmployeeNumber, e.FullName,
                    b.BonusTypeId, t.BonusTypeDescription, b.BonusValue,
                    b.ReasonCode, r.ReasonDescription,
                    b.GrossAmount, b.TaxAmount, b.NetAmount))
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResponse<BonusListItemDto>.Create(data, filter.PageIndex, filter.PageSize, totalRecords);
    }

    public Task<Employee?> FindEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken) =>
        context.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.DepartmentCode == departmentCode && e.EmployeeNumber == employeeNumber, cancellationToken);

    public async Task<IReadOnlyList<EmployeeProjectHours>> GetProjectHoursAsync(
        string departmentCode, int employeeNumber, CancellationToken cancellationToken) =>
        await context.EmployeeProjectHours.AsNoTracking()
            .Where(p => p.DepartmentCode == departmentCode && p.EmployeeNumber == employeeNumber)
            .OrderBy(p => p.EmployeeProjectHoursId)
            .ToListAsync(cancellationToken);

    public Task<bool> IsActiveReasonAsync(string reasonCode, CancellationToken cancellationToken) =>
        context.BonusReasons.AnyAsync(r => r.ReasonCode == reasonCode && r.State, cancellationToken);

    public Task<bool> HasActiveBonusAsync(
        string departmentCode, int employeeNumber, string period, CancellationToken cancellationToken) =>
        context.Bonuses.AnyAsync(b =>
            b.DepartmentCode == departmentCode &&
            b.EmployeeNumber == employeeNumber &&
            b.Period == period &&
            !b.IsDeleted &&
            b.BonusStateCode != BonusStates.Cancelled, cancellationToken);

    public Task<Bonus?> FindAsync(int bonusId, CancellationToken cancellationToken) =>
        context.Bonuses.FirstOrDefaultAsync(b => b.BonusId == bonusId && !b.IsDeleted, cancellationToken);

    public void Add(Bonus bonus) => context.Bonuses.Add(bonus);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException
            { Number: UniqueIndexViolation or UniqueConstraintViolation })
        {
            // Dos operaciones simultáneas pasaron la validación previa; el índice único de la BD frenó la segunda.
            throw new BusinessException("CONCURRENT_CONFLICT",
                "Otra operación modificó el mismo registro. Actualiza el listado e intenta nuevamente.");
        }
    }
}
