DROP TABLE IF EXISTS `orders`;
DROP TABLE IF EXISTS `customers`;
DROP TABLE IF EXISTS `address`;
DROP TABLE IF EXISTS `TestTable`;
DROP TABLE IF EXISTS `Files`;
DROP TABLE IF EXISTS `DateTimeInfo`;

CREATE TABLE IF NOT EXISTS `address` (
  `id` INT PRIMARY KEY,
  `name` VARCHAR(50),
  `street` VARCHAR(100),
  `city` VARCHAR(100) NULL
);

CREATE TABLE IF NOT EXISTS `customers` (
  `id` INT PRIMARY KEY,
  `address_id` INT NULL,
  `name` VARCHAR(50),
  `email` VARCHAR(100),
  FOREIGN KEY (`address_id`) REFERENCES `address`(`id`) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS `orders` (
  `id` INT PRIMARY KEY,
  `customer_id` INT,
  `product` VARCHAR(50),
  `quantity` INT,
  `status` VARCHAR(20),
  FOREIGN KEY (`customer_id`) REFERENCES `customers`(`id`) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS `TestTable` (
  `id` INT NOT NULL PRIMARY KEY AUTO_INCREMENT,
  `id2` INT NULL,
  `name` VARCHAR(256) NOT NULL,
  `nick` VARCHAR(256) NULL,
  `record_created` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `number` INT NOT NULL,
  `custom_id` VARCHAR(36) NULL,
  `custom_status` INT NOT NULL
);

CREATE TABLE IF NOT EXISTS `Files` (
    `id` INT NOT NULL PRIMARY KEY AUTO_INCREMENT,
    `bin` BLOB
);

CREATE TABLE `DateTimeInfo` (
    `MyId` INT PRIMARY KEY,
    `DateTime` DATETIME NOT NULL,
    `TimeSpan` TIME NOT NULL,
    `DateOnly` DATE NOT NULL,
    `TimeOnly` TIME NOT NULL
);