/*
  SchoolBus BE — seed demo full (~10 bản ghi / bảng dữ liệu chính), đúng thứ tự FK.
  Điều kiện: đã chạy migration (dotnet ef database update) trên SQL Server.
  Có thể chạy lại an toàn (idempotent) nhờ WHERE NOT EXISTS theo mã/email/tên ổn định.

  Thứ tự nội bộ:
    Roles -> Campuses -> Packages -> SystemSettings
    -> Users (guardian, driver, teacher, admin, staff) -> Wallets
    -> BusStations -> Buses -> BusRoutes -> BusRouteStations
    -> BusAssignments -> BusSchedules (có thể nhiều ca / cùng xe & ngày trong tuần) -> BusTripProgresses
    -> Students -> StudentBusAssignments
    -> Orders -> Payments -> TransactionLogs
    -> Attendances -> FaceRecognitionLogs -> Notifications
    -> BusDamageReports -> BusTrackings
*/

SET NOCOUNT ON;

/* ========== 1. Roles ========== */
INSERT INTO Roles (Name)
SELECT v.Name
FROM (VALUES
    ('teacher'),
    ('admin'),
    ('student'),
    ('guardian'),
    ('driver'),
    ('staff')
) AS v(Name)
WHERE NOT EXISTS (SELECT 1 FROM Roles r WHERE r.Name = v.Name);

/* ========== 2. Campuses (10) ========== */
INSERT INTO Campuses (Code, Name, Address, Phone, IsActive, ImageUrl)
SELECT v.Code, v.Name, v.Address, v.Phone, v.IsActive, v.ImageUrl
FROM (VALUES
    ('CS001', N'Campus Quận 1', N'123 Nguyễn Huệ, Quận 1', '0901000001', 1, 'https://example.com/campus-1.jpg'),
    ('CS002', N'Campus Quận 3', N'45 Võ Văn Tần, Quận 3', '0901000002', 1, 'https://example.com/campus-2.jpg'),
    ('CS003', N'Campus Bình Thạnh', N'78 Điện Biên Phủ, Bình Thạnh', '0901000003', 1, 'https://example.com/campus-3.jpg'),
    ('CS004', N'Campus Gò Vấp', N'12 Phan Văn Trị, Gò Vấp', '0901000004', 1, 'https://example.com/campus-4.jpg'),
    ('CS005', N'Campus Phú Nhuận', N'89 Nguyễn Văn Trỗi, Phú Nhuận', '0901000005', 1, 'https://example.com/campus-5.jpg'),
    ('CS006', N'Campus Tân Bình', N'56 Hoàng Văn Thụ, Tân Bình', '0901000006', 1, 'https://example.com/campus-6.jpg'),
    ('CS007', N'Campus Thủ Đức', N'34 Võ Văn Ngân, Thủ Đức', '0901000007', 1, 'https://example.com/campus-7.jpg'),
    ('CS008', N'Campus Bình Tân', N'90 Kinh Dương Vương, Bình Tân', '0901000008', 1, 'https://example.com/campus-8.jpg'),
    ('CS009', N'Campus Tân Phú', N'15 Lũy Bán Bích, Tân Phú', '0901000009', 1, 'https://example.com/campus-9.jpg'),
    ('CS010', N'Campus Nhà Bè', N'22 Nguyễn Bình, Nhà Bè', '0901000010', 1, 'https://example.com/campus-10.jpg')
) AS v(Code, Name, Address, Phone, IsActive, ImageUrl)
WHERE NOT EXISTS (SELECT 1 FROM Campuses c WHERE c.Code = v.Code);

/* ========== 3. Packages (10) ========== */
INSERT INTO Packages (Name, Price, DurationDays, Description, Status, CreatedAt, Type, ImageUrl)
SELECT v.Name, v.Price, v.DurationDays, v.Description, v.Status, GETUTCDATE(), v.Type, v.ImageUrl
FROM (VALUES
    (N'Gói 1 tháng', CAST(500000 AS decimal(18,2)), 30, N'Đi xe buýt trường 1 tháng', 'ACTIVE', N'SEMESTER', 'https://example.com/pkg01.jpg'),
    (N'Gói 2 tháng', CAST(950000 AS decimal(18,2)), 60, N'Tiết kiệm 2 tháng', 'ACTIVE', N'SEMESTER', 'https://example.com/pkg02.jpg'),
    (N'Gói 3 tháng', CAST(1350000 AS decimal(18,2)), 90, N'Gói quý', 'ACTIVE', N'SEMESTER', 'https://example.com/pkg03.jpg'),
    (N'Gói học kỳ 1', CAST(2500000 AS decimal(18,2)), 150, N'Theo học kỳ', 'ACTIVE', N'SEMESTER', 'https://example.com/pkg04.jpg'),
    (N'Gói cả năm', CAST(4800000 AS decimal(18,2)), 365, N'Cả năm học', 'ACTIVE', N'YEAR', 'https://example.com/pkg05.jpg'),
    (N'Gói linh hoạt 15 ngày', CAST(280000 AS decimal(18,2)), 15, N'Ngắn hạn', 'ACTIVE', N'SHORT', 'https://example.com/pkg06.jpg'),
    (N'Gói 6 tháng', CAST(2600000 AS decimal(18,2)), 180, N'Ưu đãi nửa năm', 'ACTIVE', N'SEMESTER', 'https://example.com/pkg07.jpg'),
    (N'Gói thử 7 ngày', CAST(120000 AS decimal(18,2)), 7, N'Dùng thử', 'ACTIVE', N'TRIAL', 'https://example.com/pkg08.jpg'),
    (N'Gói VIP 1 tháng', CAST(750000 AS decimal(18,2)), 30, N'Ưu tiên chỗ', 'ACTIVE', N'VIP', 'https://example.com/pkg09.jpg'),
    (N'Gói an toàn 2 tháng', CAST(1100000 AS decimal(18,2)), 60, N'Bảo hiểm kèm theo', 'ACTIVE', N'SEMESTER', 'https://example.com/pkg10.jpg')
) AS v(Name, Price, DurationDays, Description, Status, Type, ImageUrl)
WHERE NOT EXISTS (SELECT 1 FROM Packages p WHERE p.Name = v.Name);

/* ========== 4. SystemSettings (10 key) ========== */
MERGE SystemSettings AS tgt
USING (VALUES
    (N'FaceRecognition.SimilarityThreshold', N'0.8', N'Ngưỡng độ giống face recognition'),
    (N'App.TimeZone', N'SE Asia Standard Time', N'Múi giờ hiển thị'),
    (N'App.SupportPhone', N'1900xxxx', N'Hotline hỗ trợ'),
    (N'Order.GraceDays', N'3', N'Số ngày gia hạn sau hết hạn'),
    (N'Bus.DefaultSpeedKmh', N'40', N'Giới hạn tốc độ mặc định'),
    (N'Notification.BatchSize', N'500', N'Gửi push theo lô'),
    (N'Wallet.MinTopUp', N'10000', N'Số tiền nạp tối thiểu'),
    (N'Wallet.MaxTopUp', N'50000000', N'Số tiền nạp tối đa'),
    (N'Attendance.CheckWindowMinutes', N'30', N'Khung giờ check-in/out'),
    (N'Map.DefaultLat', N'10.7769', N'Vĩ độ mặc định bản đồ')
) AS src([Key], [Value], [Description])
ON tgt.[Key] = src.[Key]
WHEN MATCHED THEN UPDATE SET [Value] = src.[Value], [Description] = src.[Description]
WHEN NOT MATCHED THEN INSERT ([Key], [Value], [Description]) VALUES (src.[Key], src.[Value], src.[Description]);

/* ========== 5. Users: mật khẩu demo giống các script cũ (bcrypt) ========== */
DECLARE @hash NVARCHAR(MAX) = N'$2a$11$IGddWpI.mXVE9pVc0QaT4.i95cvUSey/DNmPV3LijXaHvJhyGEMZS';

INSERT INTO Users (Email, PasswordHash, FullName, Phone, Status, RoleId, CreatedAt)
SELECT v.Email, @hash, v.FullName, v.Phone, 0, r.Id, GETUTCDATE()
FROM (VALUES
    ('guardian01@schoolbus.local', N'Nguyễn Thị Phụ Huynh 01', '0902000001'),
    ('guardian02@schoolbus.local', N'Trần Văn Phụ Huynh 02', '0902000002'),
    ('guardian03@schoolbus.local', N'Lê Thị Phụ Huynh 03', '0902000003'),
    ('guardian04@schoolbus.local', N'Phạm Văn Phụ Huynh 04', '0902000004'),
    ('guardian05@schoolbus.local', N'Hoàng Thị Phụ Huynh 05', '0902000005'),
    ('guardian06@schoolbus.local', N'Võ Văn Phụ Huynh 06', '0902000006'),
    ('guardian07@schoolbus.local', N'Đặng Thị Phụ Huynh 07', '0902000007'),
    ('guardian08@schoolbus.local', N'Bùi Văn Phụ Huynh 08', '0902000008'),
    ('guardian09@schoolbus.local', N'Đỗ Thị Phụ Huynh 09', '0902000009'),
    ('guardian10@schoolbus.local', N'Vũ Văn Phụ Huynh 10', '0902000010')
) AS v(Email, FullName, Phone)
INNER JOIN Roles r ON r.Name = 'guardian'
WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.Email = v.Email);

INSERT INTO Users (Email, PasswordHash, FullName, Phone, Status, RoleId, CreatedAt, DriverLicenseNumber, DriverLicenseClass, DriverLicenseExpiryDate)
SELECT v.Email, @hash, v.FullName, v.Phone, 0, r.Id, GETUTCDATE(), v.LicenseNo, N'B2', DATEADD(YEAR, 2, GETUTCDATE())
FROM (VALUES
    ('driver01@schoolbus.local', N'Tài xế 01', '0903000001', N'GPLX-DRV-01'),
    ('driver02@schoolbus.local', N'Tài xế 02', '0903000002', N'GPLX-DRV-02'),
    ('driver03@schoolbus.local', N'Tài xế 03', '0903000003', N'GPLX-DRV-03'),
    ('driver04@schoolbus.local', N'Tài xế 04', '0903000004', N'GPLX-DRV-04'),
    ('driver05@schoolbus.local', N'Tài xế 05', '0903000005', N'GPLX-DRV-05'),
    ('driver06@schoolbus.local', N'Tài xế 06', '0903000006', N'GPLX-DRV-06'),
    ('driver07@schoolbus.local', N'Tài xế 07', '0903000007', N'GPLX-DRV-07'),
    ('driver08@schoolbus.local', N'Tài xế 08', '0903000008', N'GPLX-DRV-08'),
    ('driver09@schoolbus.local', N'Tài xế 09', '0903000009', N'GPLX-DRV-09'),
    ('driver10@schoolbus.local', N'Tài xế 10', '0903000010', N'GPLX-DRV-10')
) AS v(Email, FullName, Phone, LicenseNo)
INNER JOIN Roles r ON r.Name = 'driver'
WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.Email = v.Email);

INSERT INTO Users (Email, PasswordHash, FullName, Phone, Status, RoleId, CreatedAt)
SELECT v.Email, @hash, v.FullName, v.Phone, 0, r.Id, GETUTCDATE()
FROM (VALUES
    ('teacher01@schoolbus.local', N'Cô giáo đưa đón 01', '0904000001'),
    ('teacher02@schoolbus.local', N'Thầy giáo đưa đón 02', '0904000002'),
    ('teacher03@schoolbus.local', N'Cô giáo đưa đón 03', '0904000003'),
    ('teacher04@schoolbus.local', N'Thầy giáo đưa đón 04', '0904000004'),
    ('teacher05@schoolbus.local', N'Cô giáo đưa đón 05', '0904000005'),
    ('teacher06@schoolbus.local', N'Thầy giáo đưa đón 06', '0904000006'),
    ('teacher07@schoolbus.local', N'Cô giáo đưa đón 07', '0904000007'),
    ('teacher08@schoolbus.local', N'Thầy giáo đưa đón 08', '0904000008'),
    ('teacher09@schoolbus.local', N'Cô giáo đưa đón 09', '0904000009'),
    ('teacher10@schoolbus.local', N'Thầy giáo đưa đón 10', '0904000010')
) AS v(Email, FullName, Phone)
INNER JOIN Roles r ON r.Name = 'teacher'
WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.Email = v.Email);

INSERT INTO Users (Email, PasswordHash, FullName, Phone, Status, RoleId, CreatedAt)
SELECT v.Email, @hash, v.FullName, v.Phone, 0, r.Id, GETUTCDATE()
FROM (VALUES
    ('admin@schoolbus.local', N'Quản trị hệ thống', '0905000000')
) AS v(Email, FullName, Phone)
INNER JOIN Roles r ON r.Name = 'admin'
WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.Email = v.Email);

INSERT INTO Users (Email, PasswordHash, FullName, Phone, Status, RoleId, CreatedAt)
SELECT v.Email, @hash, v.FullName, v.Phone, 0, r.Id, GETUTCDATE()
FROM (VALUES
    ('staff01@schoolbus.local', N'Nhân sự vận hành 01', '0906000001'),
    ('staff02@schoolbus.local', N'Nhân sự vận hành 02', '0906000002'),
    ('staff03@schoolbus.local', N'Nhân sự vận hành 03', '0906000003'),
    ('staff04@schoolbus.local', N'Nhân sự vận hành 04', '0906000004'),
    ('staff05@schoolbus.local', N'Nhân sự vận hành 05', '0906000005'),
    ('staff06@schoolbus.local', N'Nhân sự vận hành 06', '0906000006'),
    ('staff07@schoolbus.local', N'Nhân sự vận hành 07', '0906000007'),
    ('staff08@schoolbus.local', N'Nhân sự vận hành 08', '0906000008'),
    ('staff09@schoolbus.local', N'Nhân sự vận hành 09', '0906000009'),
    ('staff10@schoolbus.local', N'Nhân sự vận hành 10', '0906000010')
) AS v(Email, FullName, Phone)
INNER JOIN Roles r ON r.Name = 'staff'
WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.Email = v.Email);

/* ========== 6. Wallets (một user một ví; phụ huynh có số dư demo) ========== */
INSERT INTO Wallets (UserId, Balance)
SELECT u.Id,
       CASE WHEN r.Name = N'guardian' THEN CAST(5000000 AS decimal(18,2)) ELSE CAST(0 AS decimal(18,2)) END
FROM Users u
INNER JOIN Roles r ON r.Id = u.RoleId
WHERE u.Email LIKE N'%@schoolbus.local'
  AND r.Name IN (N'guardian', N'driver', N'teacher', N'admin', N'staff')
  AND NOT EXISTS (SELECT 1 FROM Wallets w WHERE w.UserId = u.Id);

/* ========== 7. BusStations (10) ========== */
INSERT INTO BusStations (Name, Address, Description, IsEnabled, Latitude, Longitude)
SELECT v.Name, v.Address, v.Description, 1, v.Lat, v.Lng
FROM (VALUES
    (N'Trạm BS001', N'Quận 1', N'Điểm đón trung tâm', 10.7769, 106.7009),
    (N'Trạm BS002', N'Quận 3', N'Điểm đón Q3', 10.7873, 106.6884),
    (N'Trạm BS003', N'Bình Thạnh', N'Điểm đón BT', 10.8112, 106.7092),
    (N'Trạm BS004', N'Gò Vấp', N'Điểm đón GV', 10.8398, 106.6660),
    (N'Trạm BS005', N'Phú Nhuận', N'Điểm đón PN', 10.7992, 106.6800),
    (N'Trạm BS006', N'Tân Bình', N'Điểm đón TB', 10.8019, 106.6525),
    (N'Trạm BS007', N'Thủ Đức', N'Điểm đón TD', 10.8494, 106.7537),
    (N'Trạm BS008', N'Bình Tân', N'Điểm đón BTan', 10.7650, 106.6034),
    (N'Trạm BS009', N'Tân Phú', N'Điểm đón TPhu', 10.7905, 106.6281),
    (N'Trạm BS010', N'Nhà Bè', N'Điểm đón NB', 10.6954, 106.7041),
    /* Trạm dọc một tuyến Campus Quận 1 (CS001) — thứ tự địa lý gần đúng */
    (N'Trạm CS001-01', N'Quận 1', N'Điểm đầu — gần Bến Thành', 10.7725, 106.6980),
    (N'Trạm CS001-02', N'Quận 1', N'Lê Lợi / Đồng Khởi', 10.7738, 106.7012),
    (N'Trạm CS001-03', N'Quận 1', N'Nguyễn Huệ (giữa)', 10.7749, 106.7040),
    (N'Trạm CS001-04', N'Quận 1', N'Pasteur — công viên', 10.7762, 106.7065),
    (N'Trạm CS001-05', N'Quận 1', N'Điểm cuối — gần campus', 10.7775, 106.7090)
) AS v(Name, Address, Description, Lat, Lng)
WHERE NOT EXISTS (SELECT 1 FROM BusStations s WHERE s.Name = v.Name);

/* ========== 8. Buses (10) ========== */
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
    SELECT 1 FROM Buses b
    WHERE b.LicensePlate = v.LicensePlate OR (b.BusNumber IS NOT NULL AND b.BusNumber = v.BusNumber)
);

/* ========== 9. BusRoutes (10 — mỗi campus 1 tuyến demo) ========== */
INSERT INTO BusRoutes (Name, IsEnabled, CampusId)
SELECT v.RouteName, 1, c.Id
FROM (VALUES
    (N'Tuyến demo CS001', 'CS001'),
    (N'Tuyến demo CS002', 'CS002'),
    (N'Tuyến demo CS003', 'CS003'),
    (N'Tuyến demo CS004', 'CS004'),
    (N'Tuyến demo CS005', 'CS005'),
    (N'Tuyến demo CS006', 'CS006'),
    (N'Tuyến demo CS007', 'CS007'),
    (N'Tuyến demo CS008', 'CS008'),
    (N'Tuyến demo CS009', 'CS009'),
    (N'Tuyến demo CS010', 'CS010')
) AS v(RouteName, CampusCode)
INNER JOIN Campuses c ON c.Code = v.CampusCode
WHERE NOT EXISTS (
    SELECT 1 FROM BusRoutes r WHERE r.Name = v.RouteName AND r.CampusId = c.Id
);

/* ========== 10. BusRouteStations ========== */
/* Tuyến CS001: một route đi qua nhiều trạm (OrderIndex 1..5). Các campus khác: 1 trạm / tuyến. */
/* Bỏ liên kết cũ seed 1-trạm (CS001 + BS001) nếu còn — tránh trùng logic với tuyến nhiều điểm */
DELETE brs
FROM BusRouteStations brs
INNER JOIN BusRoutes r ON r.Id = brs.RouteId
INNER JOIN BusStations s ON s.Id = brs.StationId
WHERE r.Name = N'Tuyến demo CS001'
  AND s.Name = N'Trạm BS001';

INSERT INTO BusRouteStations (RouteId, StationId, OrderIndex)
SELECT r.Id, bs.Id, v.Ord
FROM BusRoutes r
INNER JOIN Campuses c ON c.Id = r.CampusId AND c.Code = N'CS001'
CROSS JOIN (VALUES
    (N'Trạm CS001-01', 1),
    (N'Trạm CS001-02', 2),
    (N'Trạm CS001-03', 3),
    (N'Trạm CS001-04', 4),
    (N'Trạm CS001-05', 5)
) AS v(StationName, Ord)
INNER JOIN BusStations bs ON bs.Name = v.StationName
WHERE r.Name = N'Tuyến demo CS001'
  AND NOT EXISTS (
      SELECT 1 FROM BusRouteStations x
      WHERE x.RouteId = r.Id AND x.StationId = bs.Id AND x.OrderIndex = v.Ord
  );

INSERT INTO BusRouteStations (RouteId, StationId, OrderIndex)
SELECT r.Id, bs.Id, 1
FROM (VALUES
    (N'Tuyến demo CS002', N'Trạm BS002'),
    (N'Tuyến demo CS003', N'Trạm BS003'),
    (N'Tuyến demo CS004', N'Trạm BS004'),
    (N'Tuyến demo CS005', N'Trạm BS005'),
    (N'Tuyến demo CS006', N'Trạm BS006'),
    (N'Tuyến demo CS007', N'Trạm BS007'),
    (N'Tuyến demo CS008', N'Trạm BS008'),
    (N'Tuyến demo CS009', N'Trạm BS009'),
    (N'Tuyến demo CS010', N'Trạm BS010')
) AS v(RouteName, StationName)
INNER JOIN BusRoutes r ON r.Name = v.RouteName
INNER JOIN BusStations bs ON bs.Name = v.StationName
WHERE NOT EXISTS (
    SELECT 1 FROM BusRouteStations x WHERE x.RouteId = r.Id AND x.StationId = bs.Id
);

/* ========== 11. BusAssignments (Bus #i — Driver #i — Teacher #i) ========== */
;WITH B AS (
    SELECT b.Id, ROW_NUMBER() OVER (ORDER BY b.BusNumber) AS rn FROM Buses b WHERE b.BusNumber LIKE 'BUS-%'
),
D AS (
    SELECT u.Id, ROW_NUMBER() OVER (ORDER BY u.Email) AS rn
    FROM Users u INNER JOIN Roles r ON r.Id = u.RoleId
    WHERE r.Name = 'driver' AND u.Email LIKE 'driver%@schoolbus.local'
),
T AS (
    SELECT u.Id, ROW_NUMBER() OVER (ORDER BY u.Email) AS rn
    FROM Users u INNER JOIN Roles r ON r.Id = u.RoleId
    WHERE r.Name = 'teacher' AND u.Email LIKE 'teacher%@schoolbus.local'
)
INSERT INTO BusAssignments (BusId, DriverId, TeacherId)
SELECT B.Id, D.Id, T.Id
FROM B
INNER JOIN D ON B.rn = D.rn
INNER JOIN T ON B.rn = T.rn
WHERE NOT EXISTS (SELECT 1 FROM BusAssignments ba WHERE ba.BusId = B.Id);

/* ========== 12. BusSchedules ========== */
/*
  GET /api/BusTripProgress/DriverSchedules lọc theo DayOfWeek của rideDate (giống .NET: 0=Sunday … 6=Saturday).
  Ví dụ rideDate=2026-04-19 là Chủ nhật -> DayOfWeek=0.
  BUS-01 (tài xế driver01): 2 lịch cùng ngày trong tuần — sáng CS001, chiều CS002 — để test isRecommended / nhiều ca.
*/
INSERT INTO BusSchedules (BusId, RouteId, StartDate, EndDate, StartTime, EndTime, DayOfWeek, ShiftType, IsActive)
SELECT b.Id, r.Id,
       CAST(GETDATE() AS date),
       DATEADD(MONTH, 6, CAST(GETDATE() AS date)),
       CAST('07:00:00' AS time),
       CAST('08:30:00' AS time),
       0,
       N'PICKUP',
       1
FROM (VALUES
    ('BUS-01', N'Tuyến demo CS001'),
    ('BUS-02', N'Tuyến demo CS002'),
    ('BUS-03', N'Tuyến demo CS003'),
    ('BUS-04', N'Tuyến demo CS004'),
    ('BUS-05', N'Tuyến demo CS005'),
    ('BUS-06', N'Tuyến demo CS006'),
    ('BUS-07', N'Tuyến demo CS007'),
    ('BUS-08', N'Tuyến demo CS008'),
    ('BUS-09', N'Tuyến demo CS009'),
    ('BUS-10', N'Tuyến demo CS010')
) AS v(BusNumber, RouteName)
INNER JOIN Buses b ON b.BusNumber = v.BusNumber
INNER JOIN BusRoutes r ON r.Name = v.RouteName
WHERE NOT EXISTS (
    SELECT 1 FROM BusSchedules s WHERE s.BusId = b.Id AND s.RouteId = r.Id
);

INSERT INTO BusSchedules (BusId, RouteId, StartDate, EndDate, StartTime, EndTime, DayOfWeek, ShiftType, IsActive)
SELECT b.Id, r.Id,
       CAST(GETDATE() AS date),
       DATEADD(MONTH, 6, CAST(GETDATE() AS date)),
       CAST('16:00:00' AS time),
       CAST('17:30:00' AS time),
       0,
       N'DROPOFF',
       1
FROM Buses b
INNER JOIN BusRoutes r ON r.Name = N'Tuyến demo CS002'
WHERE b.BusNumber = N'BUS-01'
  AND NOT EXISTS (
      SELECT 1 FROM BusSchedules s
      WHERE s.BusId = b.Id AND s.RouteId = r.Id AND s.StartTime = CAST('16:00:00' AS time)
  );

/* ========== 12b. BusTripProgresses (mỗi BusSchedule: trạm đầu tiên — chỉ ArrivedAt, schema không còn DepartedAt) ========== */
IF OBJECT_ID(N'[dbo].[BusTripProgresses]', N'U') IS NOT NULL
BEGIN
    INSERT INTO BusTripProgresses (BusId, BusScheduleId, RouteId, StationId, RideDate, OrderIndex, ArrivedAt)
    SELECT
        bs.BusId,
        bs.Id,
        bs.RouteId,
        firstStop.StationId,
        CAST(GETDATE() AS date),
        firstStop.OrderIndex,
        DATEADD(MINUTE, -30, GETUTCDATE())
    FROM BusSchedules bs
    CROSS APPLY (
        SELECT TOP (1) brs.StationId, brs.OrderIndex
        FROM BusRouteStations brs
        WHERE brs.RouteId = bs.RouteId
        ORDER BY brs.OrderIndex ASC, brs.Id ASC
    ) AS firstStop
    INNER JOIN Buses b ON b.Id = bs.BusId
    WHERE b.BusNumber IN (
        'BUS-01', 'BUS-02', 'BUS-03', 'BUS-04', 'BUS-05',
        'BUS-06', 'BUS-07', 'BUS-08', 'BUS-09', 'BUS-10'
    )
      AND NOT EXISTS (
          SELECT 1
          FROM BusTripProgresses p
          WHERE p.BusScheduleId = bs.Id
            AND p.RideDate = CAST(GETDATE() AS date)
            AND p.OrderIndex = firstStop.OrderIndex
      );
END
ELSE
    PRINT N'Bỏ qua BusTripProgresses — chưa có bảng (chạy migration trước).';

/* ========== 13. Students (10 — mã học sinh duy nhất) ========== */
INSERT INTO Students (StudentCode, FullName, DateOfBirth, Gender, GuardianId, Status, CampusId)
SELECT v.StudentCode, v.FullName, v.DateOfBirth, v.Gender, u.Id, 0, c.Id
FROM (VALUES
    (N'STU20260001', N'Phạm Minh Khang', CAST('2017-05-10' AS datetime2), 'Male', 'guardian01@schoolbus.local', 'CS001'),
    (N'STU20260002', N'Phạm Bảo Ngọc', CAST('2019-09-21' AS datetime2), 'Female', 'guardian01@schoolbus.local', 'CS001'),
    (N'STU20260003', N'Trần Gia Huy', CAST('2018-12-03' AS datetime2), 'Male', 'guardian02@schoolbus.local', 'CS002'),
    (N'STU20260004', N'Lê Khánh An', CAST('2016-07-15' AS datetime2), 'Female', 'guardian03@schoolbus.local', 'CS003'),
    (N'STU20260005', N'Hoàng Gia Bảo', CAST('2017-01-20' AS datetime2), 'Male', 'guardian04@schoolbus.local', 'CS004'),
    (N'STU20260006', N'Võ Thuỳ Linh', CAST('2018-04-12' AS datetime2), 'Female', 'guardian05@schoolbus.local', 'CS005'),
    (N'STU20260007', N'Đặng An Khang', CAST('2019-11-30' AS datetime2), 'Male', 'guardian06@schoolbus.local', 'CS006'),
    (N'STU20260008', N'Bùi Mai Chi', CAST('2016-09-09' AS datetime2), 'Female', 'guardian07@schoolbus.local', 'CS007'),
    (N'STU20260009', N'Đỗ Quốc Huy', CAST('2017-08-08' AS datetime2), 'Male', 'guardian08@schoolbus.local', 'CS008'),
    (N'STU20260010', N'Vũ Bảo Châu', CAST('2018-02-14' AS datetime2), 'Female', 'guardian10@schoolbus.local', 'CS010')
) AS v(StudentCode, FullName, DateOfBirth, Gender, GuardianEmail, CampusCode)
INNER JOIN Users u ON u.Email = v.GuardianEmail
INNER JOIN Campuses c ON c.Code = v.CampusCode
WHERE NOT EXISTS (SELECT 1 FROM Students s WHERE s.StudentCode = v.StudentCode);

/* ========== 14. StudentBusAssignments (10) ========== */
INSERT INTO StudentBusAssignments (StudentId, RouteId, PickupStationId, DropOffStationId, RideDate, Note)
SELECT s.Id, r.Id, st1.Id, st2.Id, CAST(GETDATE() AS date), N'Phân công demo'
FROM (VALUES
    (N'STU20260001', N'Tuyến demo CS001'),
    (N'STU20260002', N'Tuyến demo CS001'),
    (N'STU20260003', N'Tuyến demo CS002'),
    (N'STU20260004', N'Tuyến demo CS003'),
    (N'STU20260005', N'Tuyến demo CS004'),
    (N'STU20260006', N'Tuyến demo CS005'),
    (N'STU20260007', N'Tuyến demo CS006'),
    (N'STU20260008', N'Tuyến demo CS007'),
    (N'STU20260009', N'Tuyến demo CS008'),
    (N'STU20260010', N'Tuyến demo CS010')
) AS v(StudentCode, RouteName)
INNER JOIN Students s ON s.StudentCode = v.StudentCode
INNER JOIN BusRoutes r ON r.Name = v.RouteName
CROSS APPLY (
    SELECT TOP (1) brs.StationId
    FROM BusRouteStations brs
    WHERE brs.RouteId = r.Id
    ORDER BY brs.OrderIndex, brs.Id
) p
CROSS APPLY (
    SELECT TOP (1) brs.StationId
    FROM BusRouteStations brs
    WHERE brs.RouteId = r.Id
    ORDER BY brs.OrderIndex DESC, brs.Id DESC
) d
INNER JOIN BusStations st1 ON st1.Id = p.StationId
INNER JOIN BusStations st2 ON st2.Id = d.StationId
WHERE NOT EXISTS (
    SELECT 1 FROM StudentBusAssignments x WHERE x.StudentId = s.Id AND x.RouteId = r.Id
);

/* ========== 15. Orders (10) — OrderStatus: 0 PENDING 1 PAID ... ========== */
INSERT INTO Orders (GuardianId, StudentId, PackageId, BusRouteId, Status, StartDate, EndDate, PaidAt, ExpiredAt, CreatedAt)
SELECT u.Id, s.Id, p.Id, r.Id, 1,
       CAST(GETDATE() AS date),
       DATEADD(DAY, p.DurationDays, CAST(GETDATE() AS date)),
       GETUTCDATE(),
       NULL,
       GETUTCDATE()
FROM (VALUES
    (N'STU20260001', N'guardian01@schoolbus.local', N'Gói 1 tháng', N'Tuyến demo CS001'),
    (N'STU20260002', N'guardian01@schoolbus.local', N'Gói 2 tháng', N'Tuyến demo CS001'),
    (N'STU20260003', N'guardian02@schoolbus.local', N'Gói 3 tháng', N'Tuyến demo CS002'),
    (N'STU20260004', N'guardian03@schoolbus.local', N'Gói học kỳ 1', N'Tuyến demo CS003'),
    (N'STU20260005', N'guardian04@schoolbus.local', N'Gói cả năm', N'Tuyến demo CS004'),
    (N'STU20260006', N'guardian05@schoolbus.local', N'Gói linh hoạt 15 ngày', N'Tuyến demo CS005'),
    (N'STU20260007', N'guardian06@schoolbus.local', N'Gói 6 tháng', N'Tuyến demo CS006'),
    (N'STU20260008', N'guardian07@schoolbus.local', N'Gói thử 7 ngày', N'Tuyến demo CS007'),
    (N'STU20260009', N'guardian08@schoolbus.local', N'Gói VIP 1 tháng', N'Tuyến demo CS008'),
    (N'STU20260010', N'guardian10@schoolbus.local', N'Gói an toàn 2 tháng', N'Tuyến demo CS010')
) AS v(StudentCode, GuardianEmail, PackageName, RouteName)
INNER JOIN Students s ON s.StudentCode = v.StudentCode
INNER JOIN Users u ON u.Email = v.GuardianEmail AND u.Id = s.GuardianId
INNER JOIN Packages p ON p.Name = v.PackageName
INNER JOIN BusRoutes r ON r.Name = v.RouteName
WHERE NOT EXISTS (
    SELECT 1 FROM Orders o
    WHERE o.StudentId = s.Id AND o.PackageId = p.Id AND o.Status = 1
);

/* ========== 16. Payments (10) ========== */
INSERT INTO Payments (OrderId, Method, Amount, Status, PaidAt)
SELECT o.Id, N'WALLET', pk.Price, 0, GETUTCDATE()
FROM Orders o
INNER JOIN Packages pk ON pk.Id = o.PackageId
INNER JOIN Students s ON s.Id = o.StudentId
WHERE s.StudentCode LIKE N'STU202600%'
  AND o.Status = 1
  AND NOT EXISTS (SELECT 1 FROM Payments p WHERE p.OrderId = o.Id);

/* ========== 17. TransactionLogs (10) ========== */
INSERT INTO TransactionLogs (OrderId, Method, Amount, Status, PaidAt, OldBalance, NewBalance, Sender, Receiver, Description, Code)
SELECT o.Id, N'WALLET', pk.Price, N'SUCCESS', GETUTCDATE(),
       CAST(5000000 AS decimal(18,2)) + pk.Price,
       CAST(5000000 AS decimal(18,2)),
       gu.Email, N'SYSTEM', N'Thanh toán gói demo seed', N'SEED-TXN-' + RIGHT(N'000000000' + CAST(o.Id AS NVARCHAR(20)), 9)
FROM Orders o
INNER JOIN Packages pk ON pk.Id = o.PackageId
INNER JOIN Users gu ON gu.Id = o.GuardianId
INNER JOIN Students s ON s.Id = o.StudentId
WHERE s.StudentCode LIKE N'STU202600%'
  AND o.Status = 1
  AND NOT EXISTS (SELECT 1 FROM TransactionLogs t WHERE t.OrderId = o.Id AND t.Method = N'WALLET');

/* ========== 18. Attendances (10) ========== */
INSERT INTO Attendances (StudentId, BusId, Date, CheckInTime, CheckOutTime, CheckInStationId, CheckOutStationId, Note, Method, Status)
SELECT s.Id, b.Id, CAST(GETDATE() AS date),
       CAST('07:00:00' AS time), CAST('16:30:00' AS time),
       st1.Id, st2.Id, N'Điểm danh demo', 0, 0
FROM (VALUES
    (N'STU20260001', 'BUS-01'),
    (N'STU20260002', 'BUS-01'),
    (N'STU20260003', 'BUS-02'),
    (N'STU20260004', 'BUS-03'),
    (N'STU20260005', 'BUS-04'),
    (N'STU20260006', 'BUS-05'),
    (N'STU20260007', 'BUS-06'),
    (N'STU20260008', 'BUS-07'),
    (N'STU20260009', 'BUS-08'),
    (N'STU20260010', 'BUS-09')
) AS v(StudentCode, BusNumber)
INNER JOIN Students s ON s.StudentCode = v.StudentCode
INNER JOIN Buses b ON b.BusNumber = v.BusNumber
CROSS APPLY (SELECT TOP (1) bs.Id FROM BusStations bs ORDER BY bs.Id) st1
CROSS APPLY (SELECT TOP (1) bs.Id FROM BusStations bs ORDER BY bs.Id DESC) st2
WHERE NOT EXISTS (
    SELECT 1 FROM Attendances a
    WHERE a.StudentId = s.Id AND a.Date = CAST(GETDATE() AS date) AND a.BusId = b.Id
);

/* ========== 19. FaceRecognitionLogs (10) ========== */
INSERT INTO FaceRecognitionLogs (StudentId, ConfidenceScore, ImageUrl, RecognizedAt)
SELECT s.Id, CAST(0.95 AS decimal(10,6)), N'https://example.com/face-seed.jpg', GETUTCDATE()
FROM Students s
WHERE s.StudentCode LIKE N'STU202600%'
  AND NOT EXISTS (
      SELECT 1 FROM FaceRecognitionLogs f
      WHERE f.StudentId = s.Id AND f.ImageUrl = N'https://example.com/face-seed.jpg'
  );

/* ========== 20. Notifications (10) ========== */
INSERT INTO Notifications (UserId, Message, Type, CreatedAt, IsRead)
SELECT u.Id, N'Thông báo demo: lịch xe tuần này.', N'INFO', GETUTCDATE(), 0
FROM Users u
INNER JOIN Roles r ON r.Id = u.RoleId
WHERE r.Name = N'guardian' AND u.Email LIKE N'guardian%@schoolbus.local'
  AND NOT EXISTS (
      SELECT 1 FROM Notifications n
      WHERE n.UserId = u.Id AND n.Message = N'Thông báo demo: lịch xe tuần này.'
  );

/* ========== 21. BusDamageReports (10) ========== */
INSERT INTO BusDamageReports (BusId, ReportedByUserId, Title, Description, Status, ReportedAt)
SELECT b.Id, u.Id, N'Báo cáo hư hỏng demo', N'Vết xước nhẹ thân xe', N'PENDING', GETUTCDATE()
FROM Buses b
INNER JOIN Users u ON u.Email = N'teacher01@schoolbus.local'
WHERE b.BusNumber IN ('BUS-01','BUS-02','BUS-03','BUS-04','BUS-05','BUS-06','BUS-07','BUS-08','BUS-09','BUS-10')
  AND NOT EXISTS (
      SELECT 1 FROM BusDamageReports d WHERE d.BusId = b.Id AND d.Title = N'Báo cáo hư hỏng demo'
  );

/* ========== 22. BusTrackings (10) ========== */
INSERT INTO BusTrackings (BusId, Latitude, Longitude, Speed, TrackedAt)
SELECT b.Id, 10.7769, 106.7009, 25.5, GETUTCDATE()
FROM Buses b
WHERE b.BusNumber IN ('BUS-01','BUS-02','BUS-03','BUS-04','BUS-05','BUS-06','BUS-07','BUS-08','BUS-09','BUS-10')
  AND NOT EXISTS (
      SELECT 1 FROM BusTrackings t WHERE t.BusId = b.Id AND ABS(CAST(t.Speed AS float) - 25.5) < 0.01
  );

PRINT N'Seed full_app hoàn tất (hoặc đã tồn tại — bỏ qua trùng).';
