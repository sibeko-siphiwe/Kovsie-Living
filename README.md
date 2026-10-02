# UFS Qwaqwa Campus - Housing Management System 🏢📱

[![Framework](https://shields.io)](https://microsoft.com)
[![Platform](https://shields.io)](https://android.com)
[![Database](https://shields.io)](https://microsoft.com)
[![Language](https://shields.io_|_Java_|_Kotlin-blue)]()

A complete **Client-Server Application** designed to digitize, streamline, and automate online accommodation payments and housing operations for the University of the Free State (UFS) Qwaqwa Campus.

---

## 📌 Project Overview

### Problem Statement
The UFS Qwaqwa campus student and staff housing complexes currently operate on a manual queuing system for utility and service payments (Water, Electricity, Rent, Parking, Refuse). This creates operational inefficiencies and long physical wait times for residents.

### Solution
An integrated client-server system featuring a **.NET MVC Web Application** (for administration and management) and an **Android Mobile Application** (for residents). The ecosystem enables real-time synchronization, secure digital payment simulations, and comprehensive role-based workflows to eliminate physical queues entirely.

### Core Benefits
* 🕒 **24/7 Availability:** Residents can pay fees anytime, anywhere.
* 📉 **Zero Queues:** Removes the need for physical cash handling and queues.
* 📊 **Automated Tracking:** Instant digital receipts, transaction ledgers, and reporting analytics.

---

## 📐 System Architecture

The application relies on a robust client-server model connected via secure communication protocols (**SSH / HTTPS**).

```text
┌─────────────────────────────────────────────────────────────┐
│                         DATABASE SERVER                     │
│                   (Microsoft SQL Server / Firebase)         │
│                                                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │  Users      │  │Transactions │  │ Tariffs             │ │
│  │  Roles      │  │Audit Logs   │  │ Notifications       │ │
│  │  Ratings    │  │Reports      │  │                     │ │
│  └─────────────┘  └─────────────┘  └─────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                    ▲                          ▲
                    │                          │
                    │   SSH / HTTPS            │   HTTPS
                    │                          │
        ┌───────────┴───────────┐  ┌──────────┴ 
        │                       │  │                
┌───────▼───────┐       ┌───────▼──────┐  
│               │       │              │  
│  WEB APP      │       │  MOBILE APP  │ 
│  (.NET MVC)   │◄─────►│  (Android)   │  
│               │       │              │ 
└───────────────┘       └──────────────┘ 
     ▲                        ▲
     │                        │
     │   Access Levels        │   Access Levels
     │                        │
┌────┴───┐               ┌───┴────┐
│        │               │        │
│ ADMIN  │               │CLIENT  │
│        │               │        │
└────────┘               └────────┘
```

---

## 🛠️ Technical Stack & Requirements

### Web Application
* **Framework:** .NET 5.0 MVC (C#)
* **UI Engine:** Bootstrap 
* **Authentication:** ASP.NET Core Identity
* **Reporting:** QuestPDF (PDF generation)
* **IDE:** Microsoft Visual Studio 2022

### Mobile Application
* **Target OS:** Android OS 7.0 Nougat (Minimum API 24)
* **Language:** Java / Kotlin
* **Architecture:** MVVM Pattern
* **Network & Storage:** Volley / Retrofit & SharedPreferences

### Database & Security
* **Relational DBMS:** Microsoft SQL Server 2019 (Alternative: Firebase Realtime DB)
* **ORM:** Entity Framework Core
* **Protocols:** SSH / HTTPS with SSL data encryption

---

## 👥 Role-Based Access Control (RBAC)

### 1. Admin (System Owner)
* Manage & authorize/de-authorize Financial Officers.
* Configure housing utility tariffs (Water, Electricity, Rent, Parking, Refuse).
* View global system audit logs and Officer service performance ratings.

### 2. Financial Officer
* Manage Client records (CRUD) and approve mobile self-registrations.
* Verify, manually override, or accept cash/digital payments.
* Generate individual statements and debt summaries.

### 3. Customer / Client (Student & Staff)
* Self-register via mobile/web and manage personal profiles.
* View current tariffs and execute simulated payments.
* Provide feedback/ratings (1-5 stars) for Financial Officers.

---

## 💾 Database Schema

The core schema architecture includes tracking for authentication, utility tariff adjustments, transactional ledgers, and live logging.

<details>
<summary>🔑 Click to view SQL Table Definitions</summary>

```sql
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    PasswordSalt NVARCHAR(50) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(15) NOT NULL,
    UserType CHAR(1) NOT NULL, -- 'A'=Admin, 'O'=Officer, 'C'=Client
    IsAuthorized BIT DEFAULT 0, 
    IsApproved BIT DEFAULT 0, 
    StudentID NVARCHAR(20) NULL, 
    EmployeeID NVARCHAR(20) NULL, 
    HouseNumber NVARCHAR(20) NOT NULL,
    RegistrationDate DATETIME DEFAULT GETDATE(),
    LastLogin DATETIME NULL,
    IsActive BIT DEFAULT 1
);

CREATE TABLE Tariffs (
    TariffID INT PRIMARY KEY IDENTITY(1,1),
    ServiceName NVARCHAR(50) NOT NULL, -- 'Water', 'Electricity', 'Rent', 'Parking', 'Refuse'
    UnitPrice DECIMAL(10,2) NOT NULL,
    UnitType NVARCHAR(20) NOT NULL, 
    EffectiveDate DATETIME DEFAULT GETDATE(),
    UpdatedBy INT FOREIGN KEY REFERENCES Users(UserID),
    IsActive BIT DEFAULT 1
);

CREATE TABLE Transactions (
    TransactionID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT FOREIGN KEY REFERENCES Users(UserID),
    TariffID INT FOREIGN KEY REFERENCES Tariffs(TariffID),
    Amount DECIMAL(10,2) NOT NULL,
    Units DECIMAL(10,2) NULL, 
    PaymentDate DATETIME DEFAULT GETDATE(),
    PaymentMethod NVARCHAR(20) DEFAULT 'Online',
    TransactionStatus CHAR(1) DEFAULT 'P', -- 'P'=Pending, 'C'=Completed, 'F'=Failed
    ReferenceNumber NVARCHAR(50) UNIQUE,
    Notes NVARCHAR(255) NULL
);
```
</details>

---

## 🚀 Setup & Installation

### 📲 Web Application Setup
1. Clone the repository and navigate to the project directory:
   ```bash
   git clone https://github.com
   cd ufs-housing-system/WebApp
   ```
2. Restore required NuGet dependencies:
   ```bash
   dotnet restore
   ```
3. Update `appsettings.json` with your localized SQL Connection String:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=UFSHousing;Trusted_Connection=True;"
   }
   ```
4. Perform database migrations and boot up the application:
   ```bash
   dotnet ef database update
   dotnet run
   ```

### 📱 Mobile Application Setup
1. Open the `/MobileApp` subdirectory inside **Android Studio**.
2. Configure your live hosting or localhost gateway inside `app/src/main/res/values/config.xml`:
   ```xml
   <string name="api_base_url">https://your-web-app-url.com</string>
   ```
3. Sync Gradle scripts and build the APK via `Build > Build APK(s)`.

---

## 📁 Repository Directory Structure

```text
Project_CSIQ3784_YourName/
├── WebApp/                # .NET 9.0 MVC Source Code
│   ├── Controllers/
│   ├── Models/
│   └── Views/
├── MobileApp/             # Android Studio Project Code Base
│   ├── app/
│   └── src/
├── Database/              # Migration Scripts & Schemes
│   ├── 01_CreateTables.sql
│   └── 02_SeedData.sql
├── Documentation/         # Architecture & User Handbooks
└── README.md              # Project Hub Guide
```

---

## 🎓 Academic Submission Details

* **Course Code:** CSIQ3784 
* **Institution:** University of the Free State (UFS) - Qwaqwa Campus

---
*Disclaimer: Designed and developed strictly as individual work for the end-of-year practical examination rubric.*
