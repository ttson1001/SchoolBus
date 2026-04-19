# Tích hợp Firebase Cloud Messaging (FCM) — hướng dẫn vận hành

Tài liệu hỗ trợ checklist **Phần 1** trong kế hoạch tích hợp: chuẩn bị project Firebase, file credential, và an toàn repo.

## 1. Firebase Console

1. Vào [Firebase Console](https://console.firebase.google.com/), tạo hoặc chọn project.
2. Bật **Cloud Messaging** (Build → Cloud Messaging).
3. Ghi lại **Project ID** — trùng với `Firebase:ProjectId` trong `appsettings`.

## 2. Service account (Admin SDK) cho backend

1. Project settings → **Service accounts** → **Firebase Admin SDK** → **Generate new private key**.
2. Tải file JSON; **không** commit file này (đã có rule trong [.gitignore](../.gitignore)).
3. Đặt file JSON vào thư mục [`firebase/`](../firebase/) trong workspace (ví dụ project hiện tại: `schoolbus-ccd90-firebase-adminsdk-fbsvc-eda64c6812.json`; tên file có thể khác tùy lần tải key).
4. Trong `appsettings.Development.json` (hoặc biến môi trường):

   - `Firebase:Enabled` = `true`
   - `Firebase:ProjectId` = Project ID từ Firebase (ví dụ `schoolbus-ccd90`)
   - `Firebase:CredentialsPath` = đường dẫn tương đối tới file JSON, ví dụ `firebase/schoolbus-ccd90-firebase-adminsdk-fbsvc-eda64c6812.json` (dùng `/` trên mọi OS).

ASP.NET Core cũng đọc override dạng `Firebase__Enabled`, `Firebase__ProjectId`, `Firebase__CredentialsPath`.

## 2b. Docker Compose

File [`docker-compose.yml`](../docker-compose.yml) đã cấu hình sẵn:

- **Volume:** `./firebase` → `/app/firebase` (read-only), khớp `Firebase__CredentialsPath` (ví dụ `firebase/schoolbus-ccd90-firebase-adminsdk-fbsvc-eda64c6812.json`).
- **Biến môi trường:** `Firebase__ProjectId` = `schoolbus-ccd90`, `Firebase__Enabled` — trong [`docker-compose.yml`](../docker-compose.yml) đã bật khi có file JSON đúng tên trong `firebase/` trên host.
- **Chạy lại:** `docker compose up -d --build` (hoặc tương đương).

Endpoint chẩn đoán [`FirebaseDiagnostics`](../Controllers/FirebaseDiagnosticsController.cs) chỉ hoạt động khi `ASPNETCORE_ENVIRONMENT=Development`. Nếu cần gọi `GET .../FirebaseDiagnostics/Status` trong container, tạm đổi biến môi trường `ASPNETCORE_ENVIRONMENT` trong compose (hoặc dùng `compose.override`) rồi trả lại `Production` khi xong.

## 3. Ứng dụng mobile

### Android

- Thêm app Android vào Firebase project, tải `google-services.json`, package name khớp app.

### iOS

- Thêm app iOS; trong Firebase → Project settings → **Apple apps** → upload **APNs** key hoặc certificate để FCM gửi được tới APNs.

## 4. Xác minh backend sau khi cấu hình

1. Chạy API; xem log lúc khởi động: có dòng xác nhận Firebase Admin SDK đã init (xem [Program.cs](../Program.cs)).
2. Gọi `GET /api/FirebaseDiagnostics/Status` (chỉ **Development**) — trả về trạng thái bật/tắt và đã init hay chưa.
3. Đăng nhập mobile với `deviceToken`, gọi `GET /api/Account/Me` — kiểm tra `deviceToken` trong response.
4. (Tuỳ chọn) `POST /api/FirebaseDiagnostics/SendTest` với Bearer token để gửi tin thử.

Chi tiết payload push khi điểm danh: [push-notification-payload.md](./push-notification-payload.md).

## 5. Checklist QA end-to-end (điểm danh)

1. Bật Firebase, đặt file JSON đúng path; restart API; log có dòng khởi tạo thành công.
2. `GET /api/FirebaseDiagnostics/Status` (Development) — `adminSdkInitialized: true`, `credentialsFileExists: true`.
3. Mobile: login với `deviceToken` → `GET /api/Account/Me` có `deviceToken` khớp.
4. (Tuỳ chọn) `POST /api/FirebaseDiagnostics/SendTest` — thiết bị nhận tin thử.
5. Thực hiện **Manual check-in / check-out** (hoặc FaceAI) theo luồng nghiệp vụ — phụ huynh nhận push `BOARDING` / `ALIGHTING`; thử **xuống sai trạm** để có `WRONG_DROPOFF`.
6. Kiểm tra bảng `Notifications` có bản ghi tương ứng cùng nội dung message.

**Lưu ý:** logic chống trùng theo cùng `message` + ngày có thể chặn gửi lặp — QA kỳ vọng một thông báo mỗi tổ hợp đó.
