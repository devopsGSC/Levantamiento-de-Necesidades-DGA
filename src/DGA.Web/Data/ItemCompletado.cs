using DGA.Web.Data.Entities;

namespace DGA.Web.Data;

/// <summary>
/// Marca un <see cref="SolicitudItem"/> como completado/no completado y arma la entrada de
/// bitácora correspondiente. Compartido entre el panel de Admin (puede tocar cualquier ítem)
/// y "Mis Requerimientos" (solo los ítems de su propia Unidad Ejecutora) para no duplicar la
/// validación ni el recálculo del Progreso de la solicitud.
/// </summary>
public static class ItemCompletado
{
    public static (bool Ok, string? Error, SolicitudHistorial? Historial) Marcar(
        SolicitudItem item, bool completado, string? comentario, int usuarioId)
    {
        if (!Estados.PermiteGestionItems(item.Solicitud.EstadoId))
        {
            return (false, "Solo se pueden gestionar ítems de solicitudes Aprobadas o En Proceso.", null);
        }
        if (item.UnidadEjecutoraId is null)
        {
            return (false, "Asigná una Unidad Ejecutora al ítem antes de marcarlo.", null);
        }
        if (string.IsNullOrWhiteSpace(comentario))
        {
            return (false, "Ingresá un comentario para este cambio — queda registrado en la bitácora.", null);
        }
        if (completado == item.Completado)
        {
            return (false, "El ítem ya está en ese estado.", null);
        }

        item.Completado = completado;
        item.FechaCompletado = completado ? DateTime.UtcNow : null;
        item.CompletadoPorUsuarioId = completado ? usuarioId : null;
        item.UpdatedAt = DateTime.UtcNow;

        var solicitud = item.Solicitud;
        solicitud.Progreso = Estados.CalcularProgreso(solicitud.Items.Count, solicitud.Items.Count(i => i.Completado));
        solicitud.UpdatedAt = DateTime.UtcNow;

        var historial = new SolicitudHistorial
        {
            SolicitudId = solicitud.Id,
            SolicitudItemId = item.Id,
            ItemCompletado = completado,
            UsuarioCambioId = usuarioId,
            Comentario = comentario.Trim(),
            FechaCambio = DateTime.UtcNow,
        };
        return (true, null, historial);
    }
}
