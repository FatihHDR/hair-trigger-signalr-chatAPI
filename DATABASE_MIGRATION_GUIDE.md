# Database Migration Setup

## ⚠️ Disk Space Issue

Saat ini ada masalah disk space yang mencegah instalasi package `Microsoft.EntityFrameworkCore.Tools`. 

## Langkah-langkah yang Perlu Dilakukan

### 1. Bersihkan Disk Space
Pastikan Anda memiliki cukup ruang disk available (minimal 1-2 GB free space).

### 2. Install/Restore EF Core Tools Package

Package reference sudah ditambahkan ke [HairTrigger.Chat.Api.csproj](file:///d:/hair-trigger-signalr-chat/src/HairTrigger.Chat.Api/HairTrigger.Chat.Api.csproj).

Restore packages dengan:
```powershell
dotnet restore src/HairTrigger.Chat.Api/HairTrigger.Chat.Api.csproj
```

### 3. Update Connection String

Sebelum menjalankan migration, update connection string PostgreSQL di:
- [src/HairTrigger.Chat.Api/appsettings.json](file:///d:/hair-trigger-signalr-chat/src/HairTrigger.Chat.Api/appsettings.json)

Ganti `your_password_here` dengan password PostgreSQL Anda:
```json
{
  "ConnectionStrings": {
    "ChatDatabase": "Host=localhost;Port=5432;Database=hairtrigger_chat;Username=postgres;Password=YOUR_ACTUAL_PASSWORD",
    "Redis": "localhost:6379"
  }
}
```

### 4. Buat Migration Pertama

```powershell
cd d:\hair-trigger-signalr-chat
dotnet ef migrations add InitialCreate --project src/HairTrigger.Chat.Infrastructure --startup-project src/HairTrigger.Chat.Api
```

Ini akan membuat folder `Migrations` di project Infrastructure dengan file migration script.

### 5. Apply Migration ke Database

```powershell
dotnet ef database update --project src/HairTrigger.Chat.Infrastructure --startup-project src/HairTrigger.Chat.Api
```

Perintah ini akan:
- Membuat database `hairtrigger_chat` (jika belum ada)
- Membuat tabel `Messages` dan `Rooms`
- Menerapkan semua constraint dan index

### 6. Verifikasi Database

Anda bisa verify dengan PostgreSQL client (pgAdmin, DBeaver, atau psql):

```sql
-- Connect ke database hairtrigger_chat
\c hairtrigger_chat

-- List tables
\dt

-- Lihat struktur table Messages
\d "Messages"

-- Lihat struktur table Rooms
\d "Rooms"
```

## Struktur Database yang Akan Dibuat

### Table: Messages
| Column     | Type          | Description                      |
|------------|---------------|----------------------------------|
| Id         | uuid          | Primary key                      |
| UserId     | varchar(100)  | User identifier                  |
| UserName   | varchar(100)  | Display name                     |
| Content    | varchar(4000) | Message content                  |
| RoomId     | varchar(100)  | Room identifier (nullable)       |
| SentAt     | timestamp     | Message timestamp                |
| IsDeleted  | boolean       | Soft delete flag                 |

**Indexes:**
- Primary key on `Id`
- Index on `RoomId`
- Index on `SentAt` (for efficient sorting)

### Table: Rooms
| Column     | Type          | Description                      |
|------------|---------------|----------------------------------|
| Id         | varchar       | Primary key                      |
| Name       | varchar(100)  | Room display name                |
| CreatedAt  | timestamp     | Creation timestamp               |
| IsActive   | boolean       | Active status flag               |
| MemberIds  | text          | Comma-separated member IDs       |

**Indexes:**
- Primary key on `Id`

## Troubleshooting

### Error: "dotnet-ef not found"
Install global EF Core tools:
```powershell
dotnet tool install --global dotnet-ef
```

### Error: "Could not find Microsoft.EntityFrameworkCore.Design"
The package is already included in Infrastructure project. Make sure to run restore:
```powershell
dotnet restore
```

### Error: "Connection refused" or "authentication failed"
1. Pastikan PostgreSQL server running
2. Check connection string credentials
3. Verify user memiliki permission untuk create database

### Error: Disk space issues
Bersihkan NuGet cache:
```powershell
dotnet nuget locals all --clear
```

Atau hapus folder temporary:
- `bin/` dan `obj/` folders di semua project
- `%USERPROFILE%\.nuget\packages\` (dengan hati-hati)

## Alternative: Manual Database Creation

Jika migration masih bermasalah, Anda bisa buat database secara manual dengan SQL script:

```sql
CREATE DATABASE hairtrigger_chat;

\c hairtrigger_chat

CREATE TABLE "Messages" (
    "Id" uuid PRIMARY KEY,
    "UserId" varchar(100) NOT NULL,
    "UserName" varchar(100) NOT NULL,
    "Content" varchar(4000) NOT NULL,
    "RoomId" varchar(100),
    "SentAt" timestamp NOT NULL,
    "IsDeleted" boolean NOT NULL
);

CREATE INDEX "IX_Messages_RoomId" ON "Messages" ("RoomId");
CREATE INDEX "IX_Messages_SentAt" ON "Messages" ("SentAt");

CREATE TABLE "Rooms" (
    "Id" varchar PRIMARY KEY,
    "Name" varchar(100) NOT NULL,
    "CreatedAt" timestamp NOT NULL,
    "IsActive" boolean NOT NULL,
    "MemberIds" text NOT NULL
);
```

## Setelah Migration Berhasil

Anda bisa langsung run aplikasi:

```powershell
# Run API
dotnet run --project src/HairTrigger.Chat.Api

# Run Worker (di terminal terpisah)
dotnet run --project src/HairTrigger.Chat.Worker
```

API akan tersedia di `https://localhost:7xxx` dan SignalR hub di endpoint `/hubs/chat`.
