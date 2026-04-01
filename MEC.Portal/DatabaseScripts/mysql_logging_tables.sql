CREATE TABLE IF NOT EXISTS `api_log` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `ip_address` VARCHAR(64) NOT NULL,
    `mac_address` VARCHAR(64) NULL,
    `user` VARCHAR(256) NOT NULL,
    `timestamp` DATETIME(6) NOT NULL,
    `message` LONGTEXT NOT NULL,
    `level` VARCHAR(32) NOT NULL,
    `method_name` VARCHAR(128) NOT NULL,
    `request_path` VARCHAR(512) NULL,
    `http_method` VARCHAR(16) NULL,
    `status_code` INT NULL,
    `request_body` LONGTEXT NULL,
    `response_body` LONGTEXT NULL,
    `query_string` LONGTEXT NULL,
    PRIMARY KEY (`id`),
    INDEX `ix_api_log_timestamp` (`timestamp`),
    INDEX `ix_api_log_level` (`level`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `log` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `ip_address` VARCHAR(64) NOT NULL,
    `mac_address` VARCHAR(64) NULL,
    `user` VARCHAR(256) NOT NULL,
    `timestamp` DATETIME(6) NOT NULL,
    `message` LONGTEXT NOT NULL,
    `level` VARCHAR(32) NOT NULL,
    `method_name` VARCHAR(128) NOT NULL,
    PRIMARY KEY (`id`),
    INDEX `ix_log_timestamp` (`timestamp`),
    INDEX `ix_log_level` (`level`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
