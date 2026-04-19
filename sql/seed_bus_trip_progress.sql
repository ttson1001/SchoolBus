/*
  Seed bảng BusTripProgresses — khớp entity hiện tại (chỉ xác nhận đến trạm: ArrivedAt, không còn DepartedAt).
  Cột: BusId, BusScheduleId, RouteId, StationId, RideDate, OrderIndex, ArrivedAt
  Unique: (BusScheduleId, RideDate, OrderIndex)

  Điều kiện: migration áp dụng, đã có BusSchedules + BusRouteStations.
  Mỗi BusSchedule thuộc BUS-01…BUS-10: 1 bản ghi tại trạm đầu tiên của tuyến, RideDate = ngày hiện tại.
  (BUS-01 có 2 lịch/ngày thì có 2 dòng tiến trình demo nếu chạy sau seed lịch đủ.)
*/

IF OBJECT_ID(N'[dbo].[BusTripProgresses]', N'U') IS NULL
BEGIN
    PRINT N'[BusTripProgresses] chưa tồn tại — chạy dotnet ef database update trước.';
    RETURN;
END;

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

PRINT N'Seed BusTripProgresses xong (hoặc đã có bản ghi cho ngày hôm nay).';
