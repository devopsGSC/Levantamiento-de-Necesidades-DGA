/* =====================================================================
   DGA — Levantamiento de Necesidades
   Script incremental 12: el costo estimado / cotización del ítem pasa
   de obligatorio a opcional.

   TienePresupuesto indica si el usuario cargó un monto para el ítem. En
   "No" (el valor que trae por defecto un ítem nuevo, capturado en el
   cliente) el Costo Estimado, Tipo de Costo y la cotización adjunta
   quedan ocultos en el formulario y sin usar.

   Los ítems ya guardados se marcan TienePresupuesto = 1: bajo la regla
   anterior el costo era obligatorio, así que todos tienen uno real.

   Ejecutar manualmente en SQL Server Management Studio después de
   01_schema_dga.sql — ... — 11_unidad_ejecutora.sql.
   ===================================================================== */

USE requerimientosDGA;
GO

ALTER TABLE dbo.SolicitudItems ADD TienePresupuesto BIT NOT NULL CONSTRAINT DF_SolicitudItems_TienePresupuesto DEFAULT (1);
GO
