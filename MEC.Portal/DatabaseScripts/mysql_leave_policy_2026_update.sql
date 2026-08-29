-- Bu betik yeni izin politikası sürümü devreye alınırken bir kez çalıştırılmalıdır.

ALTER TABLE `leave_agreement`
    ADD COLUMN `balance_as_of_date` DATE NULL AFTER `agreed_leave_days`,
    ADD COLUMN `current_year_earned_days` DECIMAL(10,2) NOT NULL DEFAULT 0.00 AFTER `balance_as_of_date`,
    ADD COLUMN `current_year_used_days` DECIMAL(10,2) NOT NULL DEFAULT 0.00 AFTER `current_year_earned_days`;

ALTER TABLE `leaves`
    ADD COLUMN `minimum_block_exception_requested` TINYINT(1) NOT NULL DEFAULT 0 AFTER `remaining_leave_days`;

CREATE TABLE IF NOT EXISTS `leave_policy_setting` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `effective_from` DATE NOT NULL,
    `count_saturday` TINYINT(1) NOT NULL DEFAULT 0,
    `created_date` DATETIME(6) NULL,
    `update_date` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `ux_leave_policy_setting_effective_from` (`effective_from`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Mevcut şirket uygulaması bir haftalık izinde cumartesiyi de saydığı için başlangıç değeri açıktır.
INSERT INTO `leave_policy_setting`
    (`effective_from`, `count_saturday`, `created_date`, `update_date`)
VALUES
    ('2000-01-01', 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6))
ON DUPLICATE KEY UPDATE
    `count_saturday` = VALUES(`count_saturday`),
    `update_date` = UTC_TIMESTAMP(6);

INSERT INTO `leave_type` (`name`, `code`, `is_active`, `CreatedDate`, `UpdateDate`)
VALUES
    ('Evlilik İzni', 'MARRIAGE', 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)),
    ('Ölüm İzni', 'BEREAVEMENT', 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)),
    ('Babalık İzni', 'PATERNITY', 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6))
ON DUPLICATE KEY UPDATE
    `name` = VALUES(`name`),
    `is_active` = VALUES(`is_active`),
    `UpdateDate` = UTC_TIMESTAMP(6);

-- Genel mazeret seçeneği yerine süre sınırı tanımlı alt türler kullanılır.
UPDATE `leave_type`
SET `is_active` = 0,
    `UpdateDate` = UTC_TIMESTAMP(6)
WHERE `code` = 'EXCUSE';

-- Diyanet İşleri Başkanlığı 2026 dinî günler takvimine göre arifeler öğleden sonra,
-- Ramazan Bayramı 20-22 Mart ve Kurban Bayramı 27-30 Mayıs tarihlerindedir.
INSERT INTO `holiday` (`name`, `start_date`, `end_date`, `created_date`, `update_date`)
SELECT 'Ramazan Bayramı Arifesi', '2026-03-19 13:00:00', '2026-03-20 00:00:00', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
WHERE NOT EXISTS (
    SELECT 1 FROM `holiday`
    WHERE `start_date` = '2026-03-19 13:00:00' AND `end_date` = '2026-03-20 00:00:00'
);

INSERT INTO `holiday` (`name`, `start_date`, `end_date`, `created_date`, `update_date`)
SELECT 'Ramazan Bayramı', '2026-03-20 00:00:00', '2026-03-23 00:00:00', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
WHERE NOT EXISTS (
    SELECT 1 FROM `holiday`
    WHERE `start_date` = '2026-03-20 00:00:00' AND `end_date` = '2026-03-23 00:00:00'
);

INSERT INTO `holiday` (`name`, `start_date`, `end_date`, `created_date`, `update_date`)
SELECT 'Kurban Bayramı Arifesi', '2026-05-26 13:00:00', '2026-05-27 00:00:00', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
WHERE NOT EXISTS (
    SELECT 1 FROM `holiday`
    WHERE `start_date` = '2026-05-26 13:00:00' AND `end_date` = '2026-05-27 00:00:00'
);

INSERT INTO `holiday` (`name`, `start_date`, `end_date`, `created_date`, `update_date`)
SELECT 'Kurban Bayramı', '2026-05-27 00:00:00', '2026-05-31 00:00:00', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
WHERE NOT EXISTS (
    SELECT 1 FROM `holiday`
    WHERE `start_date` = '2026-05-27 00:00:00' AND `end_date` = '2026-05-31 00:00:00'
);
