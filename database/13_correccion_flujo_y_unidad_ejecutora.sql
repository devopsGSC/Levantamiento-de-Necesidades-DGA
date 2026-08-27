/* =====================================================================
   DGA — Levantamiento de Necesidades
   Script incremental 13: corrección del orden del flujo de Estados y de
   cuándo se define la Unidad Ejecutora.

   1) El flujo correcto es Guardado Borrador -> Solicitado -> Aprobado ->
      En Proceso -> Finalizado (antes "En Proceso" quedaba con un Orden
      menor que "Aprobado"). El Progreso (%) de cada estado se corrige
      para seguir ese mismo orden (Aprobado 40%, En Proceso 60% — antes
      al revés).

   2) La Unidad Ejecutora ya no la elige el usuario al armar la
      solicitud: la define el administrador recién al aprobarla, porque
      ese análisis (quién la va a tramitar) se hace en ese momento. La
      columna pasa a admitir NULL y se le quita el valor por defecto.
      Las solicitudes que todavía no llegaron a Aprobado quedan sin
      asignar — antes tenían el valor por defecto que había elegido el
      usuario en el formulario, que ya no significa nada.

   Ejecutar manualmente en SQL Server Management Studio después de
   01_schema_dga.sql — ... — 12_presupuesto_opcional.sql.
   ===================================================================== */

USE requerimientosDGA;
GO

-- 1) Orden del flujo
UPDATE dbo.EstadosSolicitud SET Orden = 3 WHERE Id = 4;  -- Aprobado
UPDATE dbo.EstadosSolicitud SET Orden = 4 WHERE Id = 8;  -- En Proceso
GO

UPDATE dbo.Solicitudes SET Progreso = 40 WHERE EstadoId = 4;  -- Aprobado
UPDATE dbo.Solicitudes SET Progreso = 60 WHERE EstadoId = 8;  -- En Proceso
GO

-- 2) Unidad Ejecutora: opcional hasta que el admin aprueba
ALTER TABLE dbo.Solicitudes DROP CONSTRAINT DF_Solicitudes_UnidadEjecutoraId;
GO
ALTER TABLE dbo.Solicitudes ALTER COLUMN UnidadEjecutoraId TINYINT NULL;
GO
UPDATE dbo.Solicitudes SET UnidadEjecutoraId = NULL WHERE EstadoId IN (1, 2, 5); -- Guardado Borrador / Solicitado / Denegado
GO
