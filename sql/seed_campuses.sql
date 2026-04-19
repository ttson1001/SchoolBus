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
WHERE NOT EXISTS (
    SELECT 1
    FROM Campuses c
    WHERE c.Code = v.Code
);
