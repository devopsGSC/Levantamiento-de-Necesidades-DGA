using DGA.Web.Data;

namespace DGA.Web.Services;

/// <summary>
/// Genera el reporte semanal automáticamente. Revisa cada hora si la semana (lunes a
/// domingo) más reciente que ya terminó todavía no tiene su reporte automático, y si no,
/// lo genera. Al chequear en cada tick (no en un horario exacto), se autocorrige sola si
/// la app estuvo apagada justo el domingo: en el próximo tick que corra —aunque sea días
/// después— detecta que falta el reporte de esa semana y lo genera igual.
/// </summary>
public class ReporteSemanalBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReporteSemanalBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan IntervaloChequeo = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerarSiFaltaAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generando el reporte semanal automático.");
            }

            try
            {
                await Task.Delay(IntervaloChequeo, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task GenerarSiFaltaAsync()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow.ASalvador());
        var diasDesdeElDomingo = (int)hoy.DayOfWeek; // DayOfWeek: Domingo = 0 ... Sábado = 6
        var ultimoDomingo = hoy.AddDays(-diasDesdeElDomingo);
        var inicioSemana = ultimoDomingo.AddDays(-6);

        using var scope = scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ReporteSemanalService>();

        if (await servicio.ExisteReporteAutomaticoAsync(inicioSemana, ultimoDomingo))
        {
            return;
        }

        var reporte = await servicio.GenerarAsync(inicioSemana, ultimoDomingo, manual: false, usuarioId: null);
        logger.LogInformation(
            "Reporte semanal automático generado: {Inicio} a {Fin} ({Cantidad} solicitudes).",
            reporte.FechaInicio, reporte.FechaFin, reporte.CantidadSolicitudes);
    }
}
