# API Student Get With FaceAI

## Muc dich

API nay dung de lay thong tin chi tiet cua 1 hoc sinh.

Ngoai thong tin hoc sinh trong he thong, response se tra them:
- thong tin guardian
- thong tin campus
- danh sach face da dang ky tren FaceAI
- danh sach images da dang ky tren FaceAI neu co

---

## API

```http
GET /api/Student/Get/{id}
```

### Vi du

```http
GET /api/Student/Get/112
```

---

## Response thanh cong

```json
{
  "message": "Lấy student thành công",
  "data": {
    "id": 112,
    "studentCode": "ST001",
    "fullName": "Nguyen Minh Khang",
    "avatarUrl": "https://example.com/student-112.jpg",
    "dateOfBirth": "2015-09-20T00:00:00",
    "gender": "MALE",
    "guardianId": 2,
    "guardian": {
      "id": 2,
      "email": "guardian01@schoolbus.local",
      "fullName": "Nguyen Lan Huong",
      "phone": "0901000001",
      "avatarUrl": null,
      "driverLicenseNumber": null,
      "driverLicenseExpiryDate": null,
      "driverLicenseClass": null,
      "roleId": 2,
      "roleName": "guardian",
      "status": "ACTIVE",
      "createdAt": "2026-04-21T17:09:02.6299474"
    },
    "campusId": 1,
    "campusName": "Co so Quan 1",
    "status": "ACTIVE",
    "faceAiRegisteredFaces": {
      "student_id": 112,
      "student_name": "Nguyen Minh Khang",
      "total_faces": 4,
      "faces": [
        {
          "face_id": 9
        },
        {
          "face_id": 10
        },
        {
          "face_id": 11
        },
        {
          "face_id": 12
        }
      ],
      "images": [
        {
          "image_id": 101,
          "url": "https://api.faceride.site/static/student_112_1.jpg"
        },
        {
          "image_id": 102,
          "url": "https://api.faceride.site/static/student_112_2.jpg"
        }
      ]
    }
  }
}
```

---

## Y nghia field

### Thong tin hoc sinh

- `id`: id hoc sinh trong he thong
- `studentCode`: ma hoc sinh
- `fullName`: ho ten hoc sinh
- `avatarUrl`: anh dai dien hoc sinh
- `dateOfBirth`: ngay sinh
- `gender`: gioi tinh
- `status`: trang thai hoc sinh

### Thong tin guardian

- `guardianId`: id phu huynh
- `guardian`: object thong tin chi tiet phu huynh

### Thong tin campus

- `campusId`: id co so
- `campusName`: ten co so

### FaceAI

- `faceAiRegisteredFaces`: du lieu dong bo tu FaceAI
- `student_id`: id hoc sinh ben FaceAI
- `student_name`: ten hoc sinh ben FaceAI
- `total_faces`: tong so face metadata da dang ky
- `faces`: danh sach `face_id`
- `images`: danh sach anh da dang ky neu FaceAI co tra ve

---

## Khi nao `faceAiRegisteredFaces` bang `null`

Field nay co the la `null` trong cac truong hop:
- hoc sinh chua dang ky khuon mat
- FaceAI chua co du lieu cho hoc sinh nay
- FaceAI dang loi tam thoi
- backend lay du lieu hoc sinh thanh cong nhung khong lay duoc du lieu FaceAI

Vi du:

```json
{
  "message": "Lấy student thành công",
  "data": {
    "id": 112,
    "studentCode": "ST001",
    "fullName": "Nguyen Minh Khang",
    "avatarUrl": null,
    "dateOfBirth": "2015-09-20T00:00:00",
    "gender": "MALE",
    "guardianId": 2,
    "guardian": {
      "id": 2,
      "email": "guardian01@schoolbus.local",
      "fullName": "Nguyen Lan Huong",
      "phone": "0901000001",
      "avatarUrl": null,
      "driverLicenseNumber": null,
      "driverLicenseExpiryDate": null,
      "driverLicenseClass": null,
      "roleId": 2,
      "roleName": "guardian",
      "status": "ACTIVE",
      "createdAt": "2026-04-21T17:09:02.6299474"
    },
    "campusId": 1,
    "campusName": "Co so Quan 1",
    "status": "ACTIVE",
    "faceAiRegisteredFaces": null
  }
}
```

---

## Loi thuong gap

### Student khong ton tai

```json
{
  "message": "Student không tồn tại",
  "data": null
}
```

---

## Ghi chu cho FE

- FE nen check `faceAiRegisteredFaces != null` truoc khi render khuon mat
- FE nen check `images` co ton tai hay khong vi co the FaceAI chi tra `faces`
- `faces` va `images` la du lieu dong bo tu FaceAI, co the thay doi theo schema cua service FaceAI
- Neu chi can dem so khuon mat, co the uu tien dung `total_faces`
