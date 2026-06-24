DROP TABLE IF EXISTS `pdks_movement`;
DROP TABLE IF EXISTS `pdks_user`;

CREATE TABLE `pdks_user` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `sicil_no` VARCHAR(64) NOT NULL,
    `ad_soyad` VARCHAR(255) NOT NULL,
    `aktif_mi` TINYINT(1) NOT NULL DEFAULT 1,
    `created_date` DATETIME NULL,
    `update_date` DATETIME NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_pdks_user_sicil_no` (`sicil_no`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `pdks_movement` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `pdks_user_id` INT NOT NULL,
    `hareket_zamani` DATETIME NOT NULL,
    `hareket_tipi` VARCHAR(16) NOT NULL,
    `cihaz_adi` VARCHAR(255) NULL,
    `not` VARCHAR(255) NULL,
    `created_date` DATETIME NULL,
    `update_date` DATETIME NULL,
    PRIMARY KEY (`id`),
    KEY `ix_pdks_movement_user_id` (`pdks_user_id`),
    KEY `ix_pdks_movement_hareket_zamani` (`hareket_zamani`),
    KEY `ix_pdks_movement_user_datetime` (`pdks_user_id`, `hareket_zamani`),
    CONSTRAINT `fk_pdks_movement_user`
        FOREIGN KEY (`pdks_user_id`) REFERENCES `pdks_user` (`id`)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
