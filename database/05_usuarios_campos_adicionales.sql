/* =====================================================================
   DGA — Levantamiento de Necesidades
   Script incremental 05: campos adicionales de Usuario (AspNetUsers) para
   la carga masiva por Excel y el alta individual — Cargo, Aduana y
   Subdirección. Texto libre (sin FK a los catálogos de Cargo/Aduana que
   usa Solicitud: son datos descriptivos del usuario, no una selección
   restringida).

   Ejecutar manualmente en SQL Server Management Studio después de
   01_schema_dga.sql, 02_admin_principal.sql, 03_reportes_semanales.sql
   y 04_configuracion.sql.
   ===================================================================== */

USE requerimientosDGA;
GO

ALTER TABLE dbo.AspNetUsers ADD Cargo         NVARCHAR(150) NULL;
ALTER TABLE dbo.AspNetUsers ADD Aduana        NVARCHAR(150) NULL;
ALTER TABLE dbo.AspNetUsers ADD Subdireccion  NVARCHAR(150) NULL;
GO
