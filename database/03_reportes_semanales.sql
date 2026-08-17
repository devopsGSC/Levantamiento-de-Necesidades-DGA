/* =====================================================================
   DGA — Levantamiento de Necesidades
   Script incremental 03: módulo de Reportes (Admin > Reportes).

   Guarda un registro por cada reporte semanal generado (automático los
   domingos, o manual bajo demanda desde la UI). Los archivos PDF/Excel en
   sí viven en disco (App_Data/reportes), fuera de wwwroot, igual que las
   fotografías de ítems — esta tabla solo guarda su ruta relativa y metadata.

   Ejecutar manualmente en SQL Server Management Studio después de
   01_schema_dga.sql y 02_admin_principal.sql.
   ===================================================================== */

USE requerimientosDGA;
GO

CREATE TABLE dbo.ReportesSemanales (
    Id                      INT IDENTITY(1,1) NOT NULL,
    FechaInicio             DATE              NOT NULL,
    FechaFin                DATE              NOT NULL,
    CantidadSolicitudes     INT               NOT NULL,
    RutaPdf                 NVARCHAR(400)     NOT NULL,
    RutaExcel               NVARCHAR(400)     NOT NULL,
    GeneradoManualmente     BIT               NOT NULL DEFAULT (0),
    GeneradoPorUsuarioId    INT               NULL,
    GeneradoEn              DATETIME2         NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_ReportesSemanales PRIMARY KEY (Id),
    CONSTRAINT CK_ReportesSemanales_Fechas CHECK (FechaFin >= FechaInicio),
    CONSTRAINT FK_ReportesSemanales_Usuario FOREIGN KEY (GeneradoPorUsuarioId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION
);
GO

CREATE INDEX IX_ReportesSemanales_FechaFin ON dbo.ReportesSemanales (FechaFin DESC);
GO

