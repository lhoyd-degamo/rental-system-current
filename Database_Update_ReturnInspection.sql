-- Capstone Rental System - Return Inspection Database Update
-- Use this only if you do not want to use EF Core migrations.
-- Select the same database used by the DefaultConnection in appsettings.json.

IF COL_LENGTH('Borrows', 'ActualReturnDate') IS NULL
    ALTER TABLE Borrows ADD ActualReturnDate datetime2 NULL;

IF COL_LENGTH('Borrows', 'ItemCondition') IS NULL
    ALTER TABLE Borrows ADD ItemCondition nvarchar(max) NOT NULL CONSTRAINT DF_Borrows_ItemCondition DEFAULT '';

IF COL_LENGTH('Borrows', 'DamageDescription') IS NULL
    ALTER TABLE Borrows ADD DamageDescription nvarchar(max) NOT NULL CONSTRAINT DF_Borrows_DamageDescription DEFAULT '';

IF COL_LENGTH('Borrows', 'DamagePenalty') IS NULL
    ALTER TABLE Borrows ADD DamagePenalty decimal(18,2) NOT NULL CONSTRAINT DF_Borrows_DamagePenalty DEFAULT 0;

IF COL_LENGTH('Borrows', 'PenaltyAmount') IS NULL
    ALTER TABLE Borrows ADD PenaltyAmount decimal(18,2) NOT NULL CONSTRAINT DF_Borrows_PenaltyAmount DEFAULT 0;
