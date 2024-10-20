DROP TABLE IF EXISTS "orders";
DROP TABLE IF EXISTS "customers";
DROP TABLE IF EXISTS "address";
DROP TABLE IF EXISTS "TestTable";
DROP TABLE IF EXISTS Files;

CREATE TABLE IF NOT EXISTS "address" (
  "id" INTEGER PRIMARY KEY,
  "name" VARCHAR(50),
  "street" VARCHAR(100)
);

CREATE TABLE IF NOT EXISTS "customers" (
  "id" INTEGER PRIMARY KEY,
  "address_id" INTEGER NULL,
  "name" VARCHAR(50),
  "email" VARCHAR(100),
  FOREIGN KEY ("address_id") REFERENCES "address"("id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "orders" (
  "id" INTEGER PRIMARY KEY,
  "customer_id" INT,
  "product" VARCHAR(50),
  "quantity" INT,
  "status" VARCHAR(24),
  FOREIGN KEY ("customer_id") REFERENCES "customers"("id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "TestTable" (
  "id"	INTEGER NOT NULL,
  "id2" INTEGER NULL,
  "name" TEXT(256) NOT NULL,
  "nick" TEXT(256) NULL,
  "record_created" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  "number" INT NOT NULL,
  "custom_id" TEXT(36) NULL,
  "custom_status" INTEGER NOT NULL,
  PRIMARY KEY("id" AUTOINCREMENT)
);

CREATE TABLE Files (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    bin BLOB
);
