/* =====================================================================
   DGA — Levantamiento de Necesidades
   Script incremental 07: ítems de suscripción recurrente (Internet,
   Telefonía) dentro de una solicitud.

   Algunos puntos del catálogo (ver DGA.Web.Data.CatalogoSuscripciones)
   no son una compra única sino una suscripción: el ítem necesita indicar
   si es Mensual o Anual y por cuántos períodos, además del costo por
   período (ya cubierto por SolicitudItems.CostoEstimado, agregado en el
   script 06). El subtotal presupuestado pasa a calcularse en la
   aplicación como CostoEstimado * CantidadSolicitada * (CantidadPeriodos
   ?? 1) — ambas columnas quedan NULL en los ítems que no son suscripción.

   Ejecutar manualmente en SQL Server Management Studio después de
   01_schema_dga.sql, 02_admin_principal.sql, 03_reportes_semanales.sql,
   04_configuracion.sql, 05_usuarios_campos_adicionales.sql y
   06_presupuesto_items.sql.
   ===================================================================== */

USE requerimientosDGA;
GO

ALTER TABLE dbo.SolicitudItems ADD TipoSuscripcion  NVARCHAR(10) NULL;
ALTER TABLE dbo.SolicitudItems ADD CantidadPeriodos INT          NULL;
GO

ALTER TABLE dbo.SolicitudItems ADD CONSTRAINT CK_SolicitudItems_TipoSuscripcion
    CHECK (TipoSuscripcion IS NULL OR TipoSuscripcion IN (N'Mensual', N'Anual'));
ALTER TABLE dbo.SolicitudItems ADD CONSTRAINT CK_SolicitudItems_CantidadPeriodos
    CHECK (CantidadPeriodos IS NULL OR CantidadPeriodos > 0);
GO
