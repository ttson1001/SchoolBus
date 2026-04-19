/* Xe, tuyến, phân công. BusAssignments theo model hiện tại: chỉ BusId, DriverId, TeacherId (không có RouteId). */

INSERT INTO Buses (LicensePlate, Capacity, Status, BusNumber, ImageUrl, Color, BusType)
SELECT v.LicensePlate, v.Capacity, v.Status, v.BusNumber, v.ImageUrl, v.Color, v.BusType
FROM (VALUES
    ('51A-12345', 30, 'ACTIVE', 'BUS-01', 'https://example.com/bus-01.jpg', 'Yellow', '45-seat'),
    ('51A-12346', 35, 'ACTIVE', 'BUS-02', 'https://example.com/bus-02.jpg', 'Yellow', '45-seat'),
    ('51A-12347', 25, 'ACTIVE', 'BUS-03', 'https://example.com/bus-03.jpg', 'White', '29-seat'),
    ('51A-12348', 40, 'ACTIVE', 'BUS-04', 'https://example.com/bus-04.jpg', 'Blue', '50-seat'),
    ('51A-12349', 20, 'ACTIVE', 'BUS-05', 'https://example.com/bus-05.jpg', 'Red', '16-seat'),
    ('51A-12350', 32, 'ACTIVE', 'BUS-06', 'https://example.com/bus-06.jpg', 'Yellow', '45-seat'),
    ('51A-12351', 28, 'ACTIVE', 'BUS-07', 'https://example.com/bus-07.jpg', 'White', '29-seat'),
    ('51A-12352', 36, 'ACTIVE', 'BUS-08', 'https://example.com/bus-08.jpg', 'Blue', '45-seat'),
    ('51A-12353', 24, 'MAINTENANCE', 'BUS-09', 'https://example.com/bus-09.jpg', 'Gray', '16-seat'),
    ('51A-12354', 38, 'ACTIVE', 'BUS-10', 'https://example.com/bus-10.jpg', 'Yellow', '50-seat')
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
    (N'Tuyến demo CS001', 1, 'CS001'),
    (N'Tuyến demo CS002', 1, 'CS002'),
    (N'Tuyến demo CS003', 1, 'CS003'),
    (N'Tuyến demo CS004', 1, 'CS004'),
    (N'Tuyến demo CS005', 1, 'CS005'),
    (N'Tuyến demo CS006', 1, 'CS006'),
    (N'Tuyến demo CS007', 1, 'CS007'),
    (N'Tuyến demo CS008', 1, 'CS008'),
    (N'Tuyến demo CS009', 1, 'CS009'),
    (N'Tuyến demo CS010', 1, 'CS010')
) AS v(RouteName, IsEnabled, CampusCode)
INNER JOIN Campuses c ON c.Code = v.CampusCode
WHERE NOT EXISTS (
    SELECT 1
    FROM BusRoutes r
    WHERE r.Name = v.RouteName
      AND r.CampusId = c.Id
);

/* Cần user tài xế + giáo viên (seed_full_app.sql hoặc chèn thủ công) trước bước này */
;WITH B AS (
    SELECT b.Id, ROW_NUMBER() OVER (ORDER BY b.BusNumber) AS rn
    FROM Buses b
    WHERE b.BusNumber LIKE N'BUS-%'
),
D AS (
    SELECT u.Id, ROW_NUMBER() OVER (ORDER BY u.Email) AS rn
    FROM Users u
    INNER JOIN Roles r ON r.Id = u.RoleId
    WHERE r.Name = 'driver' AND u.Email LIKE N'driver%@schoolbus.local'
),
T AS (
    SELECT u.Id, ROW_NUMBER() OVER (ORDER BY u.Email) AS rn
    FROM Users u
    INNER JOIN Roles r ON r.Id = u.RoleId
    WHERE r.Name = 'teacher' AND u.Email LIKE N'teacher%@schoolbus.local'
)
INSERT INTO BusAssignments (BusId, DriverId, TeacherId)
SELECT B.Id, D.Id, T.Id
FROM B
INNER JOIN D ON B.rn = D.rn
INNER JOIN T ON B.rn = T.rn
WHERE NOT EXISTS (SELECT 1 FROM BusAssignments ba WHERE ba.BusId = B.Id);
