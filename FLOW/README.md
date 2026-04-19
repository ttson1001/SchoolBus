# Luồng tích hợp (tài liệu)


| File                                                                                                           | Nội dung                                                                                                                                                                   |
| -------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [03-luong-chuan-bi-va-tao-lich-xe-bus.md](./03-luong-chuan-bi-va-tao-lich-xe-bus.md)                           | Chuẩn bị đầy đủ (campus, trạm, xe, tuyến, khuyến nghị phân công) → tạo `BusSchedule`; checklist, `DayOfWeek`, `ShiftType`, ràng buộc overlap và `StartDate`                |
| [04-luong-guardian-hoc-sinh-faceai-dang-ky-lich-xe.md](./04-luong-guardian-hoc-sinh-faceai-dang-ky-lich-xe.md) | Phụ huynh: login → tạo học sinh → FaceAI (chính: `AddStudentFace`; `CreateStudent` tuỳ chọn) → gói/order/ví → `BusSchedule` + `StudentBusAssignment/CreateBySchedule`      |
| [05-luong-tai-xe-lich-tram-diem-danh-khuon-mat.md](./05-luong-tai-xe-lich-tram-diem-danh-khuon-mat.md)         | Tài xế: login → `BusTripProgress/DriverSchedules` + `Current` / `Arrive` → `FaceAI/RecognizeCheckIn` & `RecognizeCheckOut` (điểm danh tự động; không `Attendance/Manual*`) |
| [06-luong-teacher-tuyen-hom-nay-manual-checkin.md](./06-luong-teacher-tuyen-hom-nay-manual-checkin.md)         | Teacher: login → `TeacherSchedules` (tuyến hôm nay) → `Student/GetByCode` (MASV) → `Attendance/ManualCheckIn` (thủ công)                                                   |


