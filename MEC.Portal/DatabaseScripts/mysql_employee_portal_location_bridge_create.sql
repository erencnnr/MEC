CREATE TABLE IF NOT EXISTS `employee_portal_location` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `employee_portal_id` INT NOT NULL,
    `location_id` INT NOT NULL,
    `created_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `update_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_employee_portal_location_employee_portal_id_location_id` (`employee_portal_id`, `location_id`),
    KEY `ix_employee_portal_location_employee_portal_id` (`employee_portal_id`),
    KEY `ix_employee_portal_location_location_id` (`location_id`),
    CONSTRAINT `fk_employee_portal_location_employee_portal`
        FOREIGN KEY (`employee_portal_id`) REFERENCES `employee_portal` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_employee_portal_location_location`
        FOREIGN KEY (`location_id`) REFERENCES `location` (`id`) ON DELETE RESTRICT
);

INSERT INTO `employee_portal_location` (`employee_portal_id`, `location_id`, `created_date`, `update_date`)
SELECT `id`, `LocationId`, NOW(), NOW()
FROM `employee_portal`
WHERE `LocationId` IS NOT NULL
ON DUPLICATE KEY UPDATE
    `update_date` = VALUES(`update_date`);
