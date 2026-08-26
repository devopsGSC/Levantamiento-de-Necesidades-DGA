/* =====================================================================
   DGA — Levantamiento de Necesidades
   Script incremental 10: catálogo de Estados de Solicitud simplificado.

   De los 12 estados observados en el reconocimiento (Fase 0) solo 6 se
   usan realmente en el flujo de trabajo. Se eliminan del catálogo:
   Pendiente, Comprado, Realizado, Rechazado, Observado, Cotizado.

   El Progreso (%) de una solicitud ahora se deriva del Estado en vez de
   escribirse a mano (ver Estados.ProgresoParaEstado en el código):
     Guardado Borrador / Solicitado -> 0%
     En Proceso                     -> 40%
     Aprobado                       -> 60%
     Finalizado                     -> 100%
     Denegado                       -> sin porcentaje (no aplica)

   Ejecutar manualmente en SQL Server Management Studio después de
   01_schema_dga.sql — ... — 09_tipo_costo_y_cotizacion.sql.
   ===================================================================== */

USE requerimientosDGA;
GO

-- Reubicar cualquier solicitud/historial que quedara en un estado a eliminar,
-- antes de borrarlos, para no violar la FK de EstadosSolicitud.
UPDATE dbo.Solicitudes SET EstadoId = 2 WHERE EstadoId IN (3, 10, 11); -- Pendiente/Observado/Cotizado -> Solicitado
UPDATE dbo.Solicitudes SET EstadoId = 8 WHERE EstadoId IN (6, 7);      -- Comprado/Realizado -> En Proceso
UPDATE dbo.Solicitudes SET EstadoId = 5 WHERE EstadoId = 9;            -- Rechazado -> Denegado

UPDATE dbo.SolicitudHistorial SET EstadoAnteriorId = 2 WHERE EstadoAnteriorId IN (3, 10, 11);
UPDATE dbo.SolicitudHistorial SET EstadoAnteriorId = 8 WHERE EstadoAnteriorId IN (6, 7);
UPDATE dbo.SolicitudHistorial SET EstadoAnteriorId = 5 WHERE EstadoAnteriorId = 9;
UPDATE dbo.SolicitudHistorial SET EstadoNuevoId = 2 WHERE EstadoNuevoId IN (3, 10, 11);
UPDATE dbo.SolicitudHistorial SET EstadoNuevoId = 8 WHERE EstadoNuevoId IN (6, 7);
UPDATE dbo.SolicitudHistorial SET EstadoNuevoId = 5 WHERE EstadoNuevoId = 9;
GO

DELETE FROM dbo.EstadosSolicitud WHERE Id IN (3, 6, 7, 9, 10, 11);
GO

UPDATE dbo.EstadosSolicitud SET Orden = 1 WHERE Id = 1;  -- Guardado Borrador
UPDATE dbo.EstadosSolicitud SET Orden = 2 WHERE Id = 2;  -- Solicitado
UPDATE dbo.EstadosSolicitud SET Orden = 3 WHERE Id = 8;  -- En Proceso
UPDATE dbo.EstadosSolicitud SET Orden = 4 WHERE Id = 4;  -- Aprobado
UPDATE dbo.EstadosSolicitud SET Orden = 5 WHERE Id = 5;  -- Denegado
UPDATE dbo.EstadosSolicitud SET Orden = 6 WHERE Id = 12; -- Finalizado
GO

-- Sincronizar el Progreso guardado con el que corresponde al Estado actual de cada
-- solicitud (antes se tipeaba a mano y podía haber quedado desalineado).
UPDATE dbo.Solicitudes SET Progreso = 0   WHERE EstadoId IN (1, 2);
UPDATE dbo.Solicitudes SET Progreso = 40  WHERE EstadoId = 8;
UPDATE dbo.Solicitudes SET Progreso = 60  WHERE EstadoId = 4;
UPDATE dbo.Solicitudes SET Progreso = 100 WHERE EstadoId = 12;
UPDATE dbo.Solicitudes SET Progreso = NULL WHERE EstadoId = 5;
GO
