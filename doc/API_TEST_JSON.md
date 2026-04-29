# API Test JSON Templates

Tai lieu nay tong hop JSON test cho cac API hien co trong app (theo `Controllers/`).

## Quy uoc
- `BASE_URL`: `https://localhost:5001` (doi theo moi truong cua ban).
- Voi API `GET/DELETE` khong co body: dung `pathParams` va `query` mau.
- Voi API `multipart/form-data`: phan JSON duoi day chi la metadata minh hoa.

## AccountController (`/api/Account`)

### POST `/api/Account/Login`
```json
{
  "email": "admin@schoolbus.local",
  "password": "123456",
  "deviceToken": "fcm_device_token_here"
}
```

### GET `/api/Account/Me`
```json
{
  "headers": {
    "Authorization": "Bearer <jwt_token>"
  }
}
```

### POST `/api/Account/SendEmail`
```json
{
  "to": "parent@example.com",
  "subject": "Thong bao tu SchoolBus",
  "body": "Noi dung email test."
}
```

### POST `/api/Account/SendNotification`
```json
{
  "deviceToken": "fcm_device_token_here",
  "title": "Bus da den tram",
  "body": "Vui long dua hoc sinh ra diem don."
}
```

## AttendanceController (`/api/Attendance`)

### GET `/api/Attendance/Search`
```json
{
  "query": {
    "keyword": "ST001",
    "date": "2026-04-28",
    "campusId": 1,
    "busId": 1,
    "studentId": 1,
    "guardianId": 2,
    "status": "CHECKED_IN",
    "page": 1,
    "pageSize": 10
  }
}
```
Gia tri `status` hop le: `CHECKED_IN`, `CHECKED_OUT`.

### GET `/api/Attendance/Get/{id}`
```json
{
  "pathParams": { "id": 1 }
}
```

### GET `/api/Attendance/GetByStudent/{studentId}`
```json
{
  "pathParams": { "studentId": 1 },
  "query": {
    "fromDate": "2026-04-01",
    "toDate": "2026-04-30"
  }
}
```

### POST `/api/Attendance/ManualCheckIn`
### POST `/api/Attendance/ManualCheckOut`
```json
{
  "studentId": 1,
  "busId": 1,
  "stationId": 1,
  "imageUrl": "https://cdn.schoolbus.com/attendance/checkin.jpg",
  "date": "2026-04-28T00:00:00Z",
  "time": "07:20:00"
}
```

### DELETE `/api/Attendance/Delete/{id}`
```json
{
  "pathParams": { "id": 1 }
}
```

## BookingController (`/api/Booking`)

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

### GET `/api/Booking/GetBusRuns`
```json
{
  "query": {
    "serviceDate": "2026-04-29",
    "routeId": 1,
    "busId": 1,
    "driverId": 10,
    "teacherId": 20
  },
  "responseStudentFields": [
    "bookingId",
    "studentId",
    "studentCode",
    "studentName",
    "stationId",
    "stationName",
    "pickupAddress",
    "hasCheckedInOnThisBus",
    "currentBusId",
    "currentBusLabel",
    "isOnDifferentBusThanAssigned"
  ]
}
```

### GET `/api/Booking/Get/{id}`
```json
{
  "pathParams": { "id": 1 }
}
```

### POST `/api/Booking/Create`
```json
{
  "studentId": 1,
  "routeId": 1,
  "serviceDate": "2026-04-29T00:00:00Z",
  "startTime": "07:00:00",
  "stationId": 1,
  "pickupAddress": "55C Nguyen Thi Minh Khai, Quan 1, TP.HCM",
  "latitude": 10.762622,
  "longitude": 106.660172,
  "note": "Don tai cong truong A"
}
```

### PUT `/api/Booking/Update/{id}`
```json
{
  "studentId": 1,
  "routeId": 2,
  "serviceDate": "2026-04-30T00:00:00Z",
  "startTime": "07:15:00",
  "stationId": 2,
  "pickupAddress": "55C Nguyen Thi Minh Khai, Quan 1, TP.HCM",
  "latitude": 10.775,
  "longitude": 106.7,
  "status": "CONFIRMED",
  "note": "Cap nhat diem don"
}
```
Gia tri `status` hop le: `PENDING`, `CONFIRMED`, `CANCELLED`.

### DELETE `/api/Booking/Delete/{id}`
```json
{
  "pathParams": { "id": 1 }
}
```

### PUT `/api/Booking/AssignBusRunStaff/{busRunId}`
```json
{
  "driverId": 10,
  "teacherId": 20
}
```

### POST `/api/Booking/AutoAssignBusRuns`
```json
{
  "routeId": 1,
  "serviceDate": "2026-04-29T00:00:00Z",
  "startTime": "07:00:00"
}
```

### POST `/api/Booking/AutoAssignBusRunsByDate`
```json
{
  "query": {
    "serviceDate": "2026-04-29"
  }
}
```

## BusController (`/api/Bus`)

### GET `/api/Bus/Search`
```json
{ "query": { "keyword": "51B", "page": 1, "pageSize": 10 } }
```

### GET `/api/Bus/Get/{id}`
```json
{ "pathParams": { "id": 1 } }
```

### GET `/api/Bus/GetByCampus/{campusId}`
```json
{ "pathParams": { "campusId": 1 } }
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

### PUT `/api/Bus/Update/{id}`
```json
{
  "licensePlate": "51B-12345",
  "capacity": 40,
  "status": "ACTIVE",
  "busNumber": "BUS-01",
  "imageUrl": "https://cdn.schoolbus.com/bus-01-new.jpg",
  "color": "Yellow",
  "busType": "40-Seat"
}
```
Gia tri `status` hop le: `ACTIVE`, `DEACTIVE`, `MAINTENANCE`.

### DELETE `/api/Bus/Delete/{id}`
```json
{ "pathParams": { "id": 1 } }
```

## BusRouteController (`/api/BusRoute`)

### GET `/api/BusRoute/Search`
```json
{ "query": { "keyword": "Route A", "campusId": 1, "page": 1, "pageSize": 10 } }
```

### GET `/api/BusRoute/Active`
```json
{ "query": { "keyword": "Route", "campusId": 1, "page": 1, "pageSize": 10 } }
```

### GET `/api/BusRoute/Get/{id}`
```json
{ "pathParams": { "id": 1 } }
```

### POST `/api/BusRoute/Create`
```json
{
  "name": "Route A - Morning",
  "campusId": 1,
  "stationIds": [1, 2, 3]
}
```

### PUT `/api/BusRoute/Update/{id}`
```json
{
  "name": "Route A - Updated",
  "isEnabled": true,
  "campusId": 1,
  "stationIds": [1, 2, 4]
}
```

### DELETE `/api/BusRoute/Delete/{id}`
```json
{ "pathParams": { "id": 1 } }
```

## BusStationController (`/api/BusStation`)

### GET `/api/BusStation/Search`
```json
{ "query": { "keyword": "Tram", "page": 1, "pageSize": 10 } }
```

### GET `/api/BusStation/Get/{id}`
```json
{ "pathParams": { "id": 1 } }
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

### PUT `/api/BusStation/Update/{id}`
```json
{
  "name": "Tram Cong Vien - Cap nhat",
  "address": "125 Le Loi, Q1",
  "description": "Cap nhat mo ta",
  "latitude": 10.777,
  "longitude": 106.701,
  "isEnabled": true
}
```

### DELETE `/api/BusStation/Delete/{id}`
```json
{ "pathParams": { "id": 1 } }
```

## BusTrackingController (`/api/BusTracking`)

### POST `/api/BusTracking/Update`
```json
{
  "busId": 1,
  "latitude": 10.7733,
  "longitude": 106.6899,
  "speed": 35.5
}
```

### GET `/api/BusTracking/GetLatest/{busId}`
```json
{ "pathParams": { "busId": 1 } }
```

## BusTripProgressController (`/api/BusTripProgress`)

### POST `/api/BusTripProgress/Arrive`
```json
{
  "busId": 1,
  "busRunId": 1001,
  "stationId": 2,
  "arrivedAt": "2026-04-29T07:30:00Z"
}
```

### GET `/api/BusTripProgress/DriverSchedules`
```json
{
  "query": {
    "driverId": 10,
    "rideDate": "2026-04-29",
    "atTime": "07:00:00"
  },
  "responseStudentFields": [
    "studentId",
    "studentCode",
    "studentName",
    "stationId",
    "stationName",
    "pickupAddress",
    "pickupLatitude",
    "pickupLongitude",
    "hasCheckedInOnThisBus",
    "currentBusId",
    "currentBusLabel",
    "isOnDifferentBusThanAssigned"
  ]
}
```

### GET `/api/BusTripProgress/TeacherSchedules`
```json
{
  "query": {
    "teacherId": 20,
    "rideDate": "2026-04-29",
    "atTime": "07:00:00"
  },
  "responseStudentFields": [
    "studentId",
    "studentCode",
    "studentName",
    "stationId",
    "stationName",
    "pickupAddress",
    "pickupLatitude",
    "pickupLongitude",
    "hasCheckedInOnThisBus",
    "currentBusId",
    "currentBusLabel",
    "isOnDifferentBusThanAssigned"
  ]
}
```

### GET `/api/BusTripProgress/Current`
```json
{
  "query": {
    "busId": 1,
    "busRunId": 1001,
    "rideDate": "2026-04-29"
  }
}
```

### GET `/api/BusTripProgress/History`
```json
{
  "query": {
    "busId": 1,
    "routeId": 1,
    "campusId": 1,
    "fromDate": "2026-04-01",
    "toDate": "2026-04-30"
  },
  "responseStudentFields": [
    "studentId",
    "studentCode",
    "studentName",
    "stationId",
    "stationName",
    "pickupAddress",
    "assignmentType",
    "hasCheckedInOnThisBus",
    "currentBusId",
    "currentBusLabel",
    "isOnDifferentBusThanAssigned"
  ]
}
```

## CampusController (`/api/Campus`)

### GET `/api/Campus/Search`
```json
{ "query": { "keyword": "Co so", "page": 1, "pageSize": 10 } }
```

### GET `/api/Campus/Active`
```json
{ "query": { "keyword": "Co so", "page": 1, "pageSize": 10 } }
```

### GET `/api/Campus/Get/{id}`
```json
{ "pathParams": { "id": 1 } }
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

### PUT `/api/Campus/Update/{id}`
```json
{
  "code": "CS1",
  "name": "Campus Quan 1 - New",
  "address": "101 Nguyen Hue, Q1",
  "phone": "02838223345",
  "isActive": true,
  "imageUrl": "https://cdn.schoolbus.com/campus-1-new.jpg"
}
```

### DELETE `/api/Campus/Delete/{id}`
```json
{ "pathParams": { "id": 1 } }
```

## FaceAIController (`/api/FaceAI`)

### GET `/api/FaceAI/Health`
```json
{ "note": "No body" }
```

### POST `/api/FaceAI/CreateStudent`
```json
{
  "studentId": 1,
  "name": "Nguyen Minh Khang"
}
```

### POST `/api/FaceAI/AddStudentFace/{studentId}` (multipart/form-data)
```json
{
  "pathParams": { "studentId": 1 },
  "formData": {
    "file": "<image_file>"
  }
}
```

### GET `/api/FaceAI/GetStudents`
```json
{ "note": "No body" }
```

### GET `/api/FaceAI/GetStudentImages/{studentId}`
### GET `/api/FaceAI/GetStudentFaces/{studentId}`
### DELETE `/api/FaceAI/DeleteStudent/{studentId}`
```json
{ "pathParams": { "studentId": 1 } }
```

### DELETE `/api/FaceAI/DeleteFace/{faceId}`
```json
{ "pathParams": { "faceId": 100 } }
```

### POST `/api/FaceAI/VerifyStudent/{studentId}` (multipart/form-data)
```json
{
  "pathParams": { "studentId": 1 },
  "formData": {
    "file": "<image_file>"
  }
}
```

### POST `/api/FaceAI/Verify` (multipart/form-data)
### POST `/api/FaceAI/VerifyTop` (multipart/form-data)
### POST `/api/FaceAI/RecognizeCheckIn` (multipart/form-data)
### POST `/api/FaceAI/RecognizeCheckOut` (multipart/form-data)
```json
{
  "formData": {
    "file": "<image_file>",
    "topK": 3,
    "busId": 1,
    "stationId": 1
  }
}
```

## FaceRecognitionSettingController (`/api/FaceRecognitionSetting`)

### GET `/api/FaceRecognitionSetting/GetSimilarityThreshold`
```json
{ "note": "No body" }
```

### PUT `/api/FaceRecognitionSetting/UpdateSimilarityThreshold`
```json
{
  "similarityThreshold": 0.8
}
```

## NotificationController (`/api/Notification`)

**Authorize:** Bearer. Phụ huynh: gọi không cần `userId` (lấy theo token).

### GET `/api/Notification/Search`
```json
{
  "query": {
    "userId": null,
    "isRead": false,
    "type": null,
    "fromDate": "2026-04-01",
    "toDate": "2026-04-30",
    "page": 1,
    "pageSize": 20
  },
  "note": "Admin có thể set userId để xem thông báo user khác; user thường chỉ được userId trùng token hoặc bỏ userId."
}
```

## OrderController (`/api/Order`)

### POST `/api/Order/Create`
```json
{
  "guardianId": 2,
  "studentId": 1,
  "packageId": 1,
  "routeIds": [1, 2]
}
```

### POST `/api/Order/CreatePayOsLink`
```json
{
  "guardianId": 2,
  "studentId": 1,
  "packageId": 1,
  "routeIds": [1, 2],
  "returnUrl": "https://frontend.local/order/success",
  "cancelUrl": "https://frontend.local/order/cancel"
}
```

### POST `/api/Order/HandlePayOsWebhook`
```json
{
  "code": "00",
  "desc": "success",
  "success": true,
  "data": {
    "orderCode": 123456789,
    "amount": 500000,
    "description": "Thanh toan goi",
    "accountNumber": "9704xxxx",
    "reference": "PAYOS_REF_001",
    "transactionDateTime": "2026-04-28T10:00:00Z"
  },
  "signature": "webhook_signature_here"
}
```

### GET `/api/Order/GetPayOsStatus/{orderCode}`
```json
{ "pathParams": { "orderCode": 123456789 } }
```

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

### GET `/api/Order/Get/{id}`
### GET `/api/Order/GetByGuardian/{guardianId}`
### GET `/api/Order/GetActiveByStudent/{studentId}`
```json
{
  "pathParams": {
    "id": 1,
    "guardianId": 2,
    "studentId": 1
  }
}
```

### PUT `/api/Order/Cancel/{id}`
```json
{
  "reason": "Guardian khong co nhu cau nua",
  "refundToWallet": true
}
```

## PackageController (`/api/Package`)

### GET `/api/Package/Search`
### GET `/api/Package/Active`
```json
{ "query": { "keyword": "Goi", "page": 1, "pageSize": 10 } }
```

### GET `/api/Package/Get/{id}`
```json
{ "pathParams": { "id": 1 } }
```

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
Gia tri `status` thuong dung: `ACTIVE` (API khong gioi han enum cung trong service, nhung endpoint `Active` dang loc theo `ACTIVE`).

### PUT `/api/Package/Update/{id}`
```json
{
  "name": "Goi 3 thang - New",
  "price": 1250000,
  "durationDays": 90,
  "routeLimit": 3,
  "description": "Cap nhat mo ta",
  "status": "ACTIVE",
  "type": "MONTHLY",
  "imageUrl": "https://cdn.schoolbus.com/package-3m-new.jpg"
}
```
Gia tri `status` thuong dung: `ACTIVE` (API khong gioi han enum cung trong service, nhung endpoint `Active` dang loc theo `ACTIVE`).

### DELETE `/api/Package/Delete/{id}`
```json
{ "pathParams": { "id": 1 } }
```

## RoleController (`/api/Role`)

### GET `/api/Role/Search`
```json
{ "query": { "keyword": "teacher", "page": 1, "pageSize": 10 } }
```

### GET `/api/Role/Get/{id}`
```json
{ "pathParams": { "id": 1 } }
```

### POST `/api/Role/Create`
### PUT `/api/Role/Update/{id}`
```json
{
  "name": "staff"
}
```

### DELETE `/api/Role/Delete/{id}`
```json
{ "pathParams": { "id": 1 } }
```

## StudentController (`/api/Student`)

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

### GET `/api/Student/Get/{id}`
### GET `/api/Student/GetByCode/{studentCode}`
### GET `/api/Student/GetByCampus/{campusId}`
### GET `/api/Student/GetByGuardian/{guardianId}`
```json
{
  "pathParams": {
    "id": 1,
    "studentCode": "ST001",
    "campusId": 1,
    "guardianId": 2
  }
}
```

### GET `/api/Student/GetByGuardianPhone`
```json
{
  "query": {
    "phoneNumber": "0902302001"
  }
}
```

### GET `/api/Student/GetMyStudents`
```json
{
  "headers": {
    "Authorization": "Bearer <guardian_jwt_token>"
  }
}
```

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

### POST `/api/Student/ImportByGuardianEmail` (multipart/form-data)
```json
{
  "formData": {
    "file": "<student-import-by-guardian-email-template.xlsx>"
  }
}
```

### PUT `/api/Student/Update/{id}`
```json
{
  "studentCode": "ST001",
  "fullName": "Nguyen Minh Khang Updated",
  "avatarUrl": "https://cdn.schoolbus.com/students/st001-new.jpg",
  "dateOfBirth": "2015-03-12T00:00:00Z",
  "gender": "male",
  "guardianId": 2,
  "campusId": 1,
  "status": "ACTIVE"
}
```
Gia tri `status` hop le: `ACTIVE`, `DISABLED`.

### DELETE `/api/Student/Delete/{id}`
```json
{ "pathParams": { "id": 1 } }
```

## TransactionLogController (`/api/TransactionLog`)

### GET `/api/TransactionLog/Search`
```json
{
  "query": {
    "keyword": "PAYOS",
    "method": "PAYOS",
    "status": "SUCCESS",
    "orderId": 1,
    "code": "TXN001",
    "fromDate": "2026-04-01",
    "toDate": "2026-04-30",
    "page": 1,
    "pageSize": 10
  }
}
```
Gia tri `status`: API hien tai cho phep chuoi tu do (service khong enum cứng). Neu dung cho thanh toan, nen dung bo gia tri chuan nhu `PENDING`, `PAID`, `CANCELLED`, `FAILED`, `SUCCESS`.

### GET `/api/TransactionLog/Get/{id}`
```json
{ "pathParams": { "id": 1 } }
```

### POST `/api/TransactionLog/Create`
```json
{
  "orderId": 1,
  "method": "PAYOS",
  "amount": 500000,
  "status": "SUCCESS",
  "paidAt": "2026-04-28T10:00:00Z",
  "oldBalance": 1000000,
  "newBalance": 1500000,
  "sender": "Guardian Wallet",
  "receiver": "SchoolBus Wallet",
  "description": "Thanh toan don hang #1",
  "code": "TXN001"
}
```
Gia tri `status`: API hien tai cho phep chuoi tu do (khuyen nghi dung bo gia tri chuan nhu tren de de loc/bao cao).

### PUT `/api/TransactionLog/Update/{id}`
```json
{
  "orderId": 1,
  "method": "PAYOS",
  "amount": 500000,
  "status": "SUCCESS",
  "paidAt": "2026-04-28T10:00:00Z",
  "oldBalance": 1000000,
  "newBalance": 1500000,
  "sender": "Guardian Wallet",
  "receiver": "SchoolBus Wallet",
  "description": "Cap nhat giao dich",
  "code": "TXN001-UPD"
}
```
Gia tri `status`: API hien tai cho phep chuoi tu do (khuyen nghi dung bo gia tri chuan nhu tren de de loc/bao cao).

### DELETE `/api/TransactionLog/Delete/{id}`
```json
{ "pathParams": { "id": 1 } }
```

## UploadController (`/api/Upload`)

### POST `/api/Upload/Image` (multipart/form-data)
```json
{
  "formData": {
    "file": "<image_file>"
  }
}
```

## UserController (`/api/User`)

### GET `/api/User/Search`
```json
{
  "query": {
    "keyword": "Nguyen",
    "role": "teacher",
    "status": "ACTIVE",
    "isAssignedToBus": false,
    "page": 1,
    "pageSize": 10
  }
}
```
Gia tri `status` hop le: `ACTIVE`, `DISABLED`.

### GET `/api/User/Get/{id}`
```json
{ "pathParams": { "id": 1 } }
```

### POST `/api/User/Import` (multipart/form-data)
```json
{
  "formData": {
    "file": "<user_import_template.xlsx>"
  }
}
```

### POST `/api/User/Create`
```json
{
  "email": "staff.new@schoolbus.com",
  "password": "123456",
  "fullName": "Nguyen Hai Dang",
  "avatarUrl": "https://cdn.schoolbus.com/users/staff-new.jpg",
  "phone": "0932304999",
  "role": "staff"
}
```

### PUT `/api/User/Update/{id}`
```json
{
  "email": "staff.updated@schoolbus.com",
  "password": "123456",
  "fullName": "Nguyen Hai Dang Updated",
  "avatarUrl": "https://cdn.schoolbus.com/users/staff-updated.jpg",
  "phone": "0932304888",
  "deviceToken": "fcm_device_token_here",
  "driverLicenseNumber": "B2-123456789",
  "driverLicenseClass": "B2",
  "driverLicenseExpiryDate": "2030-12-31T00:00:00Z",
  "role": "driver",
  "status": "ACTIVE"
}
```
Gia tri `status` hop le: `ACTIVE`, `DISABLED`.

### DELETE `/api/User/Delete/{id}`
```json
{ "pathParams": { "id": 1 } }
```

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

## WalletController (`/api/Wallet`)

### GET `/api/Wallet/Search`
```json
{ "query": { "keyword": "guardian", "page": 1, "pageSize": 10 } }
```

### GET `/api/Wallet/GetByUser/{userId}`
```json
{ "pathParams": { "userId": 2 } }
```

### GET `/api/Wallet/TransactionHistory/{walletId}`
```json
{
  "pathParams": { "walletId": 1 },
  "query": {
    "fromDate": "2026-04-01",
    "toDate": "2026-04-30",
    "method": "PAYOS",
    "status": "SUCCESS",
    "page": 1,
    "pageSize": 10
  }
}
```
Gia tri `status` thuong dung cho top-up wallet: `PENDING`, `PAID`, `CANCELLED`, `FAILED` (co the gap them `PAYOS_URL_VERIFICATION` o buoc tao link payOS).

### POST `/api/Wallet/TopUp`
```json
{
  "userId": 2,
  "amount": 500000
}
```

### POST `/api/Wallet/CreatePayOsTopUpLink`
```json
{
  "userId": 2,
  "amount": 500000,
  "returnUrl": "https://frontend.local/wallet/success",
  "cancelUrl": "https://frontend.local/wallet/cancel"
}
```

### POST `/api/Wallet/HandlePayOsWebhook`
```json
{
  "code": "00",
  "desc": "success",
  "success": true,
  "data": {
    "orderCode": 22334455,
    "amount": 500000,
    "description": "Topup wallet",
    "accountNumber": "9704xxxx",
    "reference": "WALLET_TOPUP_001",
    "transactionDateTime": "2026-04-28T10:00:00Z"
  },
  "signature": "webhook_signature_here"
}
```

### GET `/api/Wallet/GetPayOsTopUpStatus/{orderCode}`
```json
{
  "pathParams": { "orderCode": 22334455 }
}
```

