INSERT INTO Campuses (Code, Name, Address, Phone, IsActive, ImageUrl)
SELECT v.Code, v.Name, v.Address, v.Phone, v.IsActive, v.ImageUrl
FROM (VALUES
    ('CS001', 'Campus Quận 1', '123 Nguyễn Huệ, Quận 1', '0901000001', 1, 'https://example.com/campus-1.jpg'),
    ('CS002', 'Campus Quận 3', '45 Võ Văn Tần, Quận 3', '0901000002', 1, 'https://example.com/campus-2.jpg'),
    ('CS003', 'Campus Bình Thạnh', '78 Điện Biên Phủ, Bình Thạnh', '0901000003', 1, 'https://example.com/campus-3.jpg'),
    ('CS004', 'Campus Gò Vấp', '12 Phan Văn Trị, Gò Vấp', '0901000004', 1, 'https://example.com/campus-4.jpg'),
    ('CS005', 'Campus Phú Nhuận', '89 Nguyễn Văn Trỗi, Phú Nhuận', '0901000005', 1, 'https://example.com/campus-5.jpg')
) AS v(Code, Name, Address, Phone, IsActive, ImageUrl)
WHERE NOT EXISTS (
    SELECT 1
    FROM Campuses c
    WHERE c.Code = v.Code
);
