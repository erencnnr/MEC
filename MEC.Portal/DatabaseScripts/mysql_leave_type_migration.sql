CREATE TABLE IF NOT EXISTS `leave_type` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(255) NOT NULL,
    `code` VARCHAR(64) NOT NULL,
    `is_active` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedDate` DATETIME(6) NULL,
    `UpdateDate` DATETIME(6) NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_leave_type_code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `leave_type` (`id`, `name`, `code`, `is_active`, `CreatedDate`)
VALUES
    (1, 'Yıllık İzin', 'ANNUAL', 1, UTC_TIMESTAMP(6)),
    (2, 'Mazeret İzni', 'EXCUSE', 1, UTC_TIMESTAMP(6)),
    (3, 'Diğer', 'OTHER', 1, UTC_TIMESTAMP(6)),
    (4, 'Ücretsiz İzin', 'UNPAID', 1, UTC_TIMESTAMP(6)),
    (5, 'Hastalık İzni', 'SICK', 1, UTC_TIMESTAMP(6)),
    (6, 'Doğum İzni', 'MATERNITY', 1, UTC_TIMESTAMP(6)),
    (7, 'Babalık İzni', 'PATERNITY', 1, UTC_TIMESTAMP(6))
ON DUPLICATE KEY UPDATE
    `name` = VALUES(`name`),
    `is_active` = VALUES(`is_active`),
    `UpdateDate` = UTC_TIMESTAMP(6);

ALTER TABLE `leaves`
    ADD COLUMN `leave_type_id` INT NULL AFTER `end_date`;

UPDATE `leaves`
SET `leave_type_id` = CASE
    WHEN `leave_type` IN ('Yıllık İzin', 'Yillik Izin') THEN 1
    WHEN `leave_type` IN ('Mazeret İzni', 'Mazeret Izni') THEN 2
    WHEN `leave_type` IN ('Diğer', 'Diger') THEN 3
    WHEN `leave_type` IN ('Ücretsiz İzin', 'Ucretsiz Izin') THEN 4
    WHEN `leave_type` IN ('Hastalık İzni', 'Hastalik Izni') THEN 5
    WHEN `leave_type` IN ('Doğum İzni', 'Dogum Izni') THEN 6
    WHEN `leave_type` IN ('Babalık İzni', 'Babalik Izni') THEN 7
    ELSE NULL
END
WHERE `leave_type_id` IS NULL;

SELECT `id`, `leave_type`
FROM `leaves`
WHERE `leave_type_id` IS NULL;

ALTER TABLE `leaves`
    ADD INDEX `ix_leaves_leave_type_id` (`leave_type_id`);

ALTER TABLE `leaves`
    ADD CONSTRAINT `fk_leaves_leave_type_id`
        FOREIGN KEY (`leave_type_id`) REFERENCES `leave_type`(`id`)
        ON DELETE RESTRICT
        ON UPDATE CASCADE;

ALTER TABLE `leaves`
    MODIFY COLUMN `leave_type_id` INT NOT NULL;
