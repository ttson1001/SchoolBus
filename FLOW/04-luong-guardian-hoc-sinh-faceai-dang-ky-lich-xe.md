# Luồng phụ huynh: Đăng ký học sinh → FaceAI (khuôn mặt) → Đăng ký lịch / điểm đón trả xe

Tài liệu mô tả **thứ tự nghiệp vụ và API** trong BE hiện tại để phụ huynh: **đăng nhập**, **tạo hồ sơ học sinh**, **đăng ký khuôn mặt** (một bước FE: `**AddStudentFace`**), mua gói (order) nếu cần, rồi đăng ký đi xe theo lịch (`StudentBusAssignment` gắn `BusSchedule`). `**CreateStudent` trên FaceAI** là **tuỳ chọn** — có thể bỏ qua nếu luồng chỉ cần upload ảnh.

Envelope chung: `{ "message": "...", "data": ... }` (`ResponseDto`). Tên field JSON mặc định **camelCase**.

**Tài liệu liên quan:** [luong-02-phu-huynh-dang-nhap-tao-profile-hoc-sinh.md](../docs/logic-api/luong-02-phu-huynh-dang-nhap-tao-profile-hoc-sinh.md), [03-luong-chuan-bi-va-tao-lich-xe-bus.md](./03-luong-chuan-bi-va-tao-lich-xe-bus.md) (dữ liệu lịch xe do admin tạo trước).

---

## 1. Điều kiện nền (do admin / hệ thống)

Phụ huynh **không** tạo campus, tuyến hay `BusSchedule`; cần đã có trong DB:

- **Campus** active (học sinh gắn `campusId`).
- **Tuyến** + **trạm** + **lịch chạy** (`BusSchedule`) cho campus/tuyến tương ứng.
- (Tuỳ seed / vận hành) **ví** cho guardian: `Order` trừ tiền ví yêu cầu guardian **đã có ví** và **đủ số dư** khi dùng `POST /api/Order/Create`.

---

## 2. Bảng API theo giai đoạn


| Giai đoạn                         | Method | Endpoint                                                | Mục đích                                                                                                    |
| --------------------------------- | ------ | ------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| Đăng nhập                         | POST   | `/api/Account/Login`                                    | Lấy JWT                                                                                                     |
| Profile PH                        | GET    | `/api/Account/Me`                                       | `id` = `guardianId`, `roleName`                                                                             |
| Danh sách HS                      | GET    | `/api/Student/GetMyStudents`                            | Cần **Bearer**; chỉ guardian                                                                                |
| Campus                            | GET    | `/api/Campus/Search` hoặc `/api/Campus/Active`          | Chọn cơ sở                                                                                                  |
| Tạo HS                            | POST   | `/api/Student/Create`                                   | Tạo profile; **không trả `studentId` trong body response** — cần gọi lại Get/Search                         |
| Lấy `studentId`                   | GET    | `/api/Student/GetMyStudents` hoặc `Get/{id}` / `Search` | Lấy `id` học sinh cho FaceAI                                                                                |
| FaceAI — đăng ký khuôn mặt        | POST   | `/api/FaceAI/AddStudentFace/{studentId}`                | `**multipart/form-data`**, field `file` — **bước chính**; FE có thể gom **một màn** chỉ gọi API này.        |
| FaceAI — tạo subject *(tuỳ chọn)* | POST   | `/api/FaceAI/CreateStudent`                             | Tạo subject trên dịch vụ FaceAI **trước** khi add face — **có thể bỏ qua** nếu không tách bước / không cần. |
| FaceAI (tuỳ chọn)                 | GET    | `/api/FaceAI/GetStudentFaces/{studentId}`               | Kiểm tra face đã lưu                                                                                        |
| Gói dịch vụ                       | GET    | `/api/Package/Active`                                   | Chọn `packageId`                                                                                            |
| Ví                                | GET    | `/api/Wallet/GetByUser/{userId}`                        | `userId` = `guardianId`; xem số dư                                                                          |
| Mua gói (ví)                      | POST   | `/api/Order/Create`                                     | Trừ ví; cần đủ tiền                                                                                         |
| Mua gói (PayOS)                   | POST   | `/api/Order/CreatePayOsLink`                            | Tạo link thanh toán; order `PENDING` đến khi webhook thành công                                             |
| Lịch theo campus                  | GET    | `/api/BusSchedule/GetByCampus/{campusId}`               | `campusId` trùng campus của học sinh                                                                        |
| Đăng ký đi xe                     | POST   | `/api/StudentBusAssignment/CreateBySchedule`            | Gắn `busScheduleId` + ngày + đón/trả                                                                        |


**Lưu ý bảo mật:** Nhiều endpoint (Student `Create`, FaceAI, …) **không** gắn `[Authorize]` trong code — FE vẫn nên gửi **Bearer** sau login và chỉ hiển thị luồng phụ huynh khi token hợp lệ; production nên siết policy ở backend.

---

## 3. Luồng tổng quát

```text
POST /api/Account/Login
        │
        ▼
GET /api/Account/Me  →  guardianId (= data.id)
        │
        ▼
GET /api/Student/GetMyStudents
        │
        ├─ Chưa có học sinh / cần thêm
        │     GET /api/Campus/Active
        │     POST /api/Student/Create  (guardianId, campusId, …)
        │     GET /api/Student/GetMyStudents  →  studentId (= data[].id)
        │
        └─ Đã có studentId
              │
              ├─ (tuỳ chọn) POST /api/FaceAI/CreateStudent  { studentId, name }   ← bỏ qua cũng được
              │
              ▼
        POST /api/FaceAI/AddStudentFace/{studentId}  (multipart: file ảnh)   ← đăng ký mặt, một bước chính
              │
              ▼
        GET /api/Package/Active  →  packageId
        GET /api/Wallet/GetByUser/{guardianId}
              │
              ▼
        POST /api/Order/Create  hoặc  POST /api/Order/CreatePayOsLink
              │
              ▼
        GET /api/BusSchedule/GetByCampus/{campusId}
              │
              ▼
        POST /api/StudentBusAssignment/CreateBySchedule
              │
              ▼
        GET /api/StudentBusAssignment/GetByGuardian/{guardianId}?rideDate=…
```

---

## 4. JSON mẫu (FE ghép nhanh)

### 4.1 Đăng nhập

**POST** `/api/Account/Login`

```json
{
  "email": "guardian01@schoolbus.local",
  "password": "123456",
  "deviceToken": null
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

### 4.2 Tài khoản hiện tại

**GET** `/api/Account/Me` — Header: `Authorization: Bearer <token>`

```json
{
  "message": "Lấy thông tin tài khoản thành công.",
  "data": {
    "id": 5,
    "email": "guardian01@schoolbus.local",
    "fullName": "Nguyễn Thị Phụ Huynh",
    "phone": "0902000001",
    "roleName": "guardian",
    "status": "ACTIVE",
    "createdAt": "2026-04-01T00:00:00Z"
  }
}
```

Dùng `**data.id**` làm `**guardianId**` khi tạo học sinh / order / tra ví.

### 4.3 Tạo học sinh

**POST** `/api/Student/Create`

```json
{
  "studentCode": "STU20260099",
  "fullName": "Nguyễn Văn An",
  "avatarUrl": null,
  "dateOfBirth": "2017-05-10T00:00:00Z",
  "gender": "Male",
  "guardianId": 5,
  "campusId": 1
}
```

**Response 200** — `Create` không gán `data` trong controller; FE nhận `data: null` (hoặc không có field `data` tuỳ cấu hình serializer):

```json
{
  "message": "Tạo student thành công",
  "data": null
}
```

Sau đó gọi `**GET /api/Student/GetMyStudents`** để lấy `**id`** và `studentCode`.

#### 4.3.1 Danh sách học sinh của phụ huynh

**GET** `/api/Student/GetMyStudents` — Bearer bắt buộc; role `guardian`.

**Response 200** — `data` là **mảng** `StudentDto` (`id`, `studentCode`, `fullName`, `guardianId`, `campusId`, `campusName`, …):

```json
{
  "message": "Lấy danh sách student thành công",
  "data": [
    {
      "id": 12,
      "studentCode": "STU20260099",
      "fullName": "Nguyễn Văn An",
      "avatarUrl": null,
      "dateOfBirth": "2017-05-10T00:00:00Z",
      "gender": "Male",
      "guardianId": 5,
      "guardianName": "Nguyễn Thị Phụ Huynh",
      "campusId": 1,
      "campusName": "Cơ sở A",
      "status": "ACTIVE"
    }
  ]
}
```

### 4.4 FaceAI — đăng ký khuôn mặt (một bước FE)

Luồng giao diện có thể **chỉ** gọi `**AddStudentFace`** (upload ảnh); không bắt buộc tách màn `**CreateStudent`**.

#### Bước chính — upload ảnh

**POST** `/api/FaceAI/AddStudentFace/{studentId}`  
`Content-Type: multipart/form-data`


| Part   | Giá trị                    |
| ------ | -------------------------- |
| `file` | file ảnh (`image/jpeg`, …) |


**Response 200** — `message` = "Đăng ký khuôn mặt học sinh thành công". `data` là **JSON gốc** do dịch vụ FaceAI (BE forward `JsonNode`), không phải DTO cố định trong repo. Ví dụ **minh hoạ** (field thực tế có thể khác):

```json
{
  "message": "Đăng ký khuôn mặt học sinh thành công",
  "data": {
    "face_id": 101,
    "student_id": 12,
    "image_url": "https://example.com/faces/101.jpg"
  }
}
```

**Lỗi thường gặp:** file rỗng / không phải `image/`* → `message` lỗi từ `FaceAIService`.

#### Tuỳ chọn — tạo subject trên FaceAI trước (có thể bỏ qua)

**POST** `/api/FaceAI/CreateStudent`

DTO dùng `**studentId` kiểu `int`**. Nếu `Student.Id` trong DB vượt `int.MaxValue`, cần điều chỉnh BE; với id nhỏ, gửi số nguyên.

```json
{
  "studentId": 12,
  "name": "Nguyễn Văn An"
}
```

**Response 200** — `data` là object JSON do **FaceAI service** trả về (BE forward / chuẩn hoá URL). Ví dụ **minh hoạ**:

```json
{
  "message": "Tạo student trên FaceAI thành công",
  "data": {
    "student_id": 12,
    "name": "Nguyễn Văn An",
    "status": "created"
  }
}
```

Cấu trúc thực tế phụ thuộc bản FaceAI đang chạy.

*Nếu triển khai FaceAI **bắt buộc** có subject trước khi gắn ảnh, gọi `CreateStudent` rồi mới `AddStudentFace`; nếu không, **chỉ `AddStudentFace`** là đủ cho luồng đơn giản.*

#### (Tuỳ chọn) GET danh sách face metadata

**GET** `/api/FaceAI/GetStudentFaces/{studentId}` — `data` cũng là JSON từ FaceAI. Ví dụ minh hoạ:

```json
{
  "message": "Lấy face metadata của student thành công",
  "data": [
    {
      "id": 101,
      "student_id": 12,
      "image_url": "https://example.com/faces/101.jpg"
    }
  ]
}
```

### 4.5 Gói dịch vụ & ví

**GET** `/api/Package/Active?page=1&pageSize=20` — `data` là `PagedResult<PackageDto>`: `totalItems`, `page`, `pageSize`, `items`.

**Response 200** (mẫu):

```json
{
  "message": "Lấy danh sách package thành công",
  "data": {
    "totalItems": 2,
    "page": 1,
    "pageSize": 20,
    "items": [
      {
        "id": 1,
        "name": "Gói 30 ngày",
        "price": 300000,
        "durationDays": 30,
        "description": "Đi xe buýt trọn gói",
        "status": "ACTIVE",
        "createdAt": "2026-01-01T00:00:00Z",
        "type": null,
        "imageUrl": null
      }
    ]
  }
}
```

**GET** `/api/Campus/Active?page=1&pageSize=20` — cùng kiểu phân trang; `items[]` là `CampusDto`:

```json
{
  "message": "Lấy danh sách campus thành công",
  "data": {
    "totalItems": 1,
    "page": 1,
    "pageSize": 20,
    "items": [
      {
        "id": 1,
        "code": "CS-A",
        "name": "Cơ sở A",
        "address": "123 Đường X",
        "phone": "0280000000",
        "isActive": true,
        "imageUrl": null
      }
    ]
  }
}
```

**GET** `/api/Wallet/GetByUser/{userId}` — `userId` = guardian.

```json
{
  "message": "Lấy ví thành công",
  "data": {
    "id": 1,
    "userId": 5,
    "userName": "Nguyễn Thị Phụ Huynh",
    "email": "guardian01@schoolbus.local",
    "balance": 500000
  }
}
```

### 4.6 Mua gói (trừ ví)

**POST** `/api/Order/Create`

```json
{
  "guardianId": 5,
  "studentId": 12,
  "packageId": 1
}
```

Điều kiện (trích `OrderService`): guardian đúng role, học sinh thuộc guardian, **đã có ví**, **số dư ≥ giá gói**, học sinh **chưa có order active** (logic hết hạn/gói trong service).

**Response 200** (`OrderDto`):

```json
{
  "message": "Tao order thanh cong",
  "data": {
    "id": 100,
    "guardianId": 5,
    "guardianName": "…",
    "studentId": 12,
    "studentName": "Nguyễn Văn An",
    "packageId": 1,
    "packageName": "Gói 30 ngày",
    "packagePrice": 300000,
    "durationDays": 30,
    "status": "PAID",
    "startDate": "2026-04-19T08:00:00Z",
    "endDate": "2026-05-19T08:00:00Z",
    "paidAt": "2026-04-19T08:00:00Z",
    "createdAt": "2026-04-19T08:00:00Z"
  }
}
```

**POST** `/api/Order/CreatePayOsLink` — khi không muốn trừ ví trực tiếp. Body (`OrderPayOsCreateDto`):

```json
{
  "guardianId": 5,
  "studentId": 12,
  "packageId": 1,
  "returnUrl": "https://app.example.com/payment/success",
  "cancelUrl": "https://app.example.com/payment/cancel"
}
```

**Response 200** — `data` là `OrderPayOsLinkDto` (FE mở `checkoutUrl` trong WebView / browser):

```json
{
  "message": "Tao link thanh toan payOS cho order thanh cong",
  "data": {
    "orderId": 101,
    "guardianId": 5,
    "studentId": 12,
    "packageId": 1,
    "packageName": "Gói 30 ngày",
    "orderCode": 1234567890123,
    "amount": 300000,
    "description": "Mua goi HS12",
    "checkoutUrl": "https://pay.payos.vn/web/...",
    "status": "PENDING",
    "createdAt": "2026-04-19T08:00:00Z"
  }
}
```

`description` trong BE được build từ `BuildDirectOrderDescription` (`Mua goi HS{studentId}`), không phải tên gói.

Sau khi PayOS xác nhận thanh toán, order chuyển trạng thái qua webhook (FE có thể poll `GET /api/Order/GetPayOsStatus/{orderCode}` nếu đã dùng trong app).

### 4.7 Xem lịch xe (theo campus học sinh)

**GET** `/api/BusSchedule/GetByCampus/{campusId}`

`campusId` = campus của học sinh. **Response 200** — `data` là **mảng** `BusScheduleDto`. Chọn một phần tử có `**id`** = `busScheduleId`; `**rideDate`** phải khớp `**dayOfWeek`** (quy ước **Thứ Hai = 0 … Chủ nhật = 6**, xem `ScheduleDayOfWeek`).

`startTime` / `endTime` là `TimeSpan`, JSON thường là chuỗi dạng `"HH:mm:ss"`.

```json
{
  "message": "Lấy danh sách bus schedule thành công",
  "data": [
    {
      "id": 100,
      "busId": 2,
      "busLabel": "Xe 01",
      "routeId": 3,
      "routeName": "Tuyến sáng A",
      "campusId": 1,
      "campusName": "Cơ sở A",
      "startDate": "2026-04-01T00:00:00Z",
      "endDate": null,
      "startTime": "07:00:00",
      "endTime": "08:30:00",
      "dayOfWeek": 0,
      "shiftType": "MORNING",
      "isActive": true
    }
  ]
}
```

### 4.8 Đăng ký đi xe (theo lịch)

**POST** `/api/StudentBusAssignment/CreateBySchedule`

```json
{
  "studentId": 12,
  "busScheduleId": 100,
  "rideDate": "2026-04-21T00:00:00Z",
  "pickupStationId": 10,
  "dropOffStationId": 12
}
```

- `**rideDate`:** chỉ **hôm nay hoặc tương lai**; phải nằm trong khoảng hiệu lực của `BusSchedule` và **đúng thứ** so với `DayOfWeek` của lịch.
- `**pickupStationId` / `dropOffStationId`:** phải là trạm **thuộc tuyến** của lịch, trạm **enabled**.
- Một học sinh **một ngày chỉ một** bản ghi assignment (trùng ngày sẽ lỗi).

**Response 200** (`StudentBusAssignmentDto`):

```json
{
  "message": "Thiết lập điểm đón trả cho học sinh thành công",
  "data": {
    "id": 50,
    "studentId": 12,
    "studentName": "Nguyễn Văn An",
    "guardianId": 5,
    "routeId": 3,
    "routeName": "Tuyến sáng A",
    "rideDate": "2026-04-21T00:00:00Z",
    "pickupStationId": 10,
    "pickupStationName": "Trạm 1",
    "dropOffStationId": 12,
    "dropOffStationName": "Trạm 3",
    "note": "Học sinh có gói còn hiệu lực"
  }
}
```

`note` do `BuildAssignmentNoteAsync` gắn theo order trong khoảng ngày — **không chặn** tạo assignment nếu chưa có gói (vẫn tạo được, nội dung note khác).

### 4.9 Xem lại lịch đã đăng ký (phụ huynh)

**GET** `/api/StudentBusAssignment/GetByGuardian/{guardianId}?rideDate=2026-04-21`

**Response 200** — `data` là **mảng** `StudentBusAssignmentDto` (cùng shape với mục 4.8):

```json
{
  "message": "Lấy danh sách student bus assignment thành công",
  "data": [
    {
      "id": 50,
      "studentId": 12,
      "studentName": "Nguyễn Văn An",
      "guardianId": 5,
      "routeId": 3,
      "routeName": "Tuyến sáng A",
      "rideDate": "2026-04-21T00:00:00Z",
      "pickupStationId": 10,
      "pickupStationName": "Trạm 1",
      "dropOffStationId": 12,
      "dropOffStationName": "Trạm 3",
      "note": "Học sinh có gói còn hiệu lực"
    }
  ]
}
```

---

## 5. Luồng thay thế: đăng ký không gửi `busScheduleId`

**POST** `/api/StudentBusAssignment/Create` với `StudentBusAssignmentCreateDto`: gửi `**routeId`**, `**rideDate`**, `**pickupStationId**`, `**dropOffStationId**`. Service kiểm tra **trên tuyến phải có ít nhất một `BusSchedule` active** khớp ngày trong tuần của `rideDate`. Không dùng `busScheduleId` trực tiếp.

**Body mẫu:**

```json
{
  "studentId": 12,
  "routeId": 3,
  "rideDate": "2026-04-21T00:00:00Z",
  "pickupStationId": 10,
  "dropOffStationId": 12
}
```

**Response 200** — cùng envelope; `data` là một `StudentBusAssignmentDto` (giống object trong mục 4.8).

---

## 6. File tham chiếu trong repo


| Thành phần         | File                                                                                           |
| ------------------ | ---------------------------------------------------------------------------------------------- |
| Student + guardian | `Controllers/StudentController .cs`, `Service/StudentService .cs`                              |
| FaceAI             | `Controllers/FaceAIController.cs`, `Service/FaceAIService.cs`, `Dto/FaceAI/`*                  |
| Order / ví         | `Service/OrderService.cs`, `Controllers/OrderController.cs`, `Controllers/WalletController.cs` |
| Đăng ký đi xe      | `Controllers/StudentBusAssignmentController.cs`, `Service/StudentBusAssignmentService.cs`      |
| Lịch (đọc)         | `Controllers/BusScheduleController.cs`, `Service/BusScheduleService.cs`                        |
| Quy ước thứ        | `Common/ScheduleDayOfWeek.cs`                                                                  |


---

*Tài liệu căn theo code BE hiện tại; thông điệp lỗi có thể không dấu trong một số service (`OrderService`).*