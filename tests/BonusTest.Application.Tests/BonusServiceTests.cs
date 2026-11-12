using BonusTest.Application.Bonuses;
using BonusTest.Application.Tests.Fakes;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Application.Tests;

public class BonusServiceTests
{
    // "Hoy" para todas las pruebas: el periodo actual es 2026-11.
    private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0);
    private const string User = "jperez";

    private readonly FakeBonusRepository _repository = new() { Employee = ActiveEmployee() };
    private readonly BonusService _service;

    public BonusServiceTests() => _service = new BonusService(_repository, new FixedTimeProvider(Now));

    private static Employee ActiveEmployee() => new()
    {
        DepartmentCode = "TECH1",
        EmployeeNumber = 101,
        FullName = "Andrea Torres Vega",
        HireDate = new DateTime(2020, 3, 1),
        MonthlySalary = 5000m,
        TaxRate = 8m,
        EmployeeStateCode = EmployeeStates.Active
    };

    private static CreateBonusRequest ValidRequest(
        string period = "2026-11", int bonusTypeId = BonusTypes.SalaryPercentage, decimal bonusValue = 10m) =>
        new("TECH1", 101, period, bonusTypeId, bonusValue, "000001");

    private static Bonus RegisteredBonus() =>
        Bonus.Register(ActiveEmployee(), "2026-11", BonusTypes.SalaryPercentage, 10m, "000001", "creator", Now);

    private async Task<BusinessException> AssertBusinessError(string expectedCode, Func<Task> action)
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(action);
        Assert.Equal(expectedCode, ex.Code);
        Assert.Empty(_repository.Added);
        Assert.Equal(0, _repository.SaveChangesCount);
        return ex;
    }

    private Task<BusinessException> AssertCreateFails(string expectedCode, CreateBonusRequest request, string? user = User) =>
        AssertBusinessError(expectedCode, () => _service.CreateAsync(request, user, CancellationToken.None));

    // ---------- CreateAsync: caso exitoso ----------

    [Fact]
    public async Task Create_ValidRequest_SavesBonusAndReturnsCalculatedAmounts()
    {
        var result = await _service.CreateAsync(ValidRequest(), "  jperez  ", CancellationToken.None);

        Assert.Equal((500m, 40m, 460m), (result.GrossAmount, result.TaxAmount, result.NetAmount));
        Assert.Equal(1, _repository.SaveChangesCount);

        var bonus = Assert.Single(_repository.Added);
        Assert.Equal(BonusStates.Registered, bonus.BonusStateCode);
        Assert.Equal(("TECH1", 101, "2026-11"), (bonus.DepartmentCode, bonus.EmployeeNumber, bonus.Period));
        Assert.Equal(User, bonus.CreatedBy);   // se guarda sin espacios
        Assert.Equal(Now, bonus.CreatedAt);    // la fecha viene del TimeProvider
    }

    [Theory]
    [InlineData("2026-11")] // mes actual
    [InlineData("2026-10")]
    [InlineData("2025-12")]
    public async Task Create_CurrentOrPastPeriod_IsAllowed(string period)
    {
        await _service.CreateAsync(ValidRequest(period: period), User, CancellationToken.None);

        Assert.Single(_repository.Added);
    }

    [Theory]
    [InlineData(BonusTypes.FixedAmount, 0.01)]
    [InlineData(BonusTypes.FixedAmount, 5000)]   // igual al sueldo
    [InlineData(BonusTypes.SalaryPercentage, 0.01)]
    [InlineData(BonusTypes.SalaryPercentage, 100)]
    public async Task Create_ValueOnTheLimits_IsAllowed(int bonusTypeId, double value)
    {
        await _service.CreateAsync(ValidRequest(bonusTypeId: bonusTypeId, bonusValue: (decimal)value), User, CancellationToken.None);

        Assert.Single(_repository.Added);
    }

    // ---------- CreateAsync: reglas de negocio ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public Task Create_WithoutUserCode_Throws(string? user) =>
        AssertCreateFails("USER_REQUIRED", ValidRequest(), user);

    [Fact]
    public Task Create_UserCodeLongerThan50_Throws() =>
        AssertCreateFails("USER_REQUIRED", ValidRequest(), new string('u', 51));

    [Fact]
    public Task Create_EmployeeNotFound_Throws()
    {
        _repository.Employee = null;
        return AssertCreateFails("EMPLOYEE_NOT_FOUND", ValidRequest());
    }

    [Fact]
    public Task Create_EmployeeTerminated_Throws()
    {
        _repository.Employee!.EmployeeStateCode = EmployeeStates.Terminated;
        return AssertCreateFails("EMPLOYEE_NOT_ACTIVE", ValidRequest());
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026-13")]
    [InlineData("2026-00")]
    [InlineData("2026-1")]
    [InlineData("26-11")]
    [InlineData("2026/11")]
    [InlineData("2026-11-01")]
    public Task Create_InvalidPeriodFormat_Throws(string period) =>
        AssertCreateFails("INVALID_PERIOD", ValidRequest(period: period));

    [Theory]
    [InlineData("2026-12")]
    [InlineData("2027-01")]
    public Task Create_FuturePeriod_Throws(string period) =>
        AssertCreateFails("FUTURE_PERIOD", ValidRequest(period: period));

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(5000.01)] // mayor al sueldo
    public Task Create_FixedAmountOutOfRange_Throws(double value) =>
        AssertCreateFails("INVALID_AMOUNT", ValidRequest(bonusTypeId: BonusTypes.FixedAmount, bonusValue: (decimal)value));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100.01)]
    public Task Create_PercentageOutOfRange_Throws(double value) =>
        AssertCreateFails("INVALID_PERCENTAGE", ValidRequest(bonusTypeId: BonusTypes.SalaryPercentage, bonusValue: (decimal)value));

    [Theory]
    [InlineData(BonusTypes.FixedAmount, 100.125)]
    [InlineData(BonusTypes.SalaryPercentage, 10.001)]
    public Task Create_MoreThanTwoDecimals_Throws(int bonusTypeId, double value) =>
        AssertCreateFails("INVALID_DECIMALS", ValidRequest(bonusTypeId: bonusTypeId, bonusValue: (decimal)value));

    [Fact]
    public Task Create_UnknownBonusType_Throws() =>
        AssertCreateFails("INVALID_BONUS_TYPE", ValidRequest(bonusTypeId: 99));

    [Fact]
    public Task Create_InactiveReason_Throws()
    {
        _repository.ReasonIsActive = false;
        return AssertCreateFails("REASON_NOT_ACTIVE", ValidRequest());
    }

    [Fact]
    public async Task Create_EmployeeAlreadyHasBonusInPeriod_Throws()
    {
        _repository.HasActiveBonus = true;

        var ex = await AssertCreateFails("BONUS_ALREADY_EXISTS", ValidRequest());
        Assert.Contains("2026-11", ex.Description);
    }

    // ---------- GetActiveEmployeeAsync ----------

    [Fact]
    public async Task GetActiveEmployee_ReturnsEmployeeData()
    {
        var dto = await _service.GetActiveEmployeeAsync("TECH1", 101, CancellationToken.None);

        Assert.Equal(new EmployeeDto("TECH1", 101, "Andrea Torres Vega", new DateTime(2020, 3, 1), 5000m, 8m), dto);
    }

    [Fact]
    public async Task GetActiveEmployee_NotFound_Throws()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.GetActiveEmployeeAsync("TECH1", 999, CancellationToken.None));
        Assert.Equal("EMPLOYEE_NOT_FOUND", ex.Code);
    }

    [Fact]
    public async Task GetActiveEmployee_Terminated_Throws()
    {
        _repository.Employee!.EmployeeStateCode = EmployeeStates.Terminated;

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.GetActiveEmployeeAsync("TECH1", 101, CancellationToken.None));
        Assert.Equal("EMPLOYEE_NOT_ACTIVE", ex.Code);
    }

    // ---------- CancelAsync ----------

    [Fact]
    public async Task Cancel_RegisteredBonus_ChangesStateAndSaves()
    {
        var bonus = _repository.Bonus = RegisteredBonus();

        await _service.CancelAsync(1, " editor ", CancellationToken.None);

        Assert.Equal(BonusStates.Cancelled, bonus.BonusStateCode);
        Assert.Equal(("editor", Now), (bonus.ModifiedBy, bonus.ModifiedAt));
        Assert.Equal(1, _repository.SaveChangesCount);
    }

    [Fact]
    public Task Cancel_BonusNotFound_Throws() =>
        AssertBusinessError("BONUS_NOT_FOUND", () => _service.CancelAsync(1, User, CancellationToken.None));

    [Fact]
    public Task Cancel_WithoutUserCode_Throws()
    {
        _repository.Bonus = RegisteredBonus();
        return AssertBusinessError("USER_REQUIRED", () => _service.CancelAsync(1, null, CancellationToken.None));
    }

    [Fact]
    public async Task Cancel_BonusAlreadyCancelled_ThrowsAndDoesNotSave()
    {
        var bonus = _repository.Bonus = RegisteredBonus();
        bonus.Cancel("otro", Now);

        await AssertBusinessError("BONUS_NOT_REGISTERED", () => _service.CancelAsync(1, User, CancellationToken.None));
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task Delete_RegisteredBonus_IsLogicalAndSaves()
    {
        var bonus = _repository.Bonus = RegisteredBonus();

        await _service.DeleteAsync(1, User, CancellationToken.None);

        Assert.True(bonus.IsDeleted);
        Assert.Equal(BonusStates.Registered, bonus.BonusStateCode);
        Assert.Equal(User, bonus.ModifiedBy);
        Assert.Equal(1, _repository.SaveChangesCount);
    }

    [Fact]
    public Task Delete_BonusNotFound_Throws() =>
        AssertBusinessError("BONUS_NOT_FOUND", () => _service.DeleteAsync(1, User, CancellationToken.None));

    // ---------- PayAsync ----------

    [Fact]
    public async Task Pay_WithProjectHours_DistributesAndReturnsDetail()
    {
        var bonus = _repository.Bonus = RegisteredBonus();
        _repository.ProjectHours =
        [
            new() { EmployeeProjectHoursId = 1, ProjectName = "Portal de clientes", Hours = 120m },
            new() { EmployeeProjectHoursId = 2, ProjectName = "App móvil", Hours = 40m }
        ];

        var result = await _service.PayAsync(1, User, CancellationToken.None);

        Assert.Equal(BonusStates.Paid, bonus.BonusStateCode);
        Assert.Equal(500m, result.GrossAmount);
        Assert.Equal(
            [new BonusDistributionDto("Portal de clientes", 120m, 375m), new BonusDistributionDto("App móvil", 40m, 125m)],
            result.Distributions);
        Assert.Equal(1, _repository.SaveChangesCount);
    }

    [Fact]
    public async Task Pay_WithoutProjectHours_ThrowsAndDoesNotSave()
    {
        var bonus = _repository.Bonus = RegisteredBonus();

        await AssertBusinessError("NO_PROJECT_HOURS", () => _service.PayAsync(1, User, CancellationToken.None));
        Assert.Equal(BonusStates.Registered, bonus.BonusStateCode);
    }

    [Fact]
    public Task Pay_BonusNotFound_Throws() =>
        AssertBusinessError("BONUS_NOT_FOUND", () => _service.PayAsync(1, User, CancellationToken.None));
}
