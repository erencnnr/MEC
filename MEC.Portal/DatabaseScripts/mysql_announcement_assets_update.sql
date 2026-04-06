CREATE TABLE IF NOT EXISTS `announcement_attachment` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `announcement_id` INT NOT NULL,
    `file_name` VARCHAR(255) NOT NULL,
    `original_file_name` VARCHAR(255) NOT NULL,
    `relative_path` VARCHAR(500) NOT NULL,
    `content_type` VARCHAR(255) NOT NULL,
    `size_bytes` BIGINT NOT NULL,
    `created_date` DATETIME(6) NULL,
    `update_date` DATETIME(6) NULL,
    PRIMARY KEY (`id`),
    INDEX `ix_announcement_attachment_announcement_id` (`announcement_id`),
    CONSTRAINT `fk_announcement_attachment_announcement`
        FOREIGN KEY (`announcement_id`) REFERENCES `announcement`(`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `announcement_image` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `announcement_id` INT NOT NULL,
    `file_name` VARCHAR(255) NOT NULL,
    `original_file_name` VARCHAR(255) NOT NULL,
    `relative_path` VARCHAR(500) NOT NULL,
    `content_type` VARCHAR(255) NOT NULL,
    `size_bytes` BIGINT NOT NULL,
    `created_date` DATETIME(6) NULL,
    `update_date` DATETIME(6) NULL,
    PRIMARY KEY (`id`),
    INDEX `ix_announcement_image_announcement_id` (`announcement_id`),
    CONSTRAINT `fk_announcement_image_announcement`
        FOREIGN KEY (`announcement_id`) REFERENCES `announcement`(`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
