-- İzin akışı EmployeeId alanında employee_portal.Id kullanır.
-- Mevcut kayıtların employee_portal karşılığı kontrol edilmeden bu script çalıştırılmamalıdır.

ALTER TABLE `leaves`
    DROP FOREIGN KEY `FK_leave_employee_EmployeeId`;

ALTER TABLE `leaves`
    ADD CONSTRAINT `FK_leaves_employee_portal_EmployeeId`
        FOREIGN KEY (`EmployeeId`) REFERENCES `employee_portal` (`Id`)
        ON DELETE RESTRICT;
