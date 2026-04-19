# Luồng phụ huynh: Xem lịch sử điểm danh của học sinh & thông báo liên quan

Tài liệu mô tả cách FE gọi API để **phụ huynh** xem **lịch sử điểm danh** của con, và làm rõ **thông báo** (push / dữ liệu) trong hệ thống hiện tại.

Envelope: `{ "message": "...", "data": ... }` (`ResponseDto`), **camelCase**.

**Liên quan:** [04-luong-guardian-hoc-sinh-faceai-dang-ky-lich-xe.md](./04-luong-guardian-hoc-sinh-faceai-dang-ky-lich-xe.md) (đăng nhập, `GetMyStudents`).

---

## 1. API dùng cho lịch sử điểm danh

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| GET | `/api/Account/Me` | Xác nhận `guardianId` = `data.id`, `roleName` = `guardian` |
| GET | `/api/Student/GetMyStudents` | Danh sách học sinh (lấy `studentId` hợp lệ) — **có `[Authorize]`** |
| GET | `/api/Attendance/GetByStudent/{studentId}` | **Lịch sử điểm danh theo học sinh** (khoảng ngày tuỳ chọn) |
| GET | `/api/Attendance/Search` | Tìm kiếm phân trang; lọc theo **`guardianId`** và/hoặc **`studentId`**, ngày, trạng thái, … |

**Bảo mật / UX:**

- **`GetByStudent`** trong service **chỉ** kiểm tra học sinh tồn tại — **không** kiểm tra `Student.GuardianId` trùng user đang gọi. FE **bắt buộc** chỉ truyền `studentId` lấy từ **`GetMyStudents`** (hoặc sau này BE bổ sung `[Authorize]` + kiểm tra quyền).
- **`Search`** có filter `guardianId`: khi gọi với `guardianId` = id phụ huynh đang đăng nhập, chỉ trả attendance của học sinh thuộc phụ huynh đó.

---

## 2. Luồng gợi ý (lịch sử điểm danh)

```text
POST /api/Account/Login
        │
        ▼
GET /api/Account/Me  →  guardianId
        │
        ▼
GET /api/Student/GetMyStudents  →  chọn một học sinh (studentId)
        │
        ├─ Cách A — chi tiết theo ngày (một HS)
        │     GET /api/Attendance/GetByStudent/{studentId}?fromDate=2026-04-01&toDate=2026-04-30
        │
        └─ Cách B — danh sách phân trang (một hoặc mọi HS của PH)
              GET /api/Attendance/Search?guardianId={guardianId}&studentId={optional}&date=…&page=1&pageSize=20
```

---

## 3. Chi tiết API

### 3.1 `GET /api/Attendance/GetByStudent/{studentId}`

| Query | Bắt buộc | Ghi chú |
|-------|----------|--------|
| `fromDate` | Không | Lọc `Date >= fromDate` |
| `toDate` | Không | Lọc `Date <= toDate` |

**Response 200:** `data` là **mảng** `AttendanceDto` (sắp xếp ngày mới trước): `studentName`, `busLicensePlate`, `date`, `checkInTime`, `checkOutTime`, `checkInStationName`, `checkOutStationName`, `method` (trong DB: **`MANUAL`** | **`FACE`**), `status`, `note`, …

**Lỗi:** học sinh không tồn tại.

### 3.2 `GET /api/Attendance/Search`

| Query | Ghi chú |
|-------|--------|
| `guardianId` | Chỉ attendance của học sinh thuộc phụ huynh này |
| `studentId` | Thu hẹp một học sinh |
| `date` | Một ngày cụ thể (theo code: so khớp `Date` ngày đó) |
| `status` | Theo enum attendance (ví dụ `PRESENT`) |
| `keyword` | Tìm theo tên HS, biển số, campus, tên trạm, … |
| `page`, `pageSize` | Phân trang |

**Response 200:** `data` = `PagedResult<AttendanceDto>` (`items`, `totalItems`, `page`, `pageSize`).

---

## 4. Thông báo (nhắc học sinh / điểm danh)

### 4.1 Nguồn thông báo trong hệ thống

Khi có **check-in / check-out** (thủ công hoặc qua nhận diện), `AttendanceService` có thể:

1. **Lưu bản ghi** vào bảng **`Notifications`** (`UserId` = phụ huynh, `Message`, `Type`, `IsRead`, `CreatedAt`).
2. **Gửi push** qua **Firebase** tới `deviceToken` của user phụ huynh (nếu có), kèm **data** (`type`, `studentId`, `guardianId`, `busId`, `routeName`, `attendanceDate`, `checkTime`, …).

Các `type` ví dụ: `BOARDING`, `ALIGHTING`, `WRONG_DROPOFF` (theo code gửi thông báo).

### 4.2 Phụ huynh “xem thông báo” trên app — hiện trạng BE

- **Push:** mobile nhận tin qua **Firebase Cloud Messaging** (token lưu khi login / cập nhật `deviceToken`).
- **Lịch sử thông báo trong app (danh sách đã lưu):** bảng `Notifications` đã có dữ liệu, nhưng **trong repo hiện không có `NotificationController`** / endpoint **GET** để phụ huynh tải danh sách thông báo từ API.

**Gợi ý tích hợp FE hiện tại:**

- Ưu tiên **màn hình push** + xử lý payload (`studentId`, `type`, …) để điều hướng tới chi tiết học sinh / điểm danh.
- Nếu cần **lịch sử thông báo trong app**, cần **bổ sung API** (ví dụ `GET /api/Notification/ByUser`) hoặc đồng bộ từ backend sau này — **không có trong code BE hiện tại**.

### 4.3 Liên hệ với lịch sử điểm danh

Nội dung thông báo (lên/xuống xe, trạm, giờ) **tương ứng** với các bản ghi có thể xem lại qua **`GetByStudent`** / **`Search`** — phụ huynh có thể đối chiếu **ngày + giờ + trạm** giữa push và màn lịch sử attendance.

---

## 5. JSON mẫu

### 5.1 GetMyStudents (rút gọn)

```json
{
  "message": "Lấy danh sách student thành công",
  "data": [
    { "id": 12, "fullName": "Nguyễn Văn An", "campusName": "…", "status": "ACTIVE" }
  ]
}
```

### 5.2 GetByStudent

**GET** `/api/Attendance/GetByStudent/12?fromDate=2026-04-01&toDate=2026-04-30`

```json
{
  "message": "Lấy lịch sử attendance thành công",
  "data": [
    {
      "id": 500,
      "studentId": 12,
      "studentName": "Nguyễn Văn An",
      "guardianId": 5,
      "busLicensePlate": "51A-12345",
      "date": "2026-04-19T00:00:00Z",
      "checkInTime": "06:15:00",
      "checkOutTime": "07:00:00",
      "checkInStationName": "Trạm 1",
      "checkOutStationName": "Trạm 3",
      "method": "FACE",
      "status": "PRESENT",
      "note": "Học sinh có gói còn hiệu lực"
    }
  ]
}
```

*(Giá trị `method` / `note` theo dữ liệu thật.)*

### 5.3 Search theo phụ huynh

**GET** `/api/Attendance/Search?guardianId=5&studentId=12&page=1&pageSize=10`

```json
{
  "message": "Lấy danh sách attendance thành công",
  "data": {
    "totalItems": 15,
    "page": 1,
    "pageSize": 10,
    "items": [ ]
  }
}
```

---

## 6. File tham chiếu trong repo

| Nội dung | File |
|----------|------|
| Điểm danh | `Controllers/AttendanceController.cs`, `Service/AttendanceService.cs` |
| Học sinh của tôi | `Controllers/StudentController .cs` — `GetMyStudents` |
| Thông báo + Firebase | `Service/AttendanceService.cs` — `CreateGuardianNotificationAsync`; `Service/FirebaseNotificationService.cs` |
| Entity thông báo | `Entites/Notification.cs` |

---

*Tài liệu căn theo code BE hiện tại; khi có API Notification cho phụ huynh, nên cập nhật mục 4.2.*
