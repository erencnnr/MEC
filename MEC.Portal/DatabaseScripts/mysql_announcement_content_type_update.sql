ALTER TABLE `announcement`
ADD COLUMN `content_type` TINYINT NOT NULL DEFAULT 0 AFTER `is_active`;
