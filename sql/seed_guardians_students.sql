/* Phụ huynh + học sinh (có StudentCode). Seed mở rộng: xem thêm seed_full_app.sql */

INSERT INTO Users (Email, PasswordHash, FullName, Phone, Status, RoleId, CreatedAt)
SELECT
    v.Email,
    '$2a$11$IGddWpI.mXVE9pVc0QaT4.i95cvUSey/DNmPV3LijXaHvJhyGEMZS',
    v.FullName,
    v.Phone,
    0,
    r.Id,
    GETUTCDATE()
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
WHERE NOT EXISTS (
    SELECT 1
    FROM Users u
    WHERE u.Email = v.Email
);

INSERT INTO Students (StudentCode, FullName, DateOfBirth, Gender, GuardianId, Status, CampusId)
SELECT
    v.StudentCode,
    v.FullName,
    v.DateOfBirth,
    v.Gender,
    u.Id,
    0,
    c.Id
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
WHERE NOT EXISTS (
    SELECT 1
    FROM Students s
    WHERE s.StudentCode = v.StudentCode
);
