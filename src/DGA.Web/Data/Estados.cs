namespace DGA.Web.Data;

/// <summary>
/// IDs del catálogo EstadosSolicitud (ver database/01_schema_dga.sql, sección 2.5).
/// Decisión posterior a Fase 1: catálogo recortado a los 6 estados que realmente se
/// usan en el flujo de trabajo (ver database/10_estados_simplificados.sql).
/// </summary>
public static class Estados
{
    public const byte GuardadoBorrador = 1;
    public const byte Solicitado = 2;
    public const byte Aprobado = 4;
    public const byte Denegado = 5;
    public const byte EnProceso = 8;
    public const byte Finalizado = 12;

    /// <summary>
    /// Editable por su dueño mientras el admin no haya intervenido: Guardado Borrador
    /// (todavía no se envió) y Solicitado (el propio usuario lo finalizó, el admin
    /// todavía no le cambió el estado). Cualquier otro estado lo puso un admin.
    /// </summary>
    public static bool EsEditablePorDueno(byte estadoId) => estadoId is GuardadoBorrador or Solicitado;

    /// <summary>El usuario solo puede descartar/cancelar mientras está en Borrador.</summary>
    public static bool PuedeDescartar(byte estadoId) => estadoId == GuardadoBorrador;

    /// <summary>
    /// Progreso (0-100) que corresponde a cada estado. Denegado no tiene un punto de
    /// avance definido — es una salida negativa del flujo, no un porcentaje de él.
    /// </summary>
    public static byte? ProgresoParaEstado(byte estadoId) => estadoId switch
    {
        GuardadoBorrador or Solicitado => 0,
        EnProceso => 40,
        Aprobado => 60,
        Finalizado => 100,
        _ => null,
    };
}
