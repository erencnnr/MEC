ALTER TABLE `employee_portal_child`
    ADD COLUMN IF NOT EXISTS `education_status` INT NULL AFTER `birth_date`;
