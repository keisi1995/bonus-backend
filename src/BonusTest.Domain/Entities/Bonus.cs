using BonusTest.Domain.Constants;
using BonusTest.Domain.Exceptions;
using BonusTest.Domain.Services;

namespace BonusTest.Domain.Entities;

/// <summary>
/// Bono de desempeño. Los setters son privados: el estado solo cambia mediante los métodos
/// de la entidad, que validan las transiciones permitidas (solo desde Registrado).
/// </summary>
public class Bonus
{
    public int BonusId { get; private set; }
    public string DepartmentCode { get; private set; } = null!;
    public int EmployeeNumber { get; private set; }
    public string Period { get; private set; } = null!;
    public int BonusTypeId { get; private set; }
    public decimal BonusValue { get; private set; }
    public string ReasonCode { get; private set; } = null!;
    public decimal MonthlySalary { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public string BonusStateCode { get; private set; } = null!;
    public bool IsDeleted { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public string? ModifiedBy { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    public List<BonusProjectDistribution> Distributions { get; private set; } = [];

    // Constructor que usa EF Core al leer de la base de datos.
    private Bonus() { }

    /// <summary>Crea un bono en estado Registrado con los montos ya calculados.</summary>
    public static Bonus Register(
        Employee employee, string period, int bonusTypeId, decimal bonusValue, string reasonCode,
        string userCode, DateTime now)
    {
        var amounts = BonusCalculator.Calculate(bonusTypeId, bonusValue, employee.MonthlySalary, employee.TaxRate);

        return new Bonus
        {
            DepartmentCode = employee.DepartmentCode,
            EmployeeNumber = employee.EmployeeNumber,
            Period = period,
            BonusTypeId = bonusTypeId,
            BonusValue = bonusValue,
            ReasonCode = reasonCode,
            MonthlySalary = employee.MonthlySalary,
            TaxRate = employee.TaxRate,
            GrossAmount = amounts.GrossAmount,
            TaxAmount = amounts.TaxAmount,
            NetAmount = amounts.NetAmount,
            BonusStateCode = BonusStates.Registered,
            CreatedBy = userCode,
            CreatedAt = now
        };
    }

    public void Cancel(string userCode, DateTime now)
    {
        EnsureIsRegistered("anular");
        BonusStateCode = BonusStates.Cancelled;
        MarkModified(userCode, now);
    }

    /// <summary>Eliminación lógica: el registro se conserva en la tabla.</summary>
    public void Delete(string userCode, DateTime now)
    {
        EnsureIsRegistered("eliminar");
        IsDeleted = true;
        MarkModified(userCode, now);
    }

    /// <summary>Reparte el bruto entre los proyectos en proporción a sus horas y pasa el bono a Pagado.</summary>
    public void Pay(IReadOnlyList<EmployeeProjectHours> projects, string userCode, DateTime now)
    {
        EnsureIsRegistered("pagar");
        if (projects.Count == 0 || projects.Sum(p => p.Hours) <= 0)
            throw new BusinessException("NO_PROJECT_HOURS", "El empleado no tiene horas registradas en proyectos para distribuir el bono.");

        var amounts = BonusDistributor.Distribute(GrossAmount, projects.Select(p => p.Hours).ToList());
        Distributions.AddRange(projects.Select((project, i) =>
            new BonusProjectDistribution(project, amounts[i], userCode, now)));

        BonusStateCode = BonusStates.Paid;
        MarkModified(userCode, now);
    }

    private void EnsureIsRegistered(string action)
    {
        if (BonusStateCode != BonusStates.Registered)
            throw new BusinessException("BONUS_NOT_REGISTERED", $"Solo se puede {action} un bono en estado Registrado.");
    }

    private void MarkModified(string userCode, DateTime now)
    {
        ModifiedBy = userCode;
        ModifiedAt = now;
    }
}
