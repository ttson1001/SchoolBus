# Luong 3 - Tai xe app: ho so ca nhan -> lich trinh -> den tram -> lich su da chay

Tai lieu E2E cho app tai xe theo luong ban yeu cau.
Base URL: `BASE_URL` (vd `https://api.faceride.site`).

---

## 1) Quy uoc response cho UI

BE dang dung envelope:

```json
{
  "message": "string",
  "data": {}
}
```

- Thanh cong: HTTP `200`
- Loi nghiep vu/validate: HTTP `400` (thuong `data: null`)

Mau that bai:

```json
{
  "message": "DriverId phai lon hon 0",
  "data": null
}
```

### 1.1) Status co the co (cho UI map badge/mau)

| Nhom | Field | Gia tri |
|------|-------|---------|
| Lich chuyen theo tai xe | `shiftType` | `ASSIGNED`, `BACKUP` |
| Lich chuyen theo tai xe | `isRunningNow` / `isUpcoming` / `isCompleted` / `isRecommended` | `true` / `false` |
| Trang thai hien tai chuyen | `tripStatus` (API `Current`) | `NOT_STARTED`, `AT_STATION`, `COMPLETED` |
| Lich su chuyen | `tripStatus` (API `History`) | `COMPLETED`, `IN_PROGRESS`, `HAS_ATTENDANCE`, `NO_DATA` |
| Hoc sinh tren chuyen | `hasCheckedInOnThisBus` | `true` / `false` |
| Hoc sinh tren chuyen | `isOnDifferentBusThanAssigned` | `true` / `false` |

> Ghi chu: `CurrentBusId` / `CurrentBusLabel` se co gia tri khi hoc sinh dang o mot xe khac (check-in roi nhung chua check-out).

---

## 2) Thu tu luong tai xe

1. Dang nhap tai xe.
2. Xem/sua thong tin ca nhan cua tai xe.
3. Lay lich trong ngay cua tai xe (`DriverSchedules`) va hien thi sap xep tu sang den toi.
4. Chon chuyen hien tai -> goi `Current` de biet xe dang/sap den tram nao + danh sach hoc sinh can don.
5. Khi toi tram -> bam `Arrive`.
6. Xem lich su cac chuyen da chay tu truoc den nay (chi lay chuyen cua chinh tai xe).

---

## 3) Chi tiet API

### 3.0 - Ho so ca nhan tai xe (xem/sua)

**GET** `/api/Account/Me` (Bearer token)

- Dung de lay `driverId` (user id hien tai) va profile hien tai.

**PUT** `/api/User/Update/{id}`

- Dung `id = driverId` lay tu `Account/Me`.
- Chi cho phep cap nhat 3 field: `fullName`, `phone`, `avatarUrl`.

Request mau:

```json
{
  "fullName": "Nguyen Van Driver",
  "phone": "0909000111",
  "avatarUrl": "https://res.cloudinary.com/.../driver-avatar.jpg"
}
```

Loi 400 hay gap:

```json
{ "message": "Email da ton tai.", "data": null }
```

---

### 3.1 - UI lay `busRunId` nhu the nao (quan trong)

Trong vai tro app tai xe, UI khong hard-code `busRunId` (vd `1001`) ma lay dong theo ngay:

1. Login thanh cong -> goi `GET /api/Account/Me` de lay `driverId` (chinh la `data.id`).  
2. Goi `GET /api/BusTripProgress/DriverSchedules?driverId=<driverId>&rideDate=<ngay>&atTime=<gio hien tai>`.  
3. Tu `data[]` cua API tren, moi item deu co `busRunId` + `busId`.

Rule de UI chon chuyen:

- Uu tien item co `isRunningNow = true`.
- Neu khong co, chon item co `isRecommended = true`.
- Neu van khong co, chon item dau tien theo `startTime` tang dan.

Sau khi chon item:

- Dung `busId` + `busRunId` de goi `Current`.
- Dung `busId` + `busRunId` + `nextStationId` de goi `Arrive`.

Vi du nhanh:

```json
{
  "driverIdFromMe": 15,
  "pickedRun": {
    "busRunId": 1001,
    "busId": 1,
    "isRunningNow": true
  }
}
```

---

### Buoc 1 - Login tai xe

**POST** `/api/Account/Login`

Request:

```json
{
  "email": "driver@example.com",
  "password": "123456",
  "deviceToken": "fcm_driver_optional"
}
```

Thanh cong:

```json
{
  "message": "Dang nhap thanh cong.",
  "data": {
    "token": "eyJhbGciOi..."
  }
}
```

---

### Buoc 2 - Lay lich trong ngay cua tai xe (sort sang -> toi)

**GET** `/api/BusTripProgress/DriverSchedules?driverId=15&rideDate=2026-04-29&atTime=07:00:00`

- Service dang `OrderBy(StartTime).ThenBy(RunOrder)` -> da sap xep tang dan theo gio.
- FE van nen sort lai `startTime ASC` de an toan khi merge data.

Thanh cong (rut gon):

```json
{
  "message": "L?y danh sùch l?ch ch?y c?a tùi x? thùnh cùng",
  "data": [
    {
      "busRunId": 1001,
      "busId": 1,
      "busLabel": "BUS-01",
      "routeId": 10,
      "routeName": "Tuyen A - Sang",
      "rideDate": "2026-04-29T00:00:00",
      "startTime": "07:00:00",
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
      ]
    }
  ]
}
```

Loi 400 hay gap:

```json
{ "message": "DriverId phai lon hon 0", "data": null }
```

```json
{ "message": "Tai xe khong co lich chay nao trong ngay da chon", "data": null }
```

---

### Buoc 3 - Bat dau chuyen: xem trang thai hien tai + tram tiep theo

**GET** `/api/BusTripProgress/Current?busId=1&busRunId=1001&rideDate=2026-04-29`

Thanh cong:

```json
{
  "message": "L?y tr?ng thùi chuy?n xe thùnh cùng",
  "data": {
    "busId": 1,
    "busRunId": 1001,
    "routeId": 10,
    "routeName": "Tuyen A - Sang",
    "rideDate": "2026-04-29T00:00:00",
    "startTime": "07:00:00",
    "tripStatus": "AT_STATION",
    "currentStationId": 1,
    "currentStationName": "Tram A",
    "nextStationId": 2,
    "nextStationName": "Tram Cong vien",
    "nextOrderIndex": 2,
    "isCompleted": false,
    "stations": []
  }
}
```

---

### Buoc 4 - Nhan Arrive khi den tram

**POST** `/api/BusTripProgress/Arrive`

Request:

```json
{
  "busId": 1,
  "busRunId": 1001,
  "stationId": 2,
  "arrivedAt": "2026-04-29T07:30:00Z"
}
```

Thanh cong:

```json
{
  "message": "Xùc nh?n ??n tr?m thùnh cùng",
  "data": {
    "id": 5001,
    "busId": 1,
    "busRunId": 1001,
    "routeId": 10,
    "routeName": "Tuyen A - Sang",
    "stationId": 2,
    "stationName": "Tram Cong vien",
    "orderIndex": 2,
    "rideDate": "2026-04-29T00:00:00",
    "arrivedAt": "2026-04-29T07:30:00Z"
  }
}
```

Loi 400 hay gap:

```json
{ "message": "Xe phai xac nhan den tram 'Tram A' truoc", "data": null }
```

```json
{ "message": "Tram khong thuoc tuyen cua chuyen xe nay", "data": null }
```

---

### Buoc 5 - Xem lich su da chay cua tai xe (tu truoc den nay)

API hien co:

**GET** `/api/BusTripProgress/History?busId=&routeId=&campusId=&fromDate=2024-01-01&toDate=2026-04-29`

Do API chua nhan `driverId` tren query, app tai xe lam nhu sau:

1. Lay `driverId` tu `Account/Me`.
2. Goi `History` voi khoang ngay rong (tu ngay can xem den hien tai).
3. FE loc client-side: chi giu item co `driverId == driverId hien tai`.
4. Sap xep `rideDate DESC`, `startTime DESC` de hien thi lich su moi nhat truoc.

Mau response (rut gon):

```json
{
  "message": "L?y l?ch s? chuy?n ?i thùnh cùng",
  "data": [
    {
      "busRunId": 1001,
      "busId": 1,
      "routeId": 10,
      "routeName": "Tuyen A - Sang",
      "rideDate": "2026-04-29T00:00:00",
      "startTime": "07:00:00",
      "driverId": 15,
      "driverName": "Nguyen Van Driver",
      "tripStatus": "COMPLETED",
      "plannedStudentCount": 20,
      "actualStudentCount": 18
    }
  ]
}
```

---

## 4) Ghi chu cho UI

- Badge run theo `isRunningNow/isUpcoming/isCompleted`.
- Danh sach hoc sinh nhat quan theo `students[]` trong `DriverSchedules`.
- Nut `Arrive` bat theo tram tiep theo (`Current.nextStationId`).
- Man ho so: luu `driverId` tu `Account/Me`, update dung `User/Update/{driverId}`.
- Man lich su: loc `driverId` client-side cho den khi BE bo sung query `driverId` cho endpoint `History`.
- Truoc khi goi `Arrive`, UI nen check `nextStationId != null`; neu `null` nghia la chuyen da hoan tat.
