-- Jumbus, phase 6: old addresses of renamed categories and products.
--
-- When the admin changes a slug, the old one is kept here and the shop
-- answers it with a permanent redirect to the new address. Run once in
-- MySQL Workbench; running it again changes nothing.

SET NAMES utf8mb4;
USE `jumbus`;

CREATE TABLE IF NOT EXISTS `SlugRedirects` (
    `Id`         int          NOT NULL AUTO_INCREMENT,
    `OwnerType`  int          NOT NULL,   -- 1 = category, 2 = product
    `OwnerId`    int          NOT NULL,
    `Culture`    varchar(8)   NOT NULL,
    `Slug`       varchar(250) NOT NULL,
    `CreatedUtc` datetime(6)  NOT NULL,
    CONSTRAINT `PK_SlugRedirects` PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_SlugRedirects_OwnerType_Culture_Slug` (`OwnerType`, `Culture`, `Slug`),
    KEY `IX_SlugRedirects_OwnerType_OwnerId` (`OwnerType`, `OwnerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
