-- Financial Management Application Database Initialization
-- This script runs once when the database is first created

USE master;
GO

-- Create database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'AIcoding')
BEGIN
    CREATE DATABASE AIcoding;
END
GO

USE AIcoding;
GO

-- Users table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
BEGIN
    CREATE TABLE Users (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Username NVARCHAR(100) NOT NULL UNIQUE,
        Email NVARCHAR(255) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        Role NVARCHAR(50) NOT NULL DEFAULT 'user',
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        IsActive BIT DEFAULT 1
    );
END
GO

-- Accounts table (bank accounts, cash, credit cards)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Accounts' AND xtype='U')
BEGIN
    CREATE TABLE Accounts (
        Id INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Type NVARCHAR(50) NOT NULL,
        Currency NVARCHAR(10) NOT NULL DEFAULT 'ARS',
        Balance DECIMAL(18,2) DEFAULT 0,
        Color NVARCHAR(20),
        Icon NVARCHAR(50),
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (UserId) REFERENCES Users(Id)
    );
END
GO

-- Categories table (Rubros)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Categories' AND xtype='U')
BEGIN
    CREATE TABLE Categories (
        Id INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Color NVARCHAR(20),
        Icon NVARCHAR(50),
        ParentId INT NULL,
        IsActive BIT DEFAULT 1,
        FOREIGN KEY (UserId) REFERENCES Users(Id),
        FOREIGN KEY (ParentId) REFERENCES Categories(Id)
    );
END
GO

-- Transactions table (Movimientos - core table)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Transactions' AND xtype='U')
BEGIN
    CREATE TABLE Transactions (
        Id INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        AccountId INT NOT NULL,
        DestinationAccountId INT NULL,
        Type NVARCHAR(20) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Description NVARCHAR(500),
        Detail NVARCHAR(MAX),
        CategoryId INT NULL,
        Date DATETIME2 NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (UserId) REFERENCES Users(Id),
        FOREIGN KEY (AccountId) REFERENCES Accounts(Id),
        FOREIGN KEY (DestinationAccountId) REFERENCES Accounts(Id),
        FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
    );
END
GO

-- Exchange Rates table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ExchangeRates' AND xtype='U')
BEGIN
    CREATE TABLE ExchangeRates (
        Id INT PRIMARY KEY IDENTITY(1,1),
        FromCurrency NVARCHAR(10) NOT NULL,
        ToCurrency NVARCHAR(10) NOT NULL,
        Rate DECIMAL(18,4) NOT NULL,
        Date DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- ============================================================
-- SEED DATA
-- ============================================================

-- Seed default admin user (password: admin123, BCrypt hash placeholder)
IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, Email, PasswordHash, Role)
    VALUES ('admin', 'admin@cashflow.local', '$2a$11$K3GzSMdRqGRFOzRDma12SuGiVXB5PXRliYrOqnXXxy1A5SAibVZIO', 'admin');
END
GO

-- Seed sample accounts for admin user
IF NOT EXISTS (SELECT * FROM Accounts WHERE Name = 'CAJA' AND UserId = 1)
BEGIN
    INSERT INTO Accounts (UserId, Name, Type, Currency, Balance, Color, Icon)
    VALUES (1, 'CAJA', 'Efectivo', 'ARS', 125000.00, '#4CAF50', 'cash');

    INSERT INTO Accounts (UserId, Name, Type, Currency, Balance, Color, Icon)
    VALUES (1, 'Banco Galicia', 'Banco', 'ARS', 850000.50, '#F57C00', 'bank');

    INSERT INTO Accounts (UserId, Name, Type, Currency, Balance, Color, Icon)
    VALUES (1, 'VISA', 'Tarjeta', 'ARS', -45320.00, '#1565C0', 'credit-card');

    INSERT INTO Accounts (UserId, Name, Type, Currency, Balance, Color, Icon)
    VALUES (1, 'Cuenta USD', 'Banco', 'USD', 2500.00, '#2E7D32', 'dollar');
END
GO

-- Seed sample categories
IF NOT EXISTS (SELECT * FROM Categories WHERE Name = 'Viaticos' AND UserId = 1)
BEGIN
    INSERT INTO Categories (UserId, Name, Color, Icon, ParentId)
    VALUES (1, 'Viaticos', '#FF9800', 'car', NULL);

    INSERT INTO Categories (UserId, Name, Color, Icon, ParentId)
    VALUES (1, 'Super', '#4CAF50', 'shopping-cart', NULL);

    INSERT INTO Categories (UserId, Name, Color, Icon, ParentId)
    VALUES (1, 'Auto', '#F44336', 'directions-car', NULL);

    INSERT INTO Categories (UserId, Name, Color, Icon, ParentId)
    VALUES (1, 'Servicios', '#2196F3', 'build', NULL);

    INSERT INTO Categories (UserId, Name, Color, Icon, ParentId)
    VALUES (1, 'Educacion', '#9C27B0', 'school', NULL);

    INSERT INTO Categories (UserId, Name, Color, Icon, ParentId)
    VALUES (1, 'Extra', '#607D8B', 'star', NULL);

    INSERT INTO Categories (UserId, Name, Color, Icon, ParentId)
    VALUES (1, 'Cable/Internet', '#00BCD4', 'wifi', NULL);

    INSERT INTO Categories (UserId, Name, Color, Icon, ParentId)
    VALUES (1, 'Sueldo', '#8BC34A', 'attach-money', NULL);

    INSERT INTO Categories (UserId, Name, Color, Icon, ParentId)
    VALUES (1, 'Freelance', '#CDDC39', 'laptop', NULL);
END
GO

-- Seed sample transactions
-- We use variables to reference account/category IDs
DECLARE @cajaid INT = (SELECT Id FROM Accounts WHERE Name = 'CAJA' AND UserId = 1);
DECLARE @galiciaid INT = (SELECT Id FROM Accounts WHERE Name = 'Banco Galicia' AND UserId = 1);
DECLARE @visaid INT = (SELECT Id FROM Accounts WHERE Name = 'VISA' AND UserId = 1);
DECLARE @usdid INT = (SELECT Id FROM Accounts WHERE Name = 'Cuenta USD' AND UserId = 1);

DECLARE @viaticosid INT = (SELECT Id FROM Categories WHERE Name = 'Viaticos' AND UserId = 1);
DECLARE @superid INT = (SELECT Id FROM Categories WHERE Name = 'Super' AND UserId = 1);
DECLARE @autoid INT = (SELECT Id FROM Categories WHERE Name = 'Auto' AND UserId = 1);
DECLARE @serviciosid INT = (SELECT Id FROM Categories WHERE Name = 'Servicios' AND UserId = 1);
DECLARE @educacionid INT = (SELECT Id FROM Categories WHERE Name = 'Educacion' AND UserId = 1);
DECLARE @extraid INT = (SELECT Id FROM Categories WHERE Name = 'Extra' AND UserId = 1);
DECLARE @cableid INT = (SELECT Id FROM Categories WHERE Name = 'Cable/Internet' AND UserId = 1);
DECLARE @sueldoid INT = (SELECT Id FROM Categories WHERE Name = 'Sueldo' AND UserId = 1);
DECLARE @freelanceid INT = (SELECT Id FROM Categories WHERE Name = 'Freelance' AND UserId = 1);

IF NOT EXISTS (SELECT * FROM Transactions WHERE UserId = 1)
BEGIN
    -- Income: Sueldo mensual
    INSERT INTO Transactions (UserId, AccountId, DestinationAccountId, Type, Amount, Description, Detail, CategoryId, Date)
    VALUES (1, @galiciaid, NULL, 'Ingreso', 950000.00, 'Sueldo Febrero', 'Deposito sueldo mensual', @sueldoid, '2026-02-01');

    -- Income: Freelance
    INSERT INTO Transactions (UserId, AccountId, DestinationAccountId, Type, Amount, Description, Detail, CategoryId, Date)
    VALUES (1, @cajaid, NULL, 'Ingreso', 150000.00, 'Proyecto freelance web', 'Desarrollo landing page cliente', @freelanceid, '2026-02-05');

    -- Expense: Supermercado
    INSERT INTO Transactions (UserId, AccountId, DestinationAccountId, Type, Amount, Description, Detail, CategoryId, Date)
    VALUES (1, @visaid, NULL, 'Egreso', 85000.00, 'Compra supermercado Coto', 'Compra semanal', @superid, '2026-02-03');

    -- Expense: Nafta
    INSERT INTO Transactions (UserId, AccountId, DestinationAccountId, Type, Amount, Description, Detail, CategoryId, Date)
    VALUES (1, @cajaid, NULL, 'Egreso', 45000.00, 'Nafta YPF', 'Tanque lleno', @autoid, '2026-02-04');

    -- Expense: Internet
    INSERT INTO Transactions (UserId, AccountId, DestinationAccountId, Type, Amount, Description, Detail, CategoryId, Date)
    VALUES (1, @galiciaid, NULL, 'Egreso', 18500.00, 'Fibertel Internet', 'Factura mensual', @cableid, '2026-02-10');

    -- Transfer: Banco a Caja
    INSERT INTO Transactions (UserId, AccountId, DestinationAccountId, Type, Amount, Description, Detail, CategoryId, Date)
    VALUES (1, @galiciaid, @cajaid, 'Transferencia', 100000.00, 'Retiro cajero', 'Extraccion para gastos', NULL, '2026-02-07');

    -- Expense: Viaticos
    INSERT INTO Transactions (UserId, AccountId, DestinationAccountId, Type, Amount, Description, Detail, CategoryId, Date)
    VALUES (1, @cajaid, NULL, 'Egreso', 12000.00, 'Peajes y estacionamiento', NULL, @viaticosid, '2026-02-08');

    -- Expense: Educacion
    INSERT INTO Transactions (UserId, AccountId, DestinationAccountId, Type, Amount, Description, Detail, CategoryId, Date)
    VALUES (1, @galiciaid, NULL, 'Egreso', 35000.00, 'Curso online Udemy', 'Curso de React avanzado', @educacionid, '2026-02-12');

    -- Expense: Servicios
    INSERT INTO Transactions (UserId, AccountId, DestinationAccountId, Type, Amount, Description, Detail, CategoryId, Date)
    VALUES (1, @visaid, NULL, 'Egreso', 22000.00, 'Factura luz EDENOR', 'Factura bimestral', @serviciosid, '2026-02-15');

    -- Expense: Super
    INSERT INTO Transactions (UserId, AccountId, DestinationAccountId, Type, Amount, Description, Detail, CategoryId, Date)
    VALUES (1, @cajaid, NULL, 'Egreso', 35000.00, 'Verduleria y carniceria', 'Compra del fin de semana', @superid, '2026-02-16');

    -- Income: Sueldo Marzo
    INSERT INTO Transactions (UserId, AccountId, DestinationAccountId, Type, Amount, Description, Detail, CategoryId, Date)
    VALUES (1, @galiciaid, NULL, 'Ingreso', 950000.00, 'Sueldo Marzo', 'Deposito sueldo mensual', @sueldoid, '2026-03-01');

    -- Expense: Auto seguro
    INSERT INTO Transactions (UserId, AccountId, DestinationAccountId, Type, Amount, Description, Detail, CategoryId, Date)
    VALUES (1, @galiciaid, NULL, 'Egreso', 55000.00, 'Seguro auto La Caja', 'Cuota mensual seguro', @autoid, '2026-03-02');

    -- Expense: Extra
    INSERT INTO Transactions (UserId, AccountId, DestinationAccountId, Type, Amount, Description, Detail, CategoryId, Date)
    VALUES (1, @cajaid, NULL, 'Egreso', 28000.00, 'Salida con amigos', 'Cena y bebidas', @extraid, '2026-03-03');

    -- Expense: Super Marzo
    INSERT INTO Transactions (UserId, AccountId, DestinationAccountId, Type, Amount, Description, Detail, CategoryId, Date)
    VALUES (1, @visaid, NULL, 'Egreso', 92000.00, 'Compra mensual Jumbo', 'Compra grande del mes', @superid, '2026-03-04');

    -- Transfer: Caja a USD
    INSERT INTO Transactions (UserId, AccountId, DestinationAccountId, Type, Amount, Description, Detail, CategoryId, Date)
    VALUES (1, @cajaid, @usdid, 'Transferencia', 50000.00, 'Compra dolares', 'Ahorro en USD', NULL, '2026-03-05');
END
GO

-- Seed exchange rate
IF NOT EXISTS (SELECT * FROM ExchangeRates WHERE FromCurrency = 'USD' AND ToCurrency = 'ARS')
BEGIN
    INSERT INTO ExchangeRates (FromCurrency, ToCurrency, Rate, Date)
    VALUES ('USD', 'ARS', 1250.0000, GETDATE());

    INSERT INTO ExchangeRates (FromCurrency, ToCurrency, Rate, Date)
    VALUES ('ARS', 'USD', 0.0008, GETDATE());
END
GO

PRINT 'Database AIcoding initialized successfully with financial management schema';
GO
