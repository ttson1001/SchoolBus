# Contract: Firebase data payload (điểm danh)

Push được gửi tới **FCM token** lưu ở `User.DeviceToken` của **phụ huynh** (`guardian`), sau khi có bản ghi `Notifications` trong DB.

## Tiêu đề (notification title)

| `type` (trong `data`) | Title hiển thị |
|----------------------|----------------|
| `BOARDING` | Học sinh đã lên xe |
| `ALIGHTING` | Học sinh đã xuống xe |
| `WRONG_DROPOFF` | Cảnh báo xuống sai điểm |
| Khác | Thông báo SchoolBus |

## Body

Nội dung văn bản tiếng Việt (không dấu trong một số câu do nghiệp vụ) — trùng `message` lưu trong bảng `Notifications`.

## Data payload (string key/value)

Tất cả giá trị là **chuỗi** (FCM `data` chỉ hỗ trợ string).

| Key | Ý nghĩa | Ví dụ |
|-----|---------|--------|
| `type` | Loại sự kiện | `BOARDING`, `ALIGHTING`, `WRONG_DROPOFF` |
| `studentId` | Id học sinh | `12` |
| `guardianId` | Id phụ huynh | `5` |
| `busId` | Id xe buýt | `2` |
| `busLicensePlate` | Biển số | `51A-12345` |
| `routeName` | Tên tuyến | `Tuyen sang A` |
| `attendanceDate` | Ngày điểm danh | `2026-04-21` |
| `checkTime` | Giờ (định dạng `hh:mm` 12h trong code) | `07:30` |

## Gợi ý tích hợp FE

- Dùng `type` + `studentId` để mở màn chi tiết học sinh / lịch sử điểm danh.
- `WRONG_DROPOFF`: ưu tiên UI cảnh báo rõ ràng.

## Lưu ý

- Backend hiện **không** expose API danh sách `Notifications` trong Controllers; lịch sử có thể đọc từ DB hoặc bổ sung API sau.
- Các luồng khác (order, ví, …) **chưa** gửi FCM trong code hiện tại.
