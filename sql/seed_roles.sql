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
WHERE NOT EXISTS (
    SELECT 1
    FROM Roles r
    WHERE r.Name = v.Name
);
