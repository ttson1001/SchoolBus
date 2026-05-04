# API BusRoute Status

## Muc dich

Bo sung `RouteStatus` cho `BusRoute` de phan biet ro:

- tuyen di
- tuyen ve

Field nay khac voi `IsEnabled`:

- `IsEnabled`: bat/tat tuyen
- `RouteStatus`: loai tuyen dang la di hay ve

## Gia tri hop le

`RouteStatus` chap nhan 2 gia tri:

- `PICKUP`: tuyen di
- `DROPOFF`: tuyen ve

Neu khong truyen khi tao moi, he thong mac dinh:

- `PICKUP`

## Entity

`BusRoute` hien tai co them:

```csharp
public string RouteStatus { get; set; } = "PICKUP";
```

## DTO da cap nhat

### BusRouteCreateDto

```json
{
  "name": "Tuyen sang Quan 1 - Co so Q1",
  "routeStatus": "PICKUP",
  "campusId": 1,
  "stationIds": [1, 2, 3]
}
```

### BusRouteUpdateDto

```json
{
  "name": "Tuyen chieu Quan 1 - Co so Q1",
  "routeStatus": "DROPOFF",
  "isEnabled": true,
  "campusId": 1,
  "stationIds": [3, 2, 1]
}
```

### BusRouteDto

Response hien tai se co them:

```json
{
  "id": 1,
  "name": "Tuyen sang Quan 1 - Co so Q1",
  "routeStatus": "PICKUP",
  "isEnabled": true,
  "campusId": 1,
  "campusName": "Co so Quan 1",
  "buses": [],
  "stations": []
}
```

## API lien quan

### POST `/api/BusRoute/Create`

### Request

```json
{
  "name": "Tuyen sang Quan 1 - Co so Q1",
  "routeStatus": "PICKUP",
  "campusId": 1,
  "stationIds": [1, 2, 3]
}
```

### Success response

```json
{
  "message": "Tạo bus route thành công",
  "data": {
    "id": 1,
    "name": "Tuyen sang Quan 1 - Co so Q1",
    "routeStatus": "PICKUP",
    "isEnabled": true,
    "campusId": 1,
    "campusName": "Co so Quan 1",
    "buses": [],
    "stations": [
      {
        "id": 1,
        "name": "Tram Cong vien Tao Dan",
        "address": "55C Nguyen Thi Minh Khai, Quan 1, TP.HCM",
        "description": null,
        "latitude": 10.77978,
        "longitude": 106.69165,
        "isEnabled": true,
        "orderIndex": 1
      }
    ]
  }
}
```

### PUT `/api/BusRoute/Update/{id}`

### Request

```json
{
  "routeStatus": "DROPOFF"
}
```

### Success response

```json
{
  "message": "Cập nhật bus route thành công",
  "data": {
    "id": 1,
    "name": "Tuyen sang Quan 1 - Co so Q1",
    "routeStatus": "DROPOFF",
    "isEnabled": true,
    "campusId": 1,
    "campusName": "Co so Quan 1",
    "buses": [],
    "stations": []
  }
}
```

## Rule validate

- Neu `RouteStatus` rong khi tao moi, he thong tu gan `PICKUP`
- Neu `RouteStatus` co gia tri, chi chap nhan:
  - `PICKUP`
  - `DROPOFF`

Neu truyen sai:

```json
{
  "message": "RouteStatus khong hop le. Chi chap nhan PICKUP hoac DROPOFF",
  "data": null
}
```

## Vi du nghiep vu

### Tuyen di

```json
{
  "name": "Tuyen sang Quan 1 - Co so Q1",
  "routeStatus": "PICKUP",
  "campusId": 1,
  "stationIds": [1, 2, 3]
}
```

Y nghia:

- day la tuyen don hoc sinh di hoc

### Tuyen ve

```json
{
  "name": "Tuyen chieu Quan 1 - Co so Q1",
  "routeStatus": "DROPOFF",
  "campusId": 1,
  "stationIds": [3, 2, 1]
}
```

Y nghia:

- day la tuyen tra hoc sinh sau gio hoc

## Luu y DB

Can tao migration cho cot moi:

```powershell
dotnet ef migrations add AddRouteStatusToBusRoute
dotnet ef database update
```
