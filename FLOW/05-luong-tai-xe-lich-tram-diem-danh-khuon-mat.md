# Luồng tài xế: Đăng nhập → Lịch hôm nay → Trạm → Xác nhận đến trạm → Điểm danh khuôn mặt

Tài liệu mô tả API trong BE để **tài xế** xem **lịch chạy trong ngày**, **danh sách trạm**, **xác nhận xe đã đến từng trạm** (theo đúng thứ tự), và **quét khuôn mặt** để ghi **điểm danh tự động** qua `**FaceAIController`** (`/api/FaceAI/RecognizeCheckIn`, `/api/FaceAI/RecognizeCheckOut`). **Không** dùng `FaceRecognitionController` cho luồng này; **không** gọi trực tiếp `Attendance/ManualCheckIn` hay `ManualCheckOut` — chỉ luồng nhận diện + ghi nhận qua FaceAI.

Envelope: `{ "message": "...", "data": ... }` (`ResponseDto`), JSON **camelCase**.

**Điều kiện nền (admin đã cấu hình):** tài xế có `**BusAssignment`** (gắn `driverId` với xe); trên xe có `**BusSchedule`** trùng **ngày / thứ** (`ScheduleDayOfWeek`: Thứ Hai = 0 … Chủ nhật = 6). Xem thêm [03-luong-chuan-bi-va-tao-lich-xe-bus.md](./03-luong-chuan-bi-va-tao-lich-xe-bus.md).

---

## 1. Bảng API theo giai đoạn


| Giai đoạn                    | Method | Endpoint                               | Mục đích                                                                                        |
| ---------------------------- | ------ | -------------------------------------- | ----------------------------------------------------------------------------------------------- |
| Đăng nhập                    | POST   | `/api/Account/Login`                   | Lấy JWT                                                                                         |
| Xác nhận role                | GET    | `/api/Account/Me`                      | `data.id` = user id; `**roleName` = `driver`**                                                  |
| Lịch trong ngày              | GET    | `/api/BusTripProgress/DriverSchedules` | Lịch của tài xế + **trạm** + đã qua trạm chưa (`isVisited`)                                     |
| Trạng thái chuyến (tuỳ chọn) | GET    | `/api/BusTripProgress/Current`         | Trạm tiếp theo, trạng thái chuyến, danh sách trạm                                               |
| Xác nhận đến trạm            | POST   | `/api/BusTripProgress/Arrive`          | Ghi nhận xe đã tới trạm (**phải đúng trạm kế tiếp** theo thứ tự tuyến)                          |
| Điểm danh tự động (đón)      | POST   | `/api/FaceAI/RecognizeCheckIn`         | `multipart`: ảnh + `busId` + `stationId` → nhận diện FaceAI + ghi nhận điểm danh (**check-in**) |
| Điểm danh tự động (trả)      | POST   | `/api/FaceAI/RecognizeCheckOut`        | Tương tự → **check-out**                                                                        |


**Lưu ý:**

- `DriverSchedules` nhận `**driverId`** = `**User.Id`** của tài xế (cùng id với `BusAssignment.DriverId`).
- App tài xế **chỉ** gọi `**FaceAI`** cho điểm danh có ảnh; **không** gọi `/api/FaceRecognition/`* (trùng chức năng cũ — bỏ cho luồng tài xế).
- **Không** gọi `POST /api/Attendance/ManualCheckIn` / `ManualCheckOut` — điểm danh chỉ qua **RecognizeCheckIn / RecognizeCheckOut** (nhận diện tự động).
- Học sinh cần đã có **khuôn mặt trên FaceAI** (ví dụ phụ huynh đã `AddStudentFace` — xem [04-luong-guardian-hoc-sinh-faceai-dang-ky-lich-xe.md](./04-luong-guardian-hoc-sinh-faceai-dang-ky-lich-xe.md)).
- Nhiều controller **chưa** gắn `[Authorize]` — FE vẫn nên gửi **Bearer** và chỉ mở app tài xế khi `roleName === "driver"`.

---

## 2. Luồng tổng quát

```text
POST /api/Account/Login
        │
        ▼
GET /api/Account/Me  →  driverId = data.id  (roleName = driver)
        │
        ▼
GET /api/BusTripProgress/DriverSchedules?driverId={id}&rideDate=2026-04-19
        │
        ▼
Chọn (hoặc auto) một ca / lịch  →  có busId, busScheduleId, danh sách stations[]
        │
        ├─ (tuỳ chọn) GET /api/BusTripProgress/Current?busId=&busScheduleId=&rideDate=
        │       → nextStationId, tripStatus
        │
        ▼
Với mỗi trạm khi xe đến:
        │
        ├─ POST /api/BusTripProgress/Arrive  { busId, busScheduleId, stationId }
        │
        └─ POST /api/FaceAI/RecognizeCheckIn (hoặc RecognizeCheckOut)
               multipart: file, busId, stationId [, date, time]
```

---

## 3. Chi tiết & ràng buộc

### 3.1 Lịch trong ngày — `DriverSchedules`

**GET** `/api/BusTripProgress/DriverSchedules`


| Query      | Bắt buộc | Ghi chú                                                                                 |
| ---------- | -------- | --------------------------------------------------------------------------------------- |
| `driverId` | Có       | Id user tài xế                                                                          |
| `rideDate` | Không    | Mặc định **UTC date** trong service nếu bỏ trống (`DateTime.UtcNow`)                    |
| `atTime`   | Không    | Khung giờ “hiện tại” để đánh dấu `isRunningNow` / gợi ý lịch; mặc định giờ UTC hiện tại |


**Lỗi thường gặp:** *"Tài xế chưa được phân công xe"* — chưa có `BusAssignment`.  
**Lỗi:** *"Tài xế không có lịch chạy nào trong ngày đã chọn"* — không có `BusSchedule` nào khớp xe + **thứ trong tuần** + khoảng ngày hiệu lực.

**Response `data`:** mảng `BusTripProgressDriverScheduleDto` — mỗi phần tử có `busScheduleId`, `busId`, `routeName`, `startTime`, `endTime`, `shiftType`, cờ `isRunningNow` / `isUpcoming` / `isCompleted`, `isRecommended`, và `**stations[]`** (`stationId`, `stationName`, `orderIndex`, `isVisited`, `arrivedAt`).

### 3.2 Trạng thái chuyến — `Current`

**GET** `/api/BusTripProgress/Current?busId={busId}&busScheduleId={id}&rideDate=2026-04-19`

Trả về `**nextStationId` / `nextStationName`**, `**tripStatus`** (`NOT_STARTED` | `AT_STATION` | `COMPLETED`), danh sách trạm — hợp để refresh sau mỗi lần `Arrive`.

### 3.3 Xác nhận đến trạm — `Arrive`

**POST** `/api/BusTripProgress/Arrive`

```json
{
  "busId": 5,
  "busScheduleId": 100,
  "stationId": 10,
  "arrivedAt": null
}
```

- `arrivedAt` có thể **null** — service dùng thời điểm hiện tại.
- **Bắt buộc đến đúng trạm kế tiếp** theo `OrderIndex` trên tuyến; bỏ qua hoặc sai thứ tự → lỗi (*"Xe phải xác nhận đến trạm '…' trước"*).

### 3.4 Điểm danh tự động — `FaceAI/RecognizeCheckIn` / `RecognizeCheckOut`

**POST** `/api/FaceAI/RecognizeCheckIn` hoặc `**/api/FaceAI/RecognizeCheckOut`**  
`Content-Type: multipart/form-data`


| Part (tên field) | Nội dung                                                          |
| ---------------- | ----------------------------------------------------------------- |
| `file`           | Ảnh chụp khuôn mặt học sinh                                       |
| `busId`          | Trùng xe đang chạy                                                |
| `stationId`      | Trạm đang điểm danh (nên trùng trạm hiện tại / trạm vừa `Arrive`) |
| `date`           | Tuỳ chọn — ngày điểm danh                                         |
| `time`           | Tuỳ chọn — giờ (TimeSpan)                                         |


Luồng `**FaceAIService`**: gọi API nhận diện FaceAI (`/verify` …) → khớp `studentId` → **ghi nhận điểm danh** (backend map sang bảng attendance; **FE không** gọi `Attendance/Manual`*).

**Gợi ý nghiệp vụ:** **RecognizeCheckIn** khi học sinh **lên xe** / trạm đón, **RecognizeCheckOut** khi **xuống xe** / trạm trả.

**Điều kiện:** học sinh đã có **face mẫu trên FaceAI** (đăng ký qua phụ huynh `FaceAI/AddStudentFace` hoặc quy trình tương đương). Không khớp ngưỡng → lỗi nhận diện.

---

## 4. JSON / form mẫu

### 4.1 Đăng nhập & Me (tài xế)

**POST** `/api/Account/Login` — giống user khác.

**GET** `/api/Account/Me`

```json
{
  "message": "Lấy thông tin tài khoản thành công.",
  "data": {
    "id": 8,
    "email": "driver01@schoolbus.local",
    "fullName": "Lái Xe 01",
    "roleName": "driver",
    "status": "ACTIVE"
  }
}
```

Dùng `**data.id**` làm `**driverId**`.

### 4.2 DriverSchedules — ví dụ rút gọn `data`

```json
{
  "message": "Lấy danh sách lịch chạy của tài xế thành công",
  "data": [
    {
      "busScheduleId": 100,
      "busId": 5,
      "busLabel": "BUS-01",
      "routeId": 3,
      "routeName": "Tuyến sáng A",
      "rideDate": "2026-04-19T00:00:00Z",
      "startTime": "06:00:00",
      "endTime": "08:30:00",
      "shiftType": "PICKUP",
      "isRunningNow": true,
      "isUpcoming": false,
      "isCompleted": false,
      "isRecommended": true,
      "stations": [
        {
          "stationId": 10,
          "stationName": "Trạm 1",
          "orderIndex": 1,
          "isVisited": true,
          "arrivedAt": "2026-04-19T06:05:00Z"
        },
        {
          "stationId": 11,
          "stationName": "Trạm 2",
          "orderIndex": 2,
          "isVisited": false,
          "arrivedAt": null
        }
      ]
    }
  ]
}
```

### 4.3 Current — ví dụ `data`

```json
{
  "message": "Lấy trạng thái chuyến xe thành công",
  "data": {
    "busId": 5,
    "busScheduleId": 100,
    "routeId": 3,
    "routeName": "Tuyến sáng A",
    "rideDate": "2026-04-19T00:00:00Z",
    "tripStatus": "AT_STATION",
    "currentStationId": 10,
    "currentStationName": "Trạm 1",
    "nextStationId": 11,
    "nextStationName": "Trạm 2",
    "nextOrderIndex": 2,
    "isCompleted": false,
    "stations": []
  }
}
```

*(Cấu trúc đầy đủ theo `BusTripProgressCurrentDto` + `Stations`.)*

### 4.4 Arrive

```json
{
  "busId": 5,
  "busScheduleId": 100,
  "stationId": 11,
  "arrivedAt": null
}
```

### 4.5 RecognizeCheckIn / RecognizeCheckOut — `FaceAI` (multipart)

**POST** `/api/FaceAI/RecognizeCheckIn` hoặc `/api/FaceAI/RecognizeCheckOut`  
Gửi form-data: `file` = ảnh; `busId`, `stationId` = số; `date` / `time` tuỳ chọn.

**Response `data`:** `FaceRecognitionAttendanceResultDto` — kết quả nhận diện + bản ghi điểm danh (`attendance`).

---

## 5. Sơ đồ quyết định (tóm tắt)

```text
Tài xế mở app
    │
    ▼
Login → Me (driver)
    │
    ▼
DriverSchedules( driverId, rideDate = hôm nay )
    │
    ├─ Lỗi / rỗng → Không có lịch hoặc chưa phân công xe
    │
    └─ Có lịch → Hiển thị từng ca + stations (isVisited)
              │
              ▼
         Chọn ca đang chạy (isRunningNow hoặc isRecommended)
              │
              ▼
         Tại trạm: Arrive(stationId đúng thứ tự)
              │
              ▼
         Quét mặt: POST /api/FaceAI/RecognizeCheckIn hoặc RecognizeCheckOut
              │
              ▼
         Lặp đến hết trạm / hoặc Current → isCompleted
```

---

## 6. File tham chiếu trong repo


| Thành phần                             | File                                                                                                      |
| -------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| Lịch + đến trạm                        | `Controllers/BusTripProgressController.cs`, `Service/BusTripProgressService.cs`                           |
| Điểm danh qua FaceAI (app tài xế dùng) | `Controllers/FaceAIController.cs` — `RecognizeCheckIn`, `RecognizeCheckOut`; `Service/FaceAIService.cs`   |
| DTO form điểm danh                     | `Dto/FaceRecognition/FaceRecognitionAttendanceFormDto.cs` (dùng chung tên DTO, endpoint thuộc **FaceAI**) |
| Thứ trong tuần                         | `Common/ScheduleDayOfWeek.cs`                                                                             |


---

*Tài liệu căn theo code BE hiện tại; đồng bộ múi giờ `rideDate` / `atTime` giữa FE và server (UTC vs local) nên thống nhất khi test.*