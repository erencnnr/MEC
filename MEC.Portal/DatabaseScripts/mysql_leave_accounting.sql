-- Stop the Portal and annual job; back up the database first.
-- Additive, repeatable schema installation. Existing balances are not altered.
CREATE TABLE IF NOT EXISTS `leave_accrual` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EmployeeId` int NOT NULL,
    `Cycle` int NOT NULL,
    `ServiceYear` int NOT NULL,
    `Anniversary` datetime(6) NOT NULL,
    `CreditedDays` decimal(10,2) NOT NULL,
    `CreatedDate` datetime(6) NULL,
    `UpdateDate` datetime(6) NULL,
    CONSTRAINT `PK_leave_accrual` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `leave_agreement_version` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `AgreementId` int NOT NULL,
    `EmployeeId` int NOT NULL,
    `Version` int NOT NULL,
    `Actor` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Snapshot` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedDate` datetime(6) NULL,
    `UpdateDate` datetime(6) NULL,
    CONSTRAINT `PK_leave_agreement_version` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `leave_calendar` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Year` int NOT NULL,
    `Revision` bigint NOT NULL,
    `IsApproved` tinyint(1) NOT NULL,
    `ApprovedBy` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `SourceUrl` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `DraftJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `PublishedJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedDate` datetime(6) NULL,
    `UpdateDate` datetime(6) NULL,
    CONSTRAINT `PK_leave_calendar` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `leave_calendar_version` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Year` int NOT NULL,
    `Revision` bigint NOT NULL,
    `Actor` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `SourceUrl` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `HolidaysJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedDate` datetime(6) NULL,
    `UpdateDate` datetime(6) NULL,
    CONSTRAINT `PK_leave_calendar_version` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `leave_import_batch` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `BatchHash` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Actor` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `InputJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `ResultJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedDate` datetime(6) NULL,
    `UpdateDate` datetime(6) NULL,
    CONSTRAINT `PK_leave_import_batch` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `leave_import_row` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `BatchHash` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `EmployeeId` int NOT NULL,
    `Days` decimal(10,2) NOT NULL,
    `Actor` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedDate` datetime(6) NULL,
    `UpdateDate` datetime(6) NULL,
    CONSTRAINT `PK_leave_import_row` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `leave_job_result` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `RunId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `EmployeeId` int NOT NULL,
    `BusinessDate` datetime(6) NOT NULL,
    `Success` tinyint(1) NOT NULL,
    `Error` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedDate` datetime(6) NULL,
    `UpdateDate` datetime(6) NULL,
    CONSTRAINT `PK_leave_job_result` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `leave_account` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EmployeeId` int NOT NULL,
    `Cycle` int NOT NULL,
    `Revision` bigint NOT NULL,
    `OpeningDate` datetime(6) NOT NULL,
    `ProcessedThrough` datetime(6) NOT NULL,
    `HireDate` datetime(6) NULL,
    `BirthDate` datetime(6) NULL,
    `CoveredServiceYears` int NOT NULL,
    `NeedsReview` tinyint(1) NOT NULL,
    `Balance` decimal(10,2) NOT NULL,
    `CreatedDate` datetime(6) NULL,
    `UpdateDate` datetime(6) NULL,
    CONSTRAINT `PK_leave_account` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_leave_account_employee_portal_EmployeeId` FOREIGN KEY (`EmployeeId`) REFERENCES `employee_portal` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `leave_cancellation` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `LeaveId` int NOT NULL,
    `Status` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Reason` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `RequestedBy` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `DecidedBy` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `DecisionDate` datetime(6) NULL,
    `CreatedDate` datetime(6) NULL,
    `UpdateDate` datetime(6) NULL,
    CONSTRAINT `PK_leave_cancellation` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_leave_cancellation_leaves_LeaveId` FOREIGN KEY (`LeaveId`) REFERENCES `leaves` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `leave_charge` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CalculationJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `SplitCutoff` datetime(6) NULL,
    `SplitBeforeDays` decimal(10,2) NULL,
    `LeaveId` int NOT NULL,
    `EmployeeId` int NOT NULL,
    `Days` decimal(10,2) NOT NULL,
    `IncludedInOpening` tinyint(1) NOT NULL,
    `HistoricalIncluded` tinyint(1) NULL,
    `ReviewReason` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedDate` datetime(6) NULL,
    `UpdateDate` datetime(6) NULL,
    CONSTRAINT `PK_leave_charge` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_leave_charge_leaves_LeaveId` FOREIGN KEY (`LeaveId`) REFERENCES `leaves` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `leave_movement` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EmployeeId` int NOT NULL,
    `Cycle` int NOT NULL,
    `OperationKey` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Kind` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `EffectiveDate` datetime(6) NOT NULL,
    `Days` decimal(10,2) NOT NULL,
    `LeaveId` int NULL,
    `ServiceYear` int NULL,
    `Actor` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Reason` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedDate` datetime(6) NULL,
    `UpdateDate` datetime(6) NULL,
    CONSTRAINT `PK_leave_movement` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_leave_movement_employee_portal_EmployeeId` FOREIGN KEY (`EmployeeId`) REFERENCES `employee_portal` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_leave_movement_leaves_LeaveId` FOREIGN KEY (`LeaveId`) REFERENCES `leaves` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

SET @ddl = IF(EXISTS(SELECT 1 FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name='leave_account' AND index_name='IX_leave_account_EmployeeId'), 'SELECT 1', 'CREATE UNIQUE INDEX `IX_leave_account_EmployeeId` ON `leave_account` (`EmployeeId`)');
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @ddl = IF(EXISTS(SELECT 1 FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name='leave_accrual' AND index_name='IX_leave_accrual_EmployeeId_Cycle_ServiceYear'), 'SELECT 1', 'CREATE UNIQUE INDEX `IX_leave_accrual_EmployeeId_Cycle_ServiceYear` ON `leave_accrual` (`EmployeeId`, `Cycle`, `ServiceYear`)');
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @ddl = IF(EXISTS(SELECT 1 FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name='leave_agreement_version' AND index_name='IX_leave_agreement_version_AgreementId_Version'), 'SELECT 1', 'CREATE UNIQUE INDEX `IX_leave_agreement_version_AgreementId_Version` ON `leave_agreement_version` (`AgreementId`, `Version`)');
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @ddl = IF(EXISTS(SELECT 1 FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name='leave_calendar' AND index_name='IX_leave_calendar_Year'), 'SELECT 1', 'CREATE UNIQUE INDEX `IX_leave_calendar_Year` ON `leave_calendar` (`Year`)');
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @ddl = IF(EXISTS(SELECT 1 FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name='leave_calendar_version' AND index_name='IX_leave_calendar_version_Year_Revision'), 'SELECT 1', 'CREATE UNIQUE INDEX `IX_leave_calendar_version_Year_Revision` ON `leave_calendar_version` (`Year`, `Revision`)');
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @ddl = IF(EXISTS(SELECT 1 FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name='leave_cancellation' AND index_name='IX_leave_cancellation_LeaveId'), 'SELECT 1', 'CREATE UNIQUE INDEX `IX_leave_cancellation_LeaveId` ON `leave_cancellation` (`LeaveId`)');
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @ddl = IF(EXISTS(SELECT 1 FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name='leave_charge' AND index_name='IX_leave_charge_LeaveId'), 'SELECT 1', 'CREATE UNIQUE INDEX `IX_leave_charge_LeaveId` ON `leave_charge` (`LeaveId`)');
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @ddl = IF(EXISTS(SELECT 1 FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name='leave_import_batch' AND index_name='IX_leave_import_batch_BatchHash'), 'SELECT 1', 'CREATE UNIQUE INDEX `IX_leave_import_batch_BatchHash` ON `leave_import_batch` (`BatchHash`)');
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @ddl = IF(EXISTS(SELECT 1 FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name='leave_import_row' AND index_name='IX_leave_import_row_BatchHash_Email'), 'SELECT 1', 'CREATE UNIQUE INDEX `IX_leave_import_row_BatchHash_Email` ON `leave_import_row` (`BatchHash`, `Email`)');
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @ddl = IF(EXISTS(SELECT 1 FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name='leave_movement' AND index_name='IX_leave_movement_EmployeeId_EffectiveDate'), 'SELECT 1', 'CREATE INDEX `IX_leave_movement_EmployeeId_EffectiveDate` ON `leave_movement` (`EmployeeId`, `EffectiveDate`)');
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @ddl = IF(EXISTS(SELECT 1 FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name='leave_movement' AND index_name='IX_leave_movement_EmployeeId_OperationKey'), 'SELECT 1', 'CREATE UNIQUE INDEX `IX_leave_movement_EmployeeId_OperationKey` ON `leave_movement` (`EmployeeId`, `OperationKey`)');
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @ddl = IF(EXISTS(SELECT 1 FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name='leave_movement' AND index_name='IX_leave_movement_LeaveId'), 'SELECT 1', 'CREATE INDEX `IX_leave_movement_LeaveId` ON `leave_movement` (`LeaveId`)');
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

INSERT INTO leave_calendar (Year,Revision,IsApproved,ApprovedBy,SourceUrl,DraftJson,PublishedJson,CreatedDate) SELECT 0,0,0,'','','[]','[]',UTC_TIMESTAMP() WHERE NOT EXISTS(SELECT 1 FROM leave_calendar WHERE Year=0);
