/* =====================================================================
   DGA — Levantamiento de Necesidades
   Script incremental 16: denegar un ítem puntual dentro de una solicitud
   con varios ítems, sin tener que denegar toda la solicitud.

   Motivo: una solicitud puede tener varios ítems donde solo uno no
   corresponde (ej. 6 ítems, 1 no aplica) — antes la única forma de
   "rechazar" algo era cambiar el Estado de TODA la solicitud a Denegado.
   Ahora cada ítem puede marcarse como Denegado (mutuamente excluyente
   con Completado), con un motivo obligatorio que ve el dueño de la
   solicitud sin que el admin tenga que hacer nada más. Un ítem denegado
   cuenta igual que uno completado para el Progreso (%) de la solicitud
   — ver DGA.Web.Data.ItemDenegado y DGA.Web.Data.Estados.CalcularProgreso.

   1) SolicitudItems: se agrega Denegado, MotivoDenegacion, y quién y
      cuándo lo denegó.
   2) SolicitudHistorial: se agrega ItemDenegado para registrar el evento
      (mismo patrón que ItemCompletado, ver 15_progreso_por_item.sql).

   Ejecutar manualmente en SQL Server Management Studio después de
   01_schema_dga.sql — ... — 15_progreso_por_item.sql.
   ===================================================================== */

USE requerimientosDGA;
GO

-- 1) Denegación por ítem
ALTER TABLE dbo.SolicitudItems ADD Denegado BIT NOT NULL CONSTRAINT DF_SolicitudItems_Denegado DEFAULT (0);
GO
ALTER TABLE dbo.SolicitudItems ADD MotivoDenegacion NVARCHAR(MAX) NULL;
GO
ALTER TABLE dbo.SolicitudItems ADD FechaDenegado DATETIME2 NULL;
GO
ALTER TABLE dbo.SolicitudItems ADD DenegadoPorUsuarioId INT NULL;
GO
ALTER TABLE dbo.SolicitudItems ADD CONSTRAINT FK_SolicitudItems_DenegadoPorUsuario FOREIGN KEY (DenegadoPorUsuarioId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION;
GO

-- 2) Bitácora: soporta el evento de ítem denegado
ALTER TABLE dbo.SolicitudHistorial ADD ItemDenegado BIT NULL;
GO
