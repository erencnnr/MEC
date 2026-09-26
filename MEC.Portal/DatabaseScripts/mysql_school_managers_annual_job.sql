-- Run once before deploying the new Portal. Back up the database first.
ALTER TABLE employee_portal
    ADD COLUMN annual_leave_processed_through DATE NULL;
ALTER TABLE location
    ADD COLUMN manager_employee_portal_id INT NULL,
    ADD CONSTRAINT fk_location_manager FOREIGN KEY (manager_employee_portal_id)
        REFERENCES employee_portal (Id) ON DELETE RESTRICT;

-- Preserve unambiguous existing assignments. Conflicting assignments are listed below
-- and must be resolved in Administration > Settings > Schools.
UPDATE location l
JOIN (
    SELECT epl.location_id, MIN(ep.Id) AS manager_id
    FROM employee_portal_location epl
    JOIN employee_portal ep ON ep.Id = epl.employee_portal_id
    WHERE ep.IsManager = 1 AND ep.IsDeleted = 0
    GROUP BY epl.location_id
    HAVING COUNT(DISTINCT ep.Id) = 1
) previous ON previous.location_id = l.Id
SET l.manager_employee_portal_id = previous.manager_id;

SELECT l.Id, l.name AS school_needing_manager
FROM location l WHERE l.manager_employee_portal_id IS NULL;

-- Existing balances are the opening values. The first job includes today's anniversary,
-- but never credits all historical anniversaries to employees without reconciliation.
UPDATE employee_portal
SET annual_leave_processed_through = DATE(UTC_TIMESTAMP() + INTERVAL 3 HOUR) - INTERVAL 1 DAY
WHERE annual_leave_processed_through IS NULL;
