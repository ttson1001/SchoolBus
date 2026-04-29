# Logic Admin E2E Setup And Test

Tai lieu nay mo ta lai toan bo flow hien tai cua he thong SchoolBus theo model moi:

- `Campus`
- `BusStation`
- `BusRoute`
- `Bus`
- `User`
- `Student`
- `Package`
- `Order`
- `Booking`
- `BusRun`
- `BusRunStudent`
- `BusTripProgress`
- `Attendance`

Tai lieu nay bo qua cac module da bi loai khoi runtime nhu:

- `BusSchedule`
- `BusAssignment`
- `StudentBusAssignment`

## 1. Nguyen tac hien tai

- He thong khong con dung `BusSchedule`.
- Slot booking duoc validate bang config `BookingSlots` trong `appsettings`.
- Hien tai slot mac dinh la:

```json
{
  "BookingSlots": {
    "StartHour": 6,
    "EndHour": 18,
    "StepMinutes": 60
  }
}
```

- Nghia la booking chi duoc tao o cac moc:
  - `06:00`
  - `07:00`
  - `08:00`
  - ...
  - `18:00`
- Booking phai dat truoc.
- Booking cho ngay mai bi khoa sau `20:00` hom nay.
- `BusRun` la chuyen xe thuc te trong ngay.
- `BusRunStudent` la bang map hoc sinh nao nam tren xe nao.
- `BusRun` giu thang `DriverId` va `TeacherId`.

## 2. Response format chung

### 2.1 Success

```json
{
  "message": "Tao booking thanh cong",
  "data": null
}
```

Hoac:

```json
{
  "message": "Lay danh sach booking thanh cong",
  "data": {
    "items": [],
    "totalItems": 0,
    "page": 1,
    "pageSize": 10
  }
}
```

### 2.2 Error

```json
{
  "message": "Booking khong ton tai",
  "data": null
}
```

## 3. Luong tong quat

1. Admin tao `Campus`
2. Admin tao `BusStation`
3. Admin tao `BusRoute`
4. Admin tao `Bus`
5. Admin tao `guardian`, `driver`, `teacher`
6. Admin tao `student`
7. Admin tao `package`
8. Guardian mua goi
9. Guardian tao `Booking`
10. Admin chay auto chia xe tu `Booking` sang `BusRun`
11. Admin gan `driver` va `teacher` cho tung `BusRun`
12. App tai xe / giao vien lay danh sach `BusRun` trong ngay
13. Xe den tram thi `BusTripProgress`
14. Tai xe / giao vien check-in, check-out bang `Attendance`

Luu y:
- Khi chay auto chia xe, backend hien tai se co gang gan `driver` va `teacher` ngay trong buoc tao `BusRun`.
- Cach gan la xoay tua: uu tien nguoi dang co it luot chay hon, ai lau chua duoc gan se duoc uu tien truoc.
- Neu admin muon doi tay, van co the goi API gan staff thu cong cho tung `BusRun`.

## 3.1 JSON mau theo luong tong quat

### 1. Admin tao `Campus`

```json
{
  "name": "Campus Quan 1",
  "address": "123 Nguyen Hue, Quan 1, TP.HCM",
  "latitude": 10.7769,
  "longitude": 106.7009,
  "status": "ACTIVE"
}
```

### 2. Admin tao `BusStation`

```json
{
  "name": "Tram Don Quan 1",
  "address": "45 Le Loi, Quan 1, TP.HCM",
  "latitude": 10.7745,
  "longitude": 106.7019,
  "isEnabled": true
}
```

### 3. Admin tao `BusRoute`

```json
{
  "name": "Tuyen Q1 Sang",
  "campusId": 1,
  "isEnabled": true,
  "stations": [
    {
      "stationId": 1,
      "orderIndex": 1
    },
    {
      "stationId": 2,
      "orderIndex": 2
    },
    {
      "stationId": 3,
      "orderIndex": 3
    }
  ]
}
```

### 4. Admin tao `Bus`

Xe 25 cho:

```json
{
  "licensePlate": "51A-12345",
  "capacity": 25,
  "status": "ACTIVE",
  "busNumber": "BUS-25-01",
  "color": "Yellow",
  "busType": "STANDARD",
  "campusId": 1
}
```

Xe 15 cho:

```json
{
  "licensePlate": "51A-67890",
  "capacity": 15,
  "status": "ACTIVE",
  "busNumber": "BUS-15-01",
  "color": "Yellow",
  "busType": "STANDARD",
  "campusId": 1
}
```

### 5. Admin tao `guardian`, `driver`, `teacher`

Guardian:

```json
{
  "email": "guardian01@schoolbus.local",
  "password": "123456",
  "fullName": "Nguyen Thi Guardian 01",
  "phone": "0901000001",
  "avatarUrl": null,
  "roleName": "guardian"
}
```

Driver:

```json
{
  "email": "driver01@schoolbus.local",
  "password": "123456",
  "fullName": "Tran Van Driver 01",
  "phone": "0902000001",
  "avatarUrl": null,
  "driverLicenseNumber": "123456789",
  "driverLicenseClass": "B2",
  "driverLicenseExpiryDate": "2028-12-31T00:00:00",
  "roleName": "driver"
}
```

Teacher:

```json
{
  "email": "teacher01@schoolbus.local",
  "password": "123456",
  "fullName": "Nguyen Thi Teacher 01",
  "phone": "0903000001",
  "avatarUrl": null,
  "roleName": "teacher"
}
```

### 6. Admin tao `student`

```json
{
  "studentCode": "10002",
  "fullName": "Tran Gia Bao",
  "dateOfBirth": "2015-09-20",
  "gender": "MALE",
  "guardianId": 5,
  "campusId": 1,
  "avatarUrl": null
}
```

### 7. Admin tao `package`

Goi 1 route:

```json
{
  "name": "Goi 1 Tuyen",
  "price": 1200000,
  "durationDays": 30,
  "routeLimit": 1,
  "description": "Di 1 tuyen trong 1 thang",
  "status": "ACTIVE",
  "type": "MONTHLY"
}
```

Goi 2 route:

```json
{
  "name": "Goi 2 Tuyen",
  "price": 2000000,
  "durationDays": 30,
  "routeLimit": 2,
  "description": "Di 2 tuyen trong 1 thang",
  "status": "ACTIVE",
  "type": "MONTHLY"
}
```

### 8. Guardian mua goi

```json
{
  "guardianId": 5,
  "studentId": 2,
  "packageId": 1,
  "routeIds": [1]
}
```

### 9. Guardian tao `Booking`

```json
{
  "studentId": 2,
  "routeId": 1,
  "serviceDate": "2026-04-29",
  "startTime": "07:00:00",
  "stationId": 1,
  "latitude": 10.7745,
  "longitude": 106.7019,
  "note": "Di hoc buoi sang"
}
```

### 10. Admin chay auto chia xe tu `Booking` sang `BusRun`

```json
{
  "routeId": 1,
  "serviceDate": "2026-04-29",
  "startTime": "07:00:00"
}
```

Luu y:
- Sau khi chia xe, backend se tu dong gan `driver` va `teacher` neu du nguon luc.
- Thuat toan uu tien xoay tua de tranh mot vai nguoi bi phan cong lien tuc.
- Neu khong du `driver` hoac `teacher` ranh o dung khung gio, API se bao loi.

### 11. Admin gan `driver` va `teacher` cho tung `BusRun`

```json
{
  "driverId": 21,
  "teacherId": 23
}
```

### 12. App tai xe / giao vien lay danh sach `BusRun` trong ngay

API GET nen khong co body. Query mau:

```json
{
  "serviceDate": "2026-04-29",
  "driverId": 21
}
```

Hoac:

```json
{
  "serviceDate": "2026-04-29",
  "teacherId": 23
}
```

### 13. Xe den tram thi `BusTripProgress`

```json
{
  "busId": 1,
  "busRunId": 501,
  "stationId": 1,
  "arrivedAt": "2026-04-29T06:55:00"
}
```

### 14. Tai xe / giao vien check-in, check-out bang `Attendance`

Check-in:

```json
{
  "studentId": 2,
  "busId": 1,
  "stationId": 1,
  "date": "2026-04-29",
  "time": "07:02:00",
  "imageUrl": "https://res.cloudinary.com/demo/image/upload/checkin.jpg"
}
```

Check-out:

```json
{
  "studentId": 2,
  "busId": 1,
  "stationId": 3,
  "date": "2026-04-29",
  "time": "07:45:00",
  "imageUrl": "https://res.cloudinary.com/demo/image/upload/checkout.jpg"
}
```

## 4. Rule booking

### 4.1 Tao booking

API:

`POST /api/Booking/Create`

Body mau:

```json
{
  "studentId": 2,
  "routeId": 1,
  "serviceDate": "2026-04-29",
  "startTime": "07:00:00",
  "stationId": 1,
  "latitude": 10.7745,
  "longitude": 106.7019,
  "note": "Di hoc buoi sang"
}
```

Success:

```json
{
  "message": "Tao booking thanh cong",
  "data": {
    "id": 1001,
    "studentId": 2,
    "studentCode": "10002",
    "studentName": "Tran Gia Bao",
    "guardianId": 5,
    "guardianName": "Nguyen Thi Guardian 01",
    "routeId": 1,
    "routeName": "Tuyen Q1 sang",
    "serviceDate": "2026-04-29T00:00:00",
    "startTime": "07:00:00",
    "stationId": 1,
    "stationName": "Tram Don Quan 1",
    "latitude": 10.7745,
    "longitude": 106.7019,
    "status": "PENDING",
    "note": "Di hoc buoi sang"
  }
}
```

### 4.2 Rule cutoff 20:00

- Truoc `20:00` hom nay:
  - duoc tao booking cho ngay mai
  - duoc sua booking cho ngay mai
  - duoc xoa booking cho ngay mai
- Sau `20:00` hom nay:
  - khong duoc tao, sua, xoa booking cho ngay mai nua
- Booking cho cac ngay xa hon ngay mai van thao tac duoc

Error mau:

```json
{
  "message": "Sau 20:00 khong the booking cho ngay mai",
  "data": null
}
```

### 4.3 Rule trung booking

Khong cho trung:

- `studentId`
- `routeId`
- `serviceDate`
- `startTime`

Error:

```json
{
  "message": "Booking da ton tai cho hoc sinh o khung gio nay",
  "data": null
}
```

## 5. Rule package

Package hien tai dang theo kieu:

- goi `1 route`
- goi `2 route`
- goi `3 route`

Package co `RouteLimit`.

Khi mua goi:

- bat buoc chon dung so route theo `RouteLimit`
- route phai hop le
- route phai thuoc campus hop le cua hoc sinh theo rule hien tai cua backend

Error mau:

```json
{
  "message": "Goi nay yeu cau chon dung 2 tuyen",
  "data": null
}
```

## 6. Logic chia xe tu booking sang bus run

### 6.1 API

`POST /api/Booking/AutoAssignBusRuns`

Body:

```json
{
  "routeId": 1,
  "serviceDate": "2026-04-29",
  "startTime": "07:00:00"
}
```

### 6.2 Group de chia xe

He thong chia theo tung nhom:

- `routeId`
- `serviceDate`
- `startTime`

Tat ca hoc sinh trong cung nhom se duoc chia vao cac `BusRun` cua nhom do.

### 6.3 Rule tinh so xe

Rule hien tai cua backend:

- xe `25 cho` la `xe chinh`
- xe `25 cho` chi duoc xep toi da `20 hoc sinh`
- xe `15 cho` la `xe backup`
- luon tao them `1 xe backup` sau khi chia xong xe chinh
- xe backup khong gan hoc sinh ban dau

Cong thuc tinh xe chinh:

1. Tinh so xe chinh ban dau:

```text
busCount = floor(totalStudents / 20)
neu busCount = 0 thi lay 1
```

2. Chia deu hoc sinh vao `busCount`

3. Neu xe nao vuot `20` hoc sinh thi tang `busCount` len 1 va chia lai

4. Chia xong thi cong them:

```text
+ 1 xe 15 cho BACKUP
```

### 6.4 Vi du tinh tai xe

`21` hoc sinh:

```text
11, 10, backup 0
```

`45` hoc sinh:

```text
15, 15, 15, backup 0
```

`47` hoc sinh:

```text
16, 16, 15, backup 0
```

`50` hoc sinh:

```text
17, 17, 16, backup 0
```

`51` hoc sinh:

```text
17, 17, 17, backup 0
```

`75` hoc sinh:

```text
19, 19, 19, 18, backup 0
```

### 6.5 Rule phan hoc sinh vao xe

Sau khi backend biet moi xe can bao nhieu hoc sinh, backend moi phan hoc sinh vao tung xe.

Rule moi theo nghiep vu hien tai:

- xe 1, xe 2, xe 3 deu chay full tuyen
- khong chia theo kieu xe 1 om tram dau, xe 2 om tram giua
- hoc sinh duoc rai deu tren tung tram
- muc tieu la moi xe dat dung tai muc tieu da tinh

Backend hien tai phan bo theo:

1. Group booking theo `stationId`
2. Sap xep theo thu tu tram tren route
3. Trong tung tram, sap xep tiep theo:
   - `latitude`
   - `longitude`
   - `CreatedAt`
4. O moi tram, backend rai hoc sinh vao cac xe dang con thieu tai nhieu nhat

### 6.6 Vi du de hieu cho 50 hoc sinh

Gia su cung 1 tuyen, 1 khung gio:

```text
S1 = 10 hoc sinh
S2 = 10 hoc sinh
S3 = 6 hoc sinh
S4 = 9 hoc sinh
S5 = 7 hoc sinh
S6 = 8 hoc sinh
Tong = 50 hoc sinh
```

Target load:

```text
Xe 1 = 17
Xe 2 = 17
Xe 3 = 16
Xe 4 = BACKUP
```

Mot cach rai deu dung nghiep vu:

```text
Tram   Xe1  Xe2  Xe3
S1      4    3    3
S2      3    4    3
S3      2    2    2
S4      3    3    3
S5      2    2    3
S6      3    3    2
---------------------
Tong   17   17   16
```

Y nghia:

- xe nao cung chay full tuyen
- moi tram deu co hoc sinh tren nhieu xe
- tong hoc sinh tren moi xe van can bang
- xe backup de trong cho case tre, su co, manual add

### 6.7 Success JSON mau

```json
{
  "message": "Chia hoc sinh vao xe thanh cong",
  "data": [
    {
      "id": 501,
      "routeId": 1,
      "routeName": "Tuyen Q1 sang",
      "serviceDate": "2026-04-29T00:00:00",
      "startTime": "07:00:00",
      "busId": 1,
      "busLabel": "BUS-25-01",
      "driverId": null,
      "teacherId": null,
      "seatCapacity": 25,
      "usableCapacity": 20,
      "assignedStudentCount": 17,
      "runOrder": 1,
      "status": "ASSIGNED",
      "students": [
        {
          "bookingId": 1001,
          "studentId": 2,
          "studentCode": "10002",
          "studentName": "Tran Gia Bao",
          "stationId": 1,
          "stationName": "Tram Don Quan 1"
        }
      ]
    },
    {
      "id": 502,
      "routeId": 1,
      "routeName": "Tuyen Q1 sang",
      "serviceDate": "2026-04-29T00:00:00",
      "startTime": "07:00:00",
      "busId": 2,
      "busLabel": "BUS-25-02",
      "driverId": null,
      "teacherId": null,
      "seatCapacity": 25,
      "usableCapacity": 20,
      "assignedStudentCount": 17,
      "runOrder": 2,
      "status": "ASSIGNED",
      "students": []
    },
    {
      "id": 503,
      "routeId": 1,
      "routeName": "Tuyen Q1 sang",
      "serviceDate": "2026-04-29T00:00:00",
      "startTime": "07:00:00",
      "busId": 3,
      "busLabel": "BUS-25-03",
      "driverId": null,
      "teacherId": null,
      "seatCapacity": 25,
      "usableCapacity": 20,
      "assignedStudentCount": 16,
      "runOrder": 3,
      "status": "ASSIGNED",
      "students": []
    },
    {
      "id": 504,
      "routeId": 1,
      "routeName": "Tuyen Q1 sang",
      "serviceDate": "2026-04-29T00:00:00",
      "startTime": "07:00:00",
      "busId": 4,
      "busLabel": "BUS-15-01",
      "driverId": null,
      "teacherId": null,
      "seatCapacity": 15,
      "usableCapacity": 15,
      "assignedStudentCount": 0,
      "runOrder": 4,
      "status": "BACKUP",
      "students": []
    }
  ]
}
```

### 6.8 Error JSON mau

Khong co booking:

```json
{
  "message": "Khong co booking nao de chia xe",
  "data": null
}
```

Khong du xe 25 cho:

```json
{
  "message": "Khong du xe 25 cho de chia hoc sinh vao xe chinh",
  "data": null
}
```

Khong co xe 15 cho backup:

```json
{
  "message": "Khong co xe 15 cho de chay backup cho khung gio nay",
  "data": null
}
```

## 7. Gan driver va teacher cho bus run

API:

`PUT /api/Booking/AssignBusRunStaff/{busRunId}`

Body:

```json
{
  "driverId": 21,
  "teacherId": 23
}
```

Rule:

- `driver` phai dung role `driver`
- `teacher` phai dung role `teacher`
- account phai `ACTIVE`
- bang lai `driver` phai con han
- cung `serviceDate + startTime` khong duoc gan 1 driver cho 2 `BusRun`
- cung `serviceDate + startTime` khong duoc gan 1 teacher cho 2 `BusRun`
- `driver` va `teacher` khong duoc la cung 1 nguoi

Success:

```json
{
  "message": "Gan tai xe va giao vien cho chuyen xe thanh cong",
  "data": {
    "id": 501,
    "routeId": 1,
    "routeName": "Tuyen Q1 sang",
    "serviceDate": "2026-04-29T00:00:00",
    "startTime": "07:00:00",
    "busId": 1,
    "busLabel": "BUS-25-01",
    "driverId": 21,
    "driverName": "Tran Van Driver 01",
    "teacherId": 23,
    "teacherName": "Le Thi Teacher 01",
    "seatCapacity": 25,
    "usableCapacity": 20,
    "assignedStudentCount": 17,
    "runOrder": 1,
    "status": "ASSIGNED",
    "students": []
  }
}
```

## 8. Get bus run cho app tai xe / giao vien

API:

`GET /api/Booking/GetBusRuns?serviceDate=2026-04-29`

Co the loc them:

- `routeId`
- `busId`
- `driverId`
- `teacherId`

Vi du:

`GET /api/Booking/GetBusRuns?serviceDate=2026-04-29&driverId=21`

Success:

```json
{
  "message": "Lay danh sach lich chay thuc te thanh cong",
  "data": [
    {
      "id": 501,
      "routeId": 1,
      "routeName": "Tuyen Q1 sang",
      "serviceDate": "2026-04-29T00:00:00",
      "startTime": "07:00:00",
      "busId": 1,
      "busLabel": "BUS-25-01",
      "driverId": 21,
      "driverName": "Tran Van Driver 01",
      "teacherId": 23,
      "teacherName": "Le Thi Teacher 01",
      "seatCapacity": 25,
      "usableCapacity": 20,
      "assignedStudentCount": 17,
      "runOrder": 1,
      "status": "ASSIGNED",
      "students": []
    }
  ]
}
```

## 9. Manual change driver / teacher

Do `BusRun` giu staff truc tiep, nen neu tai xe bao ban ngay do thi admin doi thang tren `BusRun`.

Y nghia:

- cung mot ngay co the `07:00` la tai xe A
- `16:00` la tai xe B
- khong can buoc theo kieu 1 xe 1 tai xe cho ca ngay nua

## 10. Attendance rule hien tai

### 10.1 Manual check-in

API:

`POST /api/Attendance/ManualCheckIn`

Body:

```json
{
  "studentId": 2,
  "busId": 1,
  "stationId": 1,
  "imageUrl": "https://res.cloudinary.com/demo/checkin.jpg",
  "date": "2026-04-29",
  "time": "07:05:00"
}
```

### 10.2 Manual check-out

API:

`POST /api/Attendance/ManualCheckOut`

Body:

```json
{
  "studentId": 2,
  "busId": 1,
  "stationId": 6,
  "imageUrl": "https://res.cloudinary.com/demo/checkout.jpg",
  "date": "2026-04-29",
  "time": "16:45:00"
}
```

### 10.3 Rule check theo route, khong khoa cung theo bus

Rule hien tai da doi:

- Backend khong bat buoc hoc sinh phai nam dung tren chiec xe da duoc chia ban dau
- Backend doi chieu theo:
  - `route`
  - `serviceDate`
  - `startTime`
- Neu hoc sinh di tre va len xe khac nhung van cung tuyen, cung ngay, cung khung gio thi van check-in/check-out duoc
- Truong hop nay he thong se them `note` de audit

Vi du note:

```text
Hoc sinh co goi con hieu luc. Hoc sinh len xuong xe khac voi xe duoc chia ban dau (51A-12345) nhung van cung tuyen va cung khung gio
```

### 10.4 Error khi hoc sinh khong thuoc nhom booking

```json
{
  "message": "Hoc sinh khong nam trong danh sach booking cua tuyen nay o khung gio da chon",
  "data": null
}
```

### 10.5 Rule check-in / check-out nhieu lan

- Neu hoc sinh da `check-in` ma chua `check-out` thi khong duoc `check-in` lai
- Phai `check-out` truoc
- Neu da `check-in` va `check-out` xong mot luot thi co the mo mot dong attendance moi trong ngay

Error:

```json
{
  "message": "Hoc sinh da check in, chi co the check out",
  "data": null
}
```

## 11. Case hoc sinh di tre

### 11.1 Di tre nhung van len xe khac cung khung gio

He thong da support:

- check theo `route + serviceDate + startTime`
- khong khoa cung theo `bus`

Nen neu hoc sinh len xe khac cung tuyen, cung khung gio, van co the check-in.

### 11.2 Di tre va len xe backup hoac xe gio sau

Rule hien tai cua backend cho case nay la:

- Neu hoc sinh di tre
- va co mot `BusRun` khac van hop le
- chi can xe do con cho
- thi van cho diem danh

Ap dung cho ca 2 truong hop:

- len `xe backup`
- len `xe gio sau`

Dieu kien de cho qua:

- cung ngay
- dung tuyen
- `BusRun` thuc te phai cung gio hoac nam trong cua so tre toi da `15 phut` sau gio booking goc
- xe con cho trong

Y nghia nghiep vu:

- staff khong can chan hoc sinh chi vi em do len tre
- neu backup hoac chuyen sau van con suc chua thi cho len xe va diem danh binh thuong
- attendance van can ghi nhan lai note de biet hoc sinh da len `xe backup` hoac `xe gio sau`

Note mong muon co the la:

```text
Hoc sinh di tre va duoc diem danh tren xe backup
```

hoac:

```text
Hoc sinh di tre va duoc diem danh tren xe gio sau
```

## 12. Bus route va campus

Hien tai `Bus` chua co `CampusId`.

Ve nghiep vu, huong dung nen la:

- moi `Bus` thuoc 1 `Campus`
- `BusRoute` thuoc 1 `Campus`
- khi auto chia xe thi chi lay xe cung `Campus` voi `Route`

Neu chua lam phan nay thi can luu y:

- co the lay nham xe campus nay sang campus khac

## 13. Checklist test nhanh

1. `POST /api/Account/Login`
2. `POST /api/Campus/Create`
3. `POST /api/BusStation/Create`
4. `POST /api/BusRoute/Create`
5. `POST /api/Bus/Create`
6. `POST /api/User/Create`
7. `POST /api/User/CreateDriver`
8. `POST /api/User/CreateTeacher`
9. `POST /api/Student/Create`
10. `POST /api/Package/Create`
11. `POST /api/Order/Create`
12. `POST /api/Booking/Create`
13. `POST /api/Booking/AutoAssignBusRuns`
14. `PUT /api/Booking/AssignBusRunStaff/{busRunId}`
15. `GET /api/Booking/GetBusRuns?serviceDate=...`
16. `POST /api/BusTripProgress/Arrive`
17. `POST /api/Attendance/ManualCheckIn`
18. `POST /api/Attendance/ManualCheckOut`

## 14. Ket luan

Nguon su that hien tai cua runtime la:

- `Booking` = nhu cau dat cho
- `BusRun` = chuyen xe thuc te theo ngay gio
- `BusRunStudent` = hoc sinh nam tren xe nao
- `Attendance` = check-in / check-out thuc te

Va nhung rule quan trong nhat hien tai la:

- cutoff `20:00` cho booking ngay mai
- chia xe theo moc `20` hoc sinh / xe chinh
- luon co `1 xe 15 cho BACKUP`
- hoc sinh tren moi tram duoc rai deu vao cac xe
- attendance check theo `route + serviceDate + startTime`, khong khoa cung theo dung `bus`
