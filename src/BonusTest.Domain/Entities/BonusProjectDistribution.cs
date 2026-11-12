namespace BonusTest.Domain.Entities;

/// <summary>Parte del bruto de un bono pagado que corresponde a un proyecto del empleado.</summary>
public class BonusProjectDistribution
{
    public int BonusProjectDistributionId { get; private set; }
    public int BonusId { get; private set; }
    public int EmployeeProjectHoursId { get; private set; }
    public string ProjectName { get; private set; } = null!;
    public decimal Hours { get; private set; }
    public decimal Amount { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private BonusProjectDistribution() { }

    internal BonusProjectDistribution(EmployeeProjectHours project, decimal amount, string userCode, DateTime now)
    {
        EmployeeProjectHoursId = project.EmployeeProjectHoursId;
        ProjectName = project.ProjectName;
        Hours = project.Hours;
        Amount = amount;
        CreatedBy = userCode;
        CreatedAt = now;
    }
}
