DROP TABLE IF EXISTS `survey_response_answer`;
DROP TABLE IF EXISTS `survey_response`;
DROP TABLE IF EXISTS `survey_question_option`;
DROP TABLE IF EXISTS `survey_question`;
DROP TABLE IF EXISTS `survey_answer`;
DROP TABLE IF EXISTS `survey_option`;
DROP TABLE IF EXISTS `survey`;

CREATE TABLE `survey` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `title` VARCHAR(255) NOT NULL,
    `description` TEXT NULL,
    `is_active` TINYINT(1) NOT NULL DEFAULT 1,
    `created_date` DATETIME NULL,
    `update_date` DATETIME NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `survey_question` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `survey_id` INT NOT NULL,
    `question_text` TEXT NOT NULL,
    `type` INT NOT NULL,
    `display_order` INT NOT NULL,
    `created_date` DATETIME NULL,
    `update_date` DATETIME NULL,
    PRIMARY KEY (`id`),
    KEY `ix_survey_question_survey_id_display_order` (`survey_id`, `display_order`),
    CONSTRAINT `fk_survey_question_survey`
        FOREIGN KEY (`survey_id`) REFERENCES `survey` (`id`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `survey_question_option` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `survey_question_id` INT NOT NULL,
    `text` VARCHAR(255) NOT NULL,
    `display_order` INT NOT NULL,
    `created_date` DATETIME NULL,
    `update_date` DATETIME NULL,
    PRIMARY KEY (`id`),
    KEY `ix_survey_question_option_question_id_display_order` (`survey_question_id`, `display_order`),
    CONSTRAINT `fk_survey_question_option_question`
        FOREIGN KEY (`survey_question_id`) REFERENCES `survey_question` (`id`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `survey_response` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `survey_id` INT NOT NULL,
    `employee_portal_id` INT NOT NULL,
    `answered_at` DATETIME NOT NULL,
    `created_date` DATETIME NULL,
    `update_date` DATETIME NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_survey_response_survey_employee` (`survey_id`, `employee_portal_id`),
    CONSTRAINT `fk_survey_response_survey`
        FOREIGN KEY (`survey_id`) REFERENCES `survey` (`id`)
        ON DELETE CASCADE,
    CONSTRAINT `fk_survey_response_employee_portal`
        FOREIGN KEY (`employee_portal_id`) REFERENCES `employee_portal` (`id`)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `survey_response_answer` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `survey_response_id` INT NOT NULL,
    `survey_question_id` INT NOT NULL,
    `rating_value` INT NULL,
    `survey_question_option_id` INT NULL,
    `created_date` DATETIME NULL,
    `update_date` DATETIME NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_survey_response_answer_response_question` (`survey_response_id`, `survey_question_id`),
    CONSTRAINT `fk_survey_response_answer_response`
        FOREIGN KEY (`survey_response_id`) REFERENCES `survey_response` (`id`)
        ON DELETE CASCADE,
    CONSTRAINT `fk_survey_response_answer_question`
        FOREIGN KEY (`survey_question_id`) REFERENCES `survey_question` (`id`)
        ON DELETE CASCADE,
    CONSTRAINT `fk_survey_response_answer_option`
        FOREIGN KEY (`survey_question_option_id`) REFERENCES `survey_question_option` (`id`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
