INSERT INTO Buses (LicensePlate, Capacity, Status, BusNumber, ImageUrl, Color, BusType)
SELECT v.LicensePlate, v.Capacity, v.Status, v.BusNumber, v.ImageUrl, v.Color, v.BusType
FROM (VALUES
    ('51A-12345', 30, 'ACTIVE', 'BUS-01', 'https://example.com/bus-01.jpg', 'Yellow', '45-seat'),
    ('51A-12346', 35, 'ACTIVE', 'BUS-02', 'https://example.com/bus-02.jpg', 'Yellow', '45-seat'),
    ('51A-12347', 25, 'ACTIVE', 'BUS-03', 'https://example.com/bus-03.jpg', 'White', '29-seat'),
    ('51A-12348', 40, 'MAINTENANCE', 'BUS-04', 'https://example.com/bus-04.jpg', 'Blue', '50-seat'),
    ('51A-12349', 20, 'DISABLED', 'BUS-05', 'https://example.com/bus-05.jpg', 'Red', '16-seat')
) AS v(LicensePlate, Capacity, Status, BusNumber, ImageUrl, Color, BusType)
WHERE NOT EXISTS (
    SELECT 1
    FROM Buses b
    WHERE b.LicensePlate = v.LicensePlate
       OR (b.BusNumber IS NOT NULL AND b.BusNumber = v.BusNumber)
);

INSERT INTO BusRoutes (Name, IsEnabled, CampusId)
SELECT v.RouteName, v.IsEnabled, c.Id
FROM (VALUES
    ('Tuyến Campus Quận 1 - Sáng', 1, 'CS001'),
    ('Tuyến Campus Quận 1 - Chiều', 1, 'CS001'),
    ('Tuyến Campus Quận 3 - Sáng', 1, 'CS002'),
    ('Tuyến Campus Bình Thạnh - Sáng', 1, 'CS003')
) AS v(RouteName, IsEnabled, CampusCode)
INNER JOIN Campuses c ON c.Code = v.CampusCode
WHERE NOT EXISTS (
    SELECT 1
    FROM BusRoutes r
    WHERE r.Name = v.RouteName
      AND r.CampusId = c.Id
);

INSERT INTO BusAssignments (BusId, DriverId, TeacherId, RouteId, ActiveDate)
SELECT
    b.Id,
    driverUser.Id,
    teacherUser.Id,
    r.Id,
    CAST(GETDATE() AS date)
FROM (VALUES
    ('BUS-01', 'Tuyến Campus Quận 1 - Sáng', 'CS001'),
    ('BUS-02', 'Tuyến Campus Quận 1 - Chiều', 'CS001'),
    ('BUS-03', 'Tuyến Campus Quận 3 - Sáng', 'CS002'),
    ('BUS-04', 'Tuyến Campus Bình Thạnh - Sáng', 'CS003')
) AS v(BusNumber, RouteName, CampusCode)
INNER JOIN Buses b ON b.BusNumber = v.BusNumber
INNER JOIN Campuses c ON c.Code = v.CampusCode
INNER JOIN BusRoutes r ON r.Name = v.RouteName AND r.CampusId = c.Id
CROSS APPLY (
    SELECT TOP 1 u.Id
    FROM Users u
    INNER JOIN Roles roleDriver ON roleDriver.Id = u.RoleId
    WHERE roleDriver.Name = 'driver'
    ORDER BY u.Id
) AS driverUser
CROSS APPLY (
    SELECT TOP 1 u.Id
    FROM Users u
    INNER JOIN Roles roleTeacher ON roleTeacher.Id = u.RoleId
    WHERE roleTeacher.Name = 'teacher'
    ORDER BY u.Id
) AS teacherUser
WHERE NOT EXISTS (
    SELECT 1
    FROM BusAssignments ba
    WHERE ba.BusId = b.Id
      AND ba.RouteId = r.Id
);
