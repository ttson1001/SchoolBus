/*
Seed du lieu thuc te cho SchoolBus (SQL Server)
- Co su dung chuoi Unicode N'...'
- Thiet ke idempotent co ban: IF NOT EXISTS cho du lieu chinh
- Uu tien du lieu theo luong co the xay ra trong app

Luu y:
1) PasswordHash duoi day la mau seed. Neu can dang nhap that, thay bang hash BCrypt hop le.
2) Chay script tren DB dung schema hien tai (BeContextModelSnapshot).
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Quan trong: doi ten DB neu moi truong cua ban khac
USE [SchoolBus2];
GO

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    THROW 50001, N'Khong tim thay bang dbo.Roles. Hay kiem tra ban dang chay dung database chua.', 1;
END
GO

BEGIN TRANSACTION;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @Today DATE = CAST(@Now AS DATE);
DECLARE @Tomorrow DATE = DATEADD(DAY, 1, @Today);
DECLARE @Yesterday DATE = DATEADD(DAY, -1, @Today);

/* =========================================================
   1) Roles
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'admin') INSERT INTO Roles(Name) VALUES('admin');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'guardian') INSERT INTO Roles(Name) VALUES('guardian');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'driver') INSERT INTO Roles(Name) VALUES('driver');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'teacher') INSERT INTO Roles(Name) VALUES('teacher');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'staff') INSERT INTO Roles(Name) VALUES('staff');

DECLARE @RoleAdmin BIGINT = (SELECT TOP 1 Id FROM Roles WHERE Name = 'admin');
DECLARE @RoleGuardian BIGINT = (SELECT TOP 1 Id FROM Roles WHERE Name = 'guardian');
DECLARE @RoleDriver BIGINT = (SELECT TOP 1 Id FROM Roles WHERE Name = 'driver');
DECLARE @RoleTeacher BIGINT = (SELECT TOP 1 Id FROM Roles WHERE Name = 'teacher');
DECLARE @RoleStaff BIGINT = (SELECT TOP 1 Id FROM Roles WHERE Name = 'staff');

/* =========================================================
   2) Users (AccountStatus: ACTIVE=0, DISABLED=1)
   ========================================================= */
DECLARE @SeedPasswordHash NVARCHAR(MAX) = N'$2a$11$9Y8QxH3QZs6QvUj3u9M4fOqVx5h6y7z8A9b0C1d2E3f4G5h6I7j8K';

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'tran.minh.khoa@schoolbus.vn')
BEGIN
    INSERT INTO Users(Email, PasswordHash, FullName, AvatarUrl, Phone, RoleId, Status, CreatedAt, DeviceToken, DriverLicenseNumber, DriverLicenseClass, DriverLicenseExpiryDate)
    VALUES (
        N'tran.minh.khoa@schoolbus.vn',
        @SeedPasswordHash,
        N'Trần Minh Khoa',
        N'https://cdn.schoolbus.vn/avatar/admin-khoa.jpg',
        N'0901234567',
        @RoleAdmin,
        0,
        @Now,
        NULL, NULL, NULL, NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'nguyen.lan.huong@schoolbus.vn')
BEGIN
    INSERT INTO Users(Email, PasswordHash, FullName, AvatarUrl, Phone, RoleId, Status, CreatedAt, DeviceToken, DriverLicenseNumber, DriverLicenseClass, DriverLicenseExpiryDate)
    VALUES (
        N'nguyen.lan.huong@schoolbus.vn',
        @SeedPasswordHash,
        N'Nguyễn Lan Hương',
        N'https://cdn.schoolbus.vn/avatar/guardian-huong.jpg',
        N'0902302001',
        @RoleGuardian,
        0,
        @Now,
        N'fcm_token_guardian_huong',
        NULL, NULL, NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'tran.mai.phuong@schoolbus.vn')
BEGIN
    INSERT INTO Users(Email, PasswordHash, FullName, AvatarUrl, Phone, RoleId, Status, CreatedAt, DeviceToken, DriverLicenseNumber, DriverLicenseClass, DriverLicenseExpiryDate)
    VALUES (
        N'tran.mai.phuong@schoolbus.vn',
        @SeedPasswordHash,
        N'Trần Mai Phương',
        N'https://cdn.schoolbus.vn/avatar/guardian-phuong.jpg',
        N'0902302002',
        @RoleGuardian,
        0,
        @Now,
        NULL,
        NULL, NULL, NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'nguyen.van.tuan@schoolbus.vn')
BEGIN
    INSERT INTO Users(Email, PasswordHash, FullName, AvatarUrl, Phone, RoleId, Status, CreatedAt, DeviceToken, DriverLicenseNumber, DriverLicenseClass, DriverLicenseExpiryDate)
    VALUES (
        N'nguyen.van.tuan@schoolbus.vn',
        @SeedPasswordHash,
        N'Nguyễn Văn Tuấn',
        N'https://cdn.schoolbus.vn/avatar/driver-tuan.jpg',
        N'0922303001',
        @RoleDriver,
        0,
        @Now,
        NULL,
        N'79B2-123456',
        N'B2',
        DATEADD(YEAR, 4, @Today)
    );
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'le.hoang.long@schoolbus.vn')
BEGIN
    INSERT INTO Users(Email, PasswordHash, FullName, AvatarUrl, Phone, RoleId, Status, CreatedAt, DeviceToken, DriverLicenseNumber, DriverLicenseClass, DriverLicenseExpiryDate)
    VALUES (
        N'le.hoang.long@schoolbus.vn',
        @SeedPasswordHash,
        N'Lê Hoàng Long',
        N'https://cdn.schoolbus.vn/avatar/driver-long.jpg',
        N'0922303002',
        @RoleDriver,
        0,
        @Now,
        NULL,
        N'59B2-654321',
        N'B2',
        DATEADD(YEAR, 3, @Today)
    );
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'tran.thu.ha@schoolbus.vn')
BEGIN
    INSERT INTO Users(Email, PasswordHash, FullName, AvatarUrl, Phone, RoleId, Status, CreatedAt, DeviceToken, DriverLicenseNumber, DriverLicenseClass, DriverLicenseExpiryDate)
    VALUES (
        N'tran.thu.ha@schoolbus.vn',
        @SeedPasswordHash,
        N'Trần Thu Hà',
        N'https://cdn.schoolbus.vn/avatar/teacher-ha.jpg',
        N'0912301001',
        @RoleTeacher,
        0,
        @Now,
        NULL, NULL, NULL, NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'pham.ngoc.linh@schoolbus.vn')
BEGIN
    INSERT INTO Users(Email, PasswordHash, FullName, AvatarUrl, Phone, RoleId, Status, CreatedAt, DeviceToken, DriverLicenseNumber, DriverLicenseClass, DriverLicenseExpiryDate)
    VALUES (
        N'pham.ngoc.linh@schoolbus.vn',
        @SeedPasswordHash,
        N'Phạm Ngọc Linh',
        N'https://cdn.schoolbus.vn/avatar/teacher-linh.jpg',
        N'0912301002',
        @RoleTeacher,
        0,
        @Now,
        NULL, NULL, NULL, NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'nguyen.hai.dang@schoolbus.vn')
BEGIN
    INSERT INTO Users(Email, PasswordHash, FullName, AvatarUrl, Phone, RoleId, Status, CreatedAt, DeviceToken, DriverLicenseNumber, DriverLicenseClass, DriverLicenseExpiryDate)
    VALUES (
        N'nguyen.hai.dang@schoolbus.vn',
        @SeedPasswordHash,
        N'Nguyễn Hải Đăng',
        N'https://cdn.schoolbus.vn/avatar/staff-dang.jpg',
        N'0932304001',
        @RoleStaff,
        0,
        @Now,
        NULL, NULL, NULL, NULL
    );
END

DECLARE @AdminId BIGINT = (SELECT TOP 1 Id FROM Users WHERE Email = N'tran.minh.khoa@schoolbus.vn');
DECLARE @Guardian1Id BIGINT = (SELECT TOP 1 Id FROM Users WHERE Email = N'nguyen.lan.huong@schoolbus.vn');
DECLARE @Guardian2Id BIGINT = (SELECT TOP 1 Id FROM Users WHERE Email = N'tran.mai.phuong@schoolbus.vn');
DECLARE @Driver1Id BIGINT = (SELECT TOP 1 Id FROM Users WHERE Email = N'nguyen.van.tuan@schoolbus.vn');
DECLARE @Driver2Id BIGINT = (SELECT TOP 1 Id FROM Users WHERE Email = N'le.hoang.long@schoolbus.vn');
DECLARE @Teacher1Id BIGINT = (SELECT TOP 1 Id FROM Users WHERE Email = N'tran.thu.ha@schoolbus.vn');
DECLARE @Teacher2Id BIGINT = (SELECT TOP 1 Id FROM Users WHERE Email = N'pham.ngoc.linh@schoolbus.vn');
DECLARE @Staff1Id BIGINT = (SELECT TOP 1 Id FROM Users WHERE Email = N'nguyen.hai.dang@schoolbus.vn');

/* =========================================================
   3) Wallets
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM Wallets WHERE UserId = @Guardian1Id)
    INSERT INTO Wallets(UserId, Balance) VALUES(@Guardian1Id, 2500000);

IF NOT EXISTS (SELECT 1 FROM Wallets WHERE UserId = @Guardian2Id)
    INSERT INTO Wallets(UserId, Balance) VALUES(@Guardian2Id, 1200000);

/* =========================================================
   4) Campus / BusStations / BusRoutes / BusRouteStations
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM Campuses WHERE Code = N'CS-Q1')
BEGIN
    INSERT INTO Campuses(Code, Name, Address, Phone, IsActive, ImageUrl)
    VALUES (
        N'CS-Q1',
        N'Cơ sở Quận 1',
        N'100 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP.HCM',
        N'02838223344',
        1,
        N'https://cdn.schoolbus.vn/campus/cs-q1.jpg'
    );
END

DECLARE @CampusQ1Id BIGINT = (SELECT TOP 1 Id FROM Campuses WHERE Code = N'CS-Q1');

IF NOT EXISTS (SELECT 1 FROM BusStations WHERE Name = N'Trạm Công viên Tao Đàn')
    INSERT INTO BusStations(Name, Address, Description, Latitude, Longitude, IsEnabled)
    VALUES (
        N'Trạm Công viên Tao Đàn',
        N'55C Nguyễn Thị Minh Khai, Quận 1, TP.HCM',
        N'Điểm đón học sinh khu trung tâm Quận 1',
        10.779780, 106.691650, 1
    );

IF NOT EXISTS (SELECT 1 FROM BusStations WHERE Name = N'Trạm Chợ Bến Thành')
    INSERT INTO BusStations(Name, Address, Description, Latitude, Longitude, IsEnabled)
    VALUES (
        N'Trạm Chợ Bến Thành',
        N'Đường Lê Lợi, Phường Bến Thành, Quận 1, TP.HCM',
        N'Điểm đón gần ga metro và chợ Bến Thành',
        10.772510, 106.698020, 1
    );

IF NOT EXISTS (SELECT 1 FROM BusStations WHERE Name = N'Trạm Nhà văn hóa Thanh niên')
    INSERT INTO BusStations(Name, Address, Description, Latitude, Longitude, IsEnabled)
    VALUES (
        N'Trạm Nhà văn hóa Thanh niên',
        N'4 Phạm Ngọc Thạch, Quận 1, TP.HCM',
        N'Điểm đón học sinh khu Hồ Con Rùa',
        10.783360, 106.693700, 1
    );

DECLARE @Station1Id BIGINT = (SELECT TOP 1 Id FROM BusStations WHERE Name = N'Trạm Công viên Tao Đàn');
DECLARE @Station2Id BIGINT = (SELECT TOP 1 Id FROM BusStations WHERE Name = N'Trạm Chợ Bến Thành');
DECLARE @Station3Id BIGINT = (SELECT TOP 1 Id FROM BusStations WHERE Name = N'Trạm Nhà văn hóa Thanh niên');

IF NOT EXISTS (SELECT 1 FROM BusRoutes WHERE Name = N'Tuyến sáng Quận 1 - Cơ sở Q1')
BEGIN
    INSERT INTO BusRoutes(Name, CampusId, IsEnabled)
    VALUES (N'Tuyến sáng Quận 1 - Cơ sở Q1', @CampusQ1Id, 1);
END

DECLARE @RouteMorningId BIGINT = (SELECT TOP 1 Id FROM BusRoutes WHERE Name = N'Tuyến sáng Quận 1 - Cơ sở Q1');

IF NOT EXISTS (SELECT 1 FROM BusRouteStations WHERE RouteId = @RouteMorningId AND StationId = @Station1Id)
    INSERT INTO BusRouteStations(RouteId, StationId, OrderIndex) VALUES(@RouteMorningId, @Station1Id, 1);

IF NOT EXISTS (SELECT 1 FROM BusRouteStations WHERE RouteId = @RouteMorningId AND StationId = @Station2Id)
    INSERT INTO BusRouteStations(RouteId, StationId, OrderIndex) VALUES(@RouteMorningId, @Station2Id, 2);

IF NOT EXISTS (SELECT 1 FROM BusRouteStations WHERE RouteId = @RouteMorningId AND StationId = @Station3Id)
    INSERT INTO BusRouteStations(RouteId, StationId, OrderIndex) VALUES(@RouteMorningId, @Station3Id, 3);

/* =========================================================
   5) Buses
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM Buses WHERE LicensePlate = N'51B-12345')
BEGIN
    INSERT INTO Buses(LicensePlate, Capacity, Status, BusNumber, ImageUrl, Color, BusType)
    VALUES (
        N'51B-12345',
        45,
        N'ACTIVE',
        N'BUS-Q1-01',
        N'https://cdn.schoolbus.vn/bus/bus-q1-01.jpg',
        N'Vàng',
        N'45 chỗ'
    );
END

IF NOT EXISTS (SELECT 1 FROM Buses WHERE LicensePlate = N'51B-67890')
BEGIN
    INSERT INTO Buses(LicensePlate, Capacity, Status, BusNumber, ImageUrl, Color, BusType)
    VALUES (
        N'51B-67890',
        16,
        N'ACTIVE',
        N'BUS-Q1-BK',
        N'https://cdn.schoolbus.vn/bus/bus-q1-backup.jpg',
        N'Vàng',
        N'16 chỗ'
    );
END

DECLARE @BusMainId BIGINT = (SELECT TOP 1 Id FROM Buses WHERE LicensePlate = N'51B-12345');
DECLARE @BusBackupId BIGINT = (SELECT TOP 1 Id FROM Buses WHERE LicensePlate = N'51B-67890');

/* =========================================================
   6) Packages
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM Packages WHERE Name = N'Gói 3 tháng tiêu chuẩn')
BEGIN
    INSERT INTO Packages(Name, Price, DurationDays, RouteLimit, Description, Status, Type, CreatedAt, ImageUrl)
    VALUES (
        N'Gói 3 tháng tiêu chuẩn',
        1800000,
        90,
        2,
        N'Gói xe buýt đưa đón 2 tuyến trong 3 tháng cho học sinh.',
        N'ACTIVE',
        N'MONTHLY',
        @Now,
        N'https://cdn.schoolbus.vn/package/goi-3-thang.jpg'
    );
END

DECLARE @PackageStandardId BIGINT = (SELECT TOP 1 Id FROM Packages WHERE Name = N'Gói 3 tháng tiêu chuẩn');

/* =========================================================
   7) Students
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM Students WHERE StudentCode = N'ST001')
BEGIN
    INSERT INTO Students(StudentCode, FullName, AvatarUrl, DateOfBirth, Gender, GuardianId, CampusId, Status)
    VALUES (
        N'ST001',
        N'Nguyễn Minh Khang',
        N'https://cdn.schoolbus.vn/student/st001.jpg',
        '2015-03-12',
        N'male',
        @Guardian1Id,
        @CampusQ1Id,
        0
    );
END

IF NOT EXISTS (SELECT 1 FROM Students WHERE StudentCode = N'ST002')
BEGIN
    INSERT INTO Students(StudentCode, FullName, AvatarUrl, DateOfBirth, Gender, GuardianId, CampusId, Status)
    VALUES (
        N'ST002',
        N'Trần Ngọc Anh',
        N'https://cdn.schoolbus.vn/student/st002.jpg',
        '2016-07-25',
        N'female',
        @Guardian2Id,
        @CampusQ1Id,
        0
    );
END

DECLARE @Student1Id BIGINT = (SELECT TOP 1 Id FROM Students WHERE StudentCode = N'ST001');
DECLARE @Student2Id BIGINT = (SELECT TOP 1 Id FROM Students WHERE StudentCode = N'ST002');

/* =========================================================
   8) Orders + Payments + TransactionLogs
      OrderStatus: PENDING=0, PAID=1, CANCELLED=2, EXPIRED=3
      PaymentStatus: SUCCESS=0, FAILED=1
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM Orders WHERE StudentId = @Student1Id AND PackageId = @PackageStandardId AND Status = 1)
BEGIN
    INSERT INTO Orders(GuardianId, StudentId, PackageId, BusRouteId, SelectedRouteIds, Status, CreatedAt, PaidAt, StartDate, EndDate, ExpiredAt)
    VALUES (
        @Guardian1Id,
        @Student1Id,
        @PackageStandardId,
        @RouteMorningId,
        N'[' + CAST(@RouteMorningId AS NVARCHAR(20)) + N']',
        1,
        DATEADD(DAY, -7, @Now),
        DATEADD(DAY, -7, @Now),
        DATEADD(DAY, -7, @Today),
        DATEADD(DAY, 83, @Today),
        NULL
    );
END

DECLARE @PaidOrderId BIGINT = (
    SELECT TOP 1 Id
    FROM Orders
    WHERE StudentId = @Student1Id AND PackageId = @PackageStandardId AND Status = 1
    ORDER BY Id DESC
);

IF NOT EXISTS (SELECT 1 FROM Payments WHERE OrderId = @PaidOrderId)
BEGIN
    INSERT INTO Payments(OrderId, Method, Amount, Status, PaidAt)
    VALUES (@PaidOrderId, N'PAYOS', 1800000, 0, DATEADD(DAY, -7, @Now));
END

IF NOT EXISTS (SELECT 1 FROM TransactionLogs WHERE OrderId = @PaidOrderId AND Code = N'PAYOS-ORDER-ST001')
BEGIN
    DECLARE @Wallet1Old DECIMAL(18,2) = 700000;
    DECLARE @Wallet1New DECIMAL(18,2) = 2500000;

    INSERT INTO TransactionLogs(OrderId, Method, Amount, Status, PaidAt, OldBalance, NewBalance, Sender, Receiver, Description, Code)
    VALUES (
        @PaidOrderId,
        N'PAYOS',
        1800000,
        N'SUCCESS',
        DATEADD(DAY, -7, @Now),
        @Wallet1Old,
        @Wallet1New,
        N'Ví phụ huynh Nguyễn Lan Hương',
        N'Ví hệ thống SchoolBus',
        N'Thanh toán đơn hàng dịch vụ xe buýt cho học sinh ST001',
        N'PAYOS-ORDER-ST001'
    );
END

/* =========================================================
   9) Bookings + BusRuns + BusRunStudents + BusTripProgress
   ========================================================= */
IF NOT EXISTS (
    SELECT 1
    FROM Bookings
    WHERE StudentId = @Student1Id
      AND RouteId = @RouteMorningId
      AND ServiceDate = @Tomorrow
      AND StartTime = '07:00:00'
)
BEGIN
    INSERT INTO Bookings(StudentId, RouteId, ServiceDate, StartTime, StationId, Latitude, Longitude, Status, Note, CreatedAt)
    VALUES (
        @Student1Id,
        @RouteMorningId,
        @Tomorrow,
        '07:00:00',
        @Station1Id,
        10.779780,
        106.691650,
        N'CONFIRMED',
        N'Đón tại cổng công viên Tao Đàn, phụ huynh chờ sẵn.',
        @Now
    );
END

IF NOT EXISTS (
    SELECT 1
    FROM Bookings
    WHERE StudentId = @Student2Id
      AND RouteId = @RouteMorningId
      AND ServiceDate = @Tomorrow
      AND StartTime = '07:00:00'
)
BEGIN
    INSERT INTO Bookings(StudentId, RouteId, ServiceDate, StartTime, StationId, Latitude, Longitude, Status, Note, CreatedAt)
    VALUES (
        @Student2Id,
        @RouteMorningId,
        @Tomorrow,
        '07:00:00',
        @Station2Id,
        10.772510,
        106.698020,
        N'CONFIRMED',
        N'Đón trước cổng chợ Bến Thành.',
        @Now
    );
END

DECLARE @Booking1Id BIGINT = (
    SELECT TOP 1 Id FROM Bookings
    WHERE StudentId = @Student1Id AND RouteId = @RouteMorningId AND ServiceDate = @Tomorrow AND StartTime = '07:00:00'
    ORDER BY Id DESC
);

DECLARE @Booking2Id BIGINT = (
    SELECT TOP 1 Id FROM Bookings
    WHERE StudentId = @Student2Id AND RouteId = @RouteMorningId AND ServiceDate = @Tomorrow AND StartTime = '07:00:00'
    ORDER BY Id DESC
);

IF NOT EXISTS (
    SELECT 1
    FROM BusRuns
    WHERE RouteId = @RouteMorningId
      AND ServiceDate = @Tomorrow
      AND StartTime = '07:00:00'
      AND RunOrder = 1
)
BEGIN
    INSERT INTO BusRuns(RouteId, ServiceDate, StartTime, BusId, DriverId, TeacherId, SeatCapacity, UsableCapacity, AssignedStudentCount, RunOrder, Status, CreatedAt)
    VALUES (
        @RouteMorningId,
        @Tomorrow,
        '07:00:00',
        @BusMainId,
        @Driver1Id,
        @Teacher1Id,
        45,
        20,
        2,
        1,
        N'ASSIGNED',
        @Now
    );
END

IF NOT EXISTS (
    SELECT 1
    FROM BusRuns
    WHERE RouteId = @RouteMorningId
      AND ServiceDate = @Tomorrow
      AND StartTime = '07:00:00'
      AND RunOrder = 2
)
BEGIN
    INSERT INTO BusRuns(RouteId, ServiceDate, StartTime, BusId, DriverId, TeacherId, SeatCapacity, UsableCapacity, AssignedStudentCount, RunOrder, Status, CreatedAt)
    VALUES (
        @RouteMorningId,
        @Tomorrow,
        '07:00:00',
        @BusBackupId,
        @Driver2Id,
        @Teacher2Id,
        16,
        15,
        0,
        2,
        N'BACKUP',
        @Now
    );
END

DECLARE @BusRunMainId BIGINT = (
    SELECT TOP 1 Id
    FROM BusRuns
    WHERE RouteId = @RouteMorningId AND ServiceDate = @Tomorrow AND StartTime = '07:00:00' AND RunOrder = 1
);

IF NOT EXISTS (SELECT 1 FROM BusRunStudents WHERE BookingId = @Booking1Id)
    INSERT INTO BusRunStudents(BusRunId, BookingId, StudentId) VALUES(@BusRunMainId, @Booking1Id, @Student1Id);

IF NOT EXISTS (SELECT 1 FROM BusRunStudents WHERE BookingId = @Booking2Id)
    INSERT INTO BusRunStudents(BusRunId, BookingId, StudentId) VALUES(@BusRunMainId, @Booking2Id, @Student2Id);

/* Co 2 moc tien do chuyen di cho ngay hom qua de hien thi lich su */
IF NOT EXISTS (
    SELECT 1 FROM BusTripProgresses
    WHERE BusRunId = @BusRunMainId AND RideDate = @Yesterday AND OrderIndex = 1
)
BEGIN
    INSERT INTO BusTripProgresses(BusId, BusRunId, RouteId, StationId, RideDate, ArrivedAt, OrderIndex)
    VALUES (
        @BusMainId,
        @BusRunMainId,
        @RouteMorningId,
        @Station1Id,
        @Yesterday,
        DATEADD(HOUR, 7, CAST(@Yesterday AS DATETIME2)),
        1
    );
END

IF NOT EXISTS (
    SELECT 1 FROM BusTripProgresses
    WHERE BusRunId = @BusRunMainId AND RideDate = @Yesterday AND OrderIndex = 2
)
BEGIN
    INSERT INTO BusTripProgresses(BusId, BusRunId, RouteId, StationId, RideDate, ArrivedAt, OrderIndex)
    VALUES (
        @BusMainId,
        @BusRunMainId,
        @RouteMorningId,
        @Station2Id,
        @Yesterday,
        DATEADD(MINUTE, 20, DATEADD(HOUR, 7, CAST(@Yesterday AS DATETIME2))),
        2
    );
END

/* =========================================================
   10) Attendance (Method: FACE=0, MANUAL=1; Status: PRESENT=0, ABSENT=1)
   ========================================================= */
IF NOT EXISTS (
    SELECT 1 FROM Attendances
    WHERE StudentId = @Student1Id AND BusId = @BusMainId AND Date = @Yesterday
)
BEGIN
    INSERT INTO Attendances(StudentId, BusId, Date, CheckInStationId, CheckInTime, CheckInImageUrl, CheckOutStationId, CheckOutTime, CheckOutImageUrl, Method, Status, Note)
    VALUES (
        @Student1Id,
        @BusMainId,
        @Yesterday,
        @Station1Id,
        '07:05:00',
        N'https://cdn.schoolbus.vn/attendance/st001-checkin.jpg',
        @Station3Id,
        '16:35:00',
        N'https://cdn.schoolbus.vn/attendance/st001-checkout.jpg',
        1,
        0,
        N'Đi học đầy đủ, có điểm danh sáng và chiều.'
    );
END

/* =========================================================
   11) BusTracking
   ========================================================= */
IF NOT EXISTS (
    SELECT 1 FROM BusTrackings
    WHERE BusId = @BusMainId
      AND TrackedAt >= DATEADD(MINUTE, -15, @Now)
)
BEGIN
    INSERT INTO BusTrackings(BusId, Latitude, Longitude, Speed, TrackedAt)
    VALUES (@BusMainId, 10.775900, 106.695400, 32.5, @Now);
END

/* =========================================================
   12) Notifications
   ========================================================= */
IF NOT EXISTS (
    SELECT 1 FROM Notifications
    WHERE UserId = @Guardian1Id
      AND Type = N'BUS_ARRIVED'
      AND CAST(CreatedAt AS DATE) = @Today
)
BEGIN
    INSERT INTO Notifications(UserId, Message, Type, IsRead, CreatedAt)
    VALUES (
        @Guardian1Id,
        N'Xe BUS-Q1-01 đã đến Trạm Công viên Tao Đàn. Vui lòng đưa bé Minh Khang ra điểm đón.',
        N'BUS_ARRIVED',
        0,
        @Now
    );
END

/* =========================================================
   13) SystemSettings
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE [Key] = N'FaceRecognition:SimilarityThreshold')
BEGIN
    INSERT INTO SystemSettings([Key], [Value], [Description])
    VALUES (
        N'FaceRecognition:SimilarityThreshold',
        N'0.80',
        N'Ngưỡng độ tương đồng nhận diện khuôn mặt cho điểm danh.'
    );
END

/* =========================================================
   14) BULK DATA theo yeu cau:
       - 10 Campus
       - 25 Phu huynh
       - 15 Tai xe
       - 30 Xe
       - 50 Hoc sinh (chia deu: 2 hoc sinh / 1 phu huynh)
   ========================================================= */

/* 14.1) Seed 10 Campus */
DECLARE @CampusIdx INT = 1;
WHILE @CampusIdx <= 10
BEGIN
    DECLARE @CampusCode NVARCHAR(20) = N'CS-' + RIGHT('00' + CAST(@CampusIdx AS NVARCHAR(2)), 2);
    DECLARE @CampusName NVARCHAR(200) =
        CASE @CampusIdx
            WHEN 1 THEN N'Cơ sở Quận 1'
            WHEN 2 THEN N'Cơ sở Quận 3'
            WHEN 3 THEN N'Cơ sở Quận 5'
            WHEN 4 THEN N'Cơ sở Quận 7'
            WHEN 5 THEN N'Cơ sở Quận 10'
            WHEN 6 THEN N'Cơ sở Bình Thạnh'
            WHEN 7 THEN N'Cơ sở Gò Vấp'
            WHEN 8 THEN N'Cơ sở Tân Bình'
            WHEN 9 THEN N'Cơ sở Thủ Đức'
            ELSE N'Cơ sở Phú Nhuận'
        END;

    DECLARE @CampusAddress NVARCHAR(300) = N'Số ' + CAST(100 + @CampusIdx AS NVARCHAR(10)) + N' Đường mẫu, TP.HCM';
    DECLARE @CampusPhone NVARCHAR(20) = N'02838' + RIGHT('0000' + CAST(1000 + @CampusIdx AS NVARCHAR(10)), 4);
    DECLARE @CampusImage NVARCHAR(300) = N'https://cdn.schoolbus.vn/campus/cs-' + RIGHT('00' + CAST(@CampusIdx AS NVARCHAR(2)), 2) + N'.jpg';

    IF NOT EXISTS (SELECT 1 FROM Campuses WHERE Code = @CampusCode)
    BEGIN
        INSERT INTO Campuses(Code, Name, Address, Phone, IsActive, ImageUrl)
        VALUES (@CampusCode, @CampusName, @CampusAddress, @CampusPhone, 1, @CampusImage);
    END

    SET @CampusIdx += 1;
END

/* 14.2) Seed 25 Phu huynh */
DECLARE @GuardianIdx INT = 1;
WHILE @GuardianIdx <= 25
BEGIN
    DECLARE @GuardianEmail NVARCHAR(200) = N'guardian' + RIGHT('00' + CAST(@GuardianIdx AS NVARCHAR(2)), 2) + N'@schoolbus.vn';
    DECLARE @GuardianPhone NVARCHAR(20) = N'09023' + RIGHT('0000' + CAST(2000 + @GuardianIdx AS NVARCHAR(10)), 4);
    DECLARE @GuardianAvatar NVARCHAR(300) = N'https://cdn.schoolbus.vn/avatar/guardian-' + RIGHT('00' + CAST(@GuardianIdx AS NVARCHAR(2)), 2) + N'.jpg';

    DECLARE @GuardianFirstName NVARCHAR(50) =
        CASE ((@GuardianIdx - 1) % 10) + 1
            WHEN 1 THEN N'Lan'
            WHEN 2 THEN N'Mai'
            WHEN 3 THEN N'Thảo'
            WHEN 4 THEN N'Ngọc'
            WHEN 5 THEN N'Thu'
            WHEN 6 THEN N'Phương'
            WHEN 7 THEN N'Hồng'
            WHEN 8 THEN N'Yến'
            WHEN 9 THEN N'Kim'
            ELSE N'Bảo'
        END;

    DECLARE @GuardianMiddleName NVARCHAR(50) =
        CASE ((@GuardianIdx - 1) % 8) + 1
            WHEN 1 THEN N'Linh'
            WHEN 2 THEN N'Hương'
            WHEN 3 THEN N'Anh'
            WHEN 4 THEN N'Như'
            WHEN 5 THEN N'Khánh'
            WHEN 6 THEN N'Minh'
            WHEN 7 THEN N'Tường'
            ELSE N'Ngân'
        END;

    DECLARE @GuardianLastName NVARCHAR(50) =
        CASE ((@GuardianIdx - 1) % 6) + 1
            WHEN 1 THEN N'Nguyễn'
            WHEN 2 THEN N'Trần'
            WHEN 3 THEN N'Lê'
            WHEN 4 THEN N'Phạm'
            WHEN 5 THEN N'Võ'
            ELSE N'Đặng'
        END;

    DECLARE @GuardianFullName NVARCHAR(200) = @GuardianLastName + N' ' + @GuardianMiddleName + N' ' + @GuardianFirstName;

    IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = @GuardianEmail)
    BEGIN
        INSERT INTO Users(Email, PasswordHash, FullName, AvatarUrl, Phone, RoleId, Status, CreatedAt, DeviceToken, DriverLicenseNumber, DriverLicenseClass, DriverLicenseExpiryDate)
        VALUES (
            @GuardianEmail,
            @SeedPasswordHash,
            @GuardianFullName,
            @GuardianAvatar,
            @GuardianPhone,
            @RoleGuardian,
            0,
            @Now,
            NULL, NULL, NULL, NULL
        );
    END

    DECLARE @GuardianUserId BIGINT = (SELECT TOP 1 Id FROM Users WHERE Email = @GuardianEmail);
    IF NOT EXISTS (SELECT 1 FROM Wallets WHERE UserId = @GuardianUserId)
    BEGIN
        INSERT INTO Wallets(UserId, Balance)
        VALUES (@GuardianUserId, CAST(800000 + (@GuardianIdx * 120000) AS DECIMAL(18,2)));
    END

    SET @GuardianIdx += 1;
END

/* 14.3) Seed 15 Tai xe */
DECLARE @DriverIdx INT = 1;
WHILE @DriverIdx <= 15
BEGIN
    DECLARE @DriverEmail NVARCHAR(200) = N'driver' + RIGHT('00' + CAST(@DriverIdx AS NVARCHAR(2)), 2) + N'@schoolbus.vn';
    DECLARE @DriverPhone NVARCHAR(20) = N'09223' + RIGHT('0000' + CAST(3000 + @DriverIdx AS NVARCHAR(10)), 4);
    DECLARE @DriverAvatar NVARCHAR(300) = N'https://cdn.schoolbus.vn/avatar/driver-' + RIGHT('00' + CAST(@DriverIdx AS NVARCHAR(2)), 2) + N'.jpg';
    DECLARE @DriverLicenseNo NVARCHAR(50) = N'79B2-' + RIGHT('000000' + CAST(100000 + @DriverIdx AS NVARCHAR(10)), 6);
    DECLARE @DriverLicenseClass NVARCHAR(20) = N'B2';
    DECLARE @DriverExpiry DATE = DATEADD(YEAR, 2 + (@DriverIdx % 4), @Today);

    DECLARE @DriverFirstName NVARCHAR(50) =
        CASE ((@DriverIdx - 1) % 10) + 1
            WHEN 1 THEN N'Tuấn'
            WHEN 2 THEN N'Long'
            WHEN 3 THEN N'Mạnh'
            WHEN 4 THEN N'Đạt'
            WHEN 5 THEN N'Khang'
            WHEN 6 THEN N'Vinh'
            WHEN 7 THEN N'Quân'
            WHEN 8 THEN N'Phúc'
            WHEN 9 THEN N'Thành'
            ELSE N'Duy'
        END;

    DECLARE @DriverMiddleName NVARCHAR(50) =
        CASE ((@DriverIdx - 1) % 6) + 1
            WHEN 1 THEN N'Văn'
            WHEN 2 THEN N'Đức'
            WHEN 3 THEN N'Hoàng'
            WHEN 4 THEN N'Minh'
            WHEN 5 THEN N'Quốc'
            ELSE N'Gia'
        END;

    DECLARE @DriverLastName NVARCHAR(50) =
        CASE ((@DriverIdx - 1) % 6) + 1
            WHEN 1 THEN N'Nguyễn'
            WHEN 2 THEN N'Trần'
            WHEN 3 THEN N'Lê'
            WHEN 4 THEN N'Phạm'
            WHEN 5 THEN N'Võ'
            ELSE N'Bùi'
        END;

    DECLARE @DriverFullName NVARCHAR(200) = @DriverLastName + N' ' + @DriverMiddleName + N' ' + @DriverFirstName;

    IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = @DriverEmail)
    BEGIN
        INSERT INTO Users(Email, PasswordHash, FullName, AvatarUrl, Phone, RoleId, Status, CreatedAt, DeviceToken, DriverLicenseNumber, DriverLicenseClass, DriverLicenseExpiryDate)
        VALUES (
            @DriverEmail,
            @SeedPasswordHash,
            @DriverFullName,
            @DriverAvatar,
            @DriverPhone,
            @RoleDriver,
            0,
            @Now,
            NULL,
            @DriverLicenseNo,
            @DriverLicenseClass,
            @DriverExpiry
        );
    END

    SET @DriverIdx += 1;
END

/* 14.4) Seed 30 Xe */
DECLARE @BusIdx INT = 1;
WHILE @BusIdx <= 30
BEGIN
    DECLARE @BusLicense NVARCHAR(30) = N'51B-' + RIGHT('00000' + CAST(30000 + @BusIdx AS NVARCHAR(10)), 5);
    DECLARE @BusNumberBulk NVARCHAR(50) = N'BUS-SEED-' + RIGHT('00' + CAST(@BusIdx AS NVARCHAR(2)), 2);
    DECLARE @BusImage NVARCHAR(300) = N'https://cdn.schoolbus.vn/bus/seed-' + RIGHT('00' + CAST(@BusIdx AS NVARCHAR(2)), 2) + N'.jpg';
    DECLARE @BusCapacity INT = CASE WHEN @BusIdx % 3 = 0 THEN 16 WHEN @BusIdx % 3 = 1 THEN 29 ELSE 45 END;
    DECLARE @BusTypeBulk NVARCHAR(50) = CASE WHEN @BusCapacity = 16 THEN N'16 chỗ' WHEN @BusCapacity = 29 THEN N'29 chỗ' ELSE N'45 chỗ' END;

    IF NOT EXISTS (SELECT 1 FROM Buses WHERE LicensePlate = @BusLicense)
    BEGIN
        INSERT INTO Buses(LicensePlate, Capacity, Status, BusNumber, ImageUrl, Color, BusType)
        VALUES (
            @BusLicense,
            @BusCapacity,
            N'ACTIVE',
            @BusNumberBulk,
            @BusImage,
            N'Vàng',
            @BusTypeBulk
        );
    END

    SET @BusIdx += 1;
END

/* 14.5) Seed 50 Hoc sinh
   - 25 phu huynh, moi phu huynh 2 hoc sinh
   - 10 campus, chia vong tron */
DECLARE @StudentIdx INT = 1;
WHILE @StudentIdx <= 50
BEGIN
    DECLARE @StudentCodeBulk NVARCHAR(50) = N'STB' + RIGHT('000' + CAST(@StudentIdx AS NVARCHAR(3)), 3);
    DECLARE @AssignedGuardianNum INT = ((@StudentIdx - 1) % 25) + 1;
    DECLARE @AssignedCampusNum INT = ((@StudentIdx - 1) % 10) + 1;

    DECLARE @AssignedGuardianEmail NVARCHAR(200) = N'guardian' + RIGHT('00' + CAST(@AssignedGuardianNum AS NVARCHAR(2)), 2) + N'@schoolbus.vn';
    DECLARE @AssignedCampusCode NVARCHAR(20) = N'CS-' + RIGHT('00' + CAST(@AssignedCampusNum AS NVARCHAR(2)), 2);

    DECLARE @AssignedGuardianId BIGINT = (SELECT TOP 1 Id FROM Users WHERE Email = @AssignedGuardianEmail);
    DECLARE @AssignedCampusId BIGINT = (SELECT TOP 1 Id FROM Campuses WHERE Code = @AssignedCampusCode);

    DECLARE @StudentFirstName NVARCHAR(50) =
        CASE ((@StudentIdx - 1) % 12) + 1
            WHEN 1 THEN N'Minh Khang'
            WHEN 2 THEN N'Ngọc Anh'
            WHEN 3 THEN N'Gia Hân'
            WHEN 4 THEN N'Hoàng Phúc'
            WHEN 5 THEN N'Bảo Nhi'
            WHEN 6 THEN N'Tuấn Kiệt'
            WHEN 7 THEN N'Khánh Linh'
            WHEN 8 THEN N'Đức Huy'
            WHEN 9 THEN N'Yến Nhi'
            WHEN 10 THEN N'Quang Vinh'
            WHEN 11 THEN N'Bảo Trâm'
            ELSE N'Thành Đạt'
        END;

    DECLARE @StudentLastName NVARCHAR(50) =
        CASE ((@StudentIdx - 1) % 6) + 1
            WHEN 1 THEN N'Nguyễn'
            WHEN 2 THEN N'Trần'
            WHEN 3 THEN N'Lê'
            WHEN 4 THEN N'Phạm'
            WHEN 5 THEN N'Võ'
            ELSE N'Đặng'
        END;

    DECLARE @StudentFullNameBulk NVARCHAR(200) = @StudentLastName + N' ' + @StudentFirstName;
    DECLARE @StudentAvatar NVARCHAR(300) = N'https://cdn.schoolbus.vn/student/stb-' + RIGHT('000' + CAST(@StudentIdx AS NVARCHAR(3)), 3) + N'.jpg';
    DECLARE @StudentDob DATE = DATEADD(DAY, -((8 * 365) + (@StudentIdx * 15)), @Today);
    DECLARE @StudentGender NVARCHAR(10) = CASE WHEN @StudentIdx % 2 = 0 THEN N'female' ELSE N'male' END;

    IF @AssignedGuardianId IS NOT NULL AND @AssignedCampusId IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Students WHERE StudentCode = @StudentCodeBulk)
        BEGIN
            INSERT INTO Students(StudentCode, FullName, AvatarUrl, DateOfBirth, Gender, GuardianId, CampusId, Status)
            VALUES (
                @StudentCodeBulk,
                @StudentFullNameBulk,
                @StudentAvatar,
                @StudentDob,
                @StudentGender,
                @AssignedGuardianId,
                @AssignedCampusId,
                0
            );
        END
    END

    SET @StudentIdx += 1;
END

/* 14.6) Seed them 59 hoc sinh (bo sung)
   - Ma hoc sinh: STX001 -> STX059
   - Chia deu guardian theo vong tron 25 phu huynh
   - Chia deu campus theo vong tron 10 co so */
DECLARE @ExtraStudentIdx INT = 1;
WHILE @ExtraStudentIdx <= 59
BEGIN
    DECLARE @ExtraStudentCode NVARCHAR(50) = N'STX' + RIGHT('000' + CAST(@ExtraStudentIdx AS NVARCHAR(3)), 3);
    DECLARE @ExtraGuardianNum INT = ((@ExtraStudentIdx - 1) % 25) + 1;
    DECLARE @ExtraCampusNum INT = ((@ExtraStudentIdx - 1) % 10) + 1;

    DECLARE @ExtraGuardianEmail NVARCHAR(200) = N'guardian' + RIGHT('00' + CAST(@ExtraGuardianNum AS NVARCHAR(2)), 2) + N'@schoolbus.vn';
    DECLARE @ExtraCampusCode NVARCHAR(20) = N'CS-' + RIGHT('00' + CAST(@ExtraCampusNum AS NVARCHAR(2)), 2);

    DECLARE @ExtraGuardianId BIGINT = (SELECT TOP 1 Id FROM Users WHERE Email = @ExtraGuardianEmail);
    DECLARE @ExtraCampusId BIGINT = (SELECT TOP 1 Id FROM Campuses WHERE Code = @ExtraCampusCode);

    DECLARE @ExtraFirstName NVARCHAR(50) =
        CASE ((@ExtraStudentIdx - 1) % 12) + 1
            WHEN 1 THEN N'Minh Châu'
            WHEN 2 THEN N'Gia Bảo'
            WHEN 3 THEN N'Thảo Vy'
            WHEN 4 THEN N'Khánh An'
            WHEN 5 THEN N'Hoài Nam'
            WHEN 6 THEN N'Phương Nhi'
            WHEN 7 THEN N'Quốc Bảo'
            WHEN 8 THEN N'Hà My'
            WHEN 9 THEN N'Tấn Phát'
            WHEN 10 THEN N'Bảo Ngân'
            WHEN 11 THEN N'Anh Thư'
            ELSE N'Minh Nhật'
        END;

    DECLARE @ExtraLastName NVARCHAR(50) =
        CASE ((@ExtraStudentIdx - 1) % 6) + 1
            WHEN 1 THEN N'Nguyễn'
            WHEN 2 THEN N'Trần'
            WHEN 3 THEN N'Lê'
            WHEN 4 THEN N'Phạm'
            WHEN 5 THEN N'Võ'
            ELSE N'Đặng'
        END;

    DECLARE @ExtraFullName NVARCHAR(200) = @ExtraLastName + N' ' + @ExtraFirstName;
    DECLARE @ExtraAvatar NVARCHAR(300) = N'https://cdn.schoolbus.vn/student/stx-' + RIGHT('000' + CAST(@ExtraStudentIdx AS NVARCHAR(3)), 3) + N'.jpg';
    DECLARE @ExtraDob DATE = DATEADD(DAY, -((9 * 365) + (@ExtraStudentIdx * 11)), @Today);
    DECLARE @ExtraGender NVARCHAR(10) = CASE WHEN @ExtraStudentIdx % 2 = 0 THEN N'female' ELSE N'male' END;

    IF @ExtraGuardianId IS NOT NULL AND @ExtraCampusId IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Students WHERE StudentCode = @ExtraStudentCode)
        BEGIN
            INSERT INTO Students(StudentCode, FullName, AvatarUrl, DateOfBirth, Gender, GuardianId, CampusId, Status)
            VALUES (
                @ExtraStudentCode,
                @ExtraFullName,
                @ExtraAvatar,
                @ExtraDob,
                @ExtraGender,
                @ExtraGuardianId,
                @ExtraCampusId,
                0
            );
        END
    END

    SET @ExtraStudentIdx += 1;
END

/* 14.7) Seed 50 booking cho hoc sinh de test auto-assign
   - Chon TOP 50 hoc sinh theo uu tien STB%, STX%
   - Route: @RouteMorningId
   - Ngay: @Tomorrow
   - Gio: 07:00:00
   - Trang thai: PENDING
   - Diem don: chia vong tron theo station cua route */
IF @RouteMorningId IS NOT NULL
BEGIN
    DECLARE @StationPool TABLE
    (
        RowNum INT IDENTITY(1,1) PRIMARY KEY,
        StationId BIGINT
    );

    INSERT INTO @StationPool(StationId)
    SELECT brs.StationId
    FROM BusRouteStations brs
    WHERE brs.RouteId = @RouteMorningId
    ORDER BY brs.OrderIndex, brs.Id;

    DECLARE @StationCount INT = (SELECT COUNT(*) FROM @StationPool);

    IF @StationCount > 0
    BEGIN
        DECLARE @BookingTarget TABLE
        (
            RowNum INT IDENTITY(1,1) PRIMARY KEY,
            StudentId BIGINT
        );

        INSERT INTO @BookingTarget(StudentId)
        SELECT TOP 50 s.Id
        FROM Students s
        WHERE s.Status = 0
        ORDER BY
            CASE
                WHEN s.StudentCode LIKE N'STB%' THEN 1
                WHEN s.StudentCode LIKE N'STX%' THEN 2
                ELSE 3
            END,
            s.StudentCode,
            s.Id;

        DECLARE @IdxBooking INT = 1;
        DECLARE @TotalTarget INT = (SELECT COUNT(*) FROM @BookingTarget);

        WHILE @IdxBooking <= @TotalTarget
        BEGIN
            DECLARE @TargetStudentId BIGINT = (SELECT StudentId FROM @BookingTarget WHERE RowNum = @IdxBooking);
            DECLARE @StationRow INT = ((@IdxBooking - 1) % @StationCount) + 1;
            DECLARE @TargetStationId BIGINT = (SELECT StationId FROM @StationPool WHERE RowNum = @StationRow);

            IF NOT EXISTS
            (
                SELECT 1
                FROM Bookings b
                WHERE b.StudentId = @TargetStudentId
                  AND b.RouteId = @RouteMorningId
                  AND CAST(b.ServiceDate AS DATE) = @Tomorrow
                  AND b.StartTime = '07:00:00'
            )
            BEGIN
                INSERT INTO Bookings
                (
                    StudentId, RouteId, ServiceDate, StartTime, StationId,
                    Latitude, Longitude, Status, Note, CreatedAt
                )
                VALUES
                (
                    @TargetStudentId,
                    @RouteMorningId,
                    @Tomorrow,
                    '07:00:00',
                    @TargetStationId,
                    NULL,
                    NULL,
                    N'PENDING',
                    N'Seed booking bulk 50 học sinh để test auto-assign.',
                    @Now
                );
            END

            SET @IdxBooking += 1;
        END
    END
END

/* 14.8) Gan tai xe + giao vien cho bus runs test (neu dang null)
   - Ap dung cho route test, ngay @Tomorrow, gio 07:00:00
   - RunOrder 1: Driver1 + Teacher1
   - RunOrder 2: Driver2 + Teacher2 (thuong la xe backup) */
IF @RouteMorningId IS NOT NULL
BEGIN
    UPDATE br
    SET
        br.DriverId = COALESCE(br.DriverId, @Driver1Id),
        br.TeacherId = COALESCE(br.TeacherId, @Teacher1Id)
    FROM BusRuns br
    WHERE br.RouteId = @RouteMorningId
      AND CAST(br.ServiceDate AS DATE) = @Tomorrow
      AND br.StartTime = '07:00:00'
      AND br.RunOrder = 1;

    UPDATE br
    SET
        br.DriverId = COALESCE(br.DriverId, @Driver2Id),
        br.TeacherId = COALESCE(br.TeacherId, @Teacher2Id)
    FROM BusRuns br
    WHERE br.RouteId = @RouteMorningId
      AND CAST(br.ServiceDate AS DATE) = @Tomorrow
      AND br.StartTime = '07:00:00'
      AND br.RunOrder = 2;
END

/* 14.9) Bo sung lat/lon cho booking null:
   - Dat diem don gan tram cua booking (xap xi, <= 4km)
   - Giup tai xe thay toa do cu the tren ban do */
UPDATE b
SET
    b.Latitude = CAST(bs.Latitude + ((((b.Id % 5) - 2) * 0.0015)) AS float),
    b.Longitude = CAST(bs.Longitude + ((((b.Id % 7) - 3) * 0.0015)) AS float)
FROM Bookings b
JOIN BusStations bs ON bs.Id = b.StationId
WHERE (b.Latitude IS NULL OR b.Longitude IS NULL)
  AND bs.Latitude IS NOT NULL
  AND bs.Longitude IS NOT NULL;

COMMIT TRANSACTION;

PRINT N'Seed dữ liệu thực tế đã hoàn tất.';
