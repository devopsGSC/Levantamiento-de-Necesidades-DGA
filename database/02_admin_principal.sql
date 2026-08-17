/* =====================================================================
   DGA — Levantamiento de Necesidades
   Script incremental 02: agrega el concepto de "Administrador Principal".

   Contexto: solo el admin principal puede cambiar el rol de otro usuario
   que ya tiene el rol Administrador (evita que cualquier admin degrade o
   reasigne a otro admin). Cualquier admin puede seguir promoviendo un
   usuario con rol "Usuario" a "Administrador" sin restricción.

   Ejecutar manualmente en SQL Server Management Studio después de
   01_schema_dga.sql.
   ===================================================================== */

USE requerimientosDGA;
GO

ALTER TABLE dbo.AspNetUsers
    ADD EsAdminPrincipal BIT NOT NULL CONSTRAINT DF_AspNetUsers_EsAdminPrincipal DEFAULT (0);
GO

/* Si todavía no hay ningún admin marcado como principal, se designa al
   administrador más antiguo (el primero creado) como principal. No hace
   nada si ya existe uno (script seguro para volver a correr). */
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE EsAdminPrincipal = 1)
BEGIN
    UPDATE u
    SET u.EsAdminPrincipal = 1
    FROM dbo.AspNetUsers u
    WHERE u.Id = (
        SELECT TOP (1) u2.Id
        FROM dbo.AspNetUsers u2
        INNER JOIN dbo.AspNetUserRoles ur ON ur.UserId = u2.Id
        INNER JOIN dbo.AspNetRoles r ON r.Id = ur.RoleId
        WHERE r.Name = N'Administrador'
        ORDER BY u2.CreatedAt ASC, u2.Id ASC
    );
END
GO
