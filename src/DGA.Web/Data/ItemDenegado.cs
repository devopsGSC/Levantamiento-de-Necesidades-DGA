using DGA.Web.Data.Entities;

namespace DGA.Web.Data;

/// <summary>
/// Marca un <see cref="SolicitudItem"/> puntual como denegado ("no aplica") sin afectar al
/// resto de los ítems de la solicitud — pensado para el caso de una solicitud con varios
/// ítems donde solo uno no corresponde. Arma la entrada de bitácora correspondiente. Mismo
/// mecanismo que <see cref="ItemCompletado"/> (compartido entre Admin y "Mis Requerimientos")
/// para no duplicar la validación ni el recálculo del Progreso de la solicitud: un ítem
/// denegado cuenta igual que uno completado, así la solicitud puede seguir su curso sin que
/// el admin tenga que hacer nada más — el dueño ve el motivo en la bitácora.
/// </summary>
public static class ItemDenegado
{
    public static (bool Ok, string? Error, SolicitudHistorial? Historial) Marcar(
        SolicitudItem item, bool denegado, string? comentario, int usuarioId)
    {
        if (!Estados.PermiteGestionItems(item.Solicitud.EstadoId))
        {
            return (false, "Solo se pueden gestionar ítems de solicitudes Aprobadas o En Proceso.", null);
        }
        if (denegado && item.Completado)
        {
            return (false, "Este ítem ya está completado — desmarcalo antes de denegarlo.", null);
        }
        if (string.IsNullOrWhiteSpace(comentario))
        {
            return (false, "Ingresá un motivo — queda registrado en la bitácora y lo ve el dueño de la solicitud.", null);
        }
        if (denegado == item.Denegado)
        {
            return (false, "El ítem ya está en ese estado.", null);
        }

        item.Denegado = denegado;
        item.MotivoDenegacion = denegado ? comentario.Trim() : null;
        item.FechaDenegado = denegado ? DateTime.UtcNow : null;
        item.DenegadoPorUsuarioId = denegado ? usuarioId : null;
        item.UpdatedAt = DateTime.UtcNow;

        var solicitud = item.Solicitud;
        solicitud.Progreso = Estados.CalcularProgreso(solicitud.Items.Count, solicitud.Items.Count(i => i.Completado || i.Denegado));
        solicitud.UpdatedAt = DateTime.UtcNow;

        var historial = new SolicitudHistorial
        {
            SolicitudId = solicitud.Id,
            SolicitudItemId = item.Id,
            ItemDenegado = denegado,
            UsuarioCambioId = usuarioId,
            Comentario = comentario.Trim(),
            FechaCambio = DateTime.UtcNow,
        };
        return (true, null, historial);
    }
}
