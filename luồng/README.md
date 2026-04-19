# Luồng tích hợp (tài liệu)

| File | Nội dung |
|------|----------|
| [02-luong-xe-tuyen-phan-cong-lich.md](./02-luong-xe-tuyen-phan-cong-lich.md) | Xe → Tuyến → Phân công (driver/teacher) → Lịch chạy; căn theo controller/DTO/service (chỉ mô tả, không sửa code) |
| [03-luong-chuan-bi-va-tao-lich-xe-bus.md](./03-luong-chuan-bi-va-tao-lich-xe-bus.md) | Chuẩn bị đầy đủ (campus, trạm, xe, tuyến, khuyến nghị phân công) → tạo `BusSchedule`; checklist, `DayOfWeek`, `ShiftType`, ràng buộc overlap và `StartDate` |
| [04-luong-guardian-hoc-sinh-faceai-dang-ky-lich-xe.md](./04-luong-guardian-hoc-sinh-faceai-dang-ky-lich-xe.md) | Phụ huynh: login → tạo học sinh → FaceAI (chính: `AddStudentFace`; `CreateStudent` tuỳ chọn) → gói/order/ví → `BusSchedule` + `StudentBusAssignment/CreateBySchedule` |
| [05-luong-tai-xe-lich-tram-diem-danh-khuon-mat.md](./05-luong-tai-xe-lich-tram-diem-danh-khuon-mat.md) | Tài xế: login → `BusTripProgress/DriverSchedules` + `Current` / `Arrive` → `FaceAI/RecognizeCheckIn` & `RecognizeCheckOut` (điểm danh tự động; không `Attendance/Manual*`) |
| [06-luong-teacher-tuyen-hom-nay-manual-checkin.md](./06-luong-teacher-tuyen-hom-nay-manual-checkin.md) | Teacher: login → `TeacherSchedules` (tuyến hôm nay) → `Student/GetByCode` (MASV) → `Attendance/ManualCheckIn` (thủ công) |
| [07-luong-phu-huynh-lich-su-diem-danh-thong-bao.md](./07-luong-phu-huynh-lich-su-diem-danh-thong-bao.md) | Phụ huynh: `Attendance/GetByStudent` hoặc `Search` (lịch sử điểm danh); thông báo: Firebase push + DB `Notifications` (chưa có API GET danh sách) |
| [08-luong-admin-tong-hop-crud-import-setting.md](./08-luong-admin-tong-hop-crud-import-setting.md) | Admin (mục lục): import User/Student, CRUD master data, User/Role, phân công xe, HS, đơn/ví/giao dịch, attendance, báo cáo hư hỏng, setting nhận diện, upload, FaceAI health |
