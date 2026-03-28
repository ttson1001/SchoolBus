# Logic tích hợp API – Luồng 5: Phụ huynh nạp tiền và mua gói cho học sinh (FE)

Tài liệu mô tả cách FE gọi API cho luồng **phụ huynh nạp tiền vào ví** và **mua gói cho học sinh** trong hệ thống SchoolBus.

Hiện tại backend hỗ trợ 2 cách mua:
- **Mua bằng ví**
- **Mua trực tiếp bằng payOS**

Ngoài ra vẫn có nhánh **nạp tiền vào ví qua payOS** như trước.

---

## 1. API dùng trong luồng

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| POST | `/api/Account/Login` | Phụ huynh đăng nhập lấy token |
| GET | `/api/Account/Me` | Lấy thông tin tài khoản hiện tại từ token |
| GET | `/api/Student/GetMyStudents` | Lấy danh sách học sinh của guardian hiện tại |
| GET | `/api/Package/Search?page=1&pageSize=10` | Lấy danh sách package |
| GET | `/api/Package/Get/{id}` | Lấy chi tiết package |
| GET | `/api/Wallet/GetByUser/{userId}` | Lấy ví của guardian |
| POST | `/api/Wallet/TopUp` | Nạp tiền trực tiếp vào ví, phù hợp test nội bộ |
| POST | `/api/Wallet/CreatePayOsTopUpLink` | Tạo link thanh toán payOS để nạp ví |
| GET | `/api/Wallet/GetPayOsTopUpStatus/{orderCode}` | Kiểm tra trạng thái giao dịch nạp ví payOS |
| POST | `/api/Order/Create` | Mua gói cho học sinh bằng số dư ví |
| POST | `/api/Order/CreatePayOsLink` | Tạo link thanh toán payOS để mua gói trực tiếp |
| GET | `/api/Order/GetPayOsStatus/{orderCode}` | Kiểm tra trạng thái giao dịch mua gói trực tiếp bằng payOS |
| GET | `/api/Order/GetByGuardian/{guardianId}` | Xem lịch sử mua gói của guardian |
| GET | `/api/Order/GetActiveByStudent/{studentId}` | Kiểm tra gói đang hiệu lực của học sinh |

**Lưu ý:**
- FE có thể dùng `GET /api/Account/Me` để lấy `userId` hiện tại thay vì tự decode token.
- `Order/Create` hiện mua gói bằng **ví**, không nhận `busRouteId`.
- `Order/CreatePayOsLink` là luồng **mua trực tiếp bằng payOS**, không đi qua ví.
- Backend hiện kiểm tra package còn `ACTIVE` cả lúc tạo link và lúc xác nhận webhook thanh toán thành công.

---

## 2. Luồng tổng quát

```text
[Guardian đăng nhập]
      │
      ▼
POST /api/Account/Login
      │
      ▼
GET /api/Account/Me
      │
      ▼
GET /api/Student/GetMyStudents
      │
      ▼
GET /api/Package/Search
      │
      ▼
Guardian chọn học sinh + chọn gói
      │
      ▼
GET /api/Order/GetActiveByStudent/{studentId}
      │
      ├─ Đang có gói active
      │    → Khóa nút mua
      │
      └─ Chưa có gói active
           │
           ├─ Mua bằng ví
           │    │
           │    ├─ Ví đủ tiền → POST /api/Order/Create
           │    └─ Ví thiếu tiền → nạp ví rồi mua lại
           │
           └─ Mua trực tiếp bằng payOS
                │
                ▼
           POST /api/Order/CreatePayOsLink
                │
                ▼
           Guardian thanh toán payOS
                │
                ▼
           GET /api/Order/GetPayOsStatus/{orderCode}
                │
                ▼
           Nếu PAID → gói được kích hoạt trực tiếp
```

---

## 3. Chi tiết API

### 3.1 Guardian đăng nhập

- **Request**
  - **POST** `/api/Account/Login`
  - Body:
    ```json
    {
      "email": "guardian01@schoolbus.local",
      "password": "123456"
    }
    ```

- **Response 200**
  ```json
  {
    "message": "Dang nhap thanh cong.",
    "data": {
      "token": "jwt_token_here"
    }
  }
  ```

---

### 3.2 Lấy thông tin guardian hiện tại

- **Request**
  - **GET** `/api/Account/Me`
  - Header:
    - `Authorization: Bearer <guardian_token>`

- **Response 200**
  ```json
  {
    "message": "Lay thong tin tai khoan thanh cong.",
    "data": {
      "id": 5,
      "email": "guardian01@schoolbus.local",
      "fullName": "Nguyen Thi Guardian 01",
      "phone": "0901000001",
      "avatar": null,
      "roleName": "guardian",
      "status": "ACTIVE",
      "createdAt": "2026-03-27T08:00:00"
    }
  }
  ```

---

### 3.3 Lấy danh sách học sinh của guardian hiện tại

- **Request**
  - **GET** `/api/Student/GetMyStudents`

- **Response 200**
  ```json
  {
    "message": "Lay danh sach student thanh cong",
    "data": [
      {
        "id": 2,
        "fullName": "Tran Gia Bao",
        "dateOfBirth": "2018-03-16T00:00:00",
        "gender": "Male",
        "guardianId": 5,
        "guardianName": "Nguyen Thi Guardian 01",
        "campusId": 1,
        "campusName": "Campus Quan 1",
        "status": "ACTIVE"
      }
    ]
  }
  ```

---

### 3.4 Lấy danh sách package

- **Request**
  - **GET** `/api/Package/Search?page=1&pageSize=10`

- **Response 200**
  ```json
  {
    "message": "Lấy danh sách package thành công",
    "data": {
      "items": [
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
      ],
      "totalItems": 1,
      "page": 1,
      "pageSize": 10
    }
  }
  ```

---

### 3.5 Kiểm tra gói đang hiệu lực của học sinh

- **Request**
  - **GET** `/api/Order/GetActiveByStudent/2`

- **Response 200**
  - Nếu có gói active: trả `OrderDto`
  - Nếu không có gói active: `data = null`

- **Lưu ý**
  - Backend sẽ tự chuyển trạng thái sang `EXPIRED` nếu gói đã quá hạn khi gọi API này.

---

### 3.6 Lấy ví hiện tại của guardian

- **Request**
  - **GET** `/api/Wallet/GetByUser/5`

- **Response 200**
  ```json
  {
    "message": "Lay vi thanh cong",
    "data": {
      "id": 1,
      "userId": 5,
      "userName": "Nguyen Thi Guardian 01",
      "email": "guardian01@schoolbus.local",
      "balance": 200000
    }
  }
  ```

- **Lưu ý**
  - Nếu guardian chưa có ví, backend hiện sẽ tự tạo ví mới với `balance = 0`.

---

## 4. Nhánh A: Mua bằng ví

### 4.1 Nạp tiền trực tiếp vào ví, phù hợp test nội bộ

- **Request**
  - **POST** `/api/Wallet/TopUp`
  - Body:
    ```json
    {
      "userId": 5,
      "amount": 500000
    }
    ```

- **Response 200**
  ```json
  {
    "message": "Nap tien vao vi thanh cong",
    "data": {
      "id": 1,
      "userId": 5,
      "userName": "Nguyen Thi Guardian 01",
      "email": "guardian01@schoolbus.local",
      "balance": 700000
    }
  }
  ```

### 4.2 Nạp ví bằng payOS

- **Request**
  - **POST** `/api/Wallet/CreatePayOsTopUpLink`
  - Body:
    ```json
    {
      "userId": 5,
      "amount": 500000,
      "returnUrl": "https://your-frontend.com/wallet/payos-return",
      "cancelUrl": "https://your-frontend.com/wallet/payos-cancel"
    }
    ```

- **Response 200**
  ```json
  {
    "message": "Tao link nap tien payOS thanh cong",
    "data": {
      "userId": 5,
      "orderCode": 1760000000000,
      "amount": 500000,
      "description": "Nap vi GD5",
      "checkoutUrl": "https://pay.payos.vn/web/...",
      "status": "PENDING",
      "createdAt": "2026-03-27T08:30:00"
    }
  }
  ```

### 4.3 Kiểm tra trạng thái giao dịch nạp ví payOS

- **Request**
  - **GET** `/api/Wallet/GetPayOsTopUpStatus/1760000000000`

- **Response 200**
  ```json
  {
    "message": "Lay vi thanh cong",
    "data": {
      "userId": 5,
      "orderCode": 1760000000000,
      "amount": 500000,
      "status": "PAID",
      "paidAt": "2026-03-27T08:35:00",
      "walletBalance": 700000
    }
  }
  ```

### 4.4 Mua gói bằng số dư ví

- **Request**
  - **POST** `/api/Order/Create`
  - Body:
    ```json
    {
      "guardianId": 5,
      "studentId": 2,
      "packageId": 1
    }
    ```

- **Response 200**
  ```json
  {
    "message": "Tao order thanh cong",
    "data": {
      "id": 12,
      "guardianId": 5,
      "guardianName": "Nguyen Thi Guardian 01",
      "studentId": 2,
      "studentName": "Tran Gia Bao",
      "busRouteId": null,
      "busRouteName": null,
      "packageId": 1,
      "packageName": "Goi 1 thang",
      "packagePrice": 500000,
      "durationDays": 30,
      "status": "PAID",
      "startDate": "2026-03-27T08:40:00",
      "endDate": "2026-04-26T08:40:00",
      "paidAt": "2026-03-27T08:40:00",
      "expiredAt": null,
      "createdAt": "2026-03-27T08:40:00"
    }
  }
  ```

---

## 5. Nhánh B: Mua trực tiếp bằng payOS

### 5.1 Tạo link thanh toán trực tiếp cho order

- **Request**
  - **POST** `/api/Order/CreatePayOsLink`
  - Body:
    ```json
    {
      "guardianId": 5,
      "studentId": 2,
      "packageId": 1,
      "returnUrl": "https://your-frontend.com/order/payos-return",
      "cancelUrl": "https://your-frontend.com/order/payos-cancel"
    }
    ```

- **Response 200**
  ```json
  {
    "message": "Tao link thanh toan payOS cho order thanh cong",
    "data": {
      "orderId": 21,
      "guardianId": 5,
      "studentId": 2,
      "packageId": 1,
      "packageName": "Goi 1 thang",
      "orderCode": 1760000000100,
      "amount": 500000,
      "description": "Mua goi HS2",
      "checkoutUrl": "https://pay.payos.vn/web/...",
      "status": "PENDING",
      "createdAt": "2026-03-27T09:00:00"
    }
  }
  ```

- **Validation hiện có ở backend**
  - `guardianId` phải là guardian hợp lệ và active
  - `studentId` phải thuộc guardian đó
  - `packageId` phải tồn tại và `status = ACTIVE`
  - package phải có `DurationDays > 0`
  - package phải có `Price > 0`
  - giá package dùng qua payOS phải là số nguyên VND
  - nếu học sinh đang có gói còn hiệu lực thì chặn tạo link

---

### 5.2 Guardian thanh toán payOS

1. FE lấy `checkoutUrl` từ response.
2. Redirect guardian sang payOS hoặc mở webview.
3. Sau khi thanh toán xong, guardian quay lại FE qua `returnUrl`.

---

### 5.3 Kiểm tra trạng thái giao dịch mua trực tiếp bằng payOS

- **Request**
  - **GET** `/api/Order/GetPayOsStatus/1760000000100`

- **Response 200**
  ```json
  {
    "message": "Lay order thanh cong",
    "data": {
      "orderId": 21,
      "guardianId": 5,
      "studentId": 2,
      "packageId": 1,
      "packageName": "Goi 1 thang",
      "orderCode": 1760000000100,
      "amount": 500000,
      "orderStatus": "PAID",
      "transactionStatus": "SUCCESS",
      "paidAt": "2026-03-27T09:03:00",
      "startDate": "2026-03-27T09:03:00",
      "endDate": "2026-04-26T09:03:00",
      "createdAt": "2026-03-27T09:00:00"
    }
  }
  ```

- **Các trạng thái có thể gặp**
  - `orderStatus = PENDING`, `transactionStatus = PENDING`
    - Guardian chưa thanh toán xong
  - `orderStatus = PAID`, `transactionStatus = SUCCESS`
    - Mua gói thành công, gói đã được kích hoạt
  - `orderStatus = CANCELLED`, `transactionStatus = FAILED`
    - Thanh toán thất bại, hoặc package không còn hợp lệ khi xác nhận

---

### 5.4 Lưu ý xác nhận thanh toán trực tiếp

- Backend hiện có endpoint webhook riêng cho order direct:
  - **POST** `/api/Order/HandlePayOsWebhook`
- FE thông thường không cần gọi endpoint này trực tiếp.
- Sau khi guardian quay lại FE, chỉ cần poll:
  - `GET /api/Order/GetPayOsStatus/{orderCode}`

---

## 6. Lịch sử order theo guardian

### 6.1 Xem lịch sử mua gói

- **Request**
  - **GET** `/api/Order/GetByGuardian/5`

- **Response 200**
  - Trả về mảng `OrderDto`, sắp theo `id` giảm dần.

---

## 7. Logic FE gợi ý

### 7.1 Khi guardian vào màn mua gói

1. Gọi **GET** `/api/Account/Me`.
2. Gọi **GET** `/api/Student/GetMyStudents`.
3. Gọi **GET** `/api/Package/Search?page=1&pageSize=10`.
4. Gọi **GET** `/api/Wallet/GetByUser/{me.id}`.
5. Render:
   - danh sách học sinh
   - danh sách gói
   - số dư ví hiện tại
   - 2 nút mua:
     - `Mua bằng ví`
     - `Thanh toán payOS`

### 7.2 Trước khi cho mua

1. Gọi **GET** `/api/Order/GetActiveByStudent/{studentId}`.
2. Nếu `data != null`:
   - Hiển thị "Học sinh đang có gói còn hiệu lực".
   - Khóa cả nút mua bằng ví và nút mua payOS.
3. Nếu `data == null`:
   - Cho phép mua.

### 7.3 Nếu guardian chọn mua bằng ví

1. Kiểm tra số dư ví đang hiển thị.
2. Nếu ví không đủ tiền:
   - Cho nạp ví bằng `TopUp` hoặc `CreatePayOsTopUpLink`.
3. Khi ví đủ tiền:
   - Gửi **POST** `/api/Order/Create`.

### 7.4 Nếu guardian chọn mua trực tiếp bằng payOS

1. Gửi **POST** `/api/Order/CreatePayOsLink`.
2. Nhận `checkoutUrl` và `orderCode`.
3. Redirect sang payOS.
4. Khi quay lại FE:
   - poll **GET** `/api/Order/GetPayOsStatus/{orderCode}`
5. Nếu `orderStatus = PAID`:
   - cập nhật UI thành mua thành công
   - gọi lại **GET** `/api/Order/GetActiveByStudent/{studentId}`
   - gọi lại **GET** `/api/Order/GetByGuardian/{me.id}` nếu cần refresh lịch sử

---

## 8. Sơ đồ quyết định (tóm tắt)

```text
Guardian vào màn mua gói
    │
    ▼
GET /api/Account/Me
GET /api/Student/GetMyStudents
GET /api/Package/Search
GET /api/Wallet/GetByUser/{me.id}
    │
    ▼
Guardian chọn student + package
    │
    ▼
GET /api/Order/GetActiveByStudent/{studentId}
    │
    ├─ Có gói active
    │    → Khóa nút mua
    │
    └─ Không có gói active
         │
         ├─ Mua bằng ví
         │    ├─ Ví đủ tiền → POST /api/Order/Create
         │    └─ Ví thiếu tiền → nạp ví → mua lại
         │
         └─ Mua trực tiếp payOS
              ├─ POST /api/Order/CreatePayOsLink
              ├─ Redirect checkoutUrl
              ├─ GET /api/Order/GetPayOsStatus/{orderCode}
              └─ Nếu PAID → gói được kích hoạt
```

---

## 9. Lưu ý

- FE nên ưu tiên dùng `GET /api/Account/Me` để lấy `userId` hiện tại.
- FE nên ưu tiên dùng `GET /api/Student/GetMyStudents` thay vì yêu cầu guardian nhập `guardianId`.
- `Order/Create` và `Order/CreatePayOsLink` hiện vẫn cần `guardianId` trong body, nên FE lấy từ `me.id`.
- `GetActiveByStudent` là API rất hữu ích để khóa nút mua nếu học sinh đang còn hạn gói.
- Luồng direct payOS hiện có webhook riêng ở `/api/Order/HandlePayOsWebhook`.
- Luồng nạp ví payOS hiện có webhook riêng ở `/api/Wallet/HandlePayOsWebhook`.
- Nếu bạn muốn gom cả 2 luồng về **một webhook payOS chung**, mình có thể làm tiếp để triển khai production gọn hơn.
