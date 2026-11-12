namespace BonusTest.Application.Tests.Fakes;

/// <summary>
/// Reloj fijo para que las pruebas no dependan de la fecha real.
/// La zona horaria es UTC, así que la hora "local" es exactamente la indicada.
/// </summary>
public class FixedTimeProvider(DateTime now) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(now, DateTimeKind.Utc));
}
