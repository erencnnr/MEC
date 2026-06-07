CREATE TABLE IF NOT EXISTS `birthday_popup_image` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `file_name` VARCHAR(255) NOT NULL,
    `original_file_name` VARCHAR(255) NOT NULL,
    `relative_path` VARCHAR(500) NOT NULL,
    `content_type` VARCHAR(255) NOT NULL,
    `size_bytes` BIGINT NOT NULL,
    `created_date` DATETIME(6) NULL,
    `update_date` DATETIME(6) NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `birthday_popup_view` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `employee_portal_id` INT NOT NULL,
    `shown_year` INT NOT NULL,
    `shown_at` DATETIME(6) NOT NULL,
    `created_date` DATETIME(6) NULL,
    `update_date` DATETIME(6) NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_birthday_popup_view_employee_year` (`employee_portal_id`, `shown_year`),
    CONSTRAINT `fk_birthday_popup_view_employee_portal`
        FOREIGN KEY (`employee_portal_id`) REFERENCES `employee_portal` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
