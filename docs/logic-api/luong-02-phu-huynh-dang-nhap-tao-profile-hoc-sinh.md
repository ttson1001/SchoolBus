# Logic tích hợp API – Luồng 2: Phụ huynh đăng nhập lần đầu và tạo profile học sinh (FE)

Tài liệu mô tả cách FE gọi API cho luồng **phụ huynh đăng nhập lần đầu**, lấy thông tin tài khoản hiện tại, kiểm tra danh sách học sinh đang có, rồi **tạo profile học sinh**.

---

## 1. API dùng trong luồng

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| POST | `/api/Account/Login` | Phụ huynh đăng nhập lấy token |
| GET | `/api/Account/Me` | Lấy thông tin tài khoản hiện tại từ token |
| GET | `/api/Student/GetMyStudents` | Lấy danh sách học sinh của guardian hiện tại từ token |
| GET | `/api/Campus/Search?page=1&pageSize=50` | Lấy danh sách campus để phụ huynh chọn trường/cơ sở |
| POST | `/api/Student/Create` | Tạo profile học sinh |
| PUT | `/api/Student/Update/{id}` | Cập nhật profile học sinh khi cần |

**Lưu ý:**
- FE không cần tự truyền `guardianId` để lấy danh sách học sinh nữa.
- `GET /api/Account/Me` và `GET /api/Student/GetMyStudents` đều đọc trực tiếp từ JWT token.
- `guardianId` vẫn cần trong body `Create student`, nhưng FE có thể lấy từ `Account/Me`.
- `POST /api/Account/Login` hiện hỗ trợ thêm `deviceToken` dạng optional.
- Mobile nên truyền `deviceToken` để backend lưu Firebase token nhận push notification.
- Web có thể bỏ qua `deviceToken`.

---

## 2. Luồng tổng quát

```text
[Phụ huynh đăng nhập]
      |
      v
POST /api/Account/Login
      |
      v
GET /api/Account/Me
      |
      v
GET /api/Student/GetMyStudents
      |
      |- Có học sinh
      |   -> Hiển thị danh sách profile đã có
      |
      '- Chưa có học sinh
          -> Hiển thị empty state + nút "Tạo hồ sơ học sinh"
                   |
                   v
          GET /api/Campus/Search
                   |
                   v
          Phụ huynh nhập thông tin học sinh
                   |
                   v
          POST /api/Student/Create
                   |
                   v
          GET /api/Student/GetMyStudents
                   |
                   v
          Hiển thị profile học sinh vừa tạo
```

---

## 3. Chi tiết API

### 3.1 Phụ huynh đăng nhập

- **Request**
  - **POST** `/api/Account/Login`
  - Body dùng cho web:
    ```json
    {
      "email": "guardian01@schoolbus.local",
      "password": "123456"
    }
    ```
  - Body dùng cho mobile:
    ```json
    {
      "email": "guardian01@schoolbus.local",
      "password": "123456",
      "deviceToken": "YOUR_FIREBASE_DEVICE_TOKEN"
    }
    ```

- **Response 200**
  ```json
  {
    "message": "Đăng nhập thành công.",
    "data": {
      "token": "jwt_token_here"
    }
  }
  ```

---

### 3.2 Lấy thông tin tài khoản hiện tại

- **Request**
  - **GET** `/api/Account/Me`
  - Header:
    - `Authorization: Bearer <guardian_token>`

- **Response 200**
  ```json
  {
    "message": "Lấy thông tin tài khoản thành công.",
    "data": {
      "id": 5,
      "email": "guardian01@schoolbus.local",
      "fullName": "Nguyen Thi Guardian 01",
      "phone": "0901000001",
      "deviceToken": "YOUR_FIREBASE_DEVICE_TOKEN",
      "avatar": null,
      "roleName": "guardian",
      "status": "ACTIVE",
      "createdAt": "2026-03-27T08:00:00"
    }
  }
  ```

- **Mục đích**
  - FE dùng `data.id` làm `guardianId` khi tạo học sinh hoặc mua gói.
  - FE dùng `roleName` để chắc chắn tài khoản hiện tại là guardian.
  - Mobile có thể kiểm tra `deviceToken` hiện đang lưu trên backend hay chưa.

---

### 3.3 Lấy danh sách học sinh của guardian hiện tại

- **Request**
  - **GET** `/api/Student/GetMyStudents`
  - Header:
    - `Authorization: Bearer <guardian_token>`

- **Response 200**
  ```json
  {
    "message": "Lấy danh sách student thành công",
    "data": [
      {
        "id": 3,
        "fullName": "SOMITH",
        "dateOfBirth": "2018-03-16T00:00:00",
        "gender": "Male",
        "guardianId": 5,
        "guardianName": "Nguyen Thi Guardian 05",
        "campusId": 1,
        "campusName": "Campus Quan 1",
        "status": "ACTIVE"
      }
    ]
  }
  ```

- **Danh sách rỗng**
  - Nếu `data = []`, FE hiển thị trạng thái "Chưa có hồ sơ học sinh".

---

### 3.4 Lấy danh sách campus để chọn cơ sở

- **Request**
  - **GET** `/api/Campus/Search?page=1&pageSize=50`

- **Response 200**
  ```json
  {
    "message": "Lấy danh sách campus thành công",
    "data": {
      "items": [
        {
          "id": 1,
          "code": "CS001",
          "name": "Campus Quan 1",
          "address": "123 Nguyen Hue",
          "phone": "0909000001",
          "isActive": true,
          "imageUrl": null
        }
      ],
      "totalItems": 1,
      "page": 1,
      "pageSize": 50
    }
  }
  ```

---

### 3.5 Tạo profile học sinh

- **Request**
  - **POST** `/api/Student/Create`
  - Header:
    - `Authorization: Bearer <guardian_token>`
  - Body:
    ```json
    {
      "fullName": "Tran Gia Bao",
      "dateOfBirth": "2018-03-16",
      "gender": "male",
      "guardianId": 5,
      "campusId": 1
    }
    ```

- **Response 200**
  ```json
  {
    "message": "Tạo student thành công",
    "data": null
  }
  ```

- **Validation hiện có ở backend**
  - `fullName` bắt buộc, tối đa 100 ký tự
  - `dateOfBirth` bắt buộc và phải nhỏ hơn ngày hiện tại
  - `gender` chỉ nhận: `male`, `female`, `other`
  - `guardianId` phải là guardian hợp lệ và đang active
  - `campusId` phải tồn tại và đang active
  - Chặn trùng học sinh khi trùng toàn bộ:
    - `fullName`
    - `dateOfBirth`
    - `gender`
    - `guardianId`
    - `campusId`

---

### 3.6 Cập nhật profile học sinh

- **Request**
  - **PUT** `/api/Student/Update/{id}`
  - Body:
    ```json
    {
      "fullName": "Tran Gia Bao Updated",
      "gender": "male",
      "campusId": 2
    }
    ```

- **Response 200**
  - Trả về `StudentDto` sau khi cập nhật.

---

## 4. Logic FE gợi ý

### 4.1 Sau khi phụ huynh login thành công

1. Lưu `token`.
2. Nếu là mobile, truyền `deviceToken` ngay từ request login.
3. Gọi **GET** `/api/Account/Me`.
4. Nếu `roleName !== "guardian"`:
   - chặn màn hình hoặc redirect.
5. Nếu là guardian:
   - gọi tiếp **GET** `/api/Student/GetMyStudents`.

### 4.2 Nếu guardian chưa có học sinh

1. Hiển thị empty state.
2. Gọi **GET** `/api/Campus/Search?page=1&pageSize=50`.
3. Render dropdown campus.
4. FE tự set `guardianId = me.id`.

### 4.3 Khi phụ huynh bấm lưu

1. Gửi **POST** `/api/Student/Create`.
2. Nếu **200**:
   - Gọi lại **GET** `/api/Student/GetMyStudents` để refresh.
   - Chuyển UI sang màn "Danh sách học sinh".
3. Nếu **400**:
   - Hiển thị message lỗi backend trả về.

---

## 5. Sơ đồ quyết định (tóm tắt)

```text
Guardian login
    |
    v
POST /api/Account/Login
    |
    v
GET /api/Account/Me
    |
    |- roleName != guardian
    |   -> Chặn truy cập màn guardian
    |
    '- roleName = guardian
         |
         v
    GET /api/Student/GetMyStudents
         |
         |- data.length > 0
         |   -> Hiển thị danh sách học sinh
         |
         '- data.length = 0
              -> Hiển thị empty state
              -> GET /api/Campus/Search
              -> POST /api/Student/Create
                   |
                   |- 200 -> GET lại danh sách học sinh
                   '- 400 -> Hiển thị lỗi validate
```

---

## 6. Lưu ý

- FE không cần tự decode token để lấy `guardianId` nữa nếu đã dùng `GET /api/Account/Me`.
- FE cũng không cần gọi `GET /api/Student/GetByGuardian/{guardianId}` cho luồng guardian hiện tại nếu đã dùng `GET /api/Student/GetMyStudents`.
- `gender` nên chuẩn hóa ngay từ FE theo 3 giá trị backend chấp nhận: `male`, `female`, `other`.
- Sau khi tạo xong profile học sinh, phụ huynh mới có thể đi tiếp các luồng khác như:
  - chọn điểm đón/trả theo ngày
  - xem gói dịch vụ
  - mua gói cho học sinh
