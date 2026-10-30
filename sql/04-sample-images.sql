-- Jumbus: one sample image for each of the 22 seeded products.
--
-- Matched by SKU, so it works whatever the product IDs are in your database.
-- The files are wwwroot/media/products/sample/{sku}.jpg (dr-001.jpg, ...).
--
-- Run the whole script once in MySQL Workbench. It is safe to run again and
-- never touches photos uploaded in the admin.

SET NAMES utf8mb4;
USE `jumbus`;

-- Workbench refuses DELETE and UPDATE without a key column by default.
SET @jumbus_safe_updates = @@SQL_SAFE_UPDATES;
SET SQL_SAFE_UPDATES = 0;

-- 1. Rows this script replaces: the seed placeholders (media/products/x.jpg,
--    whose files never existed) and the first sample script
--    (media/products/{id}/01.jpg). Uploaded photos have random 32-character
--    names, so they never match.
DELETE FROM `ProductImages`
WHERE `Path` LIKE 'media/products/%'
  AND (`Path` NOT LIKE 'media/products/%/%' OR `Path` LIKE 'media/products/%/01.jpg');

-- 2. A product whose main photo was one of those rows gets its first
--    remaining photo as the main one, as the admin does after a delete.
UPDATE `ProductImages` i
JOIN (
    SELECT x.`ProductId`, MIN(x.`Id`) AS `Id`
    FROM `ProductImages` x
    JOIN (
        SELECT `ProductId`, MIN(`DisplayOrder`) AS `DisplayOrder`
        FROM `ProductImages`
        GROUP BY `ProductId`
        HAVING MAX(`IsPrimary`) = 0
    ) f ON f.`ProductId` = x.`ProductId` AND f.`DisplayOrder` = x.`DisplayOrder`
    GROUP BY x.`ProductId`
) pick ON pick.`Id` = i.`Id`
SET i.`IsPrimary` = 1;

-- 3. The sample image, only for products that have no photo at all.
INSERT INTO `ProductImages` (`ProductId`, `Path`, `AltTextSr`, `AltTextEn`, `DisplayOrder`, `IsPrimary`)
SELECT p.`Id`, CONCAT('media/products/sample/', LOWER(p.`Sku`), '.jpg'), sr.`Name`, en.`Name`, 0, 1
FROM `Products` p
LEFT JOIN `ProductTranslations` sr ON sr.`ProductId` = p.`Id` AND sr.`Culture` = 'sr'
LEFT JOIN `ProductTranslations` en ON en.`ProductId` = p.`Id` AND en.`Culture` = 'en'
WHERE p.`Sku` IN ('DR-001', 'DR-002', 'DR-003', 'DR-004',
                  'TP-001', 'TP-002', 'TP-003', 'TP-004',
                  'OW-001', 'OW-002', 'OW-003',
                  'SC-001', 'SC-002', 'SC-003', 'SC-004', 'SC-005',
                  'MU-001', 'MU-002', 'MU-003', 'MU-004',
                  'FR-001', 'FR-002')
  AND NOT EXISTS (SELECT 1 FROM `ProductImages` i WHERE i.`ProductId` = p.`Id`);

SET SQL_SAFE_UPDATES = @jumbus_safe_updates;
