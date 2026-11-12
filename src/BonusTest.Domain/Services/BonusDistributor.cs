namespace BonusTest.Domain.Services;

/// <summary>
/// Reparte un monto en proporción a las horas. Cada parte se redondea a 2 decimales
/// y la última recibe lo que falta para que la suma cuadre exacto con el total.
/// </summary>
public static class BonusDistributor
{
    /// <returns>Un monto por cada elemento de <paramref name="hours"/>, en el mismo orden.</returns>
    public static IReadOnlyList<decimal> Distribute(decimal total, IReadOnlyList<decimal> hours)
    {
        if (hours.Count == 0)
            throw new ArgumentException("Se necesita al menos un proyecto para distribuir.", nameof(hours));

        var totalHours = hours.Sum();
        if (totalHours <= 0)
            throw new ArgumentException("El total de horas debe ser mayor a 0.", nameof(hours));

        var amounts = new decimal[hours.Count];
        var assigned = 0m;
        for (var i = 0; i < hours.Count - 1; i++)
        {
            amounts[i] = BonusCalculator.RoundMoney(total * hours[i] / totalHours);
            assigned += amounts[i];
        }
        amounts[^1] = total - assigned;

        return amounts;
    }
}
