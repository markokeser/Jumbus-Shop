SET NAMES utf8mb4;

-- Removes the placeholder image rows the original seed inserted
-- (media/products/dr-001-01.jpg and so on). Those files never existed, so
-- every product card showed a broken image. Photos uploaded through the
-- admin live one folder deeper (media/products/{id}/...) and are kept.
DELETE FROM `ProductImages`
WHERE `Path` LIKE 'media/products/%'
  AND `Path` NOT LIKE 'media/products/%/%';
