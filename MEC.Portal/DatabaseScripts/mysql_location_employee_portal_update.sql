CREATE TABLE IF NOT EXISTS `location` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(255) NOT NULL,
    `created_date` DATETIME(6) NULL,
    `update_date` DATETIME(6) NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_location_name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `location` (`name`, `created_date`, `update_date`)
VALUES
    ('Koşuyolu', NOW(6), NOW(6)),
    ('Arnavutköy', NOW(6), NOW(6)),
    ('Bahçeköy', NOW(6), NOW(6)),
    ('Genel Müdürlük', NOW(6), NOW(6))
ON DUPLICATE KEY UPDATE
    `name` = VALUES(`name`),
    `update_date` = NOW(6);

ALTER TABLE `employee_portal`
    ADD COLUMN IF NOT EXISTS `Title` VARCHAR(255) NOT NULL DEFAULT '' AFTER `Email`,
    ADD COLUMN IF NOT EXISTS `LocationId` INT NULL AFTER `BirthDate`;

CREATE INDEX `ix_employee_portal_location_id` ON `employee_portal` (`LocationId`);

ALTER TABLE `employee_portal`
    ADD CONSTRAINT `fk_employee_portal_location`
        FOREIGN KEY (`LocationId`) REFERENCES `location` (`id`);
