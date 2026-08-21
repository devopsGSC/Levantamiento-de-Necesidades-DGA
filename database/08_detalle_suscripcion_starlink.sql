/* =====================================================================
   DGA — Levantamiento de Necesidades
   Script incremental 08: nuevo Detalle "Suscripción Internet Starlink"
   bajo el elemento "Redes y telecomunicaciones" (40103) — el equipo
   Starlink (Detalle 4010303, ya existente) es una compra única, pero el
   servicio de internet en sí es una suscripción recurrente. Se agrega
   como Detalle hermano y se marca en CatalogoSuscripciones.DetalleIds
   (código) para que el formulario le pida Tipo de Suscripción y
   Cantidad de Períodos.

   Ya se agregó también al INSERT de 01_schema_dga.sql para instalaciones
   nuevas; este script es solo para bases que ya corrieron ese insert.

   Ejecutar manualmente en SQL Server Management Studio después de
   01_schema_dga.sql — .. — 07_suscripciones_items.sql.
   ===================================================================== */

USE requerimientosDGA;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Detalles WHERE Id = 4010315)
BEGIN
    INSERT INTO dbo.Detalles (Id, ElementoId, Nombre, Orden)
    VALUES (4010315, 40103, N'Suscripción Internet Starlink', 15);
END
GO
