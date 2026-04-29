# API Test - Admin Flow

Tai lieu nay chi gom API phuc vu luong Admin.

## 0) Cau hinh chung
- Base URL: `https://localhost:5001`
- Header mac dinh sau khi login:
```json
{
  "Authorization": "Bearer <admin_jwt_token>"
}
```

---

## 1) Dang nhap Admin

### POST `/api/Account/Login`
```json
{
  "email": "admin@schoolbus.local",
  "password": "123456",
  "deviceToken": "admin_device_token"
}
```

### GET `/api/Account/Me`
```json
{
  "headers": {
    "Authorization": "Bearer <admin_jwt_token>"
  }
}
```

---

## 2) Thiet lap he thong co ban (Role/Campus/Station/Route/Bus/Package)

### POST `/api/Role/Create`
```json
{
  "name": "staff"
}
```

### POST `/api/Campus/Create`
```json
{
  "code": "CS1",
  "name": "Campus Quan 1",
  "address": "100 Nguyen Hue, Q1",
  "phone": "02838223344",
  "isActive": true,
  "imageUrl": "https://cdn.schoolbus.com/campus-1.jpg"
}
```

### POST `/api/BusStation/Create`
```json
{
  "name": "Tram Cong Vien",
  "address": "123 Le Loi, Q1",
  "description": "Diem don hoc sinh khu trung tam",
  "latitude": 10.7769,
  "longitude": 106.7009,
  "isEnabled": true
}
```

### POST `/api/BusStation/Create` (Tram 2)
```json
{
  "name": "Tram Nha Van Hoa",
  "address": "45 Nguyen Du, Q1",
  "description": "Diem don hoc sinh khu dan cu Nguyen Du",
  "latitude": 10.7798,
  "longitude": 106.6951,
  "isEnabled": true
}
```

### POST `/api/BusStation/Create` (Tram 3)
```json
{
  "name": "Tram Cho Ben Thanh",
  "address": "Le Loi, Ben Thanh, Q1",
  "description": "Diem don hoc sinh gan cho Ben Thanh",
  "latitude": 10.7725,
  "longitude": 106.6980,
  "isEnabled": true
}
```

### POST `/api/BusRoute/Create`
```json
{
  "name": "Route A - Morning",
  "campusId": 1,
  "stationIds": [1, 2, 3]
}
```

### POST `/api/Bus/Create`
```json
{
  "licensePlate": "51B-12345",
  "capacity": 45,
  "status": "ACTIVE",
  "busNumber": "BUS-01",
  "imageUrl": "https://cdn.schoolbus.com/bus-01.jpg",
  "color": "Yellow",
  "busType": "45-Seat"
}
```
Gia tri `status` hop le: `ACTIVE`, `DEACTIVE`, `MAINTENANCE`.

### POST `/api/Package/Create`
```json
{
  "name": "Goi 3 thang",
  "price": 1200000,
  "durationDays": 90,
  "routeLimit": 2,
  "description": "Goi cho hoc sinh di 2 tuyen",
  "status": "ACTIVE",
  "type": "MONTHLY",
  "imageUrl": "https://cdn.schoolbus.com/package-3m.jpg"
}
```

---

## 3) Quan ly User (Teacher/Driver/Staff/Guardian)

### POST `/api/User/CreateTeacher`
```json
{
  "email": "teacher.new@schoolbus.com",
  "password": "123456",
  "fullName": "Tran Thu Ha",
  "avatarUrl": "https://cdn.schoolbus.com/users/teacher-new.jpg",
  "phone": "0912301999"
}
```

### POST `/api/User/CreateDriver`
```json
{
  "email": "driver.new@schoolbus.com",
  "password": "123456",
  "fullName": "Nguyen Van Tuan",
  "avatarUrl": "https://cdn.schoolbus.com/users/driver-new.jpg",
  "phone": "0922303999",
  "driverLicenseNumber": "B2-99887766",
  "driverLicenseClass": "B2",
  "driverLicenseExpiryDate": "2030-12-31T00:00:00Z"
}
```

### POST `/api/User/Create`
```json
{
  "email": "guardian.new@schoolbus.com",
  "password": "123456",
  "fullName": "Nguyen Lan Huong",
  "avatarUrl": "https://cdn.schoolbus.com/users/guardian-new.jpg",
  "phone": "0902302999",
  "role": "guardian"
}
```

### GET `/api/User/Search`
```json
{
  "query": {
    "keyword": "Nguyen",
    "role": "guardian",
    "status": "ACTIVE",
    "isAssignedToBus": false,
    "page": 1,
    "pageSize": 10
  }
}
```
Gia tri `status` hop le: `ACTIVE`, `DISABLED`.

---

## 4) Quan ly Student

### POST `/api/Student/Create`
```json
{
  "studentCode": "ST001",
  "fullName": "Nguyen Minh Khang",
  "avatarUrl": "https://cdn.schoolbus.com/students/st001.jpg",
  "dateOfBirth": "2015-03-12T00:00:00Z",
  "gender": "male",
  "guardianId": 2,
  "campusId": 1
}
```

### GET `/api/Student/Search`
```json
{
  "query": {
    "keyword": "Khang",
    "campusId": 1,
    "guardianId": 2,
    "status": "ACTIVE",
    "page": 1,
    "pageSize": 10
  }
}
```
Gia tri `status` hop le: `ACTIVE`, `DISABLED`.

### POST `/api/Student/ImportByGuardianEmail` (multipart/form-data)
```json
{
  "formData": {
    "file": "<student-import-by-guardian-email-template.xlsx>"
  }
}
```

---

## 5) Booking va chia chuyen xe

### POST `/api/Booking/Create`
```json
{
  "studentId": 1,
  "routeId": 1,
  "serviceDate": "2026-04-29T00:00:00Z",
  "startTime": "07:00:00",
  "stationId": 1,
  "latitude": 10.762622,
  "longitude": 106.660172,
  "note": "Don tai cong truong A"
}
```

### GET `/api/Booking/Search`
```json
{
  "query": {
    "studentId": 1,
    "routeId": 1,
    "serviceDate": "2026-04-29",
    "status": "PENDING",
    "page": 1,
    "pageSize": 10
  }
}
```
Gia tri `status` hop le: `PENDING`, `CONFIRMED`, `CANCELLED`.

### POST `/api/Booking/AutoAssignBusRuns`
```json
{
  "routeId": 1,
  "serviceDate": "2026-04-29T00:00:00Z",
  "startTime": "07:00:00"
}
```

### PUT `/api/Booking/AssignBusRunStaff/{busRunId}`
```json
{
  "driverId": 10,
  "teacherId": 20
}
```

### GET `/api/Booking/GetBusRuns`
```json
{
  "query": {
    "serviceDate": "2026-04-29",
    "routeId": 1
  }
}
```

---

## 6) Diem danh va theo doi chuyen

### POST `/api/Attendance/ManualCheckIn`
```json
{
  "studentId": 1,
  "busId": 1,
  "stationId": 1,
  "imageUrl": "https://cdn.schoolbus.com/attendance/checkin.jpg",
  "date": "2026-04-29T00:00:00Z",
  "time": "07:20:00"
}
```

### GET `/api/Attendance/Search`
```json
{
  "query": {
    "date": "2026-04-29",
    "status": "PRESENT",
    "page": 1,
    "pageSize": 10
  }
}
```
Gia tri `status` hop le: `PRESENT`, `ABSENT`.

### GET `/api/BusTripProgress/History`
```json
{
  "query": {
    "routeId": 1,
    "campusId": 1,
    "fromDate": "2026-04-01",
    "toDate": "2026-04-30"
  }
}
```

---

## 7) Don hang, vi, giao dich (quan ly boi Admin)

### GET `/api/Order/Search`
```json
{
  "query": {
    "status": "PAID",
    "guardianId": 2,
    "studentId": 1,
    "fromDate": "2026-04-01",
    "toDate": "2026-04-30",
    "page": 1,
    "pageSize": 10
  }
}
```
Gia tri `status` hop le: `PENDING`, `PAID`, `CANCELLED`, `EXPIRED`.

### GET `/api/Wallet/Search`
```json
{
  "query": {
    "keyword": "guardian",
    "page": 1,
    "pageSize": 10
  }
}
```

### GET `/api/Wallet/TransactionHistory/{walletId}`
```json
{
  "pathParams": { "walletId": 1 },
  "query": {
    "fromDate": "2026-04-01",
    "toDate": "2026-04-30",
    "method": "PAYOS",
    "status": "PAID",
    "page": 1,
    "pageSize": 10
  }
}
```
Gia tri `status` thuong dung: `PENDING`, `PAID`, `CANCELLED`, `FAILED`.

### GET `/api/TransactionLog/Search`
```json
{
  "query": {
    "keyword": "PAYOS",
    "method": "PAYOS",
    "status": "SUCCESS",
    "fromDate": "2026-04-01",
    "toDate": "2026-04-30",
    "page": 1,
    "pageSize": 10
  }
}
```
Gia tri `status`: API cho phep chuoi tu do; de thong nhat bao cao nen dung bo gia tri co quy uoc (vd `SUCCESS`, `FAILED`, `PENDING`, `PAID`, `CANCELLED`).

