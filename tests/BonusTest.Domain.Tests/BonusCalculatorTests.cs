using BonusTest.Domain.Constants;
using BonusTest.Domain.Services;

namespace BonusTest.Domain.Tests;

public class BonusCalculatorTests
{
    [Fact]
    public void Calculate_SalaryPercentage_MatchesStatementExample()
    {
        // TECH1-101: sueldo 5,000.00, retención 8 %, bono de 10 %.
        var result = BonusCalculator.Calculate(BonusTypes.SalaryPercentage, 10m, 5000m, 8m);

        Assert.Equal(new BonusAmounts(500.00m, 40.00m, 460.00m), result);
    }

    [Fact]
    public void Calculate_FixedAmount_UsesAmountAsGross()
    {
        var result = BonusCalculator.Calculate(BonusTypes.FixedAmount, 1000m, 5000m, 8m);

        Assert.Equal(new BonusAmounts(1000.00m, 80.00m, 920.00m), result);
    }

    [Fact]
    public void Calculate_ZeroTaxRate_NetEqualsGross()
    {
        var result = BonusCalculator.Calculate(BonusTypes.FixedAmount, 300m, 2500m, 0m);

        Assert.Equal(new BonusAmounts(300.00m, 0.00m, 300.00m), result);
    }

    [Fact]
    public void Calculate_TaxAtMidpoint_RoundsHalfUp()
    {
        // 100.25 × 10 % = 10.025 → 10.03 (el redondeo bancario por defecto daría 10.02).
        var result = BonusCalculator.Calculate(BonusTypes.FixedAmount, 100.25m, 5000m, 10m);

        Assert.Equal(new BonusAmounts(100.25m, 10.03m, 90.22m), result);
    }

    [Fact]
    public void Calculate_GrossAtMidpoint_RoundsHalfUp()
    {
        // 2.50 × 5 % = 0.125 → 0.13
        var result = BonusCalculator.Calculate(BonusTypes.SalaryPercentage, 5m, 2.50m, 0m);

        Assert.Equal(0.13m, result.GrossAmount);
    }

    [Theory]
    [InlineData(0.125, 0.13)]
    [InlineData(0.124, 0.12)]
    [InlineData(2.675, 2.68)]
    [InlineData(-0.125, -0.13)]
    public void RoundMoney_RoundsToTwoDecimalsAwayFromZero(decimal value, decimal expected)
    {
        Assert.Equal(expected, BonusCalculator.RoundMoney(value));
    }

    [Fact]
    public void Calculate_UnknownBonusType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BonusCalculator.Calculate(99, 10m, 5000m, 8m));
    }
}
