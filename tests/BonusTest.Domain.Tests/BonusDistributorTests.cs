using BonusTest.Domain.Services;

namespace BonusTest.Domain.Tests;

public class BonusDistributorTests
{
    [Fact]
    public void Distribute_SingleProject_ReceivesEverything()
    {
        Assert.Equal([500.00m], BonusDistributor.Distribute(500m, [160m]));
    }

    [Fact]
    public void Distribute_ProportionalToHours()
    {
        // TECH1-101: 120 h y 40 h sobre un bruto de 500.
        Assert.Equal([375.00m, 125.00m], BonusDistributor.Distribute(500m, [120m, 40m]));
    }

    [Fact]
    public void Distribute_LastProjectAbsorbsRoundingDifference()
    {
        var amounts = BonusDistributor.Distribute(100m, [1m, 1m, 1m]);

        Assert.Equal([33.33m, 33.33m, 33.34m], amounts);
        Assert.Equal(100m, amounts.Sum());
    }

    [Fact]
    public void Distribute_EachLineRoundsHalfUp()
    {
        // 0.05 / 2 = 0.025 → 0.03; el último recibe 0.02 para cuadrar.
        Assert.Equal([0.03m, 0.02m], BonusDistributor.Distribute(0.05m, [1m, 1m]));
    }

    [Fact]
    public void Distribute_WithoutProjects_Throws()
    {
        Assert.Throws<ArgumentException>(() => BonusDistributor.Distribute(100m, []));
    }
}
