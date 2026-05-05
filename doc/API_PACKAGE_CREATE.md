# API Tao Package

File nay mo ta rieng cho API tao package trong he thong.

## Muc dich

Package la goi ma phu huynh mua cho hoc sinh.

Moi package hien tai co cac thong tin chinh:

- `name`: ten goi
- `price`: gia goi
- `durationDays`: so ngay hieu luc
- `routeLimit`: duoc chon toi da bao nhieu tuyen
- `description`: mo ta them
- `status`: trang thai package
- `type`: loai goi
- `imageUrl`: hinh anh neu co

## Endpoint

### POST `/api/Package/Create`

API nay dung de tao package moi.

## Request JSON

```json
{
  "name": "Goi 1 tuyen 1 thang",
  "price": 1200000,
  "durationDays": 30,
  "routeLimit": 1,
  "description": "Goi cho hoc sinh di 1 tuyen trong 30 ngay",
  "status": "ACTIVE",
  "type": "MONTHLY",
  "imageUrl": "https://cdn.example.com/package-1-route.png"
}
```

## Y nghia tung field

- `name`: bat buoc, khong duoc rong
- `price`: gia package
- `durationDays`: bat buoc, phai lon hon `0`
- `routeLimit`: bat buoc, phai lon hon `0`
- `description`: khong bat buoc
- `status`: bat buoc
- `type`: khong bat buoc
- `imageUrl`: khong bat buoc

## Quy uoc nghiep vu hien tai

- `routeLimit = 1`: goi 1 tuyen
- `routeLimit = 2`: goi 2 tuyen
- `routeLimit = 3`: goi 3 tuyen

Backend hien tai chua khoa cung gia tri `status` va `type`, nhung de thong nhat team nen dung:

- `status`: `ACTIVE` hoac `INACTIVE`
- `type`: `MONTHLY`, `QUARTERLY`, `YEARLY`

## Vi du tao package

### 1. Goi 1 tuyen

```json
{
  "name": "Goi 1 tuyen",
  "price": 1200000,
  "durationDays": 30,
  "routeLimit": 1,
  "description": "Hoc sinh duoc chon 1 tuyen",
  "status": "ACTIVE",
  "type": "MONTHLY",
  "imageUrl": null
}
```

### 2. Goi 2 tuyen

```json
{
  "name": "Goi 2 tuyen",
  "price": 2000000,
  "durationDays": 30,
  "routeLimit": 2,
  "description": "Hoc sinh duoc chon 2 tuyen",
  "status": "ACTIVE",
  "type": "MONTHLY",
  "imageUrl": null
}
```

### 3. Goi 3 thang 2 tuyen

```json
{
  "name": "Goi 3 thang tieu chuan",
  "price": 4800000,
  "durationDays": 90,
  "routeLimit": 2,
  "description": "Goi 3 thang cho 2 tuyen",
  "status": "ACTIVE",
  "type": "QUARTERLY",
  "imageUrl": "https://cdn.example.com/package-quarterly.png"
}
```

## Success response

API tao package hien tai tra ve message, khong tra data package vua tao.

```json
{
  "message": "Tao package thanh cong",
  "data": null
}
```

## Loi thuong gap

### Ten package rong

```json
{
  "message": "Ten package khong duoc de trong",
  "data": null
}
```

### Khong truyen status

```json
{
  "message": "Status khong duoc de trong",
  "data": null
}
```

### DurationDays <= 0

```json
{
  "message": "DurationDays phai lon hon 0",
  "data": null
}
```

### RouteLimit <= 0

```json
{
  "message": "RouteLimit phai lon hon 0",
  "data": null
}
```

## API lien quan

- `GET /api/Package/Search`
- `GET /api/Package/Active`
- `GET /api/Package/Get/{id}`
- `PUT /api/Package/Update/{id}`
- `DELETE /api/Package/Delete/{id}`

## Ghi chu cho FE

- Khi tao package theo nghiep vu hien tai, FE nen hien ro:
  - goi duoc chon toi da bao nhieu tuyen
  - package hieu luc bao lau
  - package dang hoat dong hay tam khoa
- `routeLimit` la field quan trong nhat de phan biet goi 1 tuyen, 2 tuyen, 3 tuyen.
