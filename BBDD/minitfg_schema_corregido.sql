-- =====================================================================
--  MiniTFG / LetMeCook - Base de datos MySQL para el último proyecto
--  Compatible con MySQL 8+ y Aiven.
--
--  IMPORTANTE:
--    Este script RECREA las tablas. Si ya hay datos reales, haz copia
--    antes de ejecutarlo porque hace DROP TABLE.
--
--  Cómo guarda datos la app:
--    - Usuarios.Foto, Usuarios.Banner y Skins.Imagen guardan nombres de
--      archivos locales incluidos en Resources/Images de la app.
--      Ejemplos: user.png, opbanner.jpg, sanji.jpg.
--    - Recetas.Imagen guarda la imagen subida por el usuario en Base64.
--    - PasosReceta.Video guarda el vídeo del paso en Base64.
-- =====================================================================

-- ---------------------------------------------------------------------
-- Usuarios
-- Guarda los datos de login, foto/banner activos y preferencias.
-- Foto y Banner son nombres de archivo local de Resources/Images.
-- ---------------------------------------------------------------------
CREATE TABLE Usuarios (
    Id                  INT             NOT NULL AUTO_INCREMENT,
    Nombre              VARCHAR(100)    NULL,
    Foto                VARCHAR(255)    NOT NULL DEFAULT 'user.png',
    Banner              VARCHAR(255)    NOT NULL DEFAULT 'opbanner.jpg',
    Correo              VARCHAR(150)    NOT NULL,
    Contrasena          VARCHAR(255)    NOT NULL,

    Gluten              TINYINT(1)      NOT NULL DEFAULT 0,
    Lactosa             TINYINT(1)      NOT NULL DEFAULT 0,
    Huevo               TINYINT(1)      NOT NULL DEFAULT 0,
    FrutosSecos         TINYINT(1)      NOT NULL DEFAULT 0,
    Marisco             TINYINT(1)      NOT NULL DEFAULT 0,
    Soja                TINYINT(1)      NOT NULL DEFAULT 0,
    Pescado             TINYINT(1)      NOT NULL DEFAULT 0,
    Cacahuetes          TINYINT(1)      NOT NULL DEFAULT 0,
    Sesamo              TINYINT(1)      NOT NULL DEFAULT 0,
    Sulfitos            TINYINT(1)      NOT NULL DEFAULT 0,
    Mostaza             TINYINT(1)      NOT NULL DEFAULT 0,
    Altramuces          TINYINT(1)      NOT NULL DEFAULT 0,
    Moluscos            TINYINT(1)      NOT NULL DEFAULT 0,
    Apio                TINYINT(1)      NOT NULL DEFAULT 0,

    Vegano              TINYINT(1)      NOT NULL DEFAULT 0,
    Vegetariano         TINYINT(1)      NOT NULL DEFAULT 0,

    -- Campos calculados por triggers sobre Valoraciones.
    ValoracionMedia     DOUBLE          NOT NULL DEFAULT 0,
    NumeroValoraciones  INT             NOT NULL DEFAULT 0,

    -- Puntos gastables en tienda.
    -- +10 por like recibido, +1 por bloques opcionales, +2 por descripciones de pasos y +3 por vídeo.
    Puntos              INT             NOT NULL DEFAULT 0,

    PRIMARY KEY (Id),
    UNIQUE KEY uq_usuarios_correo (Correo),
    INDEX ix_usuarios_nombre (Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------
-- Skins
-- Tienda de fotos y banners. Imagen es un archivo local de Resources/Images.
-- ---------------------------------------------------------------------
CREATE TABLE Skins (
    Id      INT             NOT NULL AUTO_INCREMENT,
    Nombre  VARCHAR(100)    NOT NULL,
    Imagen  VARCHAR(255)    NOT NULL,
    Precio  INT             NOT NULL DEFAULT 0,
    Activo  TINYINT(1)      NOT NULL DEFAULT 1,

    PRIMARY KEY (Id),
    UNIQUE KEY uq_skins_nombre (Nombre),
    INDEX ix_skins_activo_precio (Activo, Precio, Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------
-- UserSkins
-- Relación N:M entre usuarios y skins compradas.
-- ---------------------------------------------------------------------
CREATE TABLE UserSkins (
    UsuarioId   INT NOT NULL,
    SkinId      INT NOT NULL,

    PRIMARY KEY (UsuarioId, SkinId),
    CONSTRAINT fk_userskins_usuario
        FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_userskins_skin
        FOREIGN KEY (SkinId) REFERENCES Skins(Id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------
-- Recetas
-- Imagen se guarda en Base64 porque la sube el usuario desde el móvil/PC.
-- ---------------------------------------------------------------------
CREATE TABLE Recetas (
    Id                   INT             NOT NULL AUTO_INCREMENT,
    UsuarioId            INT             NOT NULL,
    Titulo               VARCHAR(200)    NULL,
    Descripcion          TEXT            NULL,
    Imagen               LONGTEXT        NULL,
    Comensales           INT             NOT NULL DEFAULT 1,
    OrigenDelPlato       VARCHAR(100)    NULL,
    TiempoPreparacion    VARCHAR(50)     NULL,
    TipoCocina           VARCHAR(100)    NULL,
    IngredientePrincipal VARCHAR(150)    NULL,

    Gluten               TINYINT(1)      NOT NULL DEFAULT 0,
    Lactosa              TINYINT(1)      NOT NULL DEFAULT 0,
    Huevo                TINYINT(1)      NOT NULL DEFAULT 0,
    FrutosSecos          TINYINT(1)      NOT NULL DEFAULT 0,
    Mariscos             TINYINT(1)      NOT NULL DEFAULT 0,
    Soja                 TINYINT(1)      NOT NULL DEFAULT 0,
    Pescado              TINYINT(1)      NOT NULL DEFAULT 0,
    Cacahuetes           TINYINT(1)      NOT NULL DEFAULT 0,
    Sesamo               TINYINT(1)      NOT NULL DEFAULT 0,
    Sulfitos             TINYINT(1)      NOT NULL DEFAULT 0,
    Mostaza              TINYINT(1)      NOT NULL DEFAULT 0,
    Altramuces           TINYINT(1)      NOT NULL DEFAULT 0,
    Moluscos             TINYINT(1)      NOT NULL DEFAULT 0,
    Apio                 TINYINT(1)      NOT NULL DEFAULT 0,
    Vegano               TINYINT(1)      NOT NULL DEFAULT 0,
    Vegetariano          TINYINT(1)      NOT NULL DEFAULT 0,

    PRIMARY KEY (Id),
    INDEX ix_recetas_usuario (UsuarioId),
    INDEX ix_recetas_titulo (Titulo),
    INDEX ix_recetas_filtros (Vegano, Vegetariano, Gluten, Lactosa),
    CONSTRAINT fk_recetas_usuario
        FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT chk_recetas_comensales
        CHECK (Comensales >= 1)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------
-- Catálogos de búsqueda por etiquetas
-- TiposCocina, OrigenesPlato e Ingredientes alimentan los buscadores
-- con lógica contains. Las tablas N:M permiten que una receta tenga varias
-- etiquetas por categoría, aunque también se mantiene el texto resumen
-- en Recetas.TipoCocina, Recetas.OrigenDelPlato e Recetas.IngredientePrincipal.
-- ---------------------------------------------------------------------
CREATE TABLE TiposCocina (
    Id      INT             NOT NULL AUTO_INCREMENT,
    Nombre  VARCHAR(100)    NOT NULL,

    PRIMARY KEY (Id),
    UNIQUE KEY uq_tipos_cocina_nombre (Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE OrigenesPlato (
    Id      INT             NOT NULL AUTO_INCREMENT,
    Nombre  VARCHAR(100)    NOT NULL,

    PRIMARY KEY (Id),
    UNIQUE KEY uq_origenes_plato_nombre (Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE Ingredientes (
    Id      INT             NOT NULL AUTO_INCREMENT,
    Nombre  VARCHAR(150)    NOT NULL,

    PRIMARY KEY (Id),
    UNIQUE KEY uq_ingredientes_nombre (Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE RecetaTiposCocina (
    RecetaId      INT NOT NULL,
    TipoCocinaId  INT NOT NULL,

    PRIMARY KEY (RecetaId, TipoCocinaId),
    CONSTRAINT fk_receta_tipos_receta
        FOREIGN KEY (RecetaId) REFERENCES Recetas(Id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_receta_tipos_tipo
        FOREIGN KEY (TipoCocinaId) REFERENCES TiposCocina(Id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE RecetaOrigenes (
    RecetaId   INT NOT NULL,
    OrigenId   INT NOT NULL,

    PRIMARY KEY (RecetaId, OrigenId),
    CONSTRAINT fk_receta_origenes_receta
        FOREIGN KEY (RecetaId) REFERENCES Recetas(Id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_receta_origenes_origen
        FOREIGN KEY (OrigenId) REFERENCES OrigenesPlato(Id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE RecetaIngredientes (
    RecetaId       INT NOT NULL,
    IngredienteId  INT NOT NULL,

    PRIMARY KEY (RecetaId, IngredienteId),
    CONSTRAINT fk_receta_ingredientes_receta
        FOREIGN KEY (RecetaId) REFERENCES Recetas(Id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_receta_ingredientes_ingrediente
        FOREIGN KEY (IngredienteId) REFERENCES Ingredientes(Id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------
-- PasosReceta
-- Cada receta puede tener varios pasos. Video es Base64 opcional.
-- ---------------------------------------------------------------------
CREATE TABLE PasosReceta (
    Id          INT         NOT NULL AUTO_INCREMENT,
    RecetaId    INT         NOT NULL,
    NumeroPaso  INT         NOT NULL,
    Descripcion TEXT        NULL,
    Video       LONGTEXT    NULL,

    PRIMARY KEY (Id),
    UNIQUE KEY uq_pasos_receta_numero (RecetaId, NumeroPaso),
    INDEX ix_pasos_receta (RecetaId, NumeroPaso),
    CONSTRAINT fk_pasos_receta
        FOREIGN KEY (RecetaId) REFERENCES Recetas(Id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT chk_pasos_numero
        CHECK (NumeroPaso >= 1)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------
-- Likes
-- Un usuario solo puede dar un like a la misma receta.
-- ---------------------------------------------------------------------
CREATE TABLE Likes (
    Id          INT NOT NULL AUTO_INCREMENT,
    UsuarioId   INT NOT NULL,
    RecetaId    INT NOT NULL,

    PRIMARY KEY (Id),
    UNIQUE KEY uq_likes_usuario_receta (UsuarioId, RecetaId),
    INDEX ix_likes_receta (RecetaId),
    INDEX ix_likes_usuario (UsuarioId),
    CONSTRAINT fk_likes_usuario
        FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_likes_receta
        FOREIGN KEY (RecetaId) REFERENCES Recetas(Id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------
-- Valoraciones
-- Valoración de un usuario a otro usuario creador.
-- La clave primaria permite actualizar la puntuación si ya existía.
-- La regla de no valorarse a uno mismo se aplica con triggers BEFORE,
-- porque MySQL no permite ese CHECK sobre columnas usadas por FKs con CASCADE.
-- ---------------------------------------------------------------------
CREATE TABLE Valoraciones (
    UsuarioValoradoId   INT         NOT NULL,
    UsuarioQueValoraId  INT         NOT NULL,
    Puntuacion          TINYINT     NOT NULL,

    PRIMARY KEY (UsuarioValoradoId, UsuarioQueValoraId),
    INDEX ix_valoraciones_valora (UsuarioQueValoraId),
    CONSTRAINT fk_valoraciones_valorado
        FOREIGN KEY (UsuarioValoradoId) REFERENCES Usuarios(Id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_valoraciones_valora
        FOREIGN KEY (UsuarioQueValoraId) REFERENCES Usuarios(Id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT chk_valoraciones_puntuacion
        CHECK (Puntuacion BETWEEN 1 AND 5)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------
-- Datos iniciales de catálogos
-- ---------------------------------------------------------------------
INSERT INTO TiposCocina (Nombre) VALUES
('Vitro'), ('Horno'), ('Microondas'), ('Fuego'), ('Airfryer'), ('Parrilla'), ('Vapor'), ('Olla');

INSERT INTO OrigenesPlato (Nombre) VALUES
('España'), ('Italia'), ('Francia'), ('México'), ('Japón'), ('China'), ('India'), ('Estados Unidos'), ('Marruecos');

INSERT INTO Ingredientes (Nombre) VALUES
('Pollo'), ('Ternera'), ('Cerdo'), ('Pescado'), ('Huevo'), ('Arroz'), ('Pasta'), ('Patata'), ('Tomate'), ('Manzana'), ('Queso'), ('Lechuga');

-- ---------------------------------------------------------------------
-- Datos iniciales de la tienda
-- Los nombres de Imagen deben existir en Resources/Images.
-- ---------------------------------------------------------------------
INSERT INTO Skins (Nombre, Imagen, Precio, Activo) VALUES
('Foto Arguiñano',            'arguinano.jpg',    5,  1),
('Foto Chicote',              'chicote.jpg',      5,  1),
('Foto Heisenberg',           'heisenberg.jpg',   8,  1),
('Foto House',                'house.jpg',        8,  1),
('Foto Sanji',                'sanji.jpg',        10, 1),
('Banner Breaking Bad',       'brbabanner.jpg',   10, 1),
('Banner Better Call Saul',   'bcsbanner.jpg',    10, 1),
('Banner Dragon Ball',        'dbbanner.jpg',     10, 1),
('Banner One Piece',          'opbanner.jpg',     10, 1),
('Banner Peaky Blinders',     'peakybanner.jpg',  10, 1);

-- ---------------------------------------------------------------------
-- Triggers de valoraciones
-- Mantienen Usuarios.ValoracionMedia y Usuarios.NumeroValoraciones.
-- ---------------------------------------------------------------------
DELIMITER $$

CREATE TRIGGER trg_valoracion_prevent_self_insert
BEFORE INSERT ON Valoraciones
FOR EACH ROW
BEGIN
    IF NEW.UsuarioValoradoId = NEW.UsuarioQueValoraId THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Un usuario no puede valorarse a si mismo';
    END IF;
END$$

CREATE TRIGGER trg_valoracion_prevent_self_update
BEFORE UPDATE ON Valoraciones
FOR EACH ROW
BEGIN
    IF NEW.UsuarioValoradoId = NEW.UsuarioQueValoraId THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Un usuario no puede valorarse a si mismo';
    END IF;
END$$

CREATE TRIGGER trg_valoracion_insert
AFTER INSERT ON Valoraciones
FOR EACH ROW
BEGIN
    UPDATE Usuarios
    SET NumeroValoraciones = (
            SELECT COUNT(*)
            FROM Valoraciones
            WHERE UsuarioValoradoId = NEW.UsuarioValoradoId
        ),
        ValoracionMedia = IFNULL((
            SELECT AVG(Puntuacion)
            FROM Valoraciones
            WHERE UsuarioValoradoId = NEW.UsuarioValoradoId
        ), 0)
    WHERE Id = NEW.UsuarioValoradoId;
END$$

CREATE TRIGGER trg_valoracion_update
AFTER UPDATE ON Valoraciones
FOR EACH ROW
BEGIN
    UPDATE Usuarios
    SET NumeroValoraciones = (
            SELECT COUNT(*)
            FROM Valoraciones
            WHERE UsuarioValoradoId = NEW.UsuarioValoradoId
        ),
        ValoracionMedia = IFNULL((
            SELECT AVG(Puntuacion)
            FROM Valoraciones
            WHERE UsuarioValoradoId = NEW.UsuarioValoradoId
        ), 0)
    WHERE Id = NEW.UsuarioValoradoId;
END$$

CREATE TRIGGER trg_valoracion_delete
AFTER DELETE ON Valoraciones
FOR EACH ROW
BEGIN
    UPDATE Usuarios
    SET NumeroValoraciones = (
            SELECT COUNT(*)
            FROM Valoraciones
            WHERE UsuarioValoradoId = OLD.UsuarioValoradoId
        ),
        ValoracionMedia = IFNULL((
            SELECT AVG(Puntuacion)
            FROM Valoraciones
            WHERE UsuarioValoradoId = OLD.UsuarioValoradoId
        ), 0)
    WHERE Id = OLD.UsuarioValoradoId;
END$$

DELIMITER ;


UPDATE Usuarios u
SET NumeroValoraciones = (
        SELECT COUNT(*)
        FROM Valoraciones v
        WHERE v.UsuarioValoradoId = u.Id
    ),
    ValoracionMedia = IFNULL((
        SELECT AVG(v.Puntuacion)
        FROM Valoraciones v
        WHERE v.UsuarioValoradoId = u.Id
    ), 0);

-- ---------------------------------------------------------------------
-- Nota para vídeos/imágenes grandes:
-- Si al subir vídeos da error de paquete demasiado grande, subir el
-- parámetro max_allowed_packet en Aiven desde la configuración del servicio.
-- Valor recomendado 64 MB o 128 MB.
-- ---------------------------------------------------------------------
