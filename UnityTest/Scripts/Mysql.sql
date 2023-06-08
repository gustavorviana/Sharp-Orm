CREATE TABLE IF NOT EXISTS customers (
  id INT PRIMARY KEY,
  name VARCHAR(50),
  email VARCHAR(100),
  address VARCHAR(100)
);
CREATE TABLE IF NOT EXISTS orders (
  id INT PRIMARY KEY,
  customer_id INT,
  product VARCHAR(50),
  quantity INT,
  status VARCHAR(20),
  FOREIGN KEY (customer_id) REFERENCES customers(id)
);
CREATE TABLE IF NOT EXISTS TestTable (
  id INT NOT NULL PRIMARY KEY,
  id2 INT NULL,
  name VARCHAR(256) NOT NULL,
  nick VARCHAR(256) NULL,
  record_created TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  number DECIMAL(13, 2) NOT NULL,
  custom_id VARCHAR(36) NULL,
  custom_status INT NOT NULL
)