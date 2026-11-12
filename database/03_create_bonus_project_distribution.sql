/* =====================================================================
   Script 3 (punto extra 1): detalle del pago de un bono repartido por proyecto.
   Requiere 02_create_bonus.sql. Se puede ejecutar más de una vez.
   ===================================================================== */

USE BonusTest;
GO

IF OBJECT_ID('dbo.BonusProjectDistribution', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BonusProjectDistribution (
        BonusProjectDistributionId INT IDENTITY(1,1) NOT NULL,
        BonusId                    INT            NOT NULL,
        EmployeeProjectHoursId     INT            NOT NULL,
        -- Copia del proyecto y sus horas al momento de pagar.
        ProjectName                VARCHAR(100)   NOT NULL,
        Hours                      DECIMAL(8,2)   NOT NULL,
        Amount                     DECIMAL(16,2)  NOT NULL,
        CreatedBy                  VARCHAR(50)    NOT NULL,
        CreatedAt                  DATETIME2(0)   NOT NULL
            CONSTRAINT DF_BonusProjectDistribution_CreatedAt DEFAULT (SYSDATETIME()),

        CONSTRAINT PK_BonusProjectDistribution PRIMARY KEY (BonusProjectDistributionId),
        CONSTRAINT FK_BonusProjectDistribution_Bonus FOREIGN KEY (BonusId) REFERENCES dbo.Bonus (BonusId),
        CONSTRAINT FK_BonusProjectDistribution_EmployeeProjectHours FOREIGN KEY (EmployeeProjectHoursId)
            REFERENCES dbo.EmployeeProjectHours (EmployeeProjectHoursId),
        CONSTRAINT UQ_BonusProjectDistribution_Bonus_Project UNIQUE (BonusId, EmployeeProjectHoursId),
        CONSTRAINT CK_BonusProjectDistribution_Amount CHECK (Amount >= 0)
    );
END
GO
