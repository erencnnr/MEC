SELECT `id`, `leave_type`, `leave_type_id`
FROM `leaves`
WHERE `leave_type_id` IS NULL;

ALTER TABLE `leaves`
    DROP COLUMN `leave_type`;
