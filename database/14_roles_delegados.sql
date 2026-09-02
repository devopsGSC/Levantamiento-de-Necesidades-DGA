/* =====================================================================
   DGA — Levantamiento de Necesidades
   Script incremental 14: roles delegados de tramitación.

   El Administrador actúa como dispatcher: al aprobar una solicitud le
   asigna una Unidad Ejecutora (Mantenimiento DGA / Compras DGA / Otro,
   ver database/11_unidad_ejecutora.sql). A partir de ahora esa asignación
   también determina a qué usuarios les "cae" la solicitud para que la
   tramiten ellos mismos (avanzar Aprobado -> En Proceso -> Finalizado).

   Se agregan 3 roles de Identity nuevos, uno por Unidad Ejecutora. La app
   también los siembra en el arranque como resguardo (DbSeeder.cs) si este
   script no corrió.

   Ejecutar manualmente en SQL Server Management Studio después de
   01_schema_dga.sql — ... — 13_correccion_flujo_y_unidad_ejecutora.sql.
   ===================================================================== */

USE requerimientosDGA;
GO

INSERT INTO dbo.AspNetRoles (Name, NormalizedName, ConcurrencyStamp) VALUES
(N'MantenimientoDGA', N'MANTENIMIENTODGA', NEWID()),
(N'ComprasDGA',       N'COMPRASDGA',       NEWID()),
(N'Otro',             N'OTRO',             NEWID());
GO
