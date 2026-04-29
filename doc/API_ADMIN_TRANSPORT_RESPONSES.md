# API Response — Admin Transport Screen

Tai lieu rieng cho 2 API admin:

- `GET https://api.faceride.site/api/Booking/GetBusRuns`
- `GET https://api.faceride.site/api/BusTripProgress/DriverSchedules`

Envelope BE dang dung:

```json
{
  "message": "string",
  "data": {}
}
```

Thanh cong: HTTP `200`.
That bai nghiep vu: HTTP `400` (thuong `data: null`).

---

## 1) GET `/api/Booking/GetBusRuns`

### Query

```json
{
  "serviceDate": "2026-04-29",
  "routeId": 10,
  "busId": 1,
  "driverId": 15,
  "teacherId": 22
}
```

- `serviceDate` la bat buoc theo nghiep vu.
- Cac param khac la tuy chon de loc.

### Response thanh cong (co du lieu)

```json
{
  "message": "Lay danh sach lich chay thuc te thanh cong",
  "data": [
    {
      "id": 1001,
      "routeId": 10,
      "routeName": "Tuyen A - Sang",
      "serviceDate": "2026-04-29T00:00:00",
      "startTime": "07:00:00",
      "busId": 1,
      "busLabel": "BUS-01",
      "driverId": 15,
      "driverName": "Tai xe Nguyen A",
      "teacherId": 22,
      "teacherName": "GV Tran B",
      "seatCapacity": 45,
      "usableCapacity": 25,
      "assignedStudentCount": 20,
      "runOrder": 1,
      "status": "ASSIGNED",
      "students": [
        {
          "bookingId": 500,
          "studentId": 3,
          "studentCode": "ST003",
          "studentName": "Le Van C",
          "stationId": 2,
          "stationName": "Tram Cong vien",
          "pickupAddress": "55C Nguyen Thi Minh Khai, Quan 1, TP.HCM",
          "hasCheckedInOnThisBus": true,
          "currentBusId": null,
          "currentBusLabel": null,
          "isOnDifferentBusThanAssigned": false
        }
      ]
    }
  ]
}
```

### Response thanh cong (khong co du lieu)

```json
{
  "message": "Lay danh sach lich chay thuc te thanh cong",
  "data": []
}
```

### Response loi (400)

```json
{
  "message": "<noi dung loi>",
  "data": null
}
```

> Ghi chu: endpoint nay thuong tra `200` + `[]` khi khong tim thay run.

---

## 2) GET `/api/BusTripProgress/DriverSchedules`

### Query

```json
{
  "driverId": 15,
  "rideDate": "2026-04-29",
  "atTime": "07:00:00"
}
```

### Response thanh cong (200)

```json
{
  "message": "L?y danh sách l?ch ch?y c?a tài x? thành công",
  "data": [
    {
      "busRunId": 1001,
      "busId": 1,
      "busLabel": "BUS-01",
      "routeId": 10,
      "routeName": "Tuyen A - Sang",
      "rideDate": "2026-04-29T00:00:00",
      "startTime": "07:00:00",
      "shiftType": "ASSIGNED",
      "isRunningNow": true,
      "isUpcoming": false,
      "isCompleted": false,
      "isRecommended": true,
      "students": [
        {
          "studentId": 3,
          "studentCode": "ST003",
          "studentName": "Le Van C",
          "stationId": 2,
          "stationName": "Tram Cong vien",
          "pickupAddress": "55C Nguyen Thi Minh Khai, Quan 1, TP.HCM",
          "pickupLatitude": 10.77678,
          "pickupLongitude": 106.69015,
          "hasCheckedInOnThisBus": true,
          "currentBusId": null,
          "currentBusLabel": null,
          "isOnDifferentBusThanAssigned": false
        }
      ],
      "stations": [
        {
          "stationId": 1,
          "stationName": "Tram A",
          "latitude": 10.77,
          "longitude": 106.69,
          "orderIndex": 1,
          "isVisited": true,
          "arrivedAt": "2026-04-29T00:15:00Z"
        }
      ]
    }
  ]
}
```

### Response loi (400) - vi du co that trong service

```json
{
  "message": "DriverId phai lon hon 0",
  "data": null
}
```

```json
{
  "message": "Tai xe khong co lich chay nao trong ngay da chon",
  "data": null
}
```

---

## Link tham chieu

- KQ thuc te `GetBusRuns`: [api.faceride.site/api/Booking/GetBusRuns](https://api.faceride.site/api/Booking/GetBusRuns)
