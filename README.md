# SchoolBus BE API

Backend API cho hệ thống SchoolBus, xây dựng bằng ASP.NET Core và SQL Server.

## Yêu cầu

### Chạy trên Ubuntu bằng Docker
- Ubuntu 22.04 hoặc mới hơn
- Docker
- Docker Compose plugin

### Chạy local trên Ubuntu
- .NET SDK 9.0
- SQL Server hoặc SQL Server container

## Chạy nhanh trên Ubuntu bằng Docker Compose

### 1. Cài Docker và Docker Compose

```bash
sudo apt update
sudo apt install -y docker.io docker-compose-v2
sudo systemctl enable docker
sudo systemctl start docker
```

Kiểm tra:

```bash
docker --version
docker compose version
```

### 2. Chạy hệ thống

Tại thư mục gốc dự án:

```bash
sudo docker compose up --build
```

Chạy nền:

```bash
sudo docker compose up --build -d
```

Sau khi container chạy xong:

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- SQL Server: `localhost:1433`

Thông tin SQL Server mặc định trong `docker-compose.yml`:

- Server: `localhost,1433`
- Database: `SchoolBus`
- User: `sa`
- Password: `SchoolBus@123456`

Lưu ý:

- Ứng dụng tự chạy migration khi khởi động vì `Program.cs` đã có `context.Database.Migrate()`.
- Lần chạy đầu có thể mất vài phút do phải pull image và khởi tạo SQL Server.

## Xem log trên Ubuntu

```bash
sudo docker compose logs -f
```

Chỉ xem log API:

```bash
sudo docker compose logs -f api
```

Chỉ xem log SQL Server:

```bash
sudo docker compose logs -f sqlserver
```

## Dừng hệ thống

```bash
sudo docker compose down
```

Nếu muốn xóa luôn dữ liệu database:

```bash
sudo docker compose down -v
```

## Chạy local trên Ubuntu không dùng Docker

### 1. Cài .NET SDK 9

Làm theo hướng dẫn Microsoft cho Ubuntu, hoặc nếu máy đã có sẵn thì kiểm tra:

```bash
dotnet --version
```

### 2. Cấu hình connection string

Sửa `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=SchoolBus;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
}
```

### 3. Restore và chạy app

```bash
dotnet restore
dotnet run
```

## Một số API đã có

- `POST /api/Account/Login`
- `POST /api/User/Create`
- `POST /api/User/Import`
- `POST /api/User/CreateTeacher`
- `POST /api/User/CreateDriver`
- `GET /api/Package/Search`
- `POST /api/Attendance/ManualCheckIn`
- `POST /api/Attendance/ManualCheckOut`
- `POST /api/BusDamageReport/Create`

## Cấu trúc chính

- `Controllers`: API endpoints
- `Service`: business logic
- `Dto`: request/response models
- `Entites`: entity models
- `Database`: DbContext
- `Migrations`: EF Core migrations

## Ghi chú

- Thư mục entity trong dự án hiện tại là `Entites`.
- Nếu bạn vừa thêm entity mới và cần tạo migration:

```bash
dotnet ef migrations add TenMigration
dotnet ef database update
```
