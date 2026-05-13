CREATE TABLE IF NOT EXISTS `overtime_request` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `employee_portal_id` INT NOT NULL,
    `start_date` DATETIME(6) NOT NULL,
    `end_date` DATETIME(6) NOT NULL,
    `requested_hours` DECIMAL(10,2) NOT NULL,
    `reason` VARCHAR(1000) NOT NULL,
    `status` INT NOT NULL DEFAULT 0,
    `decision_by` VARCHAR(255) NULL,
    `decision_date` DATETIME(6) NULL,
    `created_date` DATETIME(6) NULL,
    `update_date` DATETIME(6) NULL,
    PRIMARY KEY (`id`),
    INDEX `ix_overtime_request_employee_portal_id` (`employee_portal_id`),
    INDEX `ix_overtime_request_status` (`status`),
    CONSTRAINT `fk_overtime_request_employee_portal_id`
        FOREIGN KEY (`employee_portal_id`) REFERENCES `employee_portal` (`id`)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
