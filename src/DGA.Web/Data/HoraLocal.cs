namespace DGA.Web.Data;

/// <summary>
/// Todo lo que se guarda en la base (FechaRegistro, FechaCambio, CreatedAt, etc.) usa
/// DateTime.UtcNow — correcto para persistir, pero si se muestra tal cual en pantalla o en
/// un PDF/Excel aparece con varias horas de diferencia contra la hora real de El Salvador.
/// Este helper hace esa conversión en el punto de presentación.
///
/// El Salvador está fijo en UTC-6 todo el año (no usa horario de verano), así que un offset
/// fijo alcanza — evita depender de TimeZoneInfo, cuyo id de zona horaria difiere entre
/// Windows ("Central America Standard Time") y Linux ("America/El_Salvador"), justo lo que
/// puede variar entre el entorno de desarrollo y el servidor de producción.
/// </summary>
public static class HoraLocal
{
    private static readonly TimeSpan OffsetElSalvador = TimeSpan.FromHours(-6);

    public static DateTime ASalvador(this DateTime utc) => utc.Add(OffsetElSalvador);

    public static DateTime? ASalvador(this DateTime? utc) => utc?.Add(OffsetElSalvador);
}
