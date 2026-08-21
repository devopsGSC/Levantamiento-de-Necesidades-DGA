/* =====================================================================
   DGA — Levantamiento de Necesidades
   Script incremental 06: presupuesto estimado por ítem de solicitud.

   Costo unitario estimado que el usuario ingresa al armar cada ítem
   (0 si no lo conoce). El subtotal presupuestado de cada ítem se calcula
   en la aplicación como CostoEstimado * CantidadSolicitada; el total de
   la solicitud es la suma de esos subtotales — no se persiste un total
   aparte para no arrastrar datos desincronizados.

   Ejecutar manualmente en SQL Server Management Studio después de
   01_schema_dga.sql, 02_admin_principal.sql, 03_reportes_semanales.sql,
   04_configuracion.sql y 05_usuarios_campos_adicionales.sql.
   ===================================================================== */

USE requerimientosDGA;
GO

ALTER TABLE dbo.SolicitudItems ADD CostoEstimado DECIMAL(12,2) NOT NULL CONSTRAINT DF_SolicitudItems_CostoEstimado DEFAULT (0);
GO
