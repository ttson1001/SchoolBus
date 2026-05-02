# Flow Booking E2E cho UI

Tai lieu nay chi tap trung vao flow **Booking** de UI de trien khai.
Base URL: `BASE_URL` (vd `https://api.faceride.site`).

---

## 1) Quy uoc response

Envelope chung:

```json
{
  "message": "string",
  "data": {}
}
```

- Thanh cong: HTTP `200`
- Loi nghiep vu/validate: HTTP `400` (`data` thuong `null`)

Status booking:
- `PENDING`
- `CONFIRMED`
- `CANCELLED`

Mau loi chung:

```json
{
  "message": "Noi dung loi nghiep vu",
  "data": null
}
```

---

## 2) UI can ID gi va lay tu dau

| Can du lieu | Lay tu API | Field can dung |
|---|---|---|
| `studentId` | `GET /api/Student/GetMyStudents` (guardian) hoac `Student/Search` (admin) | `data[].id` |
| `routeId` hop le de booking | `GET /api/Order/GetActiveByStudent/{studentId}` | `data.selectedRouteIds[]` |
| `stationId` | `GET /api/BusRoute/Get/{routeId}` | `data.stations[].id` (hoac danh sach station cua route) |
| `campusId` (chi de validate/phu tro) | `GET /api/Student/Get/{studentId}` | `data.campusId` |
| `bookingId` | `GET /api/Booking/Search` | `data.items[].id` |
| `busRunId`, `busId` (van hanh sau booking) | `GET /api/Booking/GetBusRuns?serviceDate=...` | `data[].id`, `data[].busId` |

---

## 3) Flow booking cho phu huynh (tao/sua/huy)

## Buoc 1 - Lay hoc sinh cua minh

**GET** `/api/Student/GetMyStudents`

Lay `studentId` de dat lich.

Response 200 (rut gon):

```json
{
  "message": "Lấy danh sách student thành công",
  "data": [
    {
      "id": 1,
      "fullName": "Tran Binh",
      "campusId": 1,
      "campusName": "Campus Quan 1"
    }
  ]
}
```

## Buoc 2 - Lay order active cua hoc sinh (de lay route duoc phep booking)

**GET** `/api/Order/GetActiveByStudent/{studentId}`

Lay `selectedRouteIds` trong order active.

Vi du response (rut gon):

```json
{
  "message": "Lay order thanh cong",
  "data": {
    "id": 99,
    "studentId": 1,
    "status": "PAID",
    "selectedRouteIds": [10, 12]
  }
}
```

Neu chua co goi active:

```json
{
  "message": "Hoc sinh chua co goi active",
  "data": null
}
```

## Buoc 3 - Chon route tu `selectedRouteIds`

- Neu chi co 1 route -> UI auto chon route do.
- Neu co nhieu route -> UI cho phu huynh chon trong list `selectedRouteIds`.
- Khong cho chon route ngoai order active.

## Buoc 4 - Chon tram don

Cach de UI chon tram dung route:

- `GET /api/BusRoute/Get/{routeId}` de lay cac tram cua route
- Chon `stationId` tu tram thuoc route

Response 200 (rut gon):

```json
{
  "message": "Lấy bus route thành công",
  "data": {
    "id": 10,
    "name": "Tuyen A - Sang",
    "stationIds": [2, 3, 5],
    "stations": [
      { "id": 2, "name": "Tram Cong vien" },
      { "id": 3, "name": "Tram Cho Ben Thanh" }
    ]
  }
}
```

## Buoc 5 - Lay khung gio booking (WeeklySlots)

**GET** `/api/Booking/WeeklySlots`

- API tra khung gio tu hom nay den 7 ngay sau (tong 8 ngay), theo cau hinh he thong.
- UI dung de render picker gio/slot hop le truoc khi tao booking.

Response 200 (rut gon):

```json
{
  "message": "Lay khung gio booking theo tuan thanh cong",
  "data": {
    "weekStartDate": "2026-05-10",
    "weekEndDate": "2026-05-17",
    "days": [
      {
        "date": "2026-05-10",
        "dayName": "Chủ nhật",
        "slots": [
          { "startTime": "07:00", "kind": "hard" },
          { "startTime": "07:15", "kind": "soft" }
        ]
      }
    ]
  }
}
```

## Buoc 6 - Tao booking

**POST** `/api/Booking/Create`

Request:

```json
{
  "studentId": 1,
  "routeId": 10,
  "serviceDate": "2026-05-10T00:00:00Z",
  "startTime": "07:00:00",
  "stationId": 2,
  "pickupAddress": "55C Nguyen Thi Minh Khai, Quan 1, TP.HCM",
  "latitude": 10.77678,
  "longitude": 106.69015,
  "note": "Don truoc 5 phut"
}
```

Response 200 (rut gon):

```json
{
  "message": "Tao booking thanh cong",
  "data": {
    "id": 100,
    "studentId": 1,
    "routeId": 10,
    "stationId": 2,
    "status": "PENDING"
  }
}
```

Response 400 (vi du):

```json
{
  "message": "Booking da ton tai cho hoc sinh o khung gio nay",
  "data": null
}
```

```json
{
  "message": "Diem don cach tram 'Tram Cong vien' 5.20km, vuot qua gioi han 4km",
  "data": null
}
```

## Buoc 7 - Sua booking

**PUT** `/api/Booking/Update/{id}`

Request mau:

```json
{
  "startTime": "07:15:00",
  "stationId": 3,
  "pickupAddress": "12 Vo Van Tan, Quan 3, TP.HCM",
  "latitude": 10.77612,
  "longitude": 106.68777,
  "status": "CONFIRMED",
  "note": "Doi diem don"
}
```

Response 200 (rut gon):

```json
{
  "message": "Cap nhat booking thanh cong",
  "data": {
    "id": 100,
    "routeId": 10,
    "stationId": 3,
    "status": "CONFIRMED"
  }
}
```

## Buoc 8 - Huy booking

**DELETE** `/api/Booking/Delete/{id}`

Response 200:

```json
{
  "message": "Xoa booking thanh cong",
  "data": null
}
```

---

## 4) Man danh sach booking

**GET** `/api/Booking/Search?studentId=1&routeId=&serviceDate=&status=&page=1&pageSize=20`

UI nen cho loc:
- theo hoc sinh (`studentId`)
- theo ngay (`serviceDate`)
- theo trang thai (`status`: `PENDING|CONFIRMED|CANCELLED`)

Response 200 (rut gon):

```json
{
  "message": "Lay danh sach booking thanh cong",
  "data": {
    "totalItems": 2,
    "page": 1,
    "pageSize": 20,
    "items": [
      {
        "id": 100,
        "studentId": 1,
        "routeId": 10,
        "stationId": 2,
        "pickupAddress": "55C Nguyen Thi Minh Khai, Quan 1, TP.HCM",
        "status": "CONFIRMED"
      }
    ]
  }
}
```

---

## 5) Flow van hanh sau booking (admin/driver/teacher)

## 5.1 Chia xe tu dong

**POST** `/api/Booking/AutoAssignBusRuns`

```json
{
  "routeId": 10,
  "serviceDate": "2026-05-10T00:00:00Z",
  "startTime": "07:00:00"
}
```

Ket qua tao `BusRun` + gan hoc sinh vao tung run.

Response 200 (rut gon):

```json
{
  "message": "Chia hoc sinh vao xe thanh cong",
  "data": [
    {
      "id": 1001,
      "busId": 1,
      "routeId": 10,
      "startTime": "07:00:00",
      "status": "ASSIGNED"
    }
  ]
}
```

## 5.2 Xem run theo ngay

**GET** `/api/Booking/GetBusRuns?serviceDate=2026-05-10&routeId=&busId=&driverId=&teacherId=`

`students[]` trong tung run da co:
- `pickupAddress`
- `hasCheckedInOnThisBus`
- `isCurrentlyOnThisBus`
- `currentBusId`
- `currentBusLabel`
- `isOnDifferentBusThanAssigned`

=> UI van hanh co the biet hoc sinh dang o dung xe hay bi lech xe.

Response 200 (rut gon):

```json
{
  "message": "Lay danh sach lich chay thuc te thanh cong",
  "data": [
    {
      "id": 1001,
      "routeId": 10,
      "busId": 1,
      "driverId": 15,
      "teacherId": 22,
      "students": [
        {
          "bookingId": 500,
          "studentId": 3,
          "pickupAddress": "55C Nguyen Thi Minh Khai, Quan 1, TP.HCM",
          "hasCheckedInOnThisBus": true,
          "isCurrentlyOnThisBus": true,
          "currentBusId": 1,
          "currentBusLabel": "BUS-01",
          "isOnDifferentBusThanAssigned": false
        }
      ]
    }
  ]
}
```

---

## 6) Luu y tranh loi UI

- Route booking phai lay tu `Order/GetActiveByStudent/{studentId}` (`selectedRouteIds`), khong lay route random theo campus.
- `campusId` cua hoc sinh chi de doi chieu/validate them neu can.
- Voi xe chay nhieu ca trong ngay, cac API attendance nen truyen them `busRunId` de khong lon hoc sinh giua ca sang/chieu.
- Sau khi checkin/checkout, refresh lai danh sach trang thai hoc sinh tren xe.
- Neu API tra `200` + `data: []` thi xem la khong co du lieu, khong phai loi.
