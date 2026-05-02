# API Guardian Today And Current Stations

## 1. Muc dich

File nay tach rieng 2 phan moi:

- Guardian xem hom nay con di xe nao va neu co thi xem booking ngay mai
- `BusTripProgress/Current` tra hoc sinh theo tung tram

## 2. API cho guardian

### GET `/api/Booking/GetTodayBusRunsByGuardian/{guardianId}`

Muc dich:

- cho guardian xem lich hom nay con duoc xep len xe nao
- xem tai xe, giao vien, tram don
- xem hoc sinh da len dung xe chua
- xem hoc sinh co dang o xe khac khong
- xem booking cua ngay mai neu co

Query:

- `serviceDate`
- neu khong truyen thi backend tu lay ngay hien tai

Vi du:

```http
GET /api/Booking/GetTodayBusRunsByGuardian/2?serviceDate=2026-04-30
```

Response:

```json
{
  "message": "Lay danh sach con di xe trong ngay thanh cong",
  "data": [
    {
      "todayBusRun": {
        "bookingId": 1,
        "studentId": 1,
        "studentCode": "ST001",
        "studentName": "Nguyen Minh Khang",
        "studentAvatarUrl": "https://cdn.schoolbus.com/students/st001.jpg",
        "routeId": 1,
        "routeName": "Tuyen sang Quan 1 - Co so Q1",
        "serviceDate": "2026-04-30T00:00:00",
        "startTime": "07:00:00",
        "busRunId": 42,
        "busId": 1,
        "busLabel": "BUS-Q1-01",
        "driverId": 34,
        "driverName": "Nguyen Van Tuan",
        "teacherId": 50,
        "teacherName": "Le Duc Huy",
        "runOrder": 1,
        "runStatus": "ASSIGNED",
        "todayStatus": "ON_ASSIGNED_BUS",
        "stationId": 1,
        "stationName": "Tram Cong vien Tao Dan",
        "pickupAddress": "diem safe point",
        "hasCheckedInOnThisBus": true,
        "isCurrentlyOnThisBus": true,
        "currentBusId": null,
        "currentBusLabel": null,
        "isOnDifferentBusThanAssigned": false
      },
      "bookingTomorrow": {
        "id": 9,
        "studentId": 1,
        "studentCode": "ST001",
        "studentName": "Nguyen Minh Khang",
        "guardianId": 2,
        "guardianName": "Nguyen Lan Huong",
        "routeId": 1,
        "routeName": "Tuyen sang Quan 1 - Co so Q1",
        "serviceDate": "2026-05-01T00:00:00",
        "startTime": "07:00:00",
        "stationId": 1,
        "stationName": "Tram Cong vien Tao Dan",
        "stationAddress": "55C Nguyen Thi Minh Khai, Quan 1, TP.HCM",
        "pickupAddress": "diem safe point",
        "latitude": 10.77978,
        "longitude": 106.69165,
        "status": "CONFIRMED",
        "note": "Don tai cong cong vien Tao Dan, phu huynh cho san.",
        "createdAt": "2026-04-29T17:09:02.6299474"
      }
    }
  ]
}
```

Rule:

- `todayBusRun` la bus run cua ngay dang xem
- `bookingTomorrow` chi la booking cua ngay mai
- neu khong co booking ngay mai thi `bookingTomorrow = null`
- neu hoc sinh duoc xep xe A nhung thuc te len xe B:
  - `todayBusRun` van la xe A
  - `currentBusId` va `currentBusLabel` chi ra xe B
  - `isOnDifferentBusThanAssigned = true`

Gia tri `runStatus` thuong gap:

- `ASSIGNED`: xe chinh da duoc chia
- `BACKUP`: xe du phong

Gia tri `todayStatus` hop le:

- `NOT_CHECKED_IN`: hoc sinh chua len xe nao
- `ON_ASSIGNED_BUS`: hoc sinh dang o dung xe duoc phan
- `ON_DIFFERENT_BUS`: hoc sinh dang o mot xe khac voi xe duoc phan
- `CHECKED_OUT`: hoc sinh da len xe duoc phan truoc do va da xuong xe

## 3. API Current theo tung tram

### GET `/api/BusTripProgress/Current?busId={busId}&busRunId={busRunId}`

Muc dich:

- xem xe dang o dau
- xem tram tiep theo
- xem hoc sinh trong tung tram

Vi du:

```http
GET /api/BusTripProgress/Current?busId=1&busRunId=42
```

Response:

```json
{
  "message": "Lay trang thai chuyen xe thanh cong",
  "data": {
    "busId": 1,
    "busRunId": 42,
    "routeId": 1,
    "routeName": "Tuyen sang Quan 1 - Co so Q1",
    "rideDate": "2026-04-30T00:00:00",
    "startTime": "07:00:00",
    "tripStatus": "AT_STATION",
    "currentStationId": 1,
    "currentStationName": "Tram Cong vien Tao Dan",
    "arrivedAt": "2026-04-30T06:59:00",
    "nextStationId": 2,
    "nextStationName": "Tram Cho Ben Thanh",
    "nextOrderIndex": 2,
    "isCompleted": false,
    "stations": [
      {
        "stationId": 1,
        "stationName": "Tram Cong vien Tao Dan",
        "latitude": 10.77978,
        "longitude": 106.69165,
        "orderIndex": 1,
        "isVisited": true,
        "arrivedAt": "2026-04-30T06:59:00",
        "students": [
          {
            "studentId": 1,
            "studentCode": "ST001",
            "studentName": "Nguyen Minh Khang",
            "stationId": 1,
            "stationName": "Tram Cong vien Tao Dan",
            "pickupAddress": "diem safe point",
            "pickupLatitude": 10.77978,
            "pickupLongitude": 106.69165,
            "hasCheckedInOnThisBus": true,
            "isCurrentlyOnThisBus": true,
            "currentBusId": null,
            "currentBusLabel": null,
            "isOnDifferentBusThanAssigned": false
          }
        ]
      },
      {
        "stationId": 2,
        "stationName": "Tram Cho Ben Thanh",
        "latitude": 10.77251,
        "longitude": 106.69802,
        "orderIndex": 2,
        "isVisited": false,
        "arrivedAt": null,
        "students": [
          {
            "studentId": 28,
            "studentCode": "STB026",
            "studentName": "Tran Ngoc Anh",
            "stationId": 2,
            "stationName": "Tram Cho Ben Thanh",
            "pickupAddress": "diem safe point",
            "pickupLatitude": 10.77401,
            "pickupLongitude": 106.69352,
            "hasCheckedInOnThisBus": false,
            "isCurrentlyOnThisBus": false,
            "currentBusId": 8,
            "currentBusLabel": "BUS-SEED-06",
            "isOnDifferentBusThanAssigned": true
          }
        ]
      }
    ]
  }
}
```

Rule:

- `stations[].students` la hoc sinh cua tung tram
- hoc sinh duoc group theo `stationId` cua booking
- neu hoc sinh dang o xe khac thi van nam trong tram booking goc
- trang thai thuc te nam o:
  - `hasCheckedInOnThisBus`
  - `isCurrentlyOnThisBus`
  - `currentBusId`
  - `currentBusLabel`
  - `isOnDifferentBusThanAssigned`

## 4. Cach hieu nhanh cho FE

- `todayBusRun` dung cho man hinh phu huynh xem hom nay con di xe nao
- `bookingTomorrow` dung cho man hinh nhac lich ngay mai
- `Current.stations[].students` dung cho man hinh tai xe/giao vien render theo tram

## 5. Case dac biet

### Hoc sinh len dung xe

- `hasCheckedInOnThisBus = true`
- `isCurrentlyOnThisBus = true`
- `currentBusId = null`
- `isOnDifferentBusThanAssigned = false`

### Hoc sinh da xuong xe

- `hasCheckedInOnThisBus = true`
- `isCurrentlyOnThisBus = false`

### Hoc sinh len xe khac

- `hasCheckedInOnThisBus = false`
- `isCurrentlyOnThisBus = false`
- `currentBusId != null`
- `currentBusLabel != null`
- `isOnDifferentBusThanAssigned = true`
