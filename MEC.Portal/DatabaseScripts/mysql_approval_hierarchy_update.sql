ALTER TABLE `employee_portal`
    ADD COLUMN IF NOT EXISTS `IsManager` TINYINT(1) NOT NULL DEFAULT 0 AFTER `IsAdmin`;

ALTER TABLE `leaves`
    ADD COLUMN IF NOT EXISTS `manager_decision_by` VARCHAR(255) NULL AFTER `decision_date`,
    ADD COLUMN IF NOT EXISTS `manager_decision_date` DATETIME(6) NULL AFTER `manager_decision_by`;

ALTER TABLE `overtime_request`
    ADD COLUMN IF NOT EXISTS `manager_decision_by` VARCHAR(255) NULL AFTER `decision_date`,
    ADD COLUMN IF NOT EXISTS `manager_decision_date` DATETIME(6) NULL AFTER `manager_decision_by`;

-- Şema güncellemesinden sonra Portal Kullanıcıları ekranında her okul müdürü için
-- "Lokasyon Müdürü" alanını Evet yapın ve yönettiği lokasyonları atayın.
-- Mustafa Meral son onaylayıcı olarak uygulama ayarından tanımlanır; IsManager işareti gerekmez.
