# SmartComply: Audit & Compliance Management System

SmartComply is a web-based audit and compliance management system designed to streamline internal auditing processes for organizations. Built using ASP.NET Core MVC, the system enables users to manage audits, forms, corrective actions, and compliance categories with secure role-based access.

## Key Features

### ✅ Audit Management
- Create, edit, and delete audit entries
- Assign forms and track compliance status
- Generate QR codes for external audit viewing

### 🧾 Form & Compliance Management
- Manage form templates and compliance categories
- Assign forms to audits with specific due dates
- Submit completed audit forms

### 🛠️ Corrective Action Tracking
- Add, edit, and monitor corrective actions
- Track completion status and responsible staff
- Upload before/after evidence with image previews

### 📊 Reporting & Logs
- View audit summary and status
- Log all activities (insert, update, delete) with timestamps
- Export audit forms and actions as PDF

## 🔐 Role-Based Access

| Role    | Description                                             |
|---------|---------------------------------------------------------|
| Staff   | Fill out audit forms, respond to corrective actions     |
| Admin   | Manage audits, users, forms, categories, and compliance |
| Manager | View reports, assign forms, monitor audit progress      |

## 🧰 Tech Stack

- **Frontend:** Razor View (ASP.NET Core), Bootstrap 5
- **Backend:** ASP.NET Core MVC, Entity Framework Core
- **Database:** SQL Server (NeonDB)
- **QR Code Generator:** QRCoder


