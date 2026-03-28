# Logic tích hợp API – Luồng 4: Master data (FE)

Tài liệu mô tả cách FE gọi API cho luồng **quản lý master data** của hệ thống SchoolBus.

Trong phạm vi backend hiện có, nhóm master data chính gồm:
- Role
- Campus
- Bus Station
- Bus Route
- Bus
- Bus Schedule
- Package

---

## 1. API dùng trong luồng

### 1.1 Role

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| GET | `/api/Role/Search` | Tìm kiếm danh sách role |
| GET | `/api/Role/Get/{id}` | Lấy role theo id |
| POST | `/api/Role/Create` | Tạo role |
| PUT | `/api/Role/Update/{id}` | Cập nhật role |
| DELETE | `/api/Role/Delete/{id}` | Xóa role |

### 1.2 Campus

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| GET | `/api/Campus/Search` | Tìm kiếm danh sách campus |
| GET | `/api/Campus/Get/{id}` | Lấy campus theo id |
| POST | `/api/Campus/Create` | Tạo campus |
| PUT | `/api/Campus/Update/{id}` | Cập nhật campus |
| DELETE | `/api/Campus/Delete/{id}` | Xóa campus |

### 1.3 Bus Station

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| GET | `/api/BusStation/Search` | Tìm kiếm bus station |
| GET | `/api/BusStation/Get/{id}` | Lấy bus station theo id |
| POST | `/api/BusStation/Create` | Tạo bus station |
| PUT | `/api/BusStation/Update/{id}` | Cập nhật bus station |
| DELETE | `/api/BusStation/Delete/{id}` | Xóa bus station |

### 1.4 Bus Route

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| GET | `/api/BusRoute/Search` | Tìm kiếm bus route |
| GET | `/api/BusRoute/Get/{id}` | Lấy bus route theo id |
| POST | `/api/BusRoute/Create` | Tạo bus route |
| PUT | `/api/BusRoute/Update/{id}` | Cập nhật bus route |
| DELETE | `/api/BusRoute/Delete/{id}` | Xóa bus route |

### 1.5 Bus

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| GET | `/api/Bus/Search` | Tìm kiếm xe bus |
| GET | `/api/Bus/Get/{id}` | Lấy xe bus theo id |
| GET | `/api/Bus/GetByCampus/{campusId}` | Lấy danh sách bus theo campus |
| POST | `/api/Bus/Create` | Tạo bus |
| PUT | `/api/Bus/Update/{id}` | Cập nhật bus |
| DELETE | `/api/Bus/Delete/{id}` | Xóa bus |

### 1.6 Bus Schedule

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| POST | `/api/BusSchedule/Create` | Tạo lịch chạy xe |
| GET | `/api/BusSchedule/Get/{id}` | Lấy lịch theo id |
| GET | `/api/BusSchedule/GetByBus/{busId}` | Lấy lịch theo bus |
| GET | `/api/BusSchedule/GetByRoute/{routeId}` | Lấy lịch theo route |
| GET | `/api/BusSchedule/GetByCampus/{campusId}` | Lấy lịch theo campus |
| GET | `/api/BusSchedule/GetAtTime?atTime=...&campusId=...` | Lấy lịch đang chạy tại thời điểm |
| PUT | `/api/BusSchedule/Update/{id}` | Cập nhật lịch |
| DELETE | `/api/BusSchedule/Delete/{id}` | Xóa lịch |

### 1.7 Package

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| GET | `/api/Package/Search` | Tìm kiếm package |
| GET | `/api/Package/Get/{id}` | Lấy package theo id |
| POST | `/api/Package/Create` | Tạo package |
| PUT | `/api/Package/Update/{id}` | Cập nhật package |
| DELETE | `/api/Package/Delete/{id}` | Xóa package |

**Lưu ý chung:**
- Controller hiện chưa gắn `[Authorize]` trực tiếp, nhưng về nghiệp vụ FE nên coi toàn bộ luồng master data là **chỉ dành cho admin / staff vận hành**.
- Hầu hết response đều theo wrapper:
  ```json
  {
    "message": "...",
    "data": ...
  }
  ```

---

## 2. Luồng tổng quát

```text
[Admin đăng nhập]
      │
      ▼
[Vào màn Master Data]
      │
      ▼
Chọn module cần quản lý
(Role / Campus / Bus Station / Bus Route / Bus / Bus Schedule / Package)
      │
      ▼
GET Search
      │
      ▼
Render danh sách + filter + paging
      │
      ├─ Tạo mới  → POST Create
      ├─ Xem chi tiết → GET Get/{id}
      ├─ Cập nhật → PUT Update/{id}
      └─ Xóa → DELETE Delete/{id}
      │
      ▼
Refetch danh sách hoặc cập nhật UI cục bộ
```

---

## 3. Chi tiết API theo module

### 3.1 Role

- **Request tạo mới**
  - **POST** `/api/Role/Create`
  - Body:
    ```json
    {
      "name": "guardian"
    }
    ```

- **Response search**
  ```json
  {
    "message": "Lấy danh sách role thành công",
    "data": {
      "items": [
        {
          "id": 1,
          "name": "guardian"
        }
      ],
      "totalItems": 1,
      "page": 1,
      "pageSize": 10
    }
  }
  ```

---

### 3.2 Campus

- **Body tạo mới**
  ```json
  {
    "code": "CS001",
    "name": "Campus Quan 1",
    "address": "123 Nguyen Hue",
    "phone": "0909000001",
    "isActive": true,
    "imageUrl": null
  }
  ```

- **Body cập nhật**
  ```json
  {
    "name": "Campus Quan 1 Updated",
    "phone": "0909009999",
    "isActive": true
  }
  ```

- **Response item**
  ```json
  {
    "id": 1,
    "code": "CS001",
    "name": "Campus Quan 1",
    "address": "123 Nguyen Hue",
    "phone": "0909000001",
    "isActive": true,
    "imageUrl": null
  }
  ```

---

### 3.3 Bus Station

- **Body tạo mới**
  ```json
  {
    "name": "Tram Don Quan 1",
    "address": "123 Nguyen Hue, Quan 1",
    "description": "Tram don hoc sinh buoi sang",
    "latitude": 10.7769,
    "longitude": 106.7009,
    "isEnabled": true
  }
  ```

- **Response item**
  ```json
  {
    "id": 1,
    "name": "Tram Don Quan 1",
    "address": "123 Nguyen Hue, Quan 1",
    "description": "Tram don hoc sinh buoi sang",
    "latitude": 10.7769,
    "longitude": 106.7009,
    "isEnabled": true
  }
  ```

---

### 3.4 Bus Route

- **Body tạo mới**
  ```json
  {
    "name": "Tuyen Campus Quan 1 - Sang",
    "campusId": 1,
    "stationIds": [1, 2, 3]
  }
  ```

- **Body cập nhật**
  ```json
  {
    "name": "Tuyen Campus Quan 1 - Cap nhat",
    "isEnabled": true,
    "campusId": 1,
    "stationIds": [1, 3, 4]
  }
  ```

- **Response item**
  ```json
  {
    "id": 1,
    "name": "Tuyen Campus Quan 1 - Sang",
    "isEnabled": true,
    "campusId": 1,
    "campusName": "Campus Quan 1",
    "stations": [
      {
        "stationId": 1,
        "stationName": "Tram Don Quan 1",
        "orderIndex": 1
      }
    ]
  }
  ```

- **Lưu ý nghiệp vụ**
  - Backend chặn nếu `stationIds` chứa bus station đang `isEnabled = false`.

---

### 3.5 Bus

- **Body tạo mới**
  ```json
  {
    "licensePlate": "51A-12345",
    "capacity": 45,
    "status": "ACTIVE",
    "busNumber": "BUS-01",
    "imageUrl": null,
    "color": "Yellow",
    "busType": "STANDARD"
  }
  ```

- **Response item**
  ```json
  {
    "id": 1,
    "licensePlate": "51A-12345",
    "capacity": 45,
    "status": "ACTIVE",
    "busNumber": "BUS-01",
    "imageUrl": null,
    "color": "Yellow",
    "busType": "STANDARD"
  }
  ```

- **Lưu ý nghiệp vụ**
  - `GetByCampus` hiện lấy bus theo quan hệ assignment/route/campus, không phải `Bus` có `CampusId` trực tiếp.

---

### 3.6 Bus Schedule

- **Body tạo mới**
  ```json
  {
    "busId": 1,
    "routeId": 1,
    "startDate": "2026-03-23",
    "endDate": "2026-12-31",
    "startTime": "07:00:00",
    "endTime": "08:00:00",
    "dayOfWeek": 1,
    "shiftType": "PICKUP"
  }
  ```

- **Response item**
  ```json
  {
    "id": 2,
    "busId": 1,
    "busLabel": "BUS-01",
    "routeId": 1,
    "routeName": "Tuyen Campus Quan 1 - Sang",
    "campusId": 1,
    "campusName": "Campus Quan 1",
    "startDate": "2026-03-23T00:00:00",
    "endDate": "2026-12-31T00:00:00",
    "startTime": "07:00:00",
    "endTime": "08:00:00",
    "dayOfWeek": 1,
    "shiftType": "PICKUP",
    "isActive": true
  }
  ```

- **Lưu ý nghiệp vụ**
  - `dayOfWeek` theo .NET:
    - `0 = Sunday`
    - `1 = Monday`
    - `...`
    - `6 = Saturday`
  - `shiftType` hiện dùng:
    - `PICKUP`
    - `DROPOFF`
    - `ROUNDTRIP`
  - Backend chặn overlap lịch cho cùng bus.
  - Backend chỉ cho tạo/cập nhật khi:
    - bus active
    - route enabled
    - campus của route đang active

---

### 3.7 Package

- **Body tạo mới**
  ```json
  {
    "name": "Goi 1 thang",
    "price": 500000,
    "durationDays": 30,
    "description": "Goi xe bus 30 ngay",
    "status": "ACTIVE",
    "type": "MONTHLY",
    "imageUrl": null
  }
  ```

- **Response item**
  ```json
  {
    "id": 1,
    "name": "Goi 1 thang",
    "price": 500000,
    "durationDays": 30,
    "description": "Goi xe bus 30 ngay",
    "status": "ACTIVE",
    "createdAt": "2026-03-27T08:00:00",
    "type": "MONTHLY",
    "imageUrl": null
  }
  ```

---

## 4. Logic FE gợi ý

### 4.1 Màn danh sách master data

1. Admin chọn module.
2. FE gọi API `Search`.
3. Render:
   - danh sách
   - ô tìm kiếm
   - phân trang
   - nút thêm mới
   - nút sửa
   - nút xóa

### 4.2 Khi bấm thêm mới

1. Mở modal hoặc màn form.
2. FE validate những field cơ bản.
3. Gửi `POST /Create`.
4. Nếu **200**:
   - đóng form
   - refetch `Search`
5. Nếu **400**:
   - hiển thị `message` backend

### 4.3 Khi bấm sửa

1. Có thể gọi `GET /Get/{id}` để load chi tiết.
2. Bind dữ liệu lên form.
3. Gửi `PUT /Update/{id}`.
4. Nếu **200**:
   - update row cục bộ hoặc refetch list

### 4.4 Khi bấm xóa

1. Hiển thị confirm dialog.
2. Gửi `DELETE /Delete/{id}`.
3. Nếu **200**:
   - xóa item khỏi list hoặc refetch list

---

## 5. Sơ đồ quyết định (tóm tắt)

```text
Admin vào màn Master Data
    │
    ▼
Chọn module
    │
    ▼
GET Search
    │
    ├─ data.items rỗng
    │    → Hiển thị empty state
    │
    └─ data.items có dữ liệu
         → Render bảng / card / paging
         │
         ├─ Thêm mới → POST Create
         ├─ Xem chi tiết → GET Get/{id}
         ├─ Cập nhật → PUT Update/{id}
         └─ Xóa → DELETE Delete/{id}
                │
                ├─ 200 → Cập nhật lại UI
                └─ 400 → Hiển thị lỗi
```

---

## 6. Lưu ý

- FE nên chuẩn hóa một pattern chung cho mọi màn master data:
  - `Search` để load list
  - `Get` để load chi tiết
  - `Create`
  - `Update`
  - `Delete`
- Các API search thường trả phân trang trong `data.items`, `data.totalItems`, `data.page`, `data.pageSize`.
- Với `BusRoute`, `Bus`, `BusSchedule`, `Package`, nên FE hiển thị rõ trạng thái active/enabled để tránh nhập sai dữ liệu vận hành.
- Với `BusSchedule`, nên đổi `dayOfWeek` sang label dễ hiểu trên UI:
  - `0` → Chủ nhật
  - `1` → Thứ hai
  - `...`
  - `6` → Thứ bảy
- Vì controller hiện chưa gắn `[Authorize]`, FE vẫn nên xem luồng này là luồng nội bộ dành cho admin/staff và kiểm soát quyền ở tầng đăng nhập/route guard.
