/* =====================================================================
   DGA — Levantamiento de Necesidades
   Script incremental 04: módulo de Configuración (Admin > Configuración).

   1. Columna Activo en los catálogos que alimentan los formularios, para
      poder "desactivar" una opción sin borrarla (las solicitudes viejas que
      ya la usan siguen mostrándola bien; solo deja de ofrecerse para
      solicitudes nuevas).
   2. Tabla ConfiguracionSistema (fila única) con los datos de contacto/
      soporte que hoy viven en appsettings.json — se migran los valores
      actuales para no perder lo ya configurado.

   Ejecutar manualmente en SQL Server Management Studio después de
   01_schema_dga.sql, 02_admin_principal.sql y 03_reportes_semanales.sql.
   ===================================================================== */

USE requerimientosDGA;
GO

ALTER TABLE dbo.TiposAduana   ADD Activo BIT NOT NULL CONSTRAINT DF_TiposAduana_Activo   DEFAULT (1);
ALTER TABLE dbo.Aduanas       ADD Activo BIT NOT NULL CONSTRAINT DF_Aduanas_Activo       DEFAULT (1);
ALTER TABLE dbo.Cargos        ADD Activo BIT NOT NULL CONSTRAINT DF_Cargos_Activo        DEFAULT (1);
ALTER TABLE dbo.Componentes   ADD Activo BIT NOT NULL CONSTRAINT DF_Componentes_Activo   DEFAULT (1);
ALTER TABLE dbo.Subcomponentes ADD Activo BIT NOT NULL CONSTRAINT DF_Subcomponentes_Activo DEFAULT (1);
ALTER TABLE dbo.Elementos     ADD Activo BIT NOT NULL CONSTRAINT DF_Elementos_Activo     DEFAULT (1);
ALTER TABLE dbo.Detalles      ADD Activo BIT NOT NULL CONSTRAINT DF_Detalles_Activo      DEFAULT (1);
ALTER TABLE dbo.Prioridades   ADD Activo BIT NOT NULL CONSTRAINT DF_Prioridades_Activo   DEFAULT (1);
GO

CREATE TABLE dbo.ConfiguracionSistema (
    Id              INT           NOT NULL,
    SoporteTelefono NVARCHAR(50)  NOT NULL,
    SoporteCorreo   NVARCHAR(150) NOT NULL,
    SoporteHorario  NVARCHAR(150) NOT NULL,
    UpdatedAt       DATETIME2     NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_ConfiguracionSistema PRIMARY KEY (Id),
    -- Fila única a propósito: es configuración global, no una tabla de registros.
    CONSTRAINT CK_ConfiguracionSistema_FilaUnica CHECK (Id = 1)
);
GO

INSERT INTO dbo.ConfiguracionSistema (Id, SoporteTelefono, SoporteCorreo, SoporteHorario) VALUES
(1, N'0000-0000', N'soporte@dga.gob.sv', N'Lunes a viernes, 8:00 a.m. - 4:30 p.m.');
GO
