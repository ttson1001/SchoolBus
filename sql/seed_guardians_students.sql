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
    ('guardian01@schoolbus.local', 'Nguyen Thi Guardian 01', '0902000001'),
    ('guardian02@schoolbus.local', 'Tran Van Guardian 02', '0902000002'),
    ('guardian03@schoolbus.local', 'Le Thi Guardian 03', '0902000003')
) AS v(Email, FullName, Phone)
INNER JOIN Roles r ON r.Name = 'guardian'
WHERE NOT EXISTS (
    SELECT 1
    FROM Users u
    WHERE u.Email = v.Email
);

INSERT INTO Students (FullName, DateOfBirth, Gender, GuardianId, Status, CampusId)
SELECT
    v.FullName,
    v.DateOfBirth,
    v.Gender,
    u.Id,
    0,
    c.Id
FROM (VALUES
    ('Pham Minh Khang', CAST('2017-05-10' AS datetime2), 'Male', 'guardian01@schoolbus.local', 'CS001'),
    ('Pham Bao Ngoc', CAST('2019-09-21' AS datetime2), 'Female', 'guardian01@schoolbus.local', 'CS001'),
    ('Tran Gia Huy', CAST('2018-12-03' AS datetime2), 'Male', 'guardian02@schoolbus.local', 'CS002'),
    ('Le Khanh An', CAST('2016-07-15' AS datetime2), 'Female', 'guardian03@schoolbus.local', 'CS003')
) AS v(FullName, DateOfBirth, Gender, GuardianEmail, CampusCode)
INNER JOIN Users u ON u.Email = v.GuardianEmail
INNER JOIN Campuses c ON c.Code = v.CampusCode
WHERE NOT EXISTS (
    SELECT 1
    FROM Students s
    WHERE s.FullName = v.FullName
      AND s.DateOfBirth = v.DateOfBirth
      AND s.Gender = v.Gender
      AND s.GuardianId = u.Id
      AND s.CampusId = c.Id
);
