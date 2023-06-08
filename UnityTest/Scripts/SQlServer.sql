DROP TABLE IF EXISTS [Orders];
DROP TABLE IF EXISTS [Customers];
CREATE TABLE [Customers] (
      [id] INT PRIMARY KEY,
      [name] VARCHAR(50),
      [email] VARCHAR(100),
      [address] VARCHAR(100)
);
CREATE TABLE [Orders] (
      [id] INT PRIMARY KEY,
      [customer_id] INT,
      [product] VARCHAR(50),
      [quantity] INT,
      status VARCHAR(20),
      FOREIGN KEY ([customer_id]) REFERENCES [customers](id)
);
DROP TABLE IF EXISTS [TestTable];
CREATE TABLE [TestTable] (
      [id] INT NOT NULL PRIMARY KEY,
      [name] VARCHAR(256) NOT NULL,
      [nick] VARCHAR(256) NULL,
      [record_created] DATETIME DEFAULT GETDATE(),
      [number] DECIMAL(13, 2) NOT NULL,
      [custom_id] VARCHAR(36) NOT NULL,
      [custom_status] INT NOT NULL
);