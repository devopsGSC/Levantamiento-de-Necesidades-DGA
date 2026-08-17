/* =====================================================================================
   Levantamiento de Necesidades DGA — Esquema SQL Server (Fase 1)
   Generado: 2026-07-23
   Ejecutar contra una base de datos NUEVA y vacía en SQL Server Management Studio.

   Contenido:
     1. Tablas de Identity (AspNetUsers/AspNetRoles y relacionadas)
     2. Catálogos de negocio (Tipos de Aduana, Aduanas, Cargos, Componentes en
        cascada, Prioridades, Estados de Solicitud) + datos semilla
     3. Núcleo de negocio (Solicitudes, SolicitudItems, SolicitudItemFotografias,
        SolicitudHistorial)
     4. Secuencia para generación atómica de IdSolicitud (corrige el bug de
        colisión de ID encontrado en el sitio original)

   Notas de diseño (ver detalle completo en el artifact de Fase 1):
     - Los catálogos usan Id explícito (no IDENTITY) para que la semilla sea
       legible y estable.
     - Ningún FK hacia AspNetUsers usa CASCADE (evita el error de "múltiples
       rutas de cascada" de SQL Server y evita que borrar un usuario borre
       solicitudes; los usuarios se desactivan con Activo = 0, no se eliminan).
     - Solicitudes -> SolicitudItems -> SolicitudItemFotografias y
       Solicitudes -> SolicitudHistorial sí usan CASCADE (una sola ruta cada una).
   ===================================================================================== */

SET NOCOUNT ON;
GO

/* =====================================================================================
   1. IDENTITY (ASP.NET Core Identity, claves int)
   ===================================================================================== */

CREATE TABLE dbo.AspNetRoles (
    Id              INT IDENTITY(1,1) NOT NULL,
    Name            NVARCHAR(256)     NULL,
    NormalizedName  NVARCHAR(256)     NULL,
    ConcurrencyStamp NVARCHAR(MAX)    NULL,
    CONSTRAINT PK_AspNetRoles PRIMARY KEY (Id)
);
GO

CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;
GO

CREATE TABLE dbo.AspNetUsers (
    Id                      INT IDENTITY(1,1) NOT NULL,
    UserName                NVARCHAR(256)     NULL,
    NormalizedUserName      NVARCHAR(256)     NULL,
    Email                   NVARCHAR(256)     NULL,
    NormalizedEmail         NVARCHAR(256)     NULL,
    EmailConfirmed          BIT               NOT NULL DEFAULT (0),
    PasswordHash            NVARCHAR(MAX)     NULL,
    SecurityStamp           NVARCHAR(MAX)     NULL,
    ConcurrencyStamp        NVARCHAR(MAX)     NULL,
    PhoneNumber             NVARCHAR(MAX)     NULL,
    PhoneNumberConfirmed    BIT               NOT NULL DEFAULT (0),
    TwoFactorEnabled        BIT               NOT NULL DEFAULT (0),
    LockoutEnd              DATETIMEOFFSET    NULL,
    LockoutEnabled          BIT               NOT NULL DEFAULT (1),
    AccessFailedCount       INT               NOT NULL DEFAULT (0),
    -- columnas de negocio (equivalentes a los campos custom de la colección "users" original)
    Nombre                  NVARCHAR(150)     NOT NULL,
    Departamento            NVARCHAR(100)     NULL,
    PasswordTemporal        BIT               NOT NULL DEFAULT (0),
    PrimerInicioSesion      BIT               NOT NULL DEFAULT (1),
    CredencialesReenviadasEn DATETIME2        NULL,
    Activo                  BIT               NOT NULL DEFAULT (1),
    CreatedAt               DATETIME2         NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_AspNetUsers PRIMARY KEY (Id)
);
GO

CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
CREATE INDEX EmailIndex ON dbo.AspNetUsers (NormalizedEmail);
GO

CREATE TABLE dbo.AspNetUserRoles (
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_AspNetUsers FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.AspNetUserClaims (
    Id          INT IDENTITY(1,1) NOT NULL,
    UserId      INT NOT NULL,
    ClaimType   NVARCHAR(MAX) NULL,
    ClaimValue  NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetUserClaims PRIMARY KEY (Id),
    CONSTRAINT FK_AspNetUserClaims_AspNetUsers FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.AspNetUserLogins (
    LoginProvider        NVARCHAR(450) NOT NULL,
    ProviderKey          NVARCHAR(450) NOT NULL,
    ProviderDisplayName  NVARCHAR(MAX) NULL,
    UserId               INT NOT NULL,
    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_AspNetUserLogins_AspNetUsers FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.AspNetUserTokens (
    UserId          INT NOT NULL,
    LoginProvider   NVARCHAR(450) NOT NULL,
    Name            NVARCHAR(450) NOT NULL,
    Value           NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_AspNetUserTokens_AspNetUsers FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.AspNetRoleClaims (
    Id          INT IDENTITY(1,1) NOT NULL,
    RoleId      INT NOT NULL,
    ClaimType   NVARCHAR(MAX) NULL,
    ClaimValue  NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY (Id),
    CONSTRAINT FK_AspNetRoleClaims_AspNetRoles FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE
);
GO

/* =====================================================================================
   2. CATÁLOGOS DE NEGOCIO
   ===================================================================================== */

-- 2.1 Tipos de Aduana → Aduanas ------------------------------------------------------

CREATE TABLE dbo.TiposAduana (
    Id      TINYINT      NOT NULL,
    Nombre  NVARCHAR(60) NOT NULL,
    Orden   SMALLINT     NOT NULL,
    CONSTRAINT PK_TiposAduana PRIMARY KEY (Id),
    CONSTRAINT UQ_TiposAduana_Nombre UNIQUE (Nombre)
);
GO

CREATE TABLE dbo.Aduanas (
    Id            INT           NOT NULL,
    TipoAduanaId  TINYINT       NOT NULL,
    Codigo        VARCHAR(10)   NOT NULL,
    Nombre        NVARCHAR(150) NOT NULL,
    Orden         SMALLINT      NOT NULL,
    CONSTRAINT PK_Aduanas PRIMARY KEY (Id),
    CONSTRAINT FK_Aduanas_TiposAduana FOREIGN KEY (TipoAduanaId) REFERENCES dbo.TiposAduana (Id) ON DELETE NO ACTION
);
GO

INSERT INTO dbo.TiposAduana (Id, Nombre, Orden) VALUES
(1, N'ZONAS FRANCAS', 1),
(2, N'ADUANAS DE FRONTERAS', 2),
(3, N'PUERTOS', 3),
(4, N'AEROPUERTO', 4),
(5, N'COURIER', 5),
(6, N'ADUANAS INTERNAS', 6);
GO

INSERT INTO dbo.Aduanas (Id, TipoAduanaId, Codigo, Nombre, Orden) VALUES
-- ZONAS FRANCAS (1)
(101, 1, N'16', N'Z.F. SAN MARCOS', 1),
(102, 1, N'17', N'Z.F. EL PEDREGAL', 2),
(103, 1, N'18', N'Z.F. SAN BARTOLO', 3),
(104, 1, N'20', N'Z.F. EXPORTSALVA', 4),
(105, 1, N'21', N'Z.F. AMERICAN PARK', 5),
(106, 1, N'23', N'Z.F. INTERNACIONAL', 6),
(107, 1, N'24', N'DELG. Z.FCA DIEZ', 7),
(108, 1, N'26', N'DELG. Z.FCA. MIRAMAR', 8),
(109, 1, N'27', N'DELG. ZFCA. STO. TOMÁS', 9),
(110, 1, N'28', N'DELG. ZFCA. SANTA TECLA', 10),
(111, 1, N'29', N'DELG. ZFCA. SANTA ANA', 11),
(112, 1, N'30', N'DELG. ZFCA. CONCORDIA INDUST. PARK', 12),
(113, 1, N'32', N'Z.F. PIPIL, S.A. DE C.V.', 13),
(114, 1, N'34', N'DELG. ZFCA. CALVO CONSERVAS', 14),
(115, 1, N'37', N'Z.FCA PARQUE INDUSTRIAL SAM-LI', 15),
(116, 1, N'38', N'Z.FCA. SAN JOSE', 16),
(117, 1, N'39', N'Z.F. LAS MERCEDES', 17),
(118, 1, N'40', N'Z.F. EMCO, S.A. DE C.V.', 18),
(119, 1, N'41', N'Z.F. GIGANTE, S.A DE C.V', 19),
(120, 1, N'42', N'Z.F. NOVABES', 20),
-- ADUANAS DE FRONTERAS (2)
(201, 2, N'04', N'TERRESTRE LAS CHINAMAS', 1),
(202, 2, N'05', N'TERRESTRE LA HACHADURA', 2),
(203, 2, N'07', N'TERRESTRE SAN CRISTOBAL', 3),
(204, 2, N'08', N'TERRESTRE ANGUIATU', 4),
(205, 2, N'09', N'TERRESTRE EL AMATILLO', 5),
(206, 2, N'11', N'TERRESTRE EL POY', 6),
(207, 2, N'12', N'TERRESTRE METALIO', 7),
-- PUERTOS (3)
(301, 3, N'02', N'MARITIMA DE ACAJUTLA', 1),
(302, 3, N'10', N'MARITIMA LA UNION', 2),
-- AEROPUERTO (4)
(401, 4, N'03', N'AÉREA MONSEÑOR ÓSCAR ARNULFO ROMERO', 1),
(402, 4, N'03', N'TERMINAL DE CARGA', 2),
(403, 4, N'03', N'TERMINAL DE PASAJEROS', 3),
(404, 4, N'31', N'DELEGACIÓN ADUANA AÉREA ILOPANGO', 4),
-- COURIER (5)
(501, 5, N'15', N'FARDOS POSTALES', 1),
(502, 5, N'76', N'DELEGACION DHL', 2),
(503, 5, N'85', N'GUTIERREZ COURIER Y CARGO', 3),
(504, 5, N'36', N'DELEG. ADUANA EL PAPALOŃ', 4),
-- ADUANAS INTERNAS (6)
(601, 6, N'01', N'SAN BARTOLO', 1),
(602, 6, N'06', N'TERRESTRE SANTA ANA', 2),
(603, 6, N'35', N'DELEG. ADUANA FERIA INTERNACIONAL', 3),
(604, 6, N'71', N'DELG.AD. ALM. DESARROLLO.(ALDESA)', 4),
(605, 6, N'72', N'DELG.AD.ALM.GRAL.DEP.OCCIDE(AGDOSA)', 5),
(606, 6, N'73', N'DELG.AD.BODEGA GRAL.DEPOSI(BODESA)', 6),
(607, 6, N'77', N'DELEG. ADUANA TRANSAUTO, S.A. de CV', 7),
(608, 6, N'80', N'ALMACENADORA NEJAPA SA DE C.V', 8),
(609, 6, N'81', N'ALMACENADORA ALMACONSA S.A. DE C.V.', 9),
(610, 6, N'83', N'ALM.GRAL.DEPOSITO OCCIDENTE (APOPA)', 10);
GO

-- 2.2 Cargos --------------------------------------------------------------------------

CREATE TABLE dbo.Cargos (
    Id      TINYINT      NOT NULL,
    Nombre  NVARCHAR(60) NOT NULL,
    Orden   SMALLINT     NOT NULL,
    CONSTRAINT PK_Cargos PRIMARY KEY (Id)
);
GO

INSERT INTO dbo.Cargos (Id, Nombre, Orden) VALUES
(1, N'Subdirector', 1),
(2, N'Jefe de Departamento', 2),
(3, N'Administrador de Aduanas', 3),
(4, N'Técnico', 4);
GO

-- 2.3 Componentes → Subcomponentes → Elementos → Detalles ------------------------------

CREATE TABLE dbo.Componentes (
    Id      TINYINT       NOT NULL,
    Nombre  NVARCHAR(120) NOT NULL,
    Orden   SMALLINT      NOT NULL,
    CONSTRAINT PK_Componentes PRIMARY KEY (Id)
);
GO

CREATE TABLE dbo.Subcomponentes (
    Id            INT           NOT NULL,
    ComponenteId  TINYINT       NOT NULL,
    Nombre        NVARCHAR(150) NOT NULL,
    Orden         SMALLINT      NOT NULL,
    CONSTRAINT PK_Subcomponentes PRIMARY KEY (Id),
    CONSTRAINT FK_Subcomponentes_Componentes FOREIGN KEY (ComponenteId) REFERENCES dbo.Componentes (Id) ON DELETE NO ACTION
);
GO

CREATE TABLE dbo.Elementos (
    Id               INT           NOT NULL,
    SubcomponenteId  INT           NOT NULL,
    Nombre           NVARCHAR(200) NOT NULL,
    TieneDetalle     BIT           NOT NULL DEFAULT (0),
    Orden            SMALLINT      NOT NULL,
    CONSTRAINT PK_Elementos PRIMARY KEY (Id),
    CONSTRAINT FK_Elementos_Subcomponentes FOREIGN KEY (SubcomponenteId) REFERENCES dbo.Subcomponentes (Id) ON DELETE NO ACTION
);
GO

CREATE TABLE dbo.Detalles (
    Id          INT           NOT NULL,
    ElementoId  INT           NOT NULL,
    Nombre      NVARCHAR(200) NOT NULL,
    Orden       SMALLINT      NOT NULL,
    CONSTRAINT PK_Detalles PRIMARY KEY (Id),
    CONSTRAINT FK_Detalles_Elementos FOREIGN KEY (ElementoId) REFERENCES dbo.Elementos (Id) ON DELETE NO ACTION
);
GO

INSERT INTO dbo.Componentes (Id, Nombre, Orden) VALUES
(1, N'Sistemas Aduaneros', 1),
(2, N'Infraestructura o Adecuaciones', 2),
(3, N'Gestión Operativa', 3),
(4, N'Mobiliario y Equipo Tecnológico', 4),
(5, N'Procesos y Normativas', 5);
GO

INSERT INTO dbo.Subcomponentes (Id, ComponenteId, Nombre, Orden) VALUES
-- Componente 1: Sistemas Aduaneros (todos hoja, sin elementos)
(101, 1, N'Modificación del Sistema Actual', 1),
(102, 1, N'Automatización de procesos', 2),
(103, 1, N'Fallas en sistema', 3),
(104, 1, N'Creación de Sistema', 4),
(105, 1, N'Otros', 5),
-- Componente 2: Infraestructura o Adecuaciones
(201, 2, N'Modernización de puestos fronterizos y aduanas', 1),
(202, 2, N'Infraestructura de inspección no intrusiva', 2),
(203, 2, N'Adecuaciones de zonas de inspección', 3),
(204, 2, N'Sistemas de videovigilancia y control', 4),
(205, 2, N'Mantenimiento preventivo y correctivo', 5),
(206, 2, N'Suministros de servicios básicos', 6),
(207, 2, N'Señalización, flujo vehicular y ordenamiento físico', 7),
(208, 2, N'Reparaciones Generales', 8),
(209, 2, N'Instalaciones Generales', 9),
(210, 2, N'Adecuaciones en zona para atención al usuario', 10),
(211, 2, N'Mejora de áreas comunes', 11),
-- Componente 3: Gestión Operativa
(301, 3, N'Fortalecimiento de recurso humano', 1),
(302, 3, N'Capacitaciones', 2),
(303, 3, N'Suministro de equipo para personal', 3),
(304, 3, N'Otros', 4),
-- Componente 4: Mobiliario y Equipo Tecnológico
(401, 4, N'Dotación de equipo tecnológico', 1),
(402, 4, N'Dotación de Mobiliario', 2),
(403, 4, N'Otros', 3),
-- Componente 5: Procesos y Normativas (todos hoja)
(501, 5, N'Análisis de procesos', 1),
(502, 5, N'Modificación de procesos', 2),
(503, 5, N'Automatización de procesos', 3),
(504, 5, N'Revisión de Normativa', 4);
GO

INSERT INTO dbo.Elementos (Id, SubcomponenteId, Nombre, TieneDetalle, Orden) VALUES
-- 202 Infraestructura de inspección no intrusiva (2)
(20201, 202, N'Escáner', 0, 1),
(20202, 202, N'Báscula', 0, 2),
-- 203 Adecuaciones de zonas de inspección (19)
(20301, 203, N'Pintura', 0, 1),
(20302, 203, N'Cielo falso', 0, 2),
(20303, 203, N'Paredes', 0, 3),
(20304, 203, N'Pisos', 0, 4),
(20305, 203, N'Techos', 0, 5),
(20306, 203, N'Cortinas', 0, 6),
(20307, 203, N'Ventanas', 0, 7),
(20308, 203, N'Puertas', 0, 8),
(20309, 203, N'Estantes metálicos', 0, 9),
(20310, 203, N'Señalética', 0, 10),
(20311, 203, N'Lámparas de emergencia con batería', 0, 11),
(20312, 203, N'Alarmas contra incendios', 0, 12),
(20313, 203, N'Detectores de humo', 0, 13),
(20314, 203, N'Luminaria', 0, 14),
(20315, 203, N'Extintores', 0, 15),
(20316, 203, N'Conos de seguridad', 0, 16),
(20317, 203, N'Instalación eléctrica', 0, 17),
(20318, 203, N'Parqueos', 0, 18),
(20319, 203, N'Rampas de carga', 0, 19),
-- 204 Sistemas de videovigilancia y control (7)
(20401, 204, N'Cámaras IP', 0, 1),
(20402, 204, N'Cámaras domo', 0, 2),
(20403, 204, N'Cámaras ojo de pez / Fisheye', 0, 3),
(20404, 204, N'Cámaras con visión nocturna / IR', 0, 4),
(20405, 204, N'CCTV', 0, 5),
(20406, 204, N'Lector de huella digital', 0, 6),
(20407, 204, N'Lector de reconocimiento facial', 0, 7),
-- 205 Mantenimiento preventivo y correctivo (14)
(20501, 205, N'Aires acondicionados', 0, 1),
(20502, 205, N'Cámaras', 0, 2),
(20503, 205, N'CCTV', 0, 3),
(20504, 205, N'Escáneres', 0, 4),
(20505, 205, N'Extintores', 0, 5),
(20506, 205, N'Equipos', 0, 6),
(20507, 205, N'Vehículos', 0, 7),
(20508, 205, N'Planta de energía eléctrica', 0, 8),
(20509, 205, N'Cisternas', 0, 9),
(20510, 205, N'Carriles neumáticos', 0, 10),
(20511, 205, N'Canaletas', 0, 11),
(20512, 205, N'Bombas de agua', 0, 12),
(20513, 205, N'Tubería para acueducto', 0, 13),
(20514, 205, N'Conexión eléctrica', 0, 14),
-- 206 Suministros de servicios básicos (4)
(20601, 206, N'Agua', 0, 1),
(20602, 206, N'Energía eléctrica', 0, 2),
(20603, 206, N'Internet', 0, 3),
(20604, 206, N'Telefonía', 0, 4),
-- 208 Reparaciones Generales (35)
(20801, 208, N'Pintura interior', 0, 1),
(20802, 208, N'Pintura exterior', 0, 2),
(20803, 208, N'Pintura anticorrosiva', 0, 3),
(20804, 208, N'Pintura epóxica', 0, 4),
(20805, 208, N'Reparación de cielo falso', 0, 5),
(20806, 208, N'Sustitución de cielo falso', 0, 6),
(20807, 208, N'Reparación de paredes de tabla yeso', 0, 7),
(20808, 208, N'Reparación de paredes de mampostería', 0, 8),
(20809, 208, N'Reparación de pisos cerámicos', 0, 9),
(20810, 208, N'Reparación de pisos de concreto', 0, 10),
(20811, 208, N'Reparación de pisos vinílicos', 0, 11),
(20812, 208, N'Impermeabilización de techos', 0, 12),
(20813, 208, N'Sustitución de láminas de techo', 0, 13),
(20814, 208, N'Reparación de canales de aguas lluvias', 0, 14),
(20815, 208, N'Reparación de cortinas metálicas', 0, 15),
(20816, 208, N'Sustitución de cortinas metálicas', 0, 16),
(20817, 208, N'Reparación de ventanas de vidrio', 0, 17),
(20818, 208, N'Reparación de ventanas de aluminio', 0, 18),
(20819, 208, N'Reparación de puertas de madera', 0, 19),
(20820, 208, N'Reparación de puertas de metal', 0, 20),
(20821, 208, N'Reparación de puertas de vidrio', 0, 21),
(20822, 208, N'Reparación de cerraduras y chapas', 0, 22),
(20823, 208, N'Reparación de bombas de agua', 0, 23),
(20824, 208, N'Reparación de tuberías de agua potable', 0, 24),
(20825, 208, N'Reparación de tuberías de aguas negras', 0, 25),
(20826, 208, N'Reparación de lavamanos', 0, 26),
(20827, 208, N'Reparación de inodoros', 0, 27),
(20828, 208, N'Reparación de mingitorios', 0, 28),
(20829, 208, N'Reparación de luminarias', 0, 29),
(20830, 208, N'Reparación de tomacorrientes e interruptores', 0, 30),
(20831, 208, N'Reparación de tableros eléctricos', 0, 31),
(20832, 208, N'Reparación de baches en parqueos', 0, 32),
(20833, 208, N'Reparación de aires acondicionados', 0, 33),
(20834, 208, N'Reparación de portones eléctricos', 0, 34),
(20835, 208, N'Reparación de cercas perimetrales', 0, 35),
-- 209 Instalaciones Generales (27)
(20901, 209, N'Instalación de cielo falso', 0, 1),
(20902, 209, N'Instalación de paredes de tabla yeso', 0, 2),
(20903, 209, N'Instalación de paredes de vidrio', 0, 3),
(20904, 209, N'Instalación de pisos cerámicos', 0, 4),
(20905, 209, N'Instalación de pisos de porcelanato', 0, 5),
(20906, 209, N'Instalación de techos de lámina', 0, 6),
(20907, 209, N'Instalación de techos de policarbonato', 0, 7),
(20908, 209, N'Instalación de cortinas metálicas', 0, 8),
(20909, 209, N'Instalación de cortinas tipo roller', 0, 9),
(20910, 209, N'Instalación de ventanas de aluminio y vidrio', 0, 10),
(20911, 209, N'Instalación de puertas de madera', 0, 11),
(20912, 209, N'Instalación de puertas de metal', 0, 12),
(20913, 209, N'Instalación de puertas de vidrio templado', 0, 13),
(20914, 209, N'Instalación de bombas de agua', 0, 14),
(20915, 209, N'Instalación de cisternas', 0, 15),
(20916, 209, N'Instalación de lavamanos', 0, 16),
(20917, 209, N'Instalación de inodoros', 0, 17),
(20918, 209, N'Instalación de mingitorios', 0, 18),
(20919, 209, N'Instalación de dispensadores de jabón y papel', 0, 19),
(20920, 209, N'Instalación de luminarias LED', 0, 20),
(20921, 209, N'Instalación de reflectores exteriores', 0, 21),
(20922, 209, N'Instalación de red eléctrica regulada', 0, 22),
(20923, 209, N'Instalación de red eléctrica normal', 0, 23),
(20924, 209, N'Instalación de fregaderos', 0, 24),
(20925, 209, N'Instalación de gabinetes de cocina', 0, 25),
(20926, 209, N'Instalación de extractores de aire', 0, 26),
(20927, 209, N'Instalación de aires acondicionados', 0, 27),
-- 211 Mejora de áreas comunes (6)
(21101, 211, N'Baños', 0, 1),
(21102, 211, N'Comedores', 0, 2),
(21103, 211, N'Cafeterías', 0, 3),
(21104, 211, N'Habitaciones de descanso', 0, 4),
(21105, 211, N'Parqueos', 0, 5),
(21106, 211, N'Área de lavado y secado', 0, 6),
-- 302 Capacitaciones (5, hoja, sin detalle)
(30201, 302, N'Formación técnica aduanera', 0, 1),
(30202, 302, N'Entrenamiento en sistemas tecnológicos', 0, 2),
(30203, 302, N'Gestión administrativa', 0, 3),
(30204, 302, N'Atención al usuario', 0, 4),
(30205, 302, N'Seguridad y salud ocupacional', 0, 5),
-- 303 Suministro de equipo para personal (1, con 25 detalles)
(30301, 303, N'Uniformes, Identificación y seguridad ocupacional', 1, 1),
-- 401 Dotación de equipo tecnológico (3, todos con detalle)
(40101, 401, N'Equipos', 1, 1),
(40102, 401, N'Equipos de inspección no intrusiva / Rayos X', 1, 2),
(40103, 401, N'Redes y telecomunicaciones', 1, 3),
-- 402 Dotación de Mobiliario (3, todos con detalle)
(40201, 402, N'Mobiliario de oficina', 1, 1),
(40202, 402, N'Equipamiento para atención al cliente', 1, 2),
(40203, 402, N'Ergonomía y adecuación de espacios', 1, 3);
GO

INSERT INTO dbo.Detalles (Id, ElementoId, Nombre, Orden) VALUES
-- 30301 Uniformes, Identificación y seguridad ocupacional (25)
(3030101, 30301, N'Pantalón', 1),
(3030102, 30301, N'Camisa', 2),
(3030103, 30301, N'Chaqueta', 3),
(3030104, 30301, N'Chaleco reflectante', 4),
(3030105, 30301, N'Casco de seguridad', 5),
(3030106, 30301, N'Calzado de seguridad', 6),
(3030107, 30301, N'Camiseta', 7),
(3030108, 30301, N'Gorra', 8),
(3030109, 30301, N'Carnet', 9),
(3030110, 30301, N'Porta carnet', 10),
(3030111, 30301, N'Guantes de protección mecánica', 11),
(3030112, 30301, N'Guantes de protección química', 12),
(3030113, 30301, N'Arnés de seguridad', 13),
(3030114, 30301, N'Mascarillas', 14),
(3030115, 30301, N'Lentes de protección', 15),
(3030116, 30301, N'Protectores auditivos', 16),
(3030117, 30301, N'Radios de comunicación', 17),
(3030118, 30301, N'Linternas', 18),
(3030119, 30301, N'Bodycams', 19),
(3030120, 30301, N'Maletines de inspección', 20),
(3030121, 30301, N'Herramientas de inspección y medición', 21),
(3030122, 30301, N'Herramientas para reparaciones', 22),
(3030123, 30301, N'Documentación y suministros administrativos', 23),
(3030124, 30301, N'Botiquín de primeros auxilios', 24),
(3030125, 30301, N'Cinta de delimitación de peligro', 25),
-- 40101 Equipos (22)
(4010101, 40101, N'Desktop', 1),
(4010102, 40101, N'Mouse', 2),
(4010103, 40101, N'Teclado', 3),
(4010104, 40101, N'Webcam', 4),
(4010105, 40101, N'Laptop', 5),
(4010106, 40101, N'UPS', 6),
(4010107, 40101, N'Headset', 7),
(4010108, 40101, N'Lectores QR o Barras', 8),
(4010109, 40101, N'Telefonía Fija', 9),
(4010110, 40101, N'Telefonía Móvil', 10),
(4010111, 40101, N'Tablet', 11),
(4010112, 40101, N'Regleta', 12),
(4010113, 40101, N'Cableado', 13),
(4010114, 40101, N'Bocina', 14),
(4010115, 40101, N'Impresora multifuncional', 15),
(4010116, 40101, N'Robots', 16),
(4010117, 40101, N'Sistema de acceso biométrico', 17),
(4010118, 40101, N'Sistema de acceso dactilar', 18),
(4010119, 40101, N'Televisor', 19),
(4010120, 40101, N'Pantallas', 20),
(4010121, 40101, N'Monitor', 21),
(4010122, 40101, N'Regulador de voltaje', 22),
-- 40102 Equipos de inspección no intrusiva / Rayos X (5)
(4010201, 40102, N'Alta energía', 1),
(4010202, 40102, N'Baja energía', 2),
(4010203, 40102, N'Móvil', 3),
(4010204, 40102, N'Básculas', 4),
(4010205, 40102, N'Detectores de sustancias', 5),
-- 40103 Redes y telecomunicaciones (14)
(4010301, 40103, N'Repetidores', 1),
(4010302, 40103, N'Antenas', 2),
(4010303, 40103, N'Starlink', 3),
(4010304, 40103, N'Fibra óptica', 4),
(4010305, 40103, N'Cable coaxial', 5),
(4010306, 40103, N'Conectores', 6),
(4010307, 40103, N'Puntos de acceso', 7),
(4010308, 40103, N'Cableado', 8),
(4010309, 40103, N'Organizadores de cableado', 9),
(4010310, 40103, N'Router', 10),
(4010311, 40103, N'UPS', 11),
(4010312, 40103, N'Telefonía Fija', 12),
(4010313, 40103, N'Telefonía Móvil', 13),
(4010314, 40103, N'PBX', 14),
-- 40201 Mobiliario de oficina (27)
(4020101, 40201, N'Sillas ejecutivas', 1),
(4020102, 40201, N'Sillas de espera', 2),
(4020103, 40201, N'Sillas giratorias', 3),
(4020104, 40201, N'Sillas', 4),
(4020105, 40201, N'Taburetes de oficina', 5),
(4020106, 40201, N'Mesa de oficina', 6),
(4020107, 40201, N'Mesa de juntas', 7),
(4020108, 40201, N'Mesa de centro', 8),
(4020109, 40201, N'Escritorio', 9),
(4020110, 40201, N'Módulos de atención', 10),
(4020111, 40201, N'Sofás', 11),
(4020112, 40201, N'Sillones', 12),
(4020113, 40201, N'Cubículo', 13),
(4020114, 40201, N'Estaciones de trabajo', 14),
(4020115, 40201, N'Mesa de reuniones', 15),
(4020116, 40201, N'Archivero metálico', 16),
(4020117, 40201, N'Estantería', 17),
(4020118, 40201, N'Librera', 18),
(4020119, 40201, N'Credenza', 19),
(4020120, 40201, N'Estantes metálicos', 20),
(4020121, 40201, N'Mamparas', 21),
(4020122, 40201, N'Lockers', 22),
(4020123, 40201, N'Trituradora de papel', 23),
(4020124, 40201, N'Racks para equipos', 24),
(4020125, 40201, N'Lámparas de escritorio', 25),
(4020126, 40201, N'Soporte de escritorio para monitor', 26),
(4020127, 40201, N'Gabinete metálico', 27),
-- 40202 Equipamiento para atención al cliente (29)
(4020201, 40202, N'Cafetera', 1),
(4020202, 40202, N'Oasis dispensador de agua fría y caliente', 2),
(4020203, 40202, N'Muebles', 3),
(4020204, 40202, N'Sillas ergonómicas para agentes', 4),
(4020205, 40202, N'Sillas para clientes', 5),
(4020206, 40202, N'Mesas auxiliares', 6),
(4020207, 40202, N'Decoraciones', 7),
(4020208, 40202, N'Dispensadores de gel antibacterial', 8),
(4020209, 40202, N'Bancas para espera', 9),
(4020210, 40202, N'Ventanillas de atención', 10),
(4020211, 40202, N'Módulos de atención', 11),
(4020212, 40202, N'Sistema de turnos', 12),
(4020213, 40202, N'Pantallas de llamado', 13),
(4020214, 40202, N'Dispensador de tickets', 14),
(4020215, 40202, N'Separadores de fila', 15),
(4020216, 40202, N'Relojes visibles', 16),
(4020217, 40202, N'Escáner documental', 17),
(4020218, 40202, N'Señalización interna', 18),
(4020219, 40202, N'Cámaras de video vigilancia', 19),
(4020220, 40202, N'TV', 20),
(4020221, 40202, N'Aire acondicionado', 21),
(4020222, 40202, N'Sistemas biométricos', 22),
(4020223, 40202, N'UPS', 23),
(4020224, 40202, N'Regletas y extensiones', 24),
(4020225, 40202, N'Trituradoras de papel', 25),
(4020226, 40202, N'Organizador de cables', 26),
(4020227, 40202, N'Papeleras', 27),
(4020228, 40202, N'Alarmas contra incendios', 28),
(4020229, 40202, N'Detectores de humo', 29),
-- 40203 Ergonomía y adecuación de espacios (6)
(4020301, 40203, N'Aire acondicionado', 1),
(4020302, 40203, N'Rampas de acceso', 2),
(4020303, 40203, N'Pasamanos', 3),
(4020304, 40203, N'Señalética', 4),
(4020305, 40203, N'Protección de cableado expuesto', 5),
(4020306, 40203, N'Luminaria', 6);
GO

-- 2.4 Prioridades -----------------------------------------------------------------------

CREATE TABLE dbo.Prioridades (
    Id      TINYINT      NOT NULL,
    Nombre  NVARCHAR(20) NOT NULL,
    Orden   SMALLINT     NOT NULL,
    CONSTRAINT PK_Prioridades PRIMARY KEY (Id)
);
GO

INSERT INTO dbo.Prioridades (Id, Nombre, Orden) VALUES
(1, N'Alta', 1),
(2, N'Media', 2),
(3, N'Baja', 3);
GO

-- 2.5 Estados de Solicitud (decisión confirmada: lista observada en vivo) --------------

CREATE TABLE dbo.EstadosSolicitud (
    Id         TINYINT      NOT NULL,
    Nombre     NVARCHAR(40) NOT NULL,
    Orden      SMALLINT     NOT NULL,
    EsInicial  BIT          NOT NULL DEFAULT (0),
    EsFinal    BIT          NOT NULL DEFAULT (0),
    CONSTRAINT PK_EstadosSolicitud PRIMARY KEY (Id),
    CONSTRAINT UQ_EstadosSolicitud_Nombre UNIQUE (Nombre)
);
GO

INSERT INTO dbo.EstadosSolicitud (Id, Nombre, Orden, EsInicial, EsFinal) VALUES
(1,  N'Guardado Borrador', 1,  1, 0),
(2,  N'Solicitado',        2,  0, 0),
(3,  N'Pendiente',         3,  0, 0),
(4,  N'Aprobado',          4,  0, 0),
(5,  N'Denegado',          5,  0, 1),
(6,  N'Comprado',          6,  0, 0),
(7,  N'Realizado',         7,  0, 0),
(8,  N'En Proceso',        8,  0, 0),
(9,  N'Rechazado',         9,  0, 1),
(10, N'Observado',         10, 0, 0),
(11, N'Cotizado',          11, 0, 0),
(12, N'Finalizado',        12, 0, 1);
GO

/* =====================================================================================
   3. NÚCLEO DE NEGOCIO
   ===================================================================================== */

CREATE TABLE dbo.Solicitudes (
    Id                    INT IDENTITY(1,1) NOT NULL,
    IdSolicitud           VARCHAR(10)       NOT NULL,   -- 'SOL-00001' — generado con dbo.SolicitudIdSequence
    UsuarioId             INT               NOT NULL,
    NombreResponsable     NVARCHAR(150)     NOT NULL,
    CargoId               TINYINT           NOT NULL,
    AduanaId              INT               NOT NULL,
    JustificacionGeneral  NVARCHAR(MAX)     NOT NULL,
    ObservacionesGenerales NVARCHAR(MAX)    NULL,
    EstadoId              TINYINT           NOT NULL,
    AdminRevisorId        INT               NULL,
    Progreso              TINYINT           NULL,        -- 0-100
    FechaRegistro         DATETIME2         NOT NULL DEFAULT (SYSUTCDATETIME()),
    FechaRevision         DATETIME2         NULL,
    FechaFinalizacion     DATETIME2         NULL,
    CreatedAt             DATETIME2         NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt             DATETIME2         NOT NULL DEFAULT (SYSUTCDATETIME()),
    IsDeleted             BIT               NOT NULL DEFAULT (0),
    DeletedAt             DATETIME2         NULL,
    CONSTRAINT PK_Solicitudes PRIMARY KEY (Id),
    CONSTRAINT UQ_Solicitudes_IdSolicitud UNIQUE (IdSolicitud),
    CONSTRAINT CK_Solicitudes_Progreso CHECK (Progreso IS NULL OR Progreso BETWEEN 0 AND 100),
    CONSTRAINT FK_Solicitudes_Usuario FOREIGN KEY (UsuarioId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Solicitudes_AdminRevisor FOREIGN KEY (AdminRevisorId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Solicitudes_Cargo FOREIGN KEY (CargoId) REFERENCES dbo.Cargos (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Solicitudes_Aduana FOREIGN KEY (AduanaId) REFERENCES dbo.Aduanas (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Solicitudes_Estado FOREIGN KEY (EstadoId) REFERENCES dbo.EstadosSolicitud (Id) ON DELETE NO ACTION
);
GO

CREATE INDEX IX_Solicitudes_UsuarioId ON dbo.Solicitudes (UsuarioId);
CREATE INDEX IX_Solicitudes_EstadoId ON dbo.Solicitudes (EstadoId);
GO

CREATE TABLE dbo.SolicitudItems (
    Id                    INT IDENTITY(1,1) NOT NULL,
    SolicitudId           INT               NOT NULL,
    NumeroItem            SMALLINT          NOT NULL,
    ComponenteId          TINYINT           NOT NULL,
    SubcomponenteId       INT               NOT NULL,
    ElementoId            INT               NULL,
    ElementoLibre         NVARCHAR(200)     NULL,  -- texto libre cuando el subcomponente no tiene catálogo de elementos
    DetalleId             INT               NULL,  -- solo si Elementos.TieneDetalle = 1
    CantidadSolicitada    INT               NOT NULL,
    PrioridadId           TINYINT           NOT NULL,
    UbicacionEspecifica   NVARCHAR(200)     NULL,
    JustificacionItem     NVARCHAR(MAX)     NULL,
    CreatedAt             DATETIME2         NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt             DATETIME2         NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_SolicitudItems PRIMARY KEY (Id),
    CONSTRAINT CK_SolicitudItems_Cantidad CHECK (CantidadSolicitada > 0),
    CONSTRAINT FK_SolicitudItems_Solicitud FOREIGN KEY (SolicitudId) REFERENCES dbo.Solicitudes (Id) ON DELETE CASCADE,
    CONSTRAINT FK_SolicitudItems_Componente FOREIGN KEY (ComponenteId) REFERENCES dbo.Componentes (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_SolicitudItems_Subcomponente FOREIGN KEY (SubcomponenteId) REFERENCES dbo.Subcomponentes (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_SolicitudItems_Elemento FOREIGN KEY (ElementoId) REFERENCES dbo.Elementos (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_SolicitudItems_Detalle FOREIGN KEY (DetalleId) REFERENCES dbo.Detalles (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_SolicitudItems_Prioridad FOREIGN KEY (PrioridadId) REFERENCES dbo.Prioridades (Id) ON DELETE NO ACTION
);
GO

CREATE INDEX IX_SolicitudItems_SolicitudId ON dbo.SolicitudItems (SolicitudId);
GO

CREATE TABLE dbo.SolicitudItemFotografias (
    Id               INT IDENTITY(1,1) NOT NULL,
    SolicitudItemId  INT               NOT NULL,
    RutaArchivo      NVARCHAR(300)     NOT NULL,
    NombreOriginal   NVARCHAR(260)     NOT NULL,
    ContentType      VARCHAR(100)      NOT NULL,
    TamanoBytes      INT               NOT NULL,
    SubidoEn         DATETIME2         NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_SolicitudItemFotografias PRIMARY KEY (Id),
    CONSTRAINT CK_SolicitudItemFotografias_Tamano CHECK (TamanoBytes > 0 AND TamanoBytes <= 10485760),
    CONSTRAINT FK_SolicitudItemFotografias_Item FOREIGN KEY (SolicitudItemId) REFERENCES dbo.SolicitudItems (Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_SolicitudItemFotografias_ItemId ON dbo.SolicitudItemFotografias (SolicitudItemId);
GO

CREATE TABLE dbo.SolicitudHistorial (
    Id                INT IDENTITY(1,1) NOT NULL,
    SolicitudId       INT               NOT NULL,
    EstadoAnteriorId  TINYINT           NULL,
    EstadoNuevoId     TINYINT           NOT NULL,
    UsuarioCambioId   INT               NULL,
    Comentario        NVARCHAR(MAX)     NULL,
    FechaCambio       DATETIME2         NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_SolicitudHistorial PRIMARY KEY (Id),
    CONSTRAINT FK_SolicitudHistorial_Solicitud FOREIGN KEY (SolicitudId) REFERENCES dbo.Solicitudes (Id) ON DELETE CASCADE,
    CONSTRAINT FK_SolicitudHistorial_EstadoAnterior FOREIGN KEY (EstadoAnteriorId) REFERENCES dbo.EstadosSolicitud (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_SolicitudHistorial_EstadoNuevo FOREIGN KEY (EstadoNuevoId) REFERENCES dbo.EstadosSolicitud (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_SolicitudHistorial_Usuario FOREIGN KEY (UsuarioCambioId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION
);
GO

CREATE INDEX IX_SolicitudHistorial_SolicitudId ON dbo.SolicitudHistorial (SolicitudId);
GO

/* =====================================================================================
   4. SECUENCIA PARA IdSolicitud (corrige el bug de colisión de ID de Fase 0)

   Uso desde la aplicación (dentro de una transacción, antes del INSERT en Solicitudes):

       DECLARE @next INT = NEXT VALUE FOR dbo.SolicitudIdSequence;
       DECLARE @idSolicitud VARCHAR(10) = 'SOL-' + RIGHT('00000' + CAST(@next AS VARCHAR(5)), 5);

   NEXT VALUE FOR es atómico a nivel de motor — dos solicitudes concurrentes nunca
   reciben el mismo número, a diferencia del cálculo en el cliente del sitio original.
   ===================================================================================== */

CREATE SEQUENCE dbo.SolicitudIdSequence
    AS INT
    START WITH 1
    INCREMENT BY 1
    NO CYCLE;
GO

/* =====================================================================================
   5. ROLES SEMILLA (Identity)
   Los USUARIOS (con su hash de contraseña) se siembran desde la aplicación C#, no aquí,
   porque el hash de Identity requiere el PasswordHasher del framework.
   ===================================================================================== */

INSERT INTO dbo.AspNetRoles (Name, NormalizedName, ConcurrencyStamp) VALUES
(N'Administrador', N'ADMINISTRADOR', NEWID()),
(N'Usuario',       N'USUARIO',       NEWID());
GO

PRINT 'Esquema DGA creado correctamente.';
GO
