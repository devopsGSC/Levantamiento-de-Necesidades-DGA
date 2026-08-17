namespace DGA.Web.Data;

/// <summary>
/// IDs del catálogo EstadosSolicitud (ver database/01_schema_dga.sql, sección 2.5).
/// Decisión de Fase 1: lista de 12 estados observada en vivo en el sitio original.
/// </summary>
public static class Estados
{
    public const byte GuardadoBorrador = 1;
    public const byte Solicitado = 2;
    public const byte Pendiente = 3;
    public const byte Aprobado = 4;
    public const byte Denegado = 5;
    public const byte Comprado = 6;
    public const byte Realizado = 7;
    public const byte EnProceso = 8;
    public const byte Rechazado = 9;
    public const byte Observado = 10;
    public const byte Cotizado = 11;
    public const byte Finalizado = 12;

    /// <summary>
    /// Editable por su dueño mientras el admin no haya intervenido: Guardado Borrador
    /// (todavía no se envió) y Solicitado (el propio usuario lo finalizó, el admin
    /// todavía no le cambió el estado). Cualquier otro estado lo puso un admin.
    /// </summary>
    public static bool EsEditablePorDueno(byte estadoId) => estadoId is GuardadoBorrador or Solicitado;

    /// <summary>El usuario solo puede descartar/cancelar mientras está en Borrador.</summary>
    public static bool PuedeDescartar(byte estadoId) => estadoId == GuardadoBorrador;
}
