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
    /// El Progreso (%) ya no se deriva del Estado — es el % de ítems marcados como
    /// completados en la solicitud (ver <see cref="ItemCompletado"/>). Este helper solo
    /// calcula ese porcentaje a partir de los conteos.
    /// </summary>
    public static byte CalcularProgreso(int totalItems, int itemsCompletados) =>
        (byte)(totalItems == 0 ? 0 : (int)Math.Round(100.0 * itemsCompletados / totalItems));

    /// <summary>La Unidad Ejecutora de cada ítem y el checklist de completado solo se
    /// gestionan mientras la solicitud está Aprobada o En Proceso — antes de Aprobado el
    /// análisis todavía no se hizo, y Finalizado/Denegado ya cerraron el flujo.</summary>
    public static bool PermiteGestionItems(byte estadoId) => estadoId is Aprobado or EnProceso;

    /// <summary>
    /// Guardado Borrador y Solicitado los pone el propio usuario (al guardar o finalizar
    /// su solicitud), no el admin. El admin solo puede mover la solicitud hacia estos.
    /// </summary>
    public static bool PuedeEstablecerAdmin(byte estadoId) => estadoId is Aprobado or Denegado or EnProceso or Finalizado;

    /// <summary>
    /// El admin no puede gestionar (cambiar de estado) una solicitud mientras está en
    /// Guardado Borrador — el usuario todavía la está editando y ni siquiera la envió.
    /// Recién puede intervenir a partir de Solicitado en adelante.
    /// </summary>
    public static bool PermiteGestionEstadoAdmin(byte estadoId) => estadoId != GuardadoBorrador;

    /// <summary>
    /// Único paso hacia adelante que puede dar el usuario de un rol delegado (Compras DGA,
    /// Mantenimiento DGA, Otro) sobre una solicitud que el admin ya le asignó. Nunca puede
    /// aprobar, denegar ni reasignar Unidad Ejecutora — eso es exclusivo del admin.
    /// </summary>
    public static byte? SiguienteEstadoDelegado(byte estadoId) => estadoId switch
    {
        Aprobado => EnProceso,
        EnProceso => Finalizado,
        _ => null,
    };
}
