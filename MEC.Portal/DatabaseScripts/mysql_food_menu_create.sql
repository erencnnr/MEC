CREATE TABLE IF NOT EXISTS `food_menu_month` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `year` INT NOT NULL,
    `month` INT NOT NULL,
    `status` INT NOT NULL DEFAULT 0,
    `file_name` VARCHAR(255) NULL,
    `original_file_name` VARCHAR(255) NULL,
    `relative_path` VARCHAR(255) NULL,
    `content_type` VARCHAR(255) NULL,
    `size_bytes` BIGINT NOT NULL DEFAULT 0,
    `page_count` INT NOT NULL DEFAULT 0,
    `parse_warnings` TEXT NULL,
    `imported_at` DATETIME NULL,
    `published_at` DATETIME NULL,
    `created_date` DATETIME NULL,
    `update_date` DATETIME NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_food_menu_month_year_month` (`year`, `month`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `food_menu_day` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `food_menu_month_id` INT NOT NULL,
    `menu_date` DATETIME NOT NULL,
    `items_text` TEXT NULL,
    `source_page_number` INT NULL,
    `display_order` INT NOT NULL DEFAULT 1,
    `created_date` DATETIME NULL,
    `update_date` DATETIME NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_food_menu_day_month_date` (`food_menu_month_id`, `menu_date`),
    CONSTRAINT `fk_food_menu_day_month`
        FOREIGN KEY (`food_menu_month_id`) REFERENCES `food_menu_month` (`id`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
