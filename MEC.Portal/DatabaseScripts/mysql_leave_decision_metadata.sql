ALTER TABLE `leaves`
    ADD COLUMN `decision_by` VARCHAR(255) NULL AFTER `status`,
    ADD COLUMN `decision_date` DATETIME NULL AFTER `decision_by`;
