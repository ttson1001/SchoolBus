# Test Flow: 50 Hoc Sinh Booking -> Auto Chia Xe Theo Diem Don

Tai lieu nay mo ta cach test luong:
- Tao/kiem tra 50 hoc sinh
- Tao booking cho ca 50 hoc sinh cung khung gio
- Goi auto assign
- Kiem tra ket qua phan bo vao xe va diem don

## 1) Dieu kien tien quyet

- Da seed du lieu bang `doc/seed_db_realistic.sql` (ban moi them 50 hoc sinh).
- API dang chay (`dotnet run`).
- Co tai khoan admin de login.
- Co it nhat 1 route hoat dong va co station.
- Co du xe `ACTIVE` (uu tien xe 25+ cho de auto-assign).

## 2) Dang nhap lay token

### API
`POST /api/Account/Login`

### Request body
```json
{
  "email": "tran.minh.khoa@schoolbus.vn",
  "password": "123456",
  "deviceToken": "admin_test_token"
}
```

### Ket qua mong doi
- HTTP `200`
- Response co token (`data.token` hoac field tuong duong).

---

## 3) Kiem tra danh sach hoc sinh du 50 nguoi

### API
`GET /api/Student/Search?page=1&pageSize=100&status=ACTIVE`

### Ket qua mong doi
- HTTP `200`
- `totalItems >= 50`
- Lay ra danh sach 50 hoc sinh can test (nen uu tien ma `STB001` -> `STB050` neu da seed theo file).

---

## 4) Tao booking cho 50 hoc sinh

Test theo 1 ngay va 1 khung gio de de phan tich.

- `serviceDate`: chon ngay mai (vi service validate > today)
- `startTime`: `07:00:00`
- `routeId`: route test (vi du `1`)

### API tao booking
`POST /api/Booking/Create`

### Request body mau
```json
{
  "studentId": 1,
  "routeId": 1,
  "serviceDate": "2026-04-30T00:00:00Z",
  "startTime": "07:00:00",
  "stationId": 1,
  "latitude": 10.77978,
  "longitude": 106.69165,
  "note": "Test booking so luong lon 50 hoc sinh"
}
```

## 4.1) Script PowerShell tao booking hang loat (50 hoc sinh)

> Ban thay `BASE_URL`, `TOKEN`, `ROUTE_ID`, `SERVICE_DATE` cho dung moi truong.

```powershell
$BASE_URL = "https://localhost:5001"
$TOKEN = "<admin_jwt_token>"
$ROUTE_ID = 1
$SERVICE_DATE = "2026-04-30T00:00:00Z"
$START_TIME = "07:00:00"

# Lay 50 hoc sinh active
$studentsResp = Invoke-RestMethod -Method GET -Uri "$BASE_URL/api/Student/Search?page=1&pageSize=100&status=ACTIVE" -Headers @{
  Authorization = "Bearer $TOKEN"
}

# Tuy theo response wrapper cua ban
$students = $studentsResp.data.items | Select-Object -First 50

$ok = 0
$fail = 0

foreach ($s in $students) {
  $body = @{
    studentId = $s.id
    routeId = $ROUTE_ID
    serviceDate = $SERVICE_DATE
    startTime = $START_TIME
    stationId = 1
    latitude = 10.77978
    longitude = 106.69165
    note = "Bulk booking for load test"
  } | ConvertTo-Json

  try {
    Invoke-RestMethod -Method POST -Uri "$BASE_URL/api/Booking/Create" -Headers @{
      Authorization = "Bearer $TOKEN"
      "Content-Type" = "application/json"
    } -Body $body
    $ok++
  }
  catch {
    $fail++
    Write-Host "Booking fail studentId=$($s.id): $($_.Exception.Message)"
  }
}

Write-Host "Create booking done. Success=$ok, Fail=$fail"
```

### Ket qua mong doi
- So booking thanh cong gan 50 (hoac dung 50 neu du dieu kien).
- Neu fail: xem loi duplicate/slot/time/route de fix.

---

## 5) Goi auto assign de chia xe

### API
`POST /api/Booking/AutoAssignBusRuns`

### Request body
```json
{
  "routeId": 1,
  "serviceDate": "2026-04-30T00:00:00Z",
  "startTime": "07:00:00"
}
```

### Ket qua mong doi
- HTTP `200`
- Tra ve danh sach `BusRun`
- Co 1 xe backup (`status = BACKUP`)
- Cac xe chinh co `status = ASSIGNED`

---

## 6) Kiem tra ket qua chia xe

### API 1: Lay danh sach bus run
`GET /api/Booking/GetBusRuns?serviceDate=2026-04-30&routeId=1`

#### Ky vong
- Tong so hoc sinh duoc gan = 50
- Moi xe chinh khong vuot `usableCapacity`
- Xe backup co `assignedStudentCount = 0` (trong case binh thuong)

### API 2: Kiem tra booking da chuyen trang thai
`GET /api/Booking/Search?routeId=1&serviceDate=2026-04-30&status=CONFIRMED&page=1&pageSize=100`

#### Ky vong
- So booking `CONFIRMED` = so booking da duoc auto assign (ly tuong la 50).

---

## 7) Kiem tra phan bo theo diem don (pickup station)

### C1 - Kiem qua API GetBusRuns
Trong tung `BusRun.Students[]`, dem theo `stationId`/`stationName`.

#### Ky vong
- Hoc sinh cung station duoc phan bo hop ly.
- Khong co hoc sinh bi mat (tong cong van = 50).

### C2 - Kiem nhanh bang SQL
```sql
DECLARE @ServiceDate DATE = '2026-04-30';
DECLARE @StartTime TIME = '07:00:00';
DECLARE @RouteId BIGINT = 1;

SELECT 
    br.Id AS BusRunId,
    br.RunOrder,
    br.Status AS BusRunStatus,
    bs.Id AS StationId,
    bs.Name AS StationName,
    COUNT(*) AS StudentCount
FROM BusRunStudents brs
JOIN BusRuns br ON br.Id = brs.BusRunId
JOIN Bookings b ON b.Id = brs.BookingId
JOIN BusStations bs ON bs.Id = b.StationId
WHERE br.RouteId = @RouteId
  AND CAST(br.ServiceDate AS DATE) = @ServiceDate
  AND br.StartTime = @StartTime
GROUP BY br.Id, br.RunOrder, br.Status, bs.Id, bs.Name
ORDER BY br.RunOrder, StationName;
```

#### Ky vong
- Co so lieu theo tung station trong tung run.
- Tong tat ca `StudentCount` = 50.

---

## 8) Checklist PASS/FAIL

- [ ] Login admin thanh cong, lay duoc token.
- [ ] Lay duoc 50 hoc sinh active de test.
- [ ] Tao booking bulk thanh cong (muc tieu 50).
- [ ] AutoAssignBusRuns tra ve 200.
- [ ] Tong hoc sinh trong BusRunStudents = 50.
- [ ] Khong xe nao vuot suc chua su dung.
- [ ] Co xe backup va khong bi nhan hoc sinh trong case du xe chinh.
- [ ] Booking da chuyen `CONFIRMED`.
- [ ] Phan bo theo diem don hop ly, khong mat hoc sinh.

---

## 9) Loi thuong gap va cach xu ly nhanh

- Loi `ServiceDate phai duoc dat truoc it nhat 1 ngay`
  - Chon `serviceDate = ngay mai` tro di.
- Loi `Khung gio booking...`
  - Dung gio theo `BookingSlots` trong appsettings (mac dinh step 60 phut).
- Loi `Khong du xe 25 cho...` / `Khong co xe 15 cho backup...`
  - Tang so xe `ACTIVE`, dam bao co xe >=25 cho va >=15 cho.
- Loi duplicate booking
  - Xoa booking cu hoac doi `serviceDate/startTime`.

