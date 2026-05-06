# API Import Hoc Sinh

## Muc dich

API nay dung de import nhieu hoc sinh bang file Excel `.xlsx`.

He thong se:
- doc tung dong trong file Excel
- tim `guardian` theo `guardianEmail`
- kiem tra `campusId`
- kiem tra trung `studentCode`
- kiem tra trung thong tin hoc sinh
- tao moi cac hoc sinh hop le

API hien co:

```http
POST /api/Student/ImportByGuardianEmail
```

Content-Type:

```http
multipart/form-data
```

Field form-data:
- `file`: file Excel `.xlsx`

---

## Rule import

- Chi ho tro file `.xlsx`
- File phai co du lieu
- Guardian phai ton tai theo `guardianEmail`
- Guardian phai co role `guardian`
- Guardian phai dang `ACTIVE`
- `campusId` phai ton tai
- Campus phai dang `ACTIVE`
- `studentCode` khong duoc trung trong DB
- `studentCode` khong duoc trung trong chinh file import
- Hoc sinh khong duoc trung full thong tin:
  - `fullName`
  - `dateOfBirth`
  - `gender`
  - `guardian`
  - `campus`

---

## Cot bat buoc trong file Excel

Header phai nam o dong 1.

He thong dang yeu cau cac cot:

```text
studentCode
fullName
dateOfBirth
gender
guardianEmail
campusId
```

Cot tuy chon:

```text
avatarUrl
```

Luu y:
- ten cot khuyen nghi dung dung y nhu tren
- khong nen doi ten header

---

## Mau file Excel

| studentCode | fullName           | dateOfBirth | gender | guardianEmail              | campusId | avatarUrl |
|-------------|--------------------|-------------|--------|----------------------------|----------|-----------|
| ST001       | Nguyen Minh Khang  | 2015-09-20  | MALE   | guardian01@schoolbus.local | 1        |           |
| ST002       | Tran Ngoc Anh      | 2016-01-05  | FEMALE | guardian02@schoolbus.local | 1        |           |
| ST003       | Le Gia Han         | 2015-12-11  | FEMALE | guardian01@schoolbus.local | 2        | https://example.com/a.jpg |

---

## Cach goi API

### cURL

```bash
curl -X POST "https://api.faceride.site/api/Student/ImportByGuardianEmail" ^
  -H "accept: */*" ^
  -H "Content-Type: multipart/form-data" ^
  -F "file=@students.xlsx"
```

### Swagger

1. Mo `POST /api/Student/ImportByGuardianEmail`
2. Chon file `.xlsx`
3. Bam `Execute`

---

## Response thanh cong

```json
{
  "message": "Import student thanh cong",
  "data": {
    "totalRows": 3,
    "successCount": 3,
    "failureCount": 0,
    "errors": []
  }
}
```

Y nghia:
- `totalRows`: tong so dong du lieu doc duoc
- `successCount`: so dong import thanh cong
- `failureCount`: so dong loi
- `errors`: danh sach loi theo tung dong

---

## Response thanh cong nhung co dong loi

```json
{
  "message": "Import student thanh cong",
  "data": {
    "totalRows": 4,
    "successCount": 2,
    "failureCount": 2,
    "errors": [
      "Dong 3: ma hoc sinh 'ST001' da ton tai.",
      "Dong 5: khong tim thay guardian voi email 'guardian999@schoolbus.local'."
    ]
  }
}
```

---

## Loi thuong gap

### Khong chon file

```json
{
  "message": "Vui long chon file import.",
  "data": null
}
```

### Sai dinh dang file

```json
{
  "message": "Chi ho tro file Excel (.xlsx).",
  "data": null
}
```

### File rong

```json
{
  "message": "File Excel khong co du lieu.",
  "data": null
}
```

### Thieu cot

```json
{
  "message": "File import thieu cot guardianemail.",
  "data": null
}
```

### Du lieu trong dong bi loi

```json
{
  "message": "Import student thanh cong",
  "data": {
    "totalRows": 1,
    "successCount": 0,
    "failureCount": 1,
    "errors": [
      "Dong 2: campusId 'abc' khong hop le."
    ]
  }
}
```

---

## Gia tri hop le cho gender

Nen dung:

```text
MALE
FEMALE
```

Neu he thong dang support them gia tri khac trong `NormalizeGender`, file import nen theo dung quy uoc cua backend.

---

## Checklist truoc khi import

- Da tao `guardian`
- Guardian da dung role `guardian`
- Guardian dang `ACTIVE`
- Da tao `campus`
- Campus dang `ACTIVE`
- File la `.xlsx`
- Header dung ten cot
- Khong trung `studentCode`

---

## Ghi chu

- API nay import hoc sinh theo `guardianEmail`, khong import theo `guardianId`
- Neu muon import lai sau khi loi, nen sua file Excel roi import lai
- Cac dong thanh cong van duoc luu, cac dong loi se nam trong `errors`
