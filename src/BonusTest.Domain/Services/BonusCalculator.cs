using BonusTest.Domain.Constants;

namespace BonusTest.Domain.Services;

public record BonusAmounts(decimal GrossAmount, decimal TaxAmount, decimal NetAmount);

/// <summary>
/// Cálculo del bono. Todos los montos se redondean a 2 decimales con el medio hacia arriba (0.125 → 0.13).
/// </summary>
public static class BonusCalculator
{
    public static BonusAmounts Calculate(int bonusTypeId, decimal bonusValue, decimal monthlySalary, decimal taxRate)
    {
        var gross = bonusTypeId switch
        {
            BonusTypes.FixedAmount => RoundMoney(bonusValue),
            BonusTypes.SalaryPercentage => RoundMoney(monthlySalary * bonusValue / 100m),
            _ => throw new ArgumentOutOfRangeException(nameof(bonusTypeId), bonusTypeId, "Tipo de bono no soportado.")
        };
        var tax = RoundMoney(gross * taxRate / 100m);

        return new BonusAmounts(gross, tax, gross - tax);
    }

    public static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
