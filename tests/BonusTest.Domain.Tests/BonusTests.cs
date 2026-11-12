using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Domain.Tests;

public class BonusTests
{
    private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0);

    private static readonly Employee Employee = new()
    {
        DepartmentCode = "TECH1",
        EmployeeNumber = 101,
        FullName = "Andrea Torres Vega",
        MonthlySalary = 5000m,
        TaxRate = 8m,
        EmployeeStateCode = EmployeeStates.Active
    };

    private static Bonus NewBonus() =>
        Bonus.Register(Employee, "2026-11", BonusTypes.SalaryPercentage, 10m, "000001", "creator", Now);

    [Fact]
    public void Register_StartsAsRegisteredWithCalculatedAmounts()
    {
        var bonus = NewBonus();

        Assert.Equal(BonusStates.Registered, bonus.BonusStateCode);
        Assert.Equal((500m, 40m, 460m), (bonus.GrossAmount, bonus.TaxAmount, bonus.NetAmount));
        Assert.Equal(("creator", Now), (bonus.CreatedBy, bonus.CreatedAt));
        Assert.False(bonus.IsDeleted);
    }

    [Fact]
    public void Cancel_FromRegistered_ChangesStateAndAudits()
    {
        var bonus = NewBonus();

        bonus.Cancel("editor", Now);

        Assert.Equal(BonusStates.Cancelled, bonus.BonusStateCode);
        Assert.Equal("editor", bonus.ModifiedBy);
    }

    [Fact]
    public void Delete_FromRegistered_IsLogical()
    {
        var bonus = NewBonus();

        bonus.Delete("editor", Now);

        Assert.True(bonus.IsDeleted);
        Assert.Equal(BonusStates.Registered, bonus.BonusStateCode);
    }

    [Fact]
    public void CancelOrDelete_WhenNotRegistered_Throws()
    {
        var bonus = NewBonus();
        bonus.Cancel("editor", Now);

        var ex = Assert.Throws<BusinessException>(() => bonus.Delete("editor", Now));
        Assert.Equal("BONUS_NOT_REGISTERED", ex.Code);
        Assert.Throws<BusinessException>(() => bonus.Cancel("editor", Now));
    }

    [Fact]
    public void Pay_DistributesGrossByHoursAndMarksPaid()
    {
        var bonus = NewBonus();
        List<EmployeeProjectHours> projects =
        [
            new() { EmployeeProjectHoursId = 1, ProjectName = "Portal de clientes", Hours = 120m },
            new() { EmployeeProjectHoursId = 2, ProjectName = "App móvil", Hours = 40m }
        ];

        bonus.Pay(projects, "payer", Now);

        Assert.Equal(BonusStates.Paid, bonus.BonusStateCode);
        Assert.Equal([375m, 125m], bonus.Distributions.Select(d => d.Amount));
        Assert.Equal(bonus.GrossAmount, bonus.Distributions.Sum(d => d.Amount));
    }

    [Fact]
    public void Pay_WithoutProjectHours_Throws()
    {
        var bonus = NewBonus();

        var ex = Assert.Throws<BusinessException>(() => bonus.Pay([], "payer", Now));
        Assert.Equal("NO_PROJECT_HOURS", ex.Code);
        Assert.Equal(BonusStates.Registered, bonus.BonusStateCode);
    }
}
