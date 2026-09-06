/* =====================================================================
   DGA — Levantamiento de Necesidades
   Script incremental 15: Unidad Ejecutora y checklist de completado por
   ÍTEM, no por solicitud.

   Motivo: una misma solicitud puede tener ítems que van hacia distintas
   Unidades Ejecutoras (ej. un ítem de mantenimiento y otro de compras),
   así que asignar UNA sola Unidad Ejecutora a toda la solicitud no
   alcanza para reflejar quién tramita cada cosa. El Progreso (%) de la
   solicitud deja de derivarse del Estado (antes 40/60/100 fijos) y pasa
   a ser el % de ítems marcados como completados.

   1) SolicitudItems: se agrega UnidadEjecutoraId (la asigna el admin,
      solo con la solicitud Aprobada o En Proceso), Completado, y quién y
      cuándo lo completó.
   2) Datos existentes — Unidad Ejecutora: toda solicitud que YA tenía una
      Unidad Ejecutora asignada a nivel de solicitud le pasa ese mismo
      valor a TODOS sus ítems (antes de borrar la columna vieja), para no
      perder en producción una asignación real que un admin ya había
      hecho — el admin puede después reasignar ítems puntuales a otra
      unidad si corresponde.
   3) Solicitudes: se elimina UnidadEjecutoraId — ya no aplica a nivel de
      solicitud (el valor ya se copió a los ítems en el paso 2).
   4) SolicitudHistorial: EstadoNuevoId pasa a admitir NULL (para poder
      registrar eventos de ítem sin que sean un cambio de Estado); se
      agrega SolicitudItemId e ItemCompletado para esos eventos.
   5) Datos existentes — Progreso: las solicitudes que ya estaban
      Finalizadas se dan por completadas en todos sus ítems (para no
      hacer retroceder su Progreso a 0%); el resto recalcula su Progreso
      a partir del nuevo criterio (% de ítems completados).

   Ejecutar manualmente en SQL Server Management Studio después de
   01_schema_dga.sql — ... — 14_roles_delegados.sql.
   ===================================================================== */

USE requerimientosDGA;
GO

-- 1) Unidad Ejecutora y checklist por ítem
ALTER TABLE dbo.SolicitudItems ADD UnidadEjecutoraId TINYINT NULL;
GO
ALTER TABLE dbo.SolicitudItems ADD CONSTRAINT FK_SolicitudItems_UnidadEjecutora FOREIGN KEY (UnidadEjecutoraId) REFERENCES dbo.UnidadesEjecutoras (Id) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.SolicitudItems ADD Completado BIT NOT NULL CONSTRAINT DF_SolicitudItems_Completado DEFAULT (0);
GO
ALTER TABLE dbo.SolicitudItems ADD FechaCompletado DATETIME2 NULL;
GO
ALTER TABLE dbo.SolicitudItems ADD CompletadoPorUsuarioId INT NULL;
GO
ALTER TABLE dbo.SolicitudItems ADD CONSTRAINT FK_SolicitudItems_CompletadoPorUsuario FOREIGN KEY (CompletadoPorUsuarioId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION;
GO

-- 2) Backfill: la Unidad Ejecutora que ya tenía la solicitud pasa a todos sus ítems
UPDATE i SET i.UnidadEjecutoraId = s.UnidadEjecutoraId
FROM dbo.SolicitudItems i
JOIN dbo.Solicitudes s ON s.Id = i.SolicitudId
WHERE s.UnidadEjecutoraId IS NOT NULL;
GO

-- 3) Unidad Ejecutora deja de existir a nivel de solicitud
ALTER TABLE dbo.Solicitudes DROP CONSTRAINT FK_Solicitudes_UnidadEjecutora;
GO
ALTER TABLE dbo.Solicitudes DROP COLUMN UnidadEjecutoraId;
GO

-- 4) Bitácora: soporta eventos de ítem además de cambios de Estado
ALTER TABLE dbo.SolicitudHistorial DROP CONSTRAINT FK_SolicitudHistorial_EstadoNuevo;
GO
ALTER TABLE dbo.SolicitudHistorial ALTER COLUMN EstadoNuevoId TINYINT NULL;
GO
ALTER TABLE dbo.SolicitudHistorial ADD CONSTRAINT FK_SolicitudHistorial_EstadoNuevo FOREIGN KEY (EstadoNuevoId) REFERENCES dbo.EstadosSolicitud (Id) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.SolicitudHistorial ADD SolicitudItemId INT NULL;
GO
ALTER TABLE dbo.SolicitudHistorial ADD CONSTRAINT FK_SolicitudHistorial_SolicitudItem FOREIGN KEY (SolicitudItemId) REFERENCES dbo.SolicitudItems (Id) ON DELETE NO ACTION;
GO
ALTER TABLE dbo.SolicitudHistorial ADD ItemCompletado BIT NULL;
GO

-- 5) Recalcular Progreso de datos existentes
UPDATE i SET i.Completado = 1, i.FechaCompletado = s.FechaFinalizacion
FROM dbo.SolicitudItems i
JOIN dbo.Solicitudes s ON s.Id = i.SolicitudId
WHERE s.EstadoId = 12; -- Finalizado
GO

UPDATE s SET s.Progreso = CASE WHEN totales.Total = 0 THEN 0 ELSE CAST(ROUND(100.0 * totales.Completados / totales.Total, 0) AS TINYINT) END
FROM dbo.Solicitudes s
CROSS APPLY (
    SELECT COUNT(*) AS Total, SUM(CASE WHEN i.Completado = 1 THEN 1 ELSE 0 END) AS Completados
    FROM dbo.SolicitudItems i
    WHERE i.SolicitudId = s.Id
) totales;
GO
