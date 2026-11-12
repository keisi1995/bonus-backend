/* =====================================================================
   Script 2: tabla Bonus (bonos de desempeño).
   Requiere 01_schema_seed.sql. Se puede ejecutar más de una vez.
   ===================================================================== */

USE BonusTest;
GO

-- Los índices filtrados exigen estas opciones en la sesión que los crea.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.Bonus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bonus (
        BonusId          INT IDENTITY(1,1) NOT NULL,
        DepartmentCode   CHAR(5)        NOT NULL,
        EmployeeNumber   INT            NOT NULL,
        Period           CHAR(7)        NOT NULL,  -- YYYY-MM
        BonusTypeId      INT            NOT NULL,
        BonusValue       DECIMAL(16,2)  NOT NULL,  -- monto fijo o porcentaje, según BonusTypeId
        ReasonCode       CHAR(6)        NOT NULL,
        -- Copia de los datos del empleado al registrar: el cálculo queda reproducible
        -- aunque después cambien su sueldo o su % de retención.
        MonthlySalary    DECIMAL(16,2)  NOT NULL,
        TaxRate          DECIMAL(5,2)   NOT NULL,
        GrossAmount      DECIMAL(16,2)  NOT NULL,
        TaxAmount        DECIMAL(16,2)  NOT NULL,
        NetAmount        DECIMAL(16,2)  NOT NULL,
        BonusStateCode   CHAR(4)        NOT NULL CONSTRAINT DF_Bonus_BonusStateCode DEFAULT ('0201'),
        IsDeleted        BIT            NOT NULL CONSTRAINT DF_Bonus_IsDeleted DEFAULT (0),
        CreatedBy        VARCHAR(50)    NOT NULL,
        CreatedAt        DATETIME2(0)   NOT NULL CONSTRAINT DF_Bonus_CreatedAt DEFAULT (SYSDATETIME()),
        ModifiedBy       VARCHAR(50)    NULL,
        ModifiedAt       DATETIME2(0)   NULL,

        CONSTRAINT PK_Bonus PRIMARY KEY (BonusId),
        CONSTRAINT FK_Bonus_Employee FOREIGN KEY (DepartmentCode, EmployeeNumber)
            REFERENCES dbo.Employee (DepartmentCode, EmployeeNumber),
        CONSTRAINT FK_Bonus_BonusType FOREIGN KEY (BonusTypeId) REFERENCES dbo.BonusType (BonusTypeId),
        CONSTRAINT FK_Bonus_BonusReason FOREIGN KEY (ReasonCode) REFERENCES dbo.BonusReason (ReasonCode),
        CONSTRAINT FK_Bonus_State FOREIGN KEY (BonusStateCode) REFERENCES dbo.State (StateCode),

        CONSTRAINT CK_Bonus_Period CHECK (
            Period LIKE '[0-9][0-9][0-9][0-9]-[0-9][0-9]'
            AND SUBSTRING(Period, 6, 2) BETWEEN '01' AND '12'),
        CONSTRAINT CK_Bonus_BonusValue CHECK (BonusValue > 0 AND (BonusTypeId <> 2 OR BonusValue <= 100)),
        CONSTRAINT CK_Bonus_Amounts CHECK (
            GrossAmount > 0 AND TaxAmount >= 0 AND NetAmount = GrossAmount - TaxAmount)
    );
END
GO

-- Regla "un solo bono activo por empleado y periodo" (activo = no anulado y no eliminado).
-- Es la última barrera si dos usuarios registran a la vez; el servicio valida antes con un mensaje claro.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Bonus_Employee_Period_Active')
    CREATE UNIQUE INDEX UX_Bonus_Employee_Period_Active
        ON dbo.Bonus (DepartmentCode, EmployeeNumber, Period)
        WHERE IsDeleted = 0 AND BonusStateCode <> '0203';
GO

-- Soporta la FK compuesta hacia Employee y el filtro del listado por área + número de empleado.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bonus_Employee_Period')
    CREATE INDEX IX_Bonus_Employee_Period
        ON dbo.Bonus (DepartmentCode, EmployeeNumber, Period);
GO

-- Filtros del listado por periodo y estado; solo indexa lo que el listado muestra (no eliminados).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bonus_Period_State')
    CREATE INDEX IX_Bonus_Period_State
        ON dbo.Bonus (Period, BonusStateCode)
        WHERE IsDeleted = 0;
GO
