CREATE TABLE IF NOT EXISTS `library_folder` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(255) NOT NULL,
    `parent_folder_id` INT NULL,
    `display_order` INT NOT NULL DEFAULT 1,
    `created_date` DATETIME(6) NULL,
    `update_date` DATETIME(6) NULL,
    PRIMARY KEY (`id`),
    INDEX `ix_library_folder_parent_folder_id` (`parent_folder_id`),
    CONSTRAINT `fk_library_folder_parent`
        FOREIGN KEY (`parent_folder_id`) REFERENCES `library_folder`(`id`)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `library_document` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `folder_id` INT NOT NULL,
    `file_name` VARCHAR(255) NOT NULL,
    `original_file_name` VARCHAR(255) NOT NULL,
    `relative_path` VARCHAR(500) NOT NULL,
    `content_type` VARCHAR(255) NOT NULL,
    `size_bytes` BIGINT NOT NULL,
    `created_date` DATETIME(6) NULL,
    `update_date` DATETIME(6) NULL,
    PRIMARY KEY (`id`),
    INDEX `ix_library_document_folder_id` (`folder_id`),
    CONSTRAINT `fk_library_document_folder`
        FOREIGN KEY (`folder_id`) REFERENCES `library_folder`(`id`)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
