# Luồng giáo viên / cán bộ đưa đón: Đăng nhập → Tuyến hôm nay → Tra MASV → Check-in thủ công

Tài liệu mô tả API để **teacher** (role `teacher`) đăng nhập app, xem **lịch xe / tuyến trong ngày** trên xe được phân công, **tìm học sinh theo mã** (`studentCode` / MASV), rồi **điểm danh bằng tay** qua `**Attendance/ManualCheckIn`** (không quét FaceAI).

Envelope: `{ "message": "...", "data": ... }` (`ResponseDto`), JSON **camelCase**.

**Điều kiện nền:** admin đã `**BusAssignment`** gắn **cùng xe** với **teacher**; có `**BusSchedule`** khớp ngày chạy. Xem [03-luong-chuan-bi-va-tao-lich-xe-bus.md](./03-luong-chuan-bi-va-tao-lich-xe-bus.md).

---

## 1. Bảng API theo giai đoạn


| Giai đoạn                     | Method | Endpoint                                | Mục đích                                                          |
| ----------------------------- | ------ | --------------------------------------- | ----------------------------------------------------------------- |
| Đăng nhập                     | POST   | `/api/Account/Login`                    | Lấy JWT                                                           |
| Xác nhận role                 | GET    | `/api/Account/Me`                       | `data.id` = `**teacherId`** (user id); `roleName` = `**teacher`** |
| Tuyến / lịch hôm nay          | GET    | `/api/BusTripProgress/TeacherSchedules` | Lịch trong ngày + **trạm** (cùng cấu trúc như tài xế)             |
| Trạng thái chuyến (tuỳ chọn)  | GET    | `/api/BusTripProgress/Current`          | Trạm tiếp theo, trạng thái chuyến                                 |
| Tra học sinh theo mã          | GET    | `/api/Student/GetByCode/{studentCode}`  | Đổi MASV → `studentId`, thông tin HS                              |
| Check-in thủ công             | POST   | `/api/Attendance/ManualCheckIn`         | Ghi nhận có mặt (lên xe / tại trạm) — `**Method` = MANUAL**       |
| Check-out thủ công (tuỳ chọn) | POST   | `/api/Attendance/ManualCheckOut`        | Khi HS đã check-in cùng ngày, chưa check-out                      |


**Lưu ý:**

- `TeacherSchedules` cần `**teacherId`** = `**User.Id`** (trùng `BusAssignment.TeacherId`).
- **Manual check-in** bắt buộc `**studentId`**, `**busId`**, `**stationId**` — lấy `**busId**` (và chọn `**stationId**`) từ lịch vừa xem; `**studentId**` từ `**GetByCode**`.
- `ValidateManualAttendance` yêu cầu **trạm thuộc route của xe trong ngày**; bus phải có **lịch chạy** khớp **thứ** (`ScheduleDayOfWeek`). Học sinh phải **tồn tại**, bus **ACTIVE**.
- Nhiều controller **chưa** `[Authorize]` — FE vẫn nên gửi **Bearer** và chỉ mở màn teacher khi `roleName === "teacher"`.

---

## 2. Luồng tổng quát

```text
POST /api/Account/Login
        │
        ▼
GET /api/Account/Me  →  teacherId = data.id  (roleName = teacher)
        │
        ▼
GET /api/BusTripProgress/TeacherSchedules?teacherId={id}&rideDate=hôm nay
        │
        ▼
Hiển thị các ca / tuyến + busId + danh sách stations[]
        │
        ├─ (tuỳ chọn) GET Current?busId=&busScheduleId=&rideDate=
        │
        ▼
Nhập MASV (studentCode)
        │
        ▼
GET /api/Student/GetByCode/{studentCode}  →  studentId
        │
        ▼
Chọn trạm điểm danh (stationId thuộc tuyến đang chọn)
        │
        ▼
POST /api/Attendance/ManualCheckIn  { studentId, busId, stationId, date?, time?, imageUrl? }
```

---

## 3. Chi tiết endpoint

### 3.1 `TeacherSchedules`

**GET** `/api/BusTripProgress/TeacherSchedules`


| Query       | Bắt buộc | Ghi chú                                      |
| ----------- | -------- | -------------------------------------------- |
| `teacherId` | Có       | = id user teacher                            |
| `rideDate`  | Không    | Mặc định ngày UTC trong service              |
| `atTime`    | Không    | Giờ tham chiếu cho `isRunningNow` / gợi ý ca |


**Lỗi:** *"Giáo viên chưa được phân công xe"* — không có `BusAssignment` với `TeacherId`.  
**Lỗi:** *"Giáo viên không có lịch chạy nào trong ngày đã chọn"* — không có `BusSchedule` khớp.

**Response `data`:** mảng giống tài xế — `BusTripProgressDriverScheduleDto` (`busScheduleId`, `busId`, `routeName`, `startTime`, `endTime`, `shiftType`, `stations[]` với `stationId`, `stationName`, `orderIndex`, `isVisited`, …).

### 3.2 Tra học sinh theo mã (MASV)

**GET** `/api/Student/GetByCode/{studentCode}`

- `studentCode` là mã trong DB (`Student.StudentCode`), ví dụ `STU20260001`.
- Nếu mã có ký tự đặc biệt, **encode** đúng trong URL.

**Response `data`:** `StudentDto` — cần `**id`** = `studentId` cho bước sau.

### 3.3 Check-in thủ công

**POST** `/api/Attendance/ManualCheckIn`

```json
{
  "studentId": 12,
  "busId": 5,
  "stationId": 10,
  "imageUrl": null,
  "date": null,
  "time": null
}
```

- `**date` / `time`:** null → server dùng **ngày/giờ hiện tại** (theo code `AttendanceService`).
- `**imageUrl`:** tuỳ chọn (ảnh minh chứng nếu có upload trước).

Sau khi thành công, bản ghi attendance có `**method`** kiểu **MANUAL** (theo enum lưu DB).

**Lỗi thường gặp:** trạm không thuộc route của bus trong ngày; bus chưa có lịch; HS đã check-in mà chưa check-out (phải **ManualCheckOut** trước khi check-in lại cùng ngày — xem logic trong `AttendanceService`).

### 3.4 Check-out thủ công (khi cần)

**POST** `/api/Attendance/ManualCheckOut` — cùng body `AttendanceManualDto`; chỉ khi HS **đã check-in** cùng ngày và **chưa check-out**.

---

## 4. JSON mẫu

### 4.1 Đăng nhập & Me

**GET** `/api/Account/Me` (Bearer)

```json
{
  "message": "Lấy thông tin tài khoản thành công.",
  "data": {
    "id": 9,
    "email": "teacher01@schoolbus.local",
    "fullName": "Cán bộ đưa đón 01",
    "roleName": "teacher",
    "status": "ACTIVE"
  }
}
```

### 4.2 GetByCode

**GET** `/api/Student/GetByCode/STU20260001`

```json
{
  "message": "Lấy student thành công",
  "data": {
    "id": 12,
    "studentCode": "STU20260001",
    "fullName": "Nguyễn Văn An",
    "campusId": 1,
    "campusName": "Campus …",
    "guardianId": 5,
    "status": "ACTIVE"
  }
}
```

### 4.3 ManualCheckIn — response rút gọn

```json
{
  "message": "Check in thủ công thành công",
  "data": {
    "id": 500,
    "studentId": 12,
    "studentName": "Nguyễn Văn An",
    "busId": 5,
    "busLicensePlate": "51A-12345",
    "date": "2026-04-19T00:00:00Z",
    "checkInTime": "06:15:00",
    "checkInStationId": 10,
    "checkInStationName": "Trạm 1",
    "method": "MANUAL",
    "status": "PRESENT"
  }
}
```

*(Field chi tiết theo `AttendanceDto` thực tế.)*

---

## 5. Gợi ý UI

1. Màn **“Hôm nay”**: gọi `TeacherSchedules` với `rideDate` = ngày làm việc (lưu ý UTC vs local).
2. User chọn **một ca** → cố định `**busId`** (và biết `**busScheduleId`** nếu cần gọi `Current`).
3. Ô nhập **MASV** → `GetByCode` → hiển thị tên HS; nếu lỗi → “Không tìm thấy mã”.
4. Chọn **trạm** trong danh sách trạm của tuyến (hoặc trạm mặc định theo nghiệp vụ).
5. Nút **Check-in** → `ManualCheckIn`. Sau đó có thể **Check-out** bằng `ManualCheckOut` khi xuống xe (nếu quy trình có 2 bước).

---

## 6. File tham chiếu trong repo


| Thành phần         | File                                                                                                             |
| ------------------ | ---------------------------------------------------------------------------------------------------------------- |
| Lịch teacher       | `BusTripProgressController.TeacherSchedules`, `BusTripProgressService.GetTeacherSchedulesAsync`                  |
| Tra HS theo mã     | `StudentController.GetByCode`, `StudentService.GetStudentByCodeAsync`                                            |
| Điểm danh thủ công | `AttendanceController.ManualCheckIn` / `ManualCheckOut`, `AttendanceService`                                     |
| DTO                | `AttendanceManualDto`, `Dto/BusTripProgress/BusTripProgressDriverScheduleDto.cs` (dùng chung cho driver/teacher) |


---

*Tài liệu căn theo code BE hiện tại.*