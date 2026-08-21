/* =====================================================================
   DGA — Levantamiento de Necesidades
   Script incremental 09: tipo de costo (Unitario/Total) y cotización
   adjunta por ítem de solicitud.

   No es lo mismo el costo estimado de UNA cerámica que el costo total
   de instalar el piso (cerámica + mano de obra + materiales). El
   usuario ahora indica cuál de los dos está ingresando en CostoEstimado
   (columna ya agregada en el script 06):
     - "Unitario": el subtotal sigue siendo CostoEstimado * Cantidad.
     - "Total": CostoEstimado ya es el total; el subtotal no vuelve a
       multiplicar por la cantidad.
   Además se puede adjuntar la cotización (imagen o PDF) que respalda
   ese monto — opcional.

   TipoCosto queda NOT NULL con default 'Unitario' para no dejar sin
   valor a los ítems ya guardados (ver database/06_presupuesto_items.sql).

   Ejecutar manualmente en SQL Server Management Studio después de
   01_schema_dga.sql — ... — 08_detalle_suscripcion_starlink.sql.
   ===================================================================== */

USE requerimientosDGA;
GO

ALTER TABLE dbo.SolicitudItems ADD TipoCosto NVARCHAR(10) NOT NULL CONSTRAINT DF_SolicitudItems_TipoCosto DEFAULT (N'Unitario');
GO

ALTER TABLE dbo.SolicitudItems ADD CONSTRAINT CK_SolicitudItems_TipoCosto CHECK (TipoCosto IN (N'Unitario', N'Total'));
GO

ALTER TABLE dbo.SolicitudItems ADD CotizacionRuta            NVARCHAR(300) NULL;
ALTER TABLE dbo.SolicitudItems ADD CotizacionNombreOriginal  NVARCHAR(260) NULL;
GO
