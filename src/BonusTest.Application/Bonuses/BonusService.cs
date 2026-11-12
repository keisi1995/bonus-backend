using System.Text.RegularExpressions;
using BonusTest.Application.Common;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Application.Bonuses;

/// <summary>
/// Casos de uso del módulo de bonos. Aquí se validan las reglas de negocio;
/// el controlador solo traduce HTTP ↔ llamadas a este servicio.
/// </summary>
public partial class BonusService(IBonusRepository repository, TimeProvider timeProvider)
{
    private const int MaxUserCodeLength = 50;

    public Task<PagedResponse<BonusListItemDto>> GetPagedAsync(BonusFilterRequest filter, CancellationToken cancellationToken) =>
        repository.GetPagedAsync(filter, cancellationToken);

    public async Task<EmployeeDto> GetActiveEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken)
    {
        var employee = await GetActiveEmployeeEntityAsync(departmentCode, employeeNumber, cancellationToken);
        return new EmployeeDto(employee.DepartmentCode, employee.EmployeeNumber, employee.FullName,
            employee.HireDate, employee.MonthlySalary, employee.TaxRate);
    }

    public async Task<BonusCreatedDto> CreateAsync(CreateBonusRequest request, string? userCode, CancellationToken cancellationToken)
    {
        var user = ValidateUserCode(userCode);
        var employee = await GetActiveEmployeeEntityAsync(request.DepartmentCode, request.EmployeeNumber, cancellationToken);

        ValidatePeriod(request.Period);
        ValidateBonusValue(request.BonusTypeId, request.BonusValue, employee.MonthlySalary);

        if (!await repository.IsActiveReasonAsync(request.ReasonCode, cancellationToken))
            throw new BusinessException("REASON_NOT_ACTIVE", "El motivo seleccionado no existe o no está activo.");

        if (await repository.HasActiveBonusAsync(employee.DepartmentCode, employee.EmployeeNumber, request.Period, cancellationToken))
            throw new BusinessException("BONUS_ALREADY_EXISTS", $"El empleado ya tiene un bono activo en el periodo {request.Period}.");

        var bonus = Bonus.Register(employee, request.Period, request.BonusTypeId, request.BonusValue,
            request.ReasonCode, user, Now());
        repository.Add(bonus);
        await repository.SaveChangesAsync(cancellationToken);

        return new BonusCreatedDto(bonus.BonusId, bonus.GrossAmount, bonus.TaxAmount, bonus.NetAmount);
    }

    public async Task CancelAsync(int bonusId, string? userCode, CancellationToken cancellationToken)
    {
        var user = ValidateUserCode(userCode);
        var bonus = await GetBonusAsync(bonusId, cancellationToken);

        bonus.Cancel(user, Now());
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int bonusId, string? userCode, CancellationToken cancellationToken)
    {
        var user = ValidateUserCode(userCode);
        var bonus = await GetBonusAsync(bonusId, cancellationToken);

        bonus.Delete(user, Now());
        await repository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Distribuye el bruto por proyecto y pasa el bono a Pagado. El detalle y el cambio de estado
    /// se guardan en un único SaveChanges, que EF Core ejecuta dentro de una sola transacción.
    /// </summary>
    public async Task<BonusPaymentDto> PayAsync(int bonusId, string? userCode, CancellationToken cancellationToken)
    {
        var user = ValidateUserCode(userCode);
        var bonus = await GetBonusAsync(bonusId, cancellationToken);
        var projects = await repository.GetProjectHoursAsync(bonus.DepartmentCode, bonus.EmployeeNumber, cancellationToken);

        bonus.Pay(projects, user, Now());
        await repository.SaveChangesAsync(cancellationToken);

        return new BonusPaymentDto(bonus.BonusId, bonus.GrossAmount,
            bonus.Distributions.Select(d => new BonusDistributionDto(d.ProjectName, d.Hours, d.Amount)).ToList());
    }

    private async Task<Employee> GetActiveEmployeeEntityAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken)
    {
        var employee = await repository.FindEmployeeAsync(departmentCode, employeeNumber, cancellationToken)
            ?? throw new BusinessException("EMPLOYEE_NOT_FOUND", $"No existe el empleado {departmentCode}-{employeeNumber}.");

        if (employee.EmployeeStateCode != EmployeeStates.Active)
            throw new BusinessException("EMPLOYEE_NOT_ACTIVE", $"El empleado {departmentCode}-{employeeNumber} no está activo.");

        return employee;
    }

    private async Task<Bonus> GetBonusAsync(int bonusId, CancellationToken cancellationToken) =>
        await repository.FindAsync(bonusId, cancellationToken)
            ?? throw new BusinessException("BONUS_NOT_FOUND", $"No existe el bono {bonusId}.");

    private void ValidatePeriod(string period)
    {
        if (string.IsNullOrWhiteSpace(period) || !PeriodRegex().IsMatch(period))
            throw new BusinessException("INVALID_PERIOD", "El periodo debe tener el formato YYYY-MM.");

        // Con el formato YYYY-MM, la comparación de textos coincide con el orden cronológico.
        var currentPeriod = Now().ToString("yyyy-MM");
        if (string.CompareOrdinal(period, currentPeriod) > 0)
            throw new BusinessException("FUTURE_PERIOD", "El periodo no puede ser posterior al mes actual.");
    }

    private static void ValidateBonusValue(int bonusTypeId, decimal bonusValue, decimal monthlySalary)
    {
        switch (bonusTypeId)
        {
            case BonusTypes.FixedAmount when bonusValue <= 0 || bonusValue > monthlySalary:
                throw new BusinessException("INVALID_AMOUNT",
                    $"El monto debe ser mayor a 0 y no mayor al sueldo mensual ({monthlySalary:N2}).");
            case BonusTypes.SalaryPercentage when bonusValue <= 0 || bonusValue > 100:
                throw new BusinessException("INVALID_PERCENTAGE", "El porcentaje debe ser mayor a 0 y hasta 100.");
            case BonusTypes.FixedAmount or BonusTypes.SalaryPercentage:
                if (decimal.Round(bonusValue, 2) != bonusValue)
                    throw new BusinessException("INVALID_DECIMALS", "El monto o porcentaje admite como máximo 2 decimales.");
                return;
            default:
                throw new BusinessException("INVALID_BONUS_TYPE", "El tipo de bono no es válido.");
        }
    }

    private static string ValidateUserCode(string? userCode)
    {
        if (string.IsNullOrWhiteSpace(userCode) || userCode.Length > MaxUserCodeLength)
            throw new BusinessException("USER_REQUIRED", "El header UserCode es obligatorio (máximo 50 caracteres).");
        return userCode.Trim();
    }

    private DateTime Now() => timeProvider.GetLocalNow().DateTime;

    [GeneratedRegex(@"^\d{4}-(0[1-9]|1[0-2])$")]
    private static partial Regex PeriodRegex();
}
