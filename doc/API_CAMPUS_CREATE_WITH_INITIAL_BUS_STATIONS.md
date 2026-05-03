# API Campus Create voi initialBusStations

## Muc dich

API tao `Campus` hien tai:
- khong con dung `latitude`, `longitude` tren `Campus`
- co the truyen kem `initialBusStations`
- neu `initialBusStations = null` hoac khong truyen thi van tao `Campus` binh thuong
- neu co `initialBusStations` thi tao them danh sach `BusStation` ngay sau khi tao `Campus`

## Endpoint

### POST `/api/Campus/Create`

## Rule

1. `Campus` chi gom:
- `code`
- `name`
- `address`
- `phone`
- `isActive`
- `imageUrl`

2. `initialBusStations` la tuy chon
- co the bo trong
- co the de `null`
- co the truyen mot list tram

3. Neu truyen `initialBusStations`
- moi tram phai co `name`
- `latitude`, `longitude` la tuy chon
- `isEnabled` neu khong truyen se mac dinh la `true`
- ten tram khong duoc trung voi tram da co trong he thong

## Request JSON

### Truong hop 1: chi tao campus, khong tao tram

```json
{
  "code": "Q1",
  "name": "Co so Quan 1",
  "address": "123 Nguyen Hue, Quan 1, TP.HCM",
  "phone": "0901000001",
  "isActive": true,
  "imageUrl": "https://example.com/campus-q1.jpg"
}
```

### Truong hop 2: tao campus, `initialBusStations = null`

```json
{
  "code": "Q1",
  "name": "Co so Quan 1",
  "address": "123 Nguyen Hue, Quan 1, TP.HCM",
  "phone": "0901000001",
  "isActive": true,
  "imageUrl": "https://example.com/campus-q1.jpg",
  "initialBusStations": null
}
```

### Truong hop 3: tao campus kem list tram

```json
{
  "code": "Q1",
  "name": "Co so Quan 1",
  "address": "123 Nguyen Hue, Quan 1, TP.HCM",
  "phone": "0901000001",
  "isActive": true,
  "imageUrl": "https://example.com/campus-q1.jpg",
  "initialBusStations": [
    {
      "name": "Tram cong truong",
      "address": "45 Le Loi, Quan 1",
      "description": "Tram don hoc sinh khu trung tam",
      "latitude": 10.7745,
      "longitude": 106.7019,
      "isEnabled": true
    },
    {
      "name": "Tram ben thanh",
      "address": "Cho Ben Thanh, Quan 1",
      "description": "Tram don hoc sinh khu cho",
      "latitude": 10.7725,
      "longitude": 106.6980,
      "isEnabled": true
    }
  ]
}
```

## Success Response

```json
{
  "message": "Tao campus thanh cong",
  "data": {
    "id": 1,
    "code": "Q1",
    "name": "Co so Quan 1",
    "address": "123 Nguyen Hue, Quan 1, TP.HCM",
    "phone": "0901000001",
    "isActive": true,
    "imageUrl": "https://example.com/campus-q1.jpg"
  }
}
```

## Error Response

### Thieu code

```json
{
  "message": "Code khong duoc de trong",
  "data": null
}
```

### Thieu ten campus

```json
{
  "message": "Ten campus khong duoc de trong",
  "data": null
}
```

### Thieu dia chi

```json
{
  "message": "Dia chi khong duoc de trong",
  "data": null
}
```

### Trung code campus

```json
{
  "message": "Code campus da ton tai",
  "data": null
}
```

### Trung ten campus

```json
{
  "message": "Ten campus da ton tai",
  "data": null
}
```

### Tram khong co ten

```json
{
  "message": "Ten bus station khong duoc de trong",
  "data": null
}
```

### Trung ten tram

```json
{
  "message": "Ten bus station da ton tai",
  "data": null
}
```

## Ghi chu cho FE

1. Khong can gui `initialBusStations` neu chua muon tao tram.
2. Neu gui `initialBusStations`, backend chi tra `CampusDto`.
3. Muon xem danh sach tram vua tao thi goi API `BusStation/Search` hoac API chi tiet tram sau do.
