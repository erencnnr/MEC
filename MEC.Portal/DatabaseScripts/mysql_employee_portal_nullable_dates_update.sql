ALTER TABLE `employee_portal`
    MODIFY COLUMN `HireDate` DATETIME NULL,
    MODIFY COLUMN `BirthDate` DATETIME NULL;

UPDATE `employee_portal`
SET `HireDate` = NULL
WHERE `HireDate` IS NOT NULL
  AND YEAR(`HireDate`) <= 1000;

UPDATE `employee_portal`
SET `BirthDate` = NULL
WHERE `BirthDate` IS NOT NULL
  AND YEAR(`BirthDate`) <= 1000;
