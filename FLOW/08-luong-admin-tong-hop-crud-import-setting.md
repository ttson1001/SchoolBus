# Luồng Admin (tổng hợp): Đăng nhập, import, master data, cấu hình, tra cứu & vận hành

Tài liệu **lập chỉ mục** các API trong BE phù hợp **màn quản trị / vận hành** (tạo–sửa–xóa, import Excel, cài đặt nhận diện, xem đơn hàng–ví–giao dịch, báo cáo hư hỏng xe, …).

---

## Chuẩn `ResponseDto` & lỗi


| Field (JSON) | Kiểu                    | Ghi chú                                                            |
| ------------ | ----------------------- | ------------------------------------------------------------------ |
| `message`    | `string | null`         | Thông báo thành công hoặc lỗi                                      |
| `data`       | `object | array | null` | Payload; một số **Create** chỉ trả `message`, `data` có thể `null` |


- **Header:** `Content-Type: application/json` (trừ `multipart`).
- **Thuộc tính JSON:** mặc định **camelCase** (`totalItems`, `similarityThreshold`, …).
- **Thành công (200):** `{ "message": "…", "data": … }`
- **Lỗi (thường 400):** `{ "message": "…", "data": null }`

**Phân trang `PagedResult<T>`** — `data`:

```json
{
  "totalItems": 42,
  "page": 1,
  "pageSize": 10,
  "items": [ ]
}
```

- `**TimeSpan**` trong body/response (ví dụ `startTime`, `endTime` lịch xe): chuỗi `"HH:mm:ss"` (ví dụ `"06:00:00"`).

**Lưu ý quan trọng:**

- **Không** có `AdminController` riêng; quyền **admin** (hoặc staff) do **JWT** (`claim Role` / decode token → `roleName`) — FE kiểm tra sau `GET /api/Account/Me` hoặc decode token.
- Hầu hết controller **chưa** gắn `[Authorize]` trong code — **về nghiệp vụ** FE chỉ mở các màn này cho tài khoản admin/staff và **luôn gửi Bearer token**; production nên siết `[Authorize]` + policy theo role.

**Tài liệu chi tiết đã có trong repo:**


| Nội dung                                                   | File                                                                                                                          |
| ---------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| Import phụ huynh (Excel)                                   | [docs/logic-api/luong-01-admin-import-danh-sach-phu-huynh.md](../docs/logic-api/luong-01-admin-import-danh-sach-phu-huynh.md) |
| Master data (Role, Campus, Trạm, Tuyến, Xe, Lịch, Package) | [docs/logic-api/luong-04-master-data.md](../docs/logic-api/luong-04-master-data.md)                                           |
| Chuẩn bị & tạo lịch xe                                     | [03-luong-chuan-bi-va-tao-lich-xe-bus.md](./03-luong-chuan-bi-va-tao-lich-xe-bus.md)                                          |


---

## 1. Xác thực & profile


| Method | Endpoint             | Mục đích                                                              |
| ------ | -------------------- | --------------------------------------------------------------------- |
| POST   | `/api/Account/Login` | Lấy JWT                                                               |
| GET    | `/api/Account/Me`    | `**[Authorize]`** — `data.roleName`, `data.id` (kiểm tra admin/staff) |


---

## 2. Import file (Excel / form)


| Method | Endpoint                             | Mục đích                                                                                     |
| ------ | ------------------------------------ | -------------------------------------------------------------------------------------------- |
| POST   | `/api/User/Import`                   | **multipart** — import user từ Excel (`roleName` / `roleId`, …). Chi tiết cột: xem luong-01. |
| POST   | `/api/Student/ImportByGuardianEmail` | **multipart** — import học sinh theo email phụ huynh (file + tham số form theo DTO).         |


---

## 3. Người dùng & vai trò

### 3.1 User


| Method | Endpoint                  | Mục đích                                |
| ------ | ------------------------- | --------------------------------------- |
| GET    | `/api/User/Search`        | `keyword`, `role`, `status`, phân trang |
| GET    | `/api/User/Get/{id}`      | Chi tiết user                           |
| POST   | `/api/User/Create`        | Tạo user (theo DTO role)                |
| PUT    | `/api/User/Update/{id}`   | Cập nhật                                |
| DELETE | `/api/User/Delete/{id}`   | Vô hiệu hoá (soft — status `DISABLED`)  |
| POST   | `/api/User/CreateDriver`  | Tạo tài xế                              |
| POST   | `/api/User/CreateTeacher` | Tạo giáo viên / cán bộ đưa đón          |


### 3.2 Role


| Method | Endpoint                | Mục đích                |
| ------ | ----------------------- | ----------------------- |
| GET    | `/api/Role/Search`      | Danh sách có phân trang |
| GET    | `/api/Role/Get/{id}`    | Chi tiết                |
| POST   | `/api/Role/Create`      | Tạo                     |
| PUT    | `/api/Role/Update/{id}` | Cập nhật                |
| DELETE | `/api/Role/Delete/{id}` | Xóa                     |


---

## 4. Master data (campus → trạm → tuyến → xe → lịch → gói)

Cùng nhóm endpoint như [luong-04-master-data](../docs/logic-api/luong-04-master-data.md). Tóm tắt:


| Module          | Search / list                                                  | Get `{id}` | Create   | Update        | Delete / ghi chú |
| --------------- | -------------------------------------------------------------- | ---------- | -------- | ------------- | ---------------- |
| **Campus**      | `GET /api/Campus/Search`, `Active`                             | `Get/{id}` | `Create` | `Update/{id}` | `Delete/{id}`    |
| **BusStation**  | `Search`                                                       | `Get/{id}` | `Create` | `Update/{id}` | `Delete/{id}`    |
| **BusRoute**    | `Search`, `Active`                                             | `Get/{id}` | `Create` | `Update/{id}` | `Delete/{id}`    |
| **Bus**         | `Search`, `GetByCampus/{campusId}`                             | `Get/{id}` | `Create` | `Update/{id}` | `Delete/{id}`    |
| **BusSchedule** | `Search`, `GetByBus`, `GetByRoute`, `GetByCampus`, `GetAtTime` | `Get/{id}` | `Create` | `Update/{id}` | `Delete/{id}`    |
| **Package**     | `Search`, `Active`                                             | `Get/{id}` | `Create` | `Update/{id}` | `Delete/{id}`    |


---

## 5. Phân công lái xe & cán bộ


| Method | Endpoint                         | Mục đích                                                        |
| ------ | -------------------------------- | --------------------------------------------------------------- |
| GET    | `/api/BusAssignment/Search`      | `busId`, `driverId`, `teacherId`, …                             |
| GET    | `/api/BusAssignment/Get/{id}`    | Chi tiết                                                        |
| POST   | `/api/BusAssignment/Create`      | Gán tài xế + teacher lên xe (upsert theo `BusId` trong service) |
| PUT    | `/api/BusAssignment/Update/{id}` | Cập nhật                                                        |
| DELETE | `/api/BusAssignment/Delete/{id}` | Xóa                                                             |


---

## 6. Học sinh (CRUD + tra cứu)


| Method | Endpoint                                  | Mục đích                                                  |
| ------ | ----------------------------------------- | --------------------------------------------------------- |
| GET    | `/api/Student/Search`                     | `keyword`, `campusId`, `guardianId`, `status`, phân trang |
| GET    | `/api/Student/Get/{id}`                   | Chi tiết                                                  |
| GET    | `/api/Student/GetByCode/{studentCode}`    | Theo mã HS                                                |
| GET    | `/api/Student/GetByCampus/{campusId}`     | Theo campus                                               |
| GET    | `/api/Student/GetByGuardian/{guardianId}` | Theo phụ huynh                                            |
| POST   | `/api/Student/Create`                     | Tạo HS                                                    |
| PUT    | `/api/Student/Update/{id}`                | Cập nhật                                                  |
| DELETE | `/api/Student/Delete/{id}`                | Xóa                                                       |


**Lưu ý:** `GET /api/Student/GetMyStudents` là luồng **phụ huynh** (token), không phải màn admin tổng quát.

---

## 7. Đăng ký đi xe (StudentBusAssignment) — xem / sửa vận hành


| Method | Endpoint                                               | Mục đích                                                |
| ------ | ------------------------------------------------------ | ------------------------------------------------------- |
| GET    | `/api/StudentBusAssignment/Search`                     | Lọc `studentId`, `guardianId`, `routeId`, `rideDate`, … |
| GET    | `/api/StudentBusAssignment/Get/{id}`                   | Chi tiết                                                |
| GET    | `/api/StudentBusAssignment/GetByStudent/{studentId}`   | Theo HS                                                 |
| GET    | `/api/StudentBusAssignment/GetByGuardian/{guardianId}` | Theo PH                                                 |
| POST   | `/api/StudentBusAssignment/Create`                     | Theo `routeId` + ngày + trạm                            |
| POST   | `/api/StudentBusAssignment/CreateBySchedule`           | Theo `busScheduleId` + ngày + trạm                      |
| PUT    | `/api/StudentBusAssignment/Update/{id}`                | Cập nhật                                                |
| PUT    | `/api/StudentBusAssignment/UpdateBySchedule/{id}`      | Cập nhật theo lịch                                      |
| DELETE | `/api/StudentBusAssignment/Delete/{id}`                | Xóa                                                     |


---

## 8. Đơn hàng gói & ví & giao dịch

### 8.1 Order


| Method | Endpoint                                    | Mục đích                                                             |
| ------ | ------------------------------------------- | -------------------------------------------------------------------- |
| POST   | `/api/Order/Create`                         | Mua gói (trừ ví) — thường dùng bởi PH; admin có thể xem luồng hỗ trợ |
| POST   | `/api/Order/CreatePayOsLink`                | Link PayOS mua gói                                                   |
| POST   | `/api/Order/HandlePayOsWebhook`             | Webhook PayOS (server)                                               |
| GET    | `/api/Order/Search`                         | Lọc `status`, `guardianId`, `studentId`, ngày                        |
| GET    | `/api/Order/Get/{id}`                       | Chi tiết đơn                                                         |
| GET    | `/api/Order/GetByGuardian/{guardianId}`     | Đơn theo PH                                                          |
| GET    | `/api/Order/GetActiveByStudent/{studentId}` | Gói đang active                                                      |
| GET    | `/api/Order/GetPayOsStatus/{orderCode}`     | Trạng thái PayOS                                                     |
| PUT    | `/api/Order/Cancel/{id}`                    | Huỷ đơn                                                              |


### 8.2 Wallet


| Method | Endpoint                                      | Mục đích                             |
| ------ | --------------------------------------------- | ------------------------------------ |
| GET    | `/api/Wallet/Search`                          | Tra ví                               |
| GET    | `/api/Wallet/GetByUser/{userId}`              | Ví theo user                         |
| GET    | `/api/Wallet/TransactionHistory/{walletId}`   | Lịch sử giao dịch ví                 |
| POST   | `/api/Wallet/TopUp`                           | Nạp tiền (trực tiếp — tuỳ nghiệp vụ) |
| POST   | `/api/Wallet/CreatePayOsTopUpLink`            | Link nạp PayOS                       |
| POST   | `/api/Wallet/HandlePayOsWebhook`              | Webhook nạp                          |
| GET    | `/api/Wallet/GetPayOsTopUpStatus/{orderCode}` | Trạng thái nạp PayOS                 |


### 8.3 TransactionLog


| Method | Endpoint                          | Mục đích                                                               |
| ------ | --------------------------------- | ---------------------------------------------------------------------- |
| GET    | `/api/TransactionLog/Search`      | `keyword`, `method`, `status`, `orderId`, `code`, `fromDate`, `toDate` |
| GET    | `/api/TransactionLog/Get/{id}`    | Chi tiết                                                               |
| POST   | `/api/TransactionLog/Create`      | Tạo bản ghi (thường hệ thống; admin ít dùng)                           |
| PUT    | `/api/TransactionLog/Update/{id}` | Cập nhật                                                               |
| DELETE | `/api/TransactionLog/Delete/{id}` | Xóa                                                                    |


---

## 9. Điểm danh & báo cáo


| Method | Endpoint                                           | Mục đích                                                                                  |
| ------ | -------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| GET    | `/api/Attendance/Search`                           | Toàn bộ bộ lọc (keyword, ngày, campus, bus, student, guardian, status) — **màn tổng hợp** |
| GET    | `/api/Attendance/Get/{id}`                         | Chi tiết bản ghi                                                                          |
| GET    | `/api/Attendance/GetByStudent/{studentId}`         | Lịch sử theo HS                                                                           |
| POST   | `/api/Attendance/ManualCheckIn` / `ManualCheckOut` | Điểm danh tay (thường teacher; admin có thể hỗ trợ nghiệp vụ)                             |


### Báo cáo hư hỏng xe


| Method | Endpoint                           | Mục đích                                  |
| ------ | ---------------------------------- | ----------------------------------------- |
| GET    | `/api/BusDamageReport/Search`      | `keyword`, `status`, `busId`, …           |
| GET    | `/api/BusDamageReport/Get/{id}`    | Chi tiết                                  |
| PUT    | `/api/BusDamageReport/Update/{id}` | Đổi trạng thái / nội dung (xử lý báo cáo) |
| DELETE | `/api/BusDamageReport/Delete/{id}` | Xóa (nếu cho phép)                        |


*(Tạo báo cáo thường từ phía lái/field — `Create` trong controller.)*

---

## 10. Cấu hình & tiện ích

### 10.1 Nhận diện khuôn mặt (ngưỡng similarity)


| Method | Endpoint                                                | Mục đích                            |
| ------ | ------------------------------------------------------- | ----------------------------------- |
| GET    | `/api/FaceRecognitionSetting/GetSimilarityThreshold`    | Đọc ngưỡng (DTO từ `SystemSetting`) |
| PUT    | `/api/FaceRecognitionSetting/UpdateSimilarityThreshold` | Body: `similarityThreshold` (0–1)   |


### 10.2 Upload ảnh (Cloudinary)


| Method | Endpoint            | Mục đích                                           |
| ------ | ------------------- | -------------------------------------------------- |
| POST   | `/api/Upload/Image` | **multipart** — ảnh dùng cho avatar, minh chứng, … |


### 10.3 Theo dõi xe (GPS)


| Method | Endpoint                             | Mục đích                   |
| ------ | ------------------------------------ | -------------------------- |
| POST   | `/api/BusTracking/Update`            | Cập nhật tọa độ            |
| GET    | `/api/BusTracking/GetLatest/{busId}` | Vị trí GPS mới nhất của xe |


### 10.4 FaceAI (vận hành / kiểm thử)


| Method | Endpoint                  | Mục đích                              |
| ------ | ------------------------- | ------------------------------------- |
| GET    | `/api/FaceAI/Health`      | Health check dịch vụ FaceAI           |
| GET    | `/api/FaceAI/GetStudents` | Danh sách subject phía FaceAI (debug) |


*(Các API `CreateStudent`, `AddStudentFace`, `Verify`, `RecognizeCheckIn` … dùng theo luồng PH/tài xế — [04](./04-luong-guardian-hoc-sinh-faceai-dang-ky-lich-xe.md), [05](./05-luong-tai-xe-lich-tram-diem-danh-khuon-mat.md).)*

---

## 11. Gợi ý nhóm màn hình admin


| Nhóm màn                  | API chính                                                                              |
| ------------------------- | -------------------------------------------------------------------------------------- |
| **Dashboard / tra cứu**   | `Order/Search`, `Attendance/Search`, `TransactionLog/Search`, `BusDamageReport/Search` |
| **Người dùng**            | `User/`*, `User/Import`, `Role/*`                                                      |
| **Danh mục & lịch**       | Campus, BusStation, BusRoute, Bus, `BusSchedule/`*, `Package/*`, `BusAssignment/*`     |
| **Học sinh & đăng ký xe** | `Student/`*, `StudentBusAssignment/*`                                                  |
| **Tài chính**             | `Wallet/`*, `Order/*`                                                                  |
| **Cấu hình**              | `FaceRecognitionSetting/`*, (cấu hình app: `appsettings` / env — ngoài API)            |


---

## 12. JSON & Response mẫu (admin / FE)

### 12.1 Đăng nhập

**POST** `/api/Account/Login`

```json
{
  "email": "admin@schoolbus.local",
  "password": "123456"
}
```

**Response 200**

```json
{
  "message": "Đăng nhập thành công.",
  "data": {
    "token": "eyJhbGciOi..."
  }
}
```

### 12.2 Tài khoản hiện tại

**GET** `/api/Account/Me` — `Authorization: Bearer <token>`

```json
{
  "message": "Lấy thông tin tài khoản thành công.",
  "data": {
    "id": 1,
    "email": "admin@schoolbus.local",
    "fullName": "Admin",
    "roleName": "admin",
    "status": "ACTIVE",
    "createdAt": "2026-01-01T00:00:00Z"
  }
}
```

### 12.3 Import user (Excel)

**POST** `/api/User/Import` — `multipart/form-data`: part `file` = `.xlsx`

**Response 200** — `data` = `UserImportResultDto`:

```json
{
  "message": "Import user thành công",
  "data": {
    "totalRows": 100,
    "successRows": 98,
    "failedRows": 2,
    "errors": [
      "Dòng 5: email đã tồn tại",
      "Dòng 12: role không hợp lệ"
    ]
  }
}
```

### 12.4 User — Search (phân trang)

**GET** `/api/User/Search?keyword=&role=guardian&status=ACTIVE&page=1&pageSize=10`

**Response 200** — `data` = `PagedResult<UserDto>`:

```json
{
  "message": "Lấy danh sách user thành công",
  "data": {
    "totalItems": 50,
    "page": 1,
    "pageSize": 10,
    "items": [
      {
        "id": 5,
        "email": "guardian01@schoolbus.local",
        "fullName": "Nguyễn Thị PH",
        "phone": "0902000001",
        "roleName": "guardian",
        "status": "ACTIVE",
        "createdAt": "2026-03-01T00:00:00Z"
      }
    ]
  }
}
```

### 12.5 User — Create

**POST** `/api/User/Create`

```json
{
  "email": "newuser@schoolbus.local",
  "password": "123456",
  "fullName": "Người dùng mới",
  "phone": "0900000000",
  "role": "guardian"
}
```

**Response 200:** `data` thường là `UserDto` (theo service).

### 12.6 Role — Create

**POST** `/api/Role/Create`

```json
{
  "name": "staff"
}
```

**Response 200:** nhiều action `Create` role chỉ trả `message`, `data` = `null` — tra cứu lại bằng `Search` / `Get/{id}`.

### 12.7 Campus — Create (ví dụ)

**POST** `/api/Campus/Create`

```json
{
  "code": "CS002",
  "name": "Cơ sở B",
  "address": "456 Đường …",
  "phone": "0280000000",
  "isActive": true,
  "imageUrl": null
}
```

**Response 200:** thường chỉ `message` — lấy `id` qua `Search` / `Get/{id}`.

### 12.8 BusSchedule — Create (ví dụ)

**POST** `/api/BusSchedule/Create`

```json
{
  "busId": 5,
  "routeId": 3,
  "startDate": "2026-04-21T00:00:00Z",
  "endDate": "2026-12-31T00:00:00Z",
  "startTime": "06:00:00",
  "endTime": "08:30:00",
  "dayOfWeek": 0,
  "shiftType": "PICKUP"
}
```

**Response 200** — `data` = `BusScheduleDto` (xem [03-luong-chuan-bi-va-tao-lich-xe-bus.md](./03-luong-chuan-bi-va-tao-lich-xe-bus.md)).

### 12.9 BusAssignment — Create

**POST** `/api/BusAssignment/Create`

```json
{
  "busId": 5,
  "driverId": 8,
  "teacherId": 9
}
```

**Response 200** — `data` = `BusAssignmentDto` (lồng `bus`, `driver`, `teacher`).

### 12.10 Student — Create

**POST** `/api/Student/Create`

```json
{
  "studentCode": "STU20260100",
  "fullName": "Trần Thị B",
  "avatarUrl": null,
  "dateOfBirth": "2016-08-01T00:00:00Z",
  "gender": "Female",
  "guardianId": 5,
  "campusId": 1
}
```

**Response 200:** thường không có `data` trong body — gọi `Search` / `Get` để lấy `id`.

### 12.11 FaceRecognitionSetting

**GET** `/api/FaceRecognitionSetting/GetSimilarityThreshold`

```json
{
  "message": "Lay SimilarityThreshold thanh cong",
  "data": {
    "similarityThreshold": 0.85,
    "source": "database"
  }
}
```

**PUT** `/api/FaceRecognitionSetting/UpdateSimilarityThreshold`

```json
{
  "similarityThreshold": 0.88
}
```

**Response 200:** `data` cùng cấu trúc `SimilarityThresholdDto`.

### 12.12 Upload ảnh

**POST** `/api/Upload/Image` — `multipart/form-data`, part `file`

**Response 200** — `data` thường chứa `url` (Cloudinary), tùy `UploadResult`:

```json
{
  "message": "Upload ảnh thành công",
  "data": {
    "url": "https://res.cloudinary.com/…/image.jpg",
    "publicId": "schoolbus/…",
    "format": "jpg",
    "bytes": 123456
  }
}
```

### 12.13 Attendance — Search (màn tổng hợp)

**GET** `/api/Attendance/Search?guardianId=5&date=2026-04-19&page=1&pageSize=10`

**Response 200** — `data` = `PagedResult<AttendanceDto>` (`items[]` có `studentName`, `busLicensePlate`, `checkInTime`, `method`, …).

### 12.14 BusDamageReport — Get / Update

**GET** `/api/BusDamageReport/Get/1` — `data` = DTO báo cáo (tiêu đề, mô tả, `status`, `busLicensePlate`, …).

**PUT** `/api/BusDamageReport/Update/1`

```json
{
  "title": "Vỏ xe trầy",
  "description": "Đã xử lý sơn",
  "status": "RESOLVED",
  "resolvedAt": "2026-04-19T10:00:00Z"
}
```

### 12.15 Order — Get (chi tiết)

**GET** `/api/Order/Get/100`

**Response 200** — `data` = `OrderDto` (`guardianName`, `studentName`, `packageName`, `status`, `startDate`, `endDate`, …).

---

## 13. File tham chiếu nhanh


| Khu vực                   | Controller (thư mục `Controllers/`)   |
| ------------------------- | ------------------------------------- |
| Account                   | `AccountController`                   |
| User                      | `UserController`                      |
| Role                      | `RoleController .cs`                  |
| Campus                    | `CampusController .cs`                |
| BusStation                | `BusStationController .cs`            |
| BusRoute                  | `BusRouteController.cs`               |
| Bus                       | `BusController .cs`                   |
| BusSchedule               | `BusScheduleController.cs`            |
| BusAssignment             | `BusAssignmentController.cs`          |
| Student                   | `StudentController .cs`               |
| StudentBusAssignment      | `StudentBusAssignmentController.cs`   |
| Order                     | `OrderController.cs`                  |
| Wallet                    | `WalletController.cs`                 |
| TransactionLog            | `TransactionLogController.cs`         |
| Attendance                | `AttendanceController.cs`             |
| BusDamageReport           | `BusDamageReportController.cs`        |
| BusTracking               | `BusTrackingController.cs`            |
| Face recognition settings | `FaceRecognitionSettingController.cs` |
| Upload                    | `UploadController.cs`                 |
| FaceAI                    | `FaceAIController.cs`                 |


---

*Tài liệu chỉ mục theo code hiện tại; thêm ví dụ JSON tại mục 12; chi tiết DTO đầy đủ xem `docs/logic-api/`, `luồng/03`, hoặc Swagger.*