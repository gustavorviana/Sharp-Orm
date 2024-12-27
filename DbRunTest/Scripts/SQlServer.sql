DROP TABLE IF EXISTS [Orders];
DROP TABLE IF EXISTS [Customers];
DROP TABLE IF EXISTS [Address];

CREATE TABLE [Address] (
  [id] INT PRIMARY KEY,
  [name] VARCHAR(50),
  [street] VARCHAR(100),
  [city] VARCHAR(100) NULL
);

CREATE TABLE [Customers] (
  [id] INT PRIMARY KEY,
  [address_id] INT NULL,
  [name] VARCHAR(50),
  [email] VARCHAR(100),
  FOREIGN KEY ([address_id]) REFERENCES [Address]([id]) ON DELETE CASCADE
);

CREATE TABLE [Orders] (
  [id] INT PRIMARY KEY,
  [customer_id] INT,
  [product] VARCHAR(50),
  [quantity] INT,
  [status] VARCHAR(20),
  FOREIGN KEY ([customer_id]) REFERENCES [Customers]([id]) ON DELETE CASCADE
);

DROP TABLE IF EXISTS [TestTable];
CREATE TABLE [TestTable] (
    [id] INT NOT NULL PRIMARY KEY,
    [id2] INT NULL,
    [name] VARCHAR(256) NOT NULL,
    [nick] VARCHAR(256) NULL,
    [record_created] datetimeoffset(7) DEFAULT GETDATE(),
    [number] INT NOT NULL,
    [custom_id] VARCHAR(36) NULL,
    [custom_status] INT NOT NULL
);

DROP TABLE IF EXISTS [Files];
CREATE TABLE Files (
    Id INT NOT NULL IDENTITY(1,1),
    bin VARBINARY(MAX),
    PRIMARY KEY (Id)
);

DROP TABLE IF EXISTS [DateTimeInfo];
CREATE TABLE [DateTimeInfo] (
    [MyId] INT PRIMARY KEY,
    [DateTime] DATETIME NOT NULL,
    [TimeSpan] TIME NOT NULL,
    [DateOnly] DATE NOT NULL,
    [TimeOnly] TIME NOT NULL
);