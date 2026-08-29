ALTER TABLE `employee_portal_child`
    ADD COLUMN IF NOT EXISTS `name` VARCHAR(100) NULL AFTER `employee_portal_id`;
