CREATE TABLE IF NOT EXISTS `leave_agreement` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `employee_portal_id` INT NOT NULL,
    `agreed_leave_days` DECIMAL(10,2) NOT NULL DEFAULT 0,
    `is_signed` TINYINT(1) NOT NULL DEFAULT 0,
    `created_date` DATETIME(6) NULL,
    `update_date` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `ux_leave_agreement_employee_portal_id` (`employee_portal_id`),
    CONSTRAINT `fk_leave_agreement_employee_portal`
        FOREIGN KEY (`employee_portal_id`) REFERENCES `employee_portal` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
