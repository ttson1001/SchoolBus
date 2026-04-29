# Luong 4 - Teacher app: ho so -> lich hom nay -> hoc sinh tren xe -> checkin/checkout

Tai lieu E2E cho app giao vien.
Base URL: `BASE_URL` (vd `https://api.faceride.site`).

---

## 1) Quy uoc response va status

Envelope BE:

```json
{
  "message": "string",
  "data": {}
}
```

- Thanh cong: HTTP `200`
- Loi nghiep vu/validate: HTTP `400` (thuong `data: null`)

### 1.1 Status UI can map

| Nhom | Field | Gia tri |
|------|-------|---------|
| Lich chuyen giao vien | `shiftType` | `ASSIGNED`, `BACKUP` |
| Lich chuyen giao vien | `isRunningNow`, `isUpcoming`, `isCompleted`, `isRecommended` | `true` / `false` |
| Trang thai chuyen hien tai | `tripStatus` (`Current`) | `NOT_STARTED`, `AT_STATION`, `COMPLETED` |
| Trang thai diem danh | `status` (`AttendanceDto`) | `CHECKED_IN`, `CHECKED_OUT` |
| Trang thai hoc sinh tren xe | `isOnBus` (`GetBusStudentStatuses`) | `true` / `false` |

---

## 2) Thu tu luong giao vien

1. Login giao vien.
2. Xem/sua thong tin ca nhan.
3. Lay lich trong ngay cua giao vien, sap xep tu sang den toi.
4. Chon chuyen hien tai (`busRunId`, `busId`).
5. Lay danh sach hoc sinh tren xe co avatar.
6. Check-in/check-out bang FaceAI.
7. Check-in/check-out thu cong (manual) khi can fallback.

---

## 3) Chi tiet API

### 3.0 - Login va lay teacherId

**POST** `/api/Account/Login`

```json
{
  "email": "teacher@example.com",
  "password": "123456",
  "deviceToken": "fcm_teacher_optional"
}
```

Sau do goi:

**GET** `/api/Account/Me` (Bearer token) -> lay `teacherId = data.id`.

---

### 3.1 - Chinh sua thong tin cua minh

**PUT** `/api/User/Update/{id}` voi `id = teacherId`.

Chi cap nhat: `fullName`, `phone`, `avatarUrl`, `password`.

Goi y doi avatar tren app teacher:

1. Upload anh truoc qua **`POST /api/Upload/Image`** (`multipart/form-data`, field `file`).  
2. Lay `data.url` tu response upload.  
3. Truyen URL do vao `avatarUrl` khi goi `PUT /api/User/Update/{teacherId}`.

Mau upload:

```json
{
  "formData": {
    "file": "<image_file>"
  }
}
```

Response upload thanh cong (rut gon):

```json
{
  "message": "Upload ảnh thành công",
  "data": {
    "url": "https://res.cloudinary.com/.../teacher-avatar.jpg",
    "publicId": "folder/teacher-avatar",
    "format": "jpg",
    "bytes": 45231
  }
}
```

```json
{
  "fullName": "Tran Thi Teacher",
  "phone": "0909222333",
  "avatarUrl": "https://res.cloudinary.com/.../teacher-avatar.jpg",
  "password": null
}
```

Loi 400 vi du:

```json
{ "message": "Email da ton tai.", "data": null }
```

---

### 3.2 - Lich trinh hom nay cua giao vien (sort sang -> toi)

**GET** `/api/BusTripProgress/TeacherSchedules?teacherId=22&rideDate=2026-04-29&atTime=07:00:00`

- Service dang sort `StartTime ASC`, `RunOrder ASC`.
- FE nen sort lai `startTime ASC` de an toan.

Response thanh cong (rut gon):

```json
{
  "message": "Lấy danh sách lịch chạy của giáo viên thành công",
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
      "students": []
    }
  ]
}
```

---

### 3.3 - Lay danh sach hoc sinh tren xe hien tai co avatar

Sau khi chon run, dung `busId` goi:

**GET** `/api/Attendance/GetBusStudentStatuses?busId=1&date=2026-04-29&busRunId=1001`

Endpoint nay co san `studentAvatarUrl`.

Response thanh cong (rut gon):

```json
{
  "message": "Lay danh sach hoc sinh tren xe va chua tren xe thanh cong",
  "data": [
    {
      "studentId": 3,
      "studentCode": "ST003",
      "studentName": "Le Van C",
      "studentAvatarUrl": "https://res.cloudinary.com/.../student-3.jpg",
      "guardianId": 42,
      "guardianName": "Nguyen Van A",
      "guardianPhone": "0909000111",
      "bookingId": 500,
      "stationId": 2,
      "stationName": "Tram Cong vien",
      "attendanceId": 9001,
      "checkInTime": "07:32:00",
      "checkOutTime": null,
      "isOnBus": true
    }
  ]
}
```

> Neu muon danh sach dang o tren xe thoi diem hien tai: `GET /api/Attendance/GetStudentsOnBus?busId=1&date=2026-04-29&busRunId=1001`.
>
> Khuyen nghi app teacher luon truyen `busRunId` de tranh lon hoc sinh giua ca sang/chieu cung 1 xe.

---

### 3.4 - Check-in bang FaceAI

**POST** `/api/FaceAI/RecognizeCheckIn` (multipart/form-data)

Form fields:
- `file`: anh chup hoc sinh
- `busId`: id xe hien tai
- `stationId`: id tram hien tai
- `date` (optional)
- `time` (optional)

Response thanh cong:

```json
{
  "message": "Nhận diện và check in thành công",
  "data": {
    "recognition": {
      "isMatched": true,
      "studentId": 3,
      "message": "Matched"
    },
    "attendance": {
      "studentId": 3,
      "busId": 1,
      "status": "CHECKED_IN",
      "checkInTime": "07:32:00"
    }
  }
}
```

---

### 3.5 - Check-out bang FaceAI

**POST** `/api/FaceAI/RecognizeCheckOut` (multipart/form-data)

Form fields giong checkin.

Response thanh cong:

```json
{
  "message": "Nhận diện và check out thành công",
  "data": {
    "recognition": {
      "isMatched": true,
      "studentId": 3
    },
    "attendance": {
      "studentId": 3,
      "busId": 1,
      "status": "CHECKED_OUT",
      "checkOutTime": "16:45:00"
    }
  }
}
```

---

### 3.6 - Check-in manual (fallback)

**POST** `/api/Attendance/ManualCheckIn`

```json
{
  "studentId": 3,
  "busId": 1,
  "stationId": 2,
  "imageUrl": "https://cdn.example/checkin-3.jpg",
  "date": "2026-04-29T00:00:00Z",
  "time": "07:32:00"
}
```

---

### 3.7 - Check-out manual (fallback)

**POST** `/api/Attendance/ManualCheckOut`

```json
{
  "studentId": 3,
  "busId": 1,
  "stationId": 5,
  "imageUrl": "https://cdn.example/checkout-3.jpg",
  "date": "2026-04-29T00:00:00Z",
  "time": "16:45:00"
}
```

Loi 400 hay gap:

```json
{ "message": "Hoc sinh chua check in", "data": null }
```

---

## 4) Luu y cho UI

- Chon run theo uu tien: `isRunningNow` -> `isRecommended` -> run dau theo gio.
- Dung `busId` cua run dang chon de goi API hoc sinh tren xe.
- Sau moi lan checkin/checkout (FaceAI hoac manual), refresh `GetBusStudentStatuses`.
- Khi FaceAI fail (khong match, anh mo, sai goc), fallback ngay sang manual.

---

## 5) Bang mapping ID cho dev UI (copy nhanh)

| Can ID/du lieu | Lay tu API nao | Field can dung |
|---|---|---|
| `teacherId` | `GET /api/Account/Me` | `data.id` |
| `busRunId`, `busId` cua chuyen hom nay | `GET /api/BusTripProgress/TeacherSchedules?...` | `data[].busRunId`, `data[].busId` |
| Chuyen hien tai de focus | `TeacherSchedules` | Uu tien item `isRunningNow=true`, neu khong co thi `isRecommended=true` |
| `nextStationId` de bam Arrive | `GET /api/BusTripProgress/Current?busId=...&busRunId=...&rideDate=...` | `data.nextStationId` |
| Danh sach hoc sinh + avatar | `GET /api/Attendance/GetBusStudentStatuses?busId=...&date=...&busRunId=...` | `data[].studentId`, `data[].studentAvatarUrl`, `data[].isOnBus` |
| `studentId` cho manual checkin/checkout | `GetBusStudentStatuses` | `data[].studentId` |
| `busId` cho checkin/checkout (FaceAI + manual) | `TeacherSchedules` item dang chon | `data[].busId` |
| `stationId` cho checkin/checkout | Uu tien `Current.nextStationId` | `data.nextStationId` (neu null => chuyen da hoan tat) |

### Trinh tu goi API an toan

1. `Login` -> lay token  
2. `Account/Me` -> lay `teacherId`  
3. `TeacherSchedules` -> chon run -> lay `busRunId`, `busId`  
4. `Current` -> lay `nextStationId`  
5. `GetBusStudentStatuses` (co `busRunId`) -> render hoc sinh/avatar + trang thai len xe
6. `Arrive` -> `busId + busRunId + nextStationId`  
7. Checkin/checkout FaceAI hoac manual  
