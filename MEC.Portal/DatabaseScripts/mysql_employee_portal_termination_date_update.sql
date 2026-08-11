ALTER TABLE `employee_portal`
    ADD COLUMN IF NOT EXISTS `TerminationDate` DATETIME NULL AFTER `HireDate`;
