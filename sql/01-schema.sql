-- =====================================================================
--  Jumbus — MySQL schema
--  Mirrors the EF Core model in Models/ and Data/Configurations/.
--
--  Generated. Re-run 00 to start clean; the script drops and recreates
--  the whole database.
-- =====================================================================

-- Force the connection encoding. Without this a client whose default is
-- latin1 reinterprets these UTF-8 bytes and silently double-encodes every
-- Serbian character on insert (čišćenje becomes ÄiÅ¡Äenje).
SET NAMES utf8mb4;

DROP DATABASE IF EXISTS `jumbus`;
CREATE DATABASE `jumbus`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;
USE `jumbus`;

-- ---------------------------------------------------------------------
--  Identity
--  Column types match what Pomelo generates for ASP.NET Core Identity:
--  string keys as varchar(255), stamps as longtext, dates as datetime(6).
-- ---------------------------------------------------------------------

CREATE TABLE `Roles` (
    `Id`               varchar(255) NOT NULL,
    `Name`             varchar(256) NULL,
    `NormalizedName`   varchar(256) NULL,
    `ConcurrencyStamp` longtext NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `RoleNameIndex` (`NormalizedName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `Users` (
    `Id`                   varchar(255) NOT NULL,
    `FirstName`            longtext NULL,
    `LastName`             longtext NULL,
    `RegisteredUtc`        datetime(6) NOT NULL,
    `PreferredCulture`     longtext NOT NULL,
    `AcceptsMarketing`     tinyint(1) NOT NULL,
    `UserName`             varchar(256) NULL,
    `NormalizedUserName`   varchar(256) NULL,
    `Email`                varchar(256) NULL,
    `NormalizedEmail`      varchar(256) NULL,
    `EmailConfirmed`       tinyint(1) NOT NULL,
    `PasswordHash`         longtext NULL,
    `SecurityStamp`        longtext NULL,
    `ConcurrencyStamp`     longtext NULL,
    `PhoneNumber`          longtext NULL,
    `PhoneNumberConfirmed` tinyint(1) NOT NULL,
    `TwoFactorEnabled`     tinyint(1) NOT NULL,
    `LockoutEnd`           datetime(6) NULL,
    `LockoutEnabled`       tinyint(1) NOT NULL,
    `AccessFailedCount`    int NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UserNameIndex` (`NormalizedUserName`),
    KEY `EmailIndex` (`NormalizedEmail`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `RoleClaims` (
    `Id`         int NOT NULL AUTO_INCREMENT,
    `RoleId`     varchar(255) NOT NULL,
    `ClaimType`  longtext NULL,
    `ClaimValue` longtext NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_RoleClaims_RoleId` (`RoleId`),
    CONSTRAINT `FK_RoleClaims_Roles` FOREIGN KEY (`RoleId`)
        REFERENCES `Roles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `UserClaims` (
    `Id`         int NOT NULL AUTO_INCREMENT,
    `UserId`     varchar(255) NOT NULL,
    `ClaimType`  longtext NULL,
    `ClaimValue` longtext NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_UserClaims_UserId` (`UserId`),
    CONSTRAINT `FK_UserClaims_Users` FOREIGN KEY (`UserId`)
        REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `UserLogins` (
    `LoginProvider`       varchar(255) NOT NULL,
    `ProviderKey`         varchar(255) NOT NULL,
    `ProviderDisplayName` longtext NULL,
    `UserId`              varchar(255) NOT NULL,
    PRIMARY KEY (`LoginProvider`, `ProviderKey`),
    KEY `IX_UserLogins_UserId` (`UserId`),
    CONSTRAINT `FK_UserLogins_Users` FOREIGN KEY (`UserId`)
        REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `UserRoles` (
    `UserId` varchar(255) NOT NULL,
    `RoleId` varchar(255) NOT NULL,
    PRIMARY KEY (`UserId`, `RoleId`),
    KEY `IX_UserRoles_RoleId` (`RoleId`),
    CONSTRAINT `FK_UserRoles_Users` FOREIGN KEY (`UserId`)
        REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_UserRoles_Roles` FOREIGN KEY (`RoleId`)
        REFERENCES `Roles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `UserTokens` (
    `UserId`        varchar(255) NOT NULL,
    `LoginProvider` varchar(255) NOT NULL,
    `Name`          varchar(255) NOT NULL,
    `Value`         longtext NULL,
    PRIMARY KEY (`UserId`, `LoginProvider`, `Name`),
    CONSTRAINT `FK_UserTokens_Users` FOREIGN KEY (`UserId`)
        REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------
--  Catalog
-- ---------------------------------------------------------------------

CREATE TABLE `Categories` (
    `Id`           int NOT NULL AUTO_INCREMENT,
    `ParentId`     int NULL,
    `Code`         varchar(64) NOT NULL,
    `DisplayOrder` int NOT NULL,
    `IsActive`     tinyint(1) NOT NULL,
    `ImagePath`    longtext NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Categories_Code` (`Code`),
    KEY `IX_Categories_ParentId` (`ParentId`),
    CONSTRAINT `FK_Categories_Categories_ParentId` FOREIGN KEY (`ParentId`)
        REFERENCES `Categories` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `CategoryTranslations` (
    `Id`              int NOT NULL AUTO_INCREMENT,
    `CategoryId`      int NOT NULL,
    `Culture`         varchar(8) NOT NULL,
    `Name`            varchar(200) NOT NULL,
    `Slug`            varchar(200) NOT NULL,
    `Description`     longtext NULL,
    `MetaTitle`       varchar(200) NULL,
    `MetaDescription` varchar(400) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_CategoryTranslations_CategoryId_Culture` (`CategoryId`, `Culture`),
    UNIQUE KEY `IX_CategoryTranslations_Culture_Slug` (`Culture`, `Slug`),
    CONSTRAINT `FK_CategoryTranslations_Categories` FOREIGN KEY (`CategoryId`)
        REFERENCES `Categories` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `Brands` (
    `Id`       int NOT NULL AUTO_INCREMENT,
    `Name`     varchar(120) NOT NULL,
    `Slug`     varchar(120) NOT NULL,
    `LogoPath` longtext NULL,
    `IsActive` tinyint(1) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Brands_Slug` (`Slug`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `AttributeDefinitions` (
    `Id`                 int NOT NULL AUTO_INCREMENT,
    `Code`               varchar(64) NOT NULL,
    `DataType`           int NOT NULL,
    `IsVariantDefining`  tinyint(1) NOT NULL,
    `IsFilterable`       tinyint(1) NOT NULL,
    `Unit`               varchar(16) NULL,
    `DisplayOrder`       int NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_AttributeDefinitions_Code` (`Code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `AttributeDefinitionTranslations` (
    `Id`                    int NOT NULL AUTO_INCREMENT,
    `AttributeDefinitionId` int NOT NULL,
    `Culture`               varchar(8) NOT NULL,
    `Name`                  varchar(120) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_ADT_Definition_Culture` (`AttributeDefinitionId`, `Culture`),
    CONSTRAINT `FK_ADT_AttributeDefinitions` FOREIGN KEY (`AttributeDefinitionId`)
        REFERENCES `AttributeDefinitions` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `AttributeOptions` (
    `Id`                    int NOT NULL AUTO_INCREMENT,
    `AttributeDefinitionId` int NOT NULL,
    `Code`                  varchar(64) NOT NULL,
    `SwatchHex`             varchar(9) NULL,
    `DisplayOrder`          int NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_AttributeOptions_Definition_Code` (`AttributeDefinitionId`, `Code`),
    CONSTRAINT `FK_AttributeOptions_AttributeDefinitions` FOREIGN KEY (`AttributeDefinitionId`)
        REFERENCES `AttributeDefinitions` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `AttributeOptionTranslations` (
    `Id`                int NOT NULL AUTO_INCREMENT,
    `AttributeOptionId` int NOT NULL,
    `Culture`           varchar(8) NOT NULL,
    `Label`             varchar(120) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_AOT_Option_Culture` (`AttributeOptionId`, `Culture`),
    CONSTRAINT `FK_AOT_AttributeOptions` FOREIGN KEY (`AttributeOptionId`)
        REFERENCES `AttributeOptions` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `CategoryAttributes` (
    `CategoryId`            int NOT NULL,
    `AttributeDefinitionId` int NOT NULL,
    `IsRequired`            tinyint(1) NOT NULL,
    `DisplayOrder`          int NOT NULL,
    PRIMARY KEY (`CategoryId`, `AttributeDefinitionId`),
    KEY `IX_CategoryAttributes_AttributeDefinitionId` (`AttributeDefinitionId`),
    CONSTRAINT `FK_CategoryAttributes_Categories` FOREIGN KEY (`CategoryId`)
        REFERENCES `Categories` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_CategoryAttributes_AttributeDefinitions` FOREIGN KEY (`AttributeDefinitionId`)
        REFERENCES `AttributeDefinitions` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `Products` (
    `Id`           int NOT NULL AUTO_INCREMENT,
    `CategoryId`   int NOT NULL,
    `BrandId`      int NULL,
    `Sku`          varchar(64) NOT NULL,
    `Status`       int NOT NULL,
    `IsFeatured`   tinyint(1) NOT NULL,
    `CreatedUtc`   datetime(6) NOT NULL,
    `PublishedUtc` datetime(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Products_Sku` (`Sku`),
    KEY `IX_Products_CategoryId_Status` (`CategoryId`, `Status`),
    KEY `IX_Products_IsFeatured` (`IsFeatured`),
    KEY `IX_Products_BrandId` (`BrandId`),
    CONSTRAINT `FK_Products_Categories` FOREIGN KEY (`CategoryId`)
        REFERENCES `Categories` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Products_Brands` FOREIGN KEY (`BrandId`)
        REFERENCES `Brands` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `ProductTranslations` (
    `Id`                int NOT NULL AUTO_INCREMENT,
    `ProductId`         int NOT NULL,
    `Culture`           varchar(8) NOT NULL,
    `Name`              varchar(250) NOT NULL,
    `Slug`              varchar(250) NOT NULL,
    `ShortDescription`  varchar(500) NULL,
    `Description`       longtext NULL,
    `Ingredients`       longtext NULL,
    `CareInstructions`  longtext NULL,
    `MetaTitle`         varchar(200) NULL,
    `MetaDescription`   varchar(400) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_ProductTranslations_ProductId_Culture` (`ProductId`, `Culture`),
    UNIQUE KEY `IX_ProductTranslations_Culture_Slug` (`Culture`, `Slug`),
    CONSTRAINT `FK_ProductTranslations_Products` FOREIGN KEY (`ProductId`)
        REFERENCES `Products` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `ProductVariants` (
    `Id`                 int NOT NULL AUTO_INCREMENT,
    `ProductId`          int NOT NULL,
    `Sku`                varchar(80) NOT NULL,
    `Price`              decimal(18,2) NOT NULL,
    `SalePrice`          decimal(18,2) NULL,
    `SaleStartsUtc`      datetime(6) NULL,
    `SaleEndsUtc`        datetime(6) NULL,
    `StockQuantity`      int NOT NULL,
    `LowStockThreshold`  int NOT NULL,
    `ExpiryDate`         date NULL,
    `WeightGrams`        int NULL,
    `IsActive`           tinyint(1) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_ProductVariants_Sku` (`Sku`),
    KEY `IX_ProductVariants_ProductId_IsActive` (`ProductId`, `IsActive`),
    CONSTRAINT `FK_ProductVariants_Products` FOREIGN KEY (`ProductId`)
        REFERENCES `Products` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `ProductImages` (
    `Id`                int NOT NULL AUTO_INCREMENT,
    `ProductId`         int NOT NULL,
    `Path`              varchar(400) NOT NULL,
    `ThumbnailPath`     varchar(400) NULL,
    `AltTextSr`         varchar(250) NULL,
    `AltTextEn`         varchar(250) NULL,
    `DisplayOrder`      int NOT NULL,
    `IsPrimary`         tinyint(1) NOT NULL,
    `AttributeOptionId` int NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_ProductImages_ProductId` (`ProductId`),
    KEY `IX_ProductImages_AttributeOptionId` (`AttributeOptionId`),
    CONSTRAINT `FK_ProductImages_Products` FOREIGN KEY (`ProductId`)
        REFERENCES `Products` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ProductImages_AttributeOptions` FOREIGN KEY (`AttributeOptionId`)
        REFERENCES `AttributeOptions` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `VariantAttributeValues` (
    `Id`                    int NOT NULL AUTO_INCREMENT,
    `ProductVariantId`      int NOT NULL,
    `AttributeDefinitionId` int NOT NULL,
    `AttributeOptionId`     int NULL,
    `NumericValue`          decimal(18,2) NULL,
    `TextValue`             varchar(400) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_VAV_Variant_Definition` (`ProductVariantId`, `AttributeDefinitionId`),
    KEY `IX_VAV_Definition_Option` (`AttributeDefinitionId`, `AttributeOptionId`),
    KEY `IX_VAV_AttributeOptionId` (`AttributeOptionId`),
    CONSTRAINT `FK_VAV_ProductVariants` FOREIGN KEY (`ProductVariantId`)
        REFERENCES `ProductVariants` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_VAV_AttributeDefinitions` FOREIGN KEY (`AttributeDefinitionId`)
        REFERENCES `AttributeDefinitions` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_VAV_AttributeOptions` FOREIGN KEY (`AttributeOptionId`)
        REFERENCES `AttributeOptions` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------
--  Orders
-- ---------------------------------------------------------------------

CREATE TABLE `Coupons` (
    `Id`                     int NOT NULL AUTO_INCREMENT,
    `Code`                   varchar(40) NOT NULL,
    `DiscountType`           int NOT NULL,
    `Value`                  decimal(18,2) NOT NULL,
    `MinimumSubtotal`        decimal(18,2) NULL,
    `MaximumDiscount`        decimal(18,2) NULL,
    `StartsUtc`              datetime(6) NULL,
    `EndsUtc`                datetime(6) NULL,
    `UsageLimit`             int NULL,
    `TimesUsed`              int NOT NULL,
    `RestrictedToCategoryId` int NULL,
    `IsActive`               tinyint(1) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Coupons_Code` (`Code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `Addresses` (
    `Id`           int NOT NULL AUTO_INCREMENT,
    `UserId`       varchar(255) NOT NULL,
    `Type`         int NOT NULL,
    `FirstName`    varchar(100) NOT NULL,
    `LastName`     varchar(100) NOT NULL,
    `Street`       varchar(250) NOT NULL,
    `StreetNumber` varchar(40) NULL,
    `City`         varchar(120) NOT NULL,
    `PostalCode`   varchar(20) NOT NULL,
    `Country`      varchar(2) NOT NULL,
    `PhoneNumber`  varchar(40) NOT NULL,
    `IsDefault`    tinyint(1) NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Addresses_UserId_Type` (`UserId`, `Type`),
    CONSTRAINT `FK_Addresses_Users` FOREIGN KEY (`UserId`)
        REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `Orders` (
    `Id`                    int NOT NULL AUTO_INCREMENT,
    `OrderNumber`           varchar(32) NOT NULL,
    `UserId`                varchar(255) NULL,
    `Status`                int NOT NULL,
    `PlacedUtc`             datetime(6) NOT NULL,
    `ConfirmedUtc`          datetime(6) NULL,
    `ShippedUtc`            datetime(6) NULL,
    `DeliveredUtc`          datetime(6) NULL,
    `Culture`               varchar(8) NOT NULL,
    `Email`                 varchar(256) NOT NULL,
    `FirstName`             varchar(100) NOT NULL,
    `LastName`              varchar(100) NOT NULL,
    `PhoneNumber`           varchar(40) NOT NULL,
    `ShippingStreet`        varchar(250) NOT NULL,
    `ShippingStreetNumber`  varchar(40) NULL,
    `ShippingCity`          varchar(120) NOT NULL,
    `ShippingPostalCode`    varchar(20) NOT NULL,
    `ShippingCountry`       varchar(2) NOT NULL,
    `BillingStreet`         varchar(250) NULL,
    `BillingCity`           varchar(120) NULL,
    `BillingPostalCode`     varchar(20) NULL,
    `CompanyName`           varchar(250) NULL,
    `CompanyTaxNumber`      varchar(40) NULL,
    `Subtotal`              decimal(18,2) NOT NULL,
    `DiscountAmount`        decimal(18,2) NOT NULL,
    `ShippingFee`           decimal(18,2) NOT NULL,
    `VatAmount`             decimal(18,2) NOT NULL,
    `Total`                 decimal(18,2) NOT NULL,
    `VatRate`               decimal(5,4) NOT NULL,
    `Currency`              varchar(3) NOT NULL,
    `CouponId`              int NULL,
    `PaymentMethod`         int NOT NULL,
    `PaymentStatus`         int NOT NULL,
    `PaymentReference`      varchar(128) NULL,
    `CourierName`           varchar(120) NULL,
    `TrackingNumber`        varchar(120) NULL,
    `CustomerNote`          varchar(1000) NULL,
    `AdminNote`             varchar(2000) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Orders_OrderNumber` (`OrderNumber`),
    KEY `IX_Orders_Status_PlacedUtc` (`Status`, `PlacedUtc`),
    KEY `IX_Orders_UserId` (`UserId`),
    KEY `IX_Orders_CouponId` (`CouponId`),
    CONSTRAINT `FK_Orders_Users` FOREIGN KEY (`UserId`)
        REFERENCES `Users` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_Orders_Coupons` FOREIGN KEY (`CouponId`)
        REFERENCES `Coupons` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `OrderItems` (
    `Id`                 int NOT NULL AUTO_INCREMENT,
    `OrderId`            int NOT NULL,
    `ProductVariantId`   int NULL,
    `ProductName`        varchar(250) NOT NULL,
    `Sku`                varchar(80) NOT NULL,
    `VariantDescription` varchar(250) NULL,
    `ImagePath`          varchar(400) NULL,
    `UnitPrice`          decimal(18,2) NOT NULL,
    `Quantity`           int NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_OrderItems_OrderId` (`OrderId`),
    KEY `IX_OrderItems_ProductVariantId` (`ProductVariantId`),
    CONSTRAINT `FK_OrderItems_Orders` FOREIGN KEY (`OrderId`)
        REFERENCES `Orders` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_OrderItems_ProductVariants` FOREIGN KEY (`ProductVariantId`)
        REFERENCES `ProductVariants` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------
--  Community
-- ---------------------------------------------------------------------

CREATE TABLE `Reviews` (
    `Id`                  int NOT NULL AUTO_INCREMENT,
    `ProductId`           int NOT NULL,
    `UserId`              varchar(255) NOT NULL,
    `Rating`              int NOT NULL,
    `Title`               varchar(200) NULL,
    `Body`                varchar(4000) NULL,
    `IsApproved`          tinyint(1) NOT NULL,
    `IsVerifiedPurchase`  tinyint(1) NOT NULL,
    `CreatedUtc`          datetime(6) NOT NULL,
    `ApprovedUtc`         datetime(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Reviews_ProductId_UserId` (`ProductId`, `UserId`),
    KEY `IX_Reviews_ProductId_IsApproved` (`ProductId`, `IsApproved`),
    KEY `IX_Reviews_UserId` (`UserId`),
    CONSTRAINT `FK_Reviews_Products` FOREIGN KEY (`ProductId`)
        REFERENCES `Products` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Reviews_Users` FOREIGN KEY (`UserId`)
        REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `WishlistItems` (
    `Id`               int NOT NULL AUTO_INCREMENT,
    `UserId`           varchar(255) NOT NULL,
    `ProductId`        int NOT NULL,
    `ProductVariantId` int NULL,
    `AddedUtc`         datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Wishlist_User_Product_Variant` (`UserId`, `ProductId`, `ProductVariantId`),
    KEY `IX_WishlistItems_ProductId` (`ProductId`),
    KEY `IX_WishlistItems_ProductVariantId` (`ProductVariantId`),
    CONSTRAINT `FK_WishlistItems_Users` FOREIGN KEY (`UserId`)
        REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_WishlistItems_Products` FOREIGN KEY (`ProductId`)
        REFERENCES `Products` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_WishlistItems_ProductVariants` FOREIGN KEY (`ProductVariantId`)
        REFERENCES `ProductVariants` (`Id`) ON DELETE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------
--  Old slugs of renamed categories and products (phase 6).
--  Answered with a permanent redirect to the current address. No foreign
--  key: the owner is a category (1) or a product (2).
-- ---------------------------------------------------------------------

CREATE TABLE `SlugRedirects` (
    `Id`         int          NOT NULL AUTO_INCREMENT,
    `OwnerType`  int          NOT NULL,
    `OwnerId`    int          NOT NULL,
    `Culture`    varchar(8)   NOT NULL,
    `Slug`       varchar(250) NOT NULL,
    `CreatedUtc` datetime(6)  NOT NULL,
    CONSTRAINT `PK_SlugRedirects` PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_SlugRedirects_OwnerType_Culture_Slug` (`OwnerType`, `Culture`, `Slug`),
    KEY `IX_SlugRedirects_OwnerType_OwnerId` (`OwnerType`, `OwnerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------
--  EF Core migration history
--  Stops `dotnet ef database update` from trying to build this schema a
--  second time. The MigrationId must match the migration file name if you
--  later generate one; see the notes in UPUTSTVO-MYSQL.md.
-- ---------------------------------------------------------------------

CREATE TABLE `__EFMigrationsHistory` (
    `MigrationId`    varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
