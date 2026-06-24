ALTER TABLE `employee_portal`
    ADD COLUMN IF NOT EXISTS `address_text` TEXT NULL AFTER `BirthDate`,
    ADD COLUMN IF NOT EXISTS `marital_status` INT NULL AFTER `address_text`,
    ADD COLUMN IF NOT EXISTS `education_university` VARCHAR(255) NULL AFTER `marital_status`,
    ADD COLUMN IF NOT EXISTS `education_faculty` VARCHAR(255) NULL AFTER `education_university`,
    ADD COLUMN IF NOT EXISTS `education_department` VARCHAR(255) NULL AFTER `education_faculty`;

CREATE TABLE IF NOT EXISTS `employee_portal_child` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `employee_portal_id` INT NOT NULL,
    `gender` INT NOT NULL,
    `birth_date` DATE NOT NULL,
    `created_date` DATETIME(6) NULL,
    `update_date` DATETIME(6) NULL,
    PRIMARY KEY (`id`),
    INDEX `ix_employee_portal_child_employee_portal_id` (`employee_portal_id`),
    CONSTRAINT `fk_employee_portal_child_employee_portal`
        FOREIGN KEY (`employee_portal_id`) REFERENCES `employee_portal` (`id`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
