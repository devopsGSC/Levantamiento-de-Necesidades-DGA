/* =====================================================================
   DGA — Levantamiento de Necesidades
   Script incremental 11: Unidad Ejecutora de la solicitud.

   Catálogo simple igual a Cargos/Prioridades (Id, Nombre, Orden, Activo),
   administrable desde Admin > Configuración. El usuario la selecciona al
   armar la solicitud, junto al Cargo. Semilla inicial: Mantenimiento DGA,
   Compras DGA, Otro.

   Ejecutar manualmente en SQL Server Management Studio después de
   01_schema_dga.sql — ... — 10_estados_simplificados.sql.
   ===================================================================== */

USE requerimientosDGA;
GO

CREATE TABLE dbo.UnidadesEjecutoras (
    Id     TINYINT      NOT NULL,
    Nombre NVARCHAR(60) NOT NULL,
    Orden  SMALLINT     NOT NULL,
    Activo BIT          NOT NULL DEFAULT (1),
    CONSTRAINT PK_UnidadesEjecutoras PRIMARY KEY (Id),
    CONSTRAINT UQ_UnidadesEjecutoras_Nombre UNIQUE (Nombre)
);
GO

INSERT INTO dbo.UnidadesEjecutoras (Id, Nombre, Orden, Activo) VALUES
(1, N'Mantenimiento DGA', 1, 1),
(2, N'Compras DGA',       2, 1),
(3, N'Otro',              3, 1);
GO

ALTER TABLE dbo.Solicitudes ADD UnidadEjecutoraId TINYINT NOT NULL CONSTRAINT DF_Solicitudes_UnidadEjecutoraId DEFAULT (1);
GO

ALTER TABLE dbo.Solicitudes ADD CONSTRAINT FK_Solicitudes_UnidadEjecutora FOREIGN KEY (UnidadEjecutoraId) REFERENCES dbo.UnidadesEjecutoras (Id) ON DELETE NO ACTION;
GO
