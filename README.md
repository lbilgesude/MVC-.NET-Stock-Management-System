# Stock Management System (Stok Takip Sistemi)

A comprehensive inventory management system built with ASP.NET Core MVC 8.0, designed to efficiently track stock movements, manage warehouses, and generate detailed reports.

## 📋 Table of Contents

- [Features](#features)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [Database Setup](#database-setup)
- [Project Structure](#project-structure)
- [Key Functionalities](#key-functionalities)
- [User Roles](#user-roles)
- [API Endpoints](#api-endpoints)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)

## ✨ Features

### Core Features
- **Stock Management**: Complete CRUD operations for stock items with brand, unit of measure, and detailed information
- **Warehouse Management**: Multi-level warehouse structure with main warehouses (Depo) and sub-warehouses (Alt Depo)
- **Stock Movements**: 
  - Stock Entry (Giriş)
  - Stock Exit (Çıkış)
  - Stock Transfer between warehouses
  - FIFO (First In First Out) algorithm for stock exit operations
- **Real-time Stock Status**: Track current stock levels across all warehouses
- **Minimum Stock Alerts**: Automatic notifications when stock levels fall below threshold
- **User Management**: Role-based access control with multiple user types
- **Activity Tracking**: Complete audit trail of all stock movements with user information
- **Reporting**: 
  - Statistical reports
  - Graphical reports with charts
  - Excel export functionality
- **Notification System**: In-app notification system for important alerts

### Advanced Features
- **FIFO Algorithm**: Automatic first-in-first-out stock management for accurate inventory tracking
- **Smart Stock Detection**: Automatic detection of warehouses with highest stock for transfer suggestions
- **Excel Export**: Export stock status and movement reports to Excel format
- **Search & Filter**: Advanced search and filtering capabilities across all modules
- **Responsive Design**: Modern, responsive UI that works on all devices

## 🛠 Technology Stack

- **Framework**: ASP.NET Core MVC 8.0
- **Database**: SQL Server
- **ORM**: Entity Framework Core 9.0.8
- **Authentication**: Cookie-based Authentication
- **Frontend**: 
  - Bootstrap 5
  - jQuery
  - Chart.js (for graphical reports)
- **Excel Export**: ClosedXML 0.105.0
- **Language**: C#

## 📦 Prerequisites

Before running this project, ensure you have the following installed:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (Express edition is sufficient)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) with C# extension
- SQL Server Management Studio (SSMS) - Optional but recommended

## 🚀 Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd StokTakip
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Update database connection string** (see Configuration section)

4. **Run database migrations**
   ```bash
   dotnet ef database update
   ```

5. **Build the project**
   ```bash
   dotnet build
   ```

6. **Run the application**
   ```bash
   dotnet run
   ```

   Or use Visual Studio:
   - Press `F5` to run with debugging
   - Press `Ctrl+F5` to run without debugging

## ⚙️ Configuration

### Database Connection

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME\\SQLEXPRESS;Database=StokTakipBgc;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Replace `YOUR_SERVER_NAME` with your SQL Server instance name.

### Application Settings

The application uses the following configuration files:
- `appsettings.json` - Production settings
- `appsettings.Development.json` - Development settings

## 🗄️ Database Setup

### Initial Setup

1. Create a new database in SQL Server:
   ```sql
   CREATE DATABASE StokTakipBgc;
   ```

2. Run Entity Framework migrations:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

### Database Schema

The system uses the following main tables:

- **KULLANICI** - User accounts
- **KULLANICI_TIP** - User role types
- **DEPO** - Main warehouses
- **ALT_DEPO** - Sub-warehouses
- **DEPO_ESLESTIRME** - Warehouse mappings
- **STOK** - Stock items
- **STOK_DURUM** - Current stock status per warehouse
- **STOK_HAREKET** - Stock movement history
- **HAREKET_TIP** - Movement types (Entry, Exit, Transfer)
- **OLCU_BIRIMI** - Units of measure
- **SORUMLU** - Responsible persons

### Initial Data

After database creation, you may need to seed initial data:
- User types (Admin, Depo Yetkilisi, Rapor Kullanıcısı)
- Movement types (Giriş, Çıkış, Transfer Giriş, Transfer Çıkış)
- Units of measure (Adet, Kg, Litre, etc.)

## 📁 Project Structure

```
StokTakip/
├── Controllers/          # MVC Controllers
│   ├── AccountController.cs
│   ├── HomeController.cs
│   ├── StoksController.cs
│   ├── StokHareketsController.cs
│   ├── RaporController.cs
│   └── ...
├── Models/              # Data models and ViewModels
│   ├── Stok.cs
│   ├── StokHareket.cs
│   ├── StokDurum.cs
│   └── ...
├── Views/               # Razor views
│   ├── Home/
│   ├── Stoks/
│   ├── StokHarekets/
│   └── ...
├── Data/                # Database context
│   └── StokTakipBgcContext.cs
├── Services/            # Business logic services
│   ├── StokDurumService.cs
│   └── UyariService.cs
├── Migrations/          # EF Core migrations
├── wwwroot/            # Static files (CSS, JS, images)
├── Program.cs          # Application entry point
└── appsettings.json    # Configuration file
```

## 🔑 Key Functionalities

### 1. Stock Entry (Stok Girişi)
- Add new stock items or update existing ones
- Automatic product creation if not exists
- Support for multiple warehouses
- Transaction-based operations for data integrity

### 2. Stock Exit (Stok Çıkışı)
- FIFO algorithm implementation
- Automatic stock level updates
- Minimum stock threshold checking
- Automatic alert generation

### 3. Stock Transfer
- Transfer between warehouses
- FIFO-based transfer operations
- Automatic stock level monitoring for both source and destination
- Transaction support for data consistency

### 4. Stock Detection & Transfer
- Automatic detection of warehouses with highest stock
- One-click transfer suggestions
- Batch transfer operations

### 5. Reporting
- **Statistical Reports**: Overview of stock statistics
- **Graphical Reports**: Visual charts and graphs
  - Monthly entry/exit trends
  - Product distribution
  - Warehouse-based stock status
  - Movement type distribution
  - Daily movement trends
  - Critical stock levels
  - Most active users
- **Excel Export**: Export reports to Excel format

### 6. Notification System
- Real-time notifications
- Minimum stock alerts
- Read/unread status tracking
- Persistent notifications stored in JSON file

## 👥 User Roles

The system supports three main user roles:

1. **Admin**
   - Full system access
   - User management
   - All stock operations
   - Report access

2. **Depo Yetkilisi** (Warehouse Manager)
   - Stock management
   - Stock movements (Entry, Exit, Transfer)
   - Stock status viewing

3. **Rapor Kullanıcısı** (Report User)
   - Read-only access to reports
   - Statistical and graphical report viewing

## 🔌 API Endpoints

### Authentication
- `GET /Account/Login` - Login page
- `POST /Account/Login` - Authenticate user
- `POST /Account/Logout` - Logout user

### Stock Operations
- `GET /Stoks` - List all stocks
- `GET /Stoks/Details/{id}` - Stock details
- `GET /Stoks/Edit/{id}` - Edit stock form
- `POST /Stoks/Edit/{id}` - Update stock
- `GET /Stoks/Delete/{id}` - Delete confirmation
- `POST /Stoks/Delete/{id}` - Delete stock
- `GET /Stoks/ExportToExcel` - Export stocks to Excel

### Stock Movements
- `GET /StokHarekets` - List all movements
- `GET /StokHarekets/Giris` - Stock entry form
- `POST /StokHarekets/Giris` - Process stock entry
- `GET /StokHarekets/Cikis` - Stock exit form
- `POST /StokHarekets/Cikis` - Process stock exit
- `GET /StokHarekets/Transfer` - Transfer form
- `POST /StokHarekets/Transfer` - Process transfer
- `GET /StokHarekets/TespitTransfer` - Smart transfer detection
- `POST /StokHarekets/TespitTransferYap` - Execute smart transfer

### Reports
- `GET /Rapor` - Report list
- `GET /Rapor/GrafikselRaporlar` - Graphical reports
- `GET /Rapor/Istatistikler` - Statistical reports

### Notifications
- `GET /Home/GetUyarilarAsync` - Get all notifications
- `POST /Home/MarkUyariAsRead` - Mark notification as read
- `POST /Home/MarkAllUyarilarAsRead` - Mark all as read
- `POST /Home/SilUyari` - Delete notification

## 💻 Usage

### First Time Setup

1. **Create Admin User**
   - Access the database directly or use a seed method
   - Create a user with role "Admin"

2. **Configure Movement Types**
   - Navigate to Tanımlamalar > Hareket Tipleri
   - Create: "Giriş", "Çıkış", "Transfer Giriş", "Transfer Çıkış"

3. **Configure Units of Measure**
   - Navigate to Tanımlamalar > Ölçü Birimleri
   - Add units like: "Adet", "Kg", "Litre", "Metre"

4. **Create Warehouses**
   - Navigate to Depolar > Ana Depolar
   - Create main warehouses
   - Navigate to Depolar > Alt Depolar
   - Create sub-warehouses
   - Navigate to Depolar > Depo Eşleştirmeleri
   - Map main warehouses to sub-warehouses

### Daily Operations

1. **Stock Entry**
   - Navigate to Stok Hareketleri > Stok Girişi
   - Enter product name, brand, quantity
   - Select warehouse and movement type
   - Submit

2. **Stock Exit**
   - Navigate to Stok Hareketleri > Stok Çıkışı
   - Select product and warehouse
   - Enter quantity (FIFO automatically applied)
   - Submit

3. **View Stock Status**
   - Navigate to Stoklar
   - View current stock levels across all warehouses
   - Use filters to search by name or warehouse

4. **Generate Reports**
   - Navigate to Raporlar
   - Select report type
   - View statistics or export to Excel

## 🔒 Security Features

- Cookie-based authentication
- Role-based authorization
- Password protection (stored in database)
- User activity tracking
- Secure session management

## 🐛 Troubleshooting

### Common Issues

1. **Database Connection Error**
   - Verify SQL Server is running
   - Check connection string in `appsettings.json`
   - Ensure database exists

2. **Migration Errors**
   - Delete existing migrations folder
   - Run `dotnet ef migrations add InitialCreate`
   - Run `dotnet ef database update`

3. **Authentication Issues**
   - Clear browser cookies
   - Verify user exists in database
   - Check user status (must be active)

## 📝 Notes

- The system uses FIFO algorithm for stock exit operations
- Minimum stock threshold is set to 10 units (configurable in code)
- Notifications are stored in JSON file: `bin/Debug/net8.0/Data/uyarilar.json`
- Excel export requires ClosedXML package
- All dates are stored in UTC format

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is proprietary software. All rights reserved.

## 👨‍💻 Author

Developed for inventory management needs.

## 📞 Support

For support and questions, please contact the development team.

---

**Version**: 1.0.0  
**Last Updated**: 2024  
**Framework**: .NET 8.0  
**Status**: Production Ready

