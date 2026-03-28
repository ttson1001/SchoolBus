# Logic tích hợp API – Luồng 1: Admin import danh sách phụ huynh vào hệ thống (FE)

Tài liệu mô tả cách FE gọi API cho luồng **admin import danh sách phụ huynh** để cấp tài khoản truy cập vào hệ thống SchoolBus.

---

## 1. API dùng trong luồng

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| POST | `/api/Account/Login` | Admin đăng nhập để lấy token |
| POST | `/api/User/Import` | Import file Excel danh sách user |
| GET | `/api/User/Search?role=guardian&page=1&pageSize=10` | Kiểm tra lại danh sách guardian sau khi import |

**Lưu ý:**
- API import là API import user chung. Muốn import phụ huynh thì từng dòng trong file Excel phải có `roleName = guardian` hoặc `roleId` trỏ tới role guardian.
- Trong code hiện tại controller chưa gắn `[Authorize]` trực tiếp, nhưng FE vẫn nên coi đây là luồng **chỉ dành cho admin** và luôn gửi **Bearer token** sau khi login.

---

## 2. Luồng tổng quát

```text
[Admin đăng nhập]
      │
      ▼
POST /api/Account/Login
      │
      ▼
[Admin vào màn "Import phụ huynh"]
      │
      ▼
Chọn file Excel
      │
      ▼
POST /api/User/Import (multipart/form-data)
      │
      ▼
Hiển thị kết quả import
(totalRows, successRows, failedRows, errors)
      │
      ▼
GET /api/User/Search?role=guardian
      │
      ▼
Hiển thị danh sách phụ huynh đã có tài khoản trong hệ thống
```

---

## 3. Chi tiết API

### 3.1 Admin đăng nhập

- **Request**
  - **POST** `/api/Account/Login`
  - Body:
    ```json
    {
      "email": "admin@schoolbus.local",
      "password": "123456"
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

- **Lưu ý**
  - API hiện chỉ trả về `token`.
  - JWT hiện đang chứa các claim:
    - `UserId`
    - `Email`
    - `Role`
  - FE có thể decode token để xác định user hiện tại là admin.

---

### 3.2 Import file danh sách phụ huynh

- **Request**
  - **POST** `/api/User/Import`
  - Header:
    - `Authorization: Bearer <admin_token>`
  - Content-Type: `multipart/form-data`
  - Form-data:
    - `file`: file Excel `.xlsx`

- **Header bắt buộc trong file Excel**
  - `email`
  - `password`
  - `roleid` hoặc `rolename`

- **Cột có thể dùng thêm**
  - `fullname`
  - `phone`
  - `status`

- **Cách import phụ huynh**
  - Mỗi dòng nên để:
    - `rolename = guardian`
  - hoặc:
    - `roleid = <id của guardian>`

- **Ví dụ dữ liệu Excel**

| email | password | fullname | phone | rolename | status |
|------|----------|----------|-------|----------|--------|
| guardian01@schoolbus.local | 123456 | Nguyen Thi Guardian 01 | 0901000001 | guardian | ACTIVE |
| guardian02@schoolbus.local | 123456 | Nguyen Thi Guardian 02 | 0901000002 | guardian | ACTIVE |

- **Response 200**
  ```json
  {
    "message": "Import user thành công",
    "data": {
      "totalRows": 2,
      "successRows": 2,
      "failedRows": 0,
      "errors": []
    }
  }
  ```

- **Ý nghĩa response**
  - `totalRows`: tổng số dòng FE gửi lên
  - `successRows`: số dòng import thành công
  - `failedRows`: số dòng lỗi
  - `errors`: danh sách lỗi chi tiết theo từng dòng

---

### 3.3 Kiểm tra lại danh sách phụ huynh sau import

- **Request**
  - **GET** `/api/User/Search?role=guardian&page=1&pageSize=10`
  - Header:
    - `Authorization: Bearer <admin_token>`

- **Response 200**
  ```json
  {
    "message": "Lấy danh sách user thành công",
    "data": {
      "items": [
        {
          "id": 5,
          "email": "guardian01@schoolbus.local",
          "fullName": "Nguyen Thi Guardian 01",
          "phone": "0901000001",
          "driverLicenseNumber": null,
          "driverLicenseClass": null,
          "driverLicenseExpiryDate": null,
          "roleName": "guardian",
          "status": "ACTIVE",
          "createdAt": "2026-03-27T08:00:00"
        }
      ],
      "totalItems": 1,
      "page": 1,
      "pageSize": 10
    }
  }
  ```

---

## 4. Logic FE gợi ý

### 4.1 Vào màn "Import phụ huynh"

1. FE kiểm tra user đã login.
2. Decode token để xác định `Role === "admin"`.
3. Hiển thị form upload file Excel.

### 4.2 Khi admin bấm Import

1. FE kiểm tra file có đuôi `.xlsx`.
2. Gửi `multipart/form-data` tới **POST** `/api/User/Import`.
3. Nếu **200**:
   - Hiển thị kết quả import từ `data`.
   - Nếu `failedRows > 0`, render từng dòng lỗi trong `errors`.
4. Nếu **400**:
   - Hiển thị message lỗi backend trả về.

### 4.3 Sau khi import xong

1. Gọi lại **GET** `/api/User/Search?role=guardian&page=1&pageSize=10`.
2. Render danh sách phụ huynh đã có trong hệ thống.
3. Có thể cho FE search theo `keyword` để tìm nhanh guardian vừa import.

---

## 5. Sơ đồ quyết định (tóm tắt)

```text
Admin vào màn "Import phụ huynh"
    │
    ▼
Chọn file Excel
    │
    ▼
POST /api/User/Import
    │
    ├─ 200
    │    │
    │    ├─ failedRows = 0
    │    │    → Thông báo import thành công
    │    │    → GET /api/User/Search?role=guardian
    │    │
    │    └─ failedRows > 0
    │         → Hiển thị successRows / failedRows / errors
    │         → Admin sửa file và import lại nếu cần
    │
    └─ 400
         → Hiển thị lỗi upload / lỗi định dạng file / lỗi dữ liệu
```

---

## 6. Lưu ý

- File import phải là `.xlsx`, không dùng `.csv`.
- Để import đúng phụ huynh, nên dùng `rolename = guardian` cho dễ quản lý hơn `roleid`.
- Import chỉ tạo **tài khoản truy cập hệ thống** cho phụ huynh; chưa tạo profile học sinh.
- Sau bước này, phụ huynh sẽ dùng chính email/password đã import để đăng nhập lần đầu.
- Controller hiện chưa gắn `[Authorize]`, nên phần giới hạn quyền admin cần được FE và kiến trúc auth tổng thể kiểm soát thống nhất.
