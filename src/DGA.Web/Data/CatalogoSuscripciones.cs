namespace DGA.Web.Data;

/// <summary>
/// IDs fijos (ver database/01_schema_dga.sql) de los puntos del catálogo que son
/// suscripciones recurrentes (Internet, Telefonía) en vez de compras únicas — necesitan
/// la lógica adicional de Tipo de Suscripción (Mensual/Anual) + Cantidad de Períodos en
/// el formulario de solicitud.
/// </summary>
public static class CatalogoSuscripciones
{
    /// <summary>Elementos "Internet" y "Telefonía" bajo el subcomponente "Suministros de
    /// servicios básicos" — no tienen Detalle, son ellos mismos la hoja del catálogo.</summary>
    public static readonly int[] ElementoIds = [20603, 20604];

    /// <summary>Detalles "Telefonía Fija" / "Telefonía Móvil" bajo los elementos "Equipos"
    /// y "Redes y telecomunicaciones", más "Suscripción Internet Starlink" bajo este último
    /// (Dotación de equipo tecnológico) — el equipo Starlink en sí (Detalle 4010303) es una
    /// compra única y no lleva esta lógica; solo la suscripción al servicio.</summary>
    public static readonly int[] DetalleIds = [4010109, 4010110, 4010312, 4010313, 4010315];
}
