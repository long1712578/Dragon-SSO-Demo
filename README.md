# Dragon SSO Demo

## Overview
This repository contains a complete microservices-based Single Sign-On (SSO) solution implemented using .NET 9 and ABP Framework 9.0.2.

### Services included:
1. **Identity Service**: Handles authentication and authorization using OpenIddict.
2. **Office Service**: Manages office resources and related operations.
3. **HRM Service**: Manages human resource management tasks.
4. **Payroll Service**: Manages payroll processing.
5. **API Gateway**: Acts as a gateway for all services using Ocelot.

---

## Database: SQLite (Development)

Identity Service sử dụng **SQLite** trong môi trường development.  
File database mặc định: `DragonSSO_Identity.db` (nằm tại thư mục chạy của `IdentityService.API`).

### Connection String (appsettings.json)
```json
"ConnectionStrings": {
  "Default": "Data Source=DragonSSO_Identity.db"
}
```

---

## Hướng dẫn Migration (EF Core + SQLite)

> Tất cả lệnh bên dưới chạy từ thư mục **gốc của solution** (`Dragon-SSO-Demo/`).

### 1. Kiểm tra migrations đã có

```powershell
dotnet ef migrations list --project src/IdentityService.EntityFrameworkCore --startup-project src/IdentityService.API
```

Kết quả sẽ hiển thị danh sách migrations và migration nào đã được áp dụng (có dấu `(Applied)`).

### 2. Tạo migration mới (khi thay đổi entity)

```powershell
dotnet ef migrations add <TênMigration> --project src/IdentityService.EntityFrameworkCore --startup-project src/IdentityService.API
```

Ví dụ:
```powershell
dotnet ef migrations add AddUserDevice --project src/IdentityService.EntityFrameworkCore --startup-project src/IdentityService.API
```

### 3. Áp dụng migration vào database SQLite

```powershell
dotnet ef database update --project src/IdentityService.EntityFrameworkCore --startup-project src/IdentityService.API
```

Lệnh này sẽ tạo file `DragonSSO_Identity.db` nếu chưa tồn tại và áp dụng tất cả migrations chưa được chạy.

### 4. Rollback về migration cụ thể

```powershell
dotnet ef database update <TênMigration> --project src/IdentityService.EntityFrameworkCore --startup-project src/IdentityService.API
```

Rollback về migration đầu tiên (xóa hết bảng):
```powershell
dotnet ef database update 0 --project src/IdentityService.EntityFrameworkCore --startup-project src/IdentityService.API
```

### 5. Xóa database và tạo lại từ đầu

```powershell
dotnet ef database drop --project src/IdentityService.EntityFrameworkCore --startup-project src/IdentityService.API
dotnet ef database update --project src/IdentityService.EntityFrameworkCore --startup-project src/IdentityService.API
```

---

## Xem nội dung SQLite Database

### Cách 1: DB Browser for SQLite (GUI — khuyến nghị)
1. Tải tại: https://sqlitebrowser.org/dl/
2. Mở file `src/IdentityService.API/DragonSSO_Identity.db` (hoặc nơi app đang chạy)
3. Tab **Browse Data** để xem dữ liệu từng bảng
4. Tab **Execute SQL** để chạy query tùy ý

### Cách 2: VS Code Extension
- Cài extension **SQLite Viewer** (tìm trong Extensions marketplace)
- Click vào file `*.db` trong Explorer để xem ngay trong VS Code

### Cách 3: CLI với sqlite3

```powershell
# Cài sqlite3 (nếu chưa có)
winget install SQLite.SQLite

# Mở database
sqlite3 src/IdentityService.API/DragonSSO_Identity.db

# Các lệnh hữu ích bên trong sqlite3:
.tables                    -- liệt kê tất cả bảng
.schema AbpUsers           -- xem cấu trúc bảng AbpUsers
SELECT * FROM AbpUsers;    -- xem dữ liệu
SELECT * FROM __EFMigrationsHistory;  -- xem migrations đã chạy
.quit                      -- thoát
```

---

## Quick Start (lần đầu setup)

```powershell
# 1. Restore packages
dotnet restore

# 2. Áp dụng migrations vào SQLite
dotnet ef database update --project src/IdentityService.EntityFrameworkCore --startup-project src/IdentityService.API

# 3. Chạy ứng dụng (sẽ tự seed data admin)
dotnet run --project src/IdentityService.API
```

---

## Dọn dẹp Git (loại bỏ file không cần thiết đã bị track)

Các file trong `.vs/`, `bin/`, `obj/`, `*.db` đã được khai báo trong `.gitignore`.  
Nếu các file này đã bị git track từ trước, chạy lệnh sau để xóa khỏi git index:

```powershell
# Chạy từ thư mục gốc của repo
git rm -r --cached .vs/
git rm -r --cached src/IdentityService.API/bin/
git rm -r --cached src/IdentityService.API/obj/
git rm -r --cached "*.db"

# Hoặc xóa toàn bộ cached files và re-add (áp dụng lại .gitignore)
git rm -r --cached .
git add .
git commit -m "chore: apply .gitignore - remove tracked build artifacts"
```

> ⚠️ Lệnh `git rm -r --cached .` chỉ xóa file khỏi git tracking, **không xóa file trên máy**.

---

## Requirements
- .NET 9 SDK
- EF Core Tools: `dotnet tool install --global dotnet-ef`

## License
This project is licensed under the MIT License.