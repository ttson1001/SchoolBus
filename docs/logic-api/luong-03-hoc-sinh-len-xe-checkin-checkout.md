# Logic tích hợp API – Luồng 3: Học sinh lên xe và tiến hành check in / check out (FE)

Tài liệu mô tả cách FE gọi API cho luồng **set điểm đón/trả theo ngày**, sau đó **check in khi học sinh lên xe** và **check out khi học sinh xuống xe**.

Luồng này hiện có 2 góc nhìn FE:
- FE/phụ huynh: xem hoặc set điểm đón/trả theo ngày cho con
- FE/tài xế hoặc giám thị: thực hiện check in / check out thực tế trên xe

---

## 1. API dùng trong luồng

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| GET | `/api/StudentBusAssignment/GetByGuardian/{guardianId}?rideDate=2026-03-27` | Phụ huynh xem cấu hình điểm đón/trả của con theo ngày |
| POST | `/api/StudentBusAssignment/Create` | Set điểm đón/trả theo ngày cho học sinh |
| PUT | `/api/StudentBusAssignment/Update/{id}` | Sửa điểm đón/trả theo ngày |
| GET | `/api/StudentBusAssignment/GetByStudent/{studentId}?rideDate=2026-03-27` | FE trên xe lấy assignment của học sinh trong ngày |
| POST | `/api/Attendance/ManualCheckIn` | Check in khi học sinh lên xe |
| POST | `/api/Attendance/ManualCheckOut` | Check out khi học sinh xuống xe |

**Lưu ý:**
- `StudentBusAssignment` hiện được dùng để lưu:
  - `studentId`
  - `busId`
  - `routeId`
  - `rideDate`
  - `pickupStationId`
  - `dropOffStationId`
- Backend hiện có side effect:
  - Khi check in: tự tạo notification DB gửi guardian
  - Khi check out: tự tạo notification DB gửi guardian
  - Nếu xuống sai điểm so với `dropOffStationId`: tạo thêm notification cảnh báo `WRONG_DROPOFF`
  - Nếu guardian đã có `deviceToken`, backend sẽ bắn thêm Firebase push về điện thoại
- Nếu `deviceToken` lỗi, hết hạn, hoặc Firebase gặp lỗi, luồng check in/check out **vẫn trả 200** nếu nghiệp vụ attendance hợp lệ

---

## 2. Luồng tổng quát

```text
[Phụ huynh chọn điểm đón/trả theo ngày]
      |
      v
POST /api/StudentBusAssignment/Create
hoặc
PUT /api/StudentBusAssignment/Update/{id}
      |
      v
[Đến ngày xe chạy]
      |
      v
FE trên xe lấy assignment của học sinh
GET /api/StudentBusAssignment/GetByStudent/{studentId}?rideDate=...
      |
      v
Học sinh lên xe
      |
      v
POST /api/Attendance/ManualCheckIn
      |
      v
Guardian nhận notification / push "đã lên xe"
      |
      v
Học sinh xuống xe
      |
      v
POST /api/Attendance/ManualCheckOut
      |
      v
Guardian nhận notification / push "đã xuống xe"
      |
      '- Nếu xuống sai điểm
           -> Guardian nhận thêm cảnh báo sai điểm xuống
```

---

## 3. Chi tiết API

### 3.1 Set điểm đón/trả theo ngày

- **Request**
  - **POST** `/api/StudentBusAssignment/Create`
  - Body:
    ```json
    {
      "studentId": 2,
      "busId": 1,
      "routeId": 1,
      "rideDate": "2026-03-27",
      "pickupStationId": 1,
      "dropOffStationId": 3
    }
    ```

- **Response 200**
  ```json
  {
    "message": "Thiết lập điểm đón trả cho học sinh thành công",
    "data": {
      "id": 10,
      "studentId": 2,
      "studentName": "Tran Gia Bao",
      "guardianId": 5,
      "busId": 1,
      "busLicensePlate": "51A-12345",
      "routeId": 1,
      "routeName": "Tuyen Campus Quan 1 - Sang",
      "rideDate": "2026-03-27T00:00:00",
      "pickupStationId": 1,
      "pickupStationName": "Tram Don Quan 1",
      "dropOffStationId": 3,
      "dropOffStationName": "Tram Don Quan 3"
    }
  }
  ```

---

### 3.2 Lấy assignment theo student trong ngày

- **Request**
  - **GET** `/api/StudentBusAssignment/GetByStudent/2?rideDate=2026-03-27`

- **Mục đích**
  - FE trên xe dùng để biết hôm nay học sinh này:
    - đi xe nào
    - tuyến nào
    - dự kiến lên ở trạm nào
    - dự kiến xuống ở trạm nào

---

### 3.3 Check in khi học sinh lên xe

- **Request**
  - **POST** `/api/Attendance/ManualCheckIn`
  - Body:
    ```json
    {
      "studentId": 2,
      "busId": 1,
      "stationId": 1,
      "date": "2026-03-27",
      "time": "07:10:00"
    }
    ```

- **Response 200**
  - Trả về `AttendanceDto` sau khi ghi nhận check in.

- **Side effect**
  - Backend tự tạo notification DB gửi guardian với nội dung học sinh đã lên xe.
  - Nếu guardian có `deviceToken`, backend bắn thêm Firebase push về điện thoại.
  - Nếu push lỗi, API vẫn thành công nếu attendance được lưu thành công.

---

### 3.4 Check out khi học sinh xuống xe

- **Request**
  - **POST** `/api/Attendance/ManualCheckOut`
  - Body:
    ```json
    {
      "studentId": 2,
      "busId": 1,
      "stationId": 3,
      "date": "2026-03-27",
      "time": "16:45:00"
    }
    ```

- **Response 200**
  - Trả về `AttendanceDto` sau khi ghi nhận check out.

- **Side effect**
  - Backend tự tạo notification DB gửi guardian với nội dung học sinh đã xuống xe.
  - Nếu `stationId` thực tế khác `dropOffStationId` trong assignment, backend tạo thêm notification cảnh báo sai điểm xuống.
  - Nếu guardian có `deviceToken`, backend bắn thêm Firebase push tương ứng về điện thoại.
  - Nếu push lỗi, API vẫn thành công nếu attendance được lưu thành công.

---

## 4. Logic FE gợi ý

### 4.1 Phía phụ huynh

1. Gọi **GET** `/api/StudentBusAssignment/GetByGuardian/{guardianId}?rideDate=...` để xem con hôm nay đón/trả ở đâu.
2. Nếu chưa có assignment cho ngày đó:
   - Hiển thị nút "Chọn điểm đón/trả".
   - Gửi **POST** `/api/StudentBusAssignment/Create`.
3. Nếu đã có assignment:
   - Có thể cho sửa bằng **PUT** `/api/StudentBusAssignment/Update/{id}`.

### 4.2 Phía tài xế / giám thị

1. Trước khi check in, gọi **GET** `/api/StudentBusAssignment/GetByStudent/{studentId}?rideDate=...`.
2. Dùng dữ liệu assignment để xác nhận đúng bus/route/trạm.
3. Khi học sinh lên xe:
   - Gửi **POST** `/api/Attendance/ManualCheckIn`.
4. Khi học sinh xuống xe:
   - Gửi **POST** `/api/Attendance/ManualCheckOut`.

### 4.3 Xử lý UI sau khi check in / check out

1. Nếu **200**:
   - Cập nhật ngay trạng thái trên UI.
   - Check in thành công -> đổi trạng thái thành "Đã lên xe".
   - Check out thành công -> đổi trạng thái thành "Đã xuống xe".
2. Nếu **400**:
   - Hiển thị lỗi backend.
   - Các lỗi thường gặp:
     - chưa có assignment phù hợp
     - station không thuộc route
     - check out trước check in
     - check in trùng
     - check out trùng

---

## 5. Sơ đồ quyết định (tóm tắt)

```text
Guardian set assignment theo ngày
    |
    v
POST /api/StudentBusAssignment/Create
    |
    v
Đến ngày chạy xe
    |
    v
GET /api/StudentBusAssignment/GetByStudent/{studentId}?rideDate=...
    |
    |- Không có assignment
    |   -> Không cho check in/check out
    |
    '- Có assignment
         |
         v
    POST /api/Attendance/ManualCheckIn
         |
         |- 200 -> Lưu attendance
         |       -> Tạo notification DB
         |       -> Nếu có deviceToken -> bắn push
         '- 400 -> Hiển thị lỗi
         |
         v
    POST /api/Attendance/ManualCheckOut
         |
         |- 200 -> Lưu attendance
         |       -> Tạo notification DB
         |       -> Nếu có deviceToken -> bắn push
         |       -> Nếu sai trạm -> thêm cảnh báo
         '- 400 -> Hiển thị lỗi
```

---

## 6. Lưu ý

- `stationId` gửi trong check in/check out là **trạm thực tế** lúc thao tác, không phải trạm dự kiến.
- Backend dùng `rideDate` để tìm assignment theo ngày; FE nên truyền ngày rõ ràng thay vì phụ thuộc hoàn toàn vào giờ hệ thống client.
- Notification hiện vừa được lưu trong DB, vừa có thể được bắn ra điện thoại qua Firebase nếu guardian đã login mobile với `deviceToken`.
- Nếu token Firebase bị lỗi hoặc không còn hợp lệ, backend chỉ log lỗi push; không làm fail API attendance.
- Nếu muốn FE phụ huynh hiển thị đúng trạng thái chuyến đi trong ngày, nên gọi song song:
  - assignment của ngày
  - attendance của ngày
- Nếu học sinh xuống sai điểm, FE tài xế vẫn sẽ nhận 200 nếu check out hợp lệ; cảnh báo sai điểm là side effect gửi về guardian.
