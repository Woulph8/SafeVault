# 🔒 SafeVault - Secure User Management System

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Entity Framework](https://img.shields.io/badge/Entity%20Framework-Core%209.0-green.svg)](https://docs.microsoft.com/en-us/ef/)
[![Security](https://img.shields.io/badge/Security-Hardened-red.svg)](#security-features)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A **production-ready**, security-hardened user management system built with ASP.NET Core 9.0. SafeVault demonstrates enterprise-grade security practices including comprehensive input validation, secure authentication, and protection against common web vulnerabilities.

## 🌟 Key Features

### 🛡️ Security-First Design
- **SQL Injection Protection** - Entity Framework Core with parameterized queries
- **XSS Prevention** - Comprehensive input sanitization and output encoding
- **CSRF Protection** - Anti-forgery tokens on all forms
- **Secure Authentication** - SHA-256 password hashing with unique salts
- **Role-Based Authorization** - Admin and User roles with proper access control
- **Security Headers** - CSP, HSTS, X-Frame-Options, and more

### 🎯 Core Functionality
- **User Registration & Login** with robust validation
- **Admin Dashboard** with real-time user statistics
- **User Profile Management** with secure password changes
- **Database Viewer** for administrators
- **Password Security Analysis** tools
- **Comprehensive Input Validation** service

### 🔧 Technical Excellence
- **Clean Architecture** with separation of concerns
- **Dependency Injection** throughout the application
- **Comprehensive Logging** with structured data
- **Responsive Design** with Bootstrap 5
- **Real-time Updates** with modern JavaScript
- **Production-Ready** error handling and validation

## 📋 Table of Contents
- [Quick Start](#-quick-start)
- [Prerequisites](#-prerequisites)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Usage](#-usage)
- [Security Features](#-security-features)
- [API Documentation](#-api-documentation)
- [Testing](#-testing)
- [Deployment](#-deployment)
- [Contributing](#-contributing)
- [License](#-license)

## 🚀 Quick Start

### Prerequisites
- **.NET 9.0 SDK** or later
- **SQL Server LocalDB** (included with Visual Studio)
- **Visual Studio 2022** or **VS Code** (recommended)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/safevault.git
   cd safevault
   ```

2. **Navigate to the project directory**
   ```bash
   cd SafeVault
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Update the database**
   ```bash
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Open your browser**
   Navigate to `https://localhost:7139` (or the URL shown in terminal)

### Default Credentials
The application seeds with default admin credentials:
- **Username**: `admin`
- **Password**: `Admin123!`

**⚠️ Important**: Change the default password immediately in production!

## ⚙️ Configuration

### Connection Strings
Update `appsettings.json` for your database:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SafeVaultDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  }
}
```

### Security Settings
The application includes secure defaults:
- **Cookie Expiration**: 8 hours with sliding expiration
- **Password Requirements**: Enforced through validation
- **HTTPS Enforcement**: Enabled by default
- **Security Headers**: Configured for XSS and clickjacking protection

## 🎯 Usage

### For Users
1. **Register** a new account with secure validation
2. **Login** with your credentials
3. **View Dashboard** with your profile information
4. **Update Profile** and change passwords securely
5. **Logout** to end your session

### For Administrators
1. **Access Admin Dashboard** with comprehensive statistics
2. **Manage Users** - view, create, and manage user accounts
3. **View System Logs** with detailed activity tracking
4. **Database Viewer** - secure access to user data
5. **Password Security Analysis** - verify hash security

## 🛡️ Security Features

SafeVault implements multiple layers of security protection:

### Input Security
- **Comprehensive Sanitization** - Custom service detects malicious patterns
- **SQL Injection Prevention** - Entity Framework parameterized queries
- **XSS Protection** - Input validation and output encoding
- **Command Injection Prevention** - Pattern detection and blocking

### Authentication & Authorization
- **Secure Password Hashing** - SHA-256 with unique salts
- **Role-Based Access Control** - Admin and User roles
- **Session Security** - HttpOnly, Secure, SameSite cookies
- **Anti-Forgery Protection** - CSRF tokens on all forms

### Infrastructure Security
- **Security Headers** - CSP, HSTS, X-Frame-Options
- **HTTPS Enforcement** - Automatic redirection
- **Error Handling** - Secure error pages without information leakage
- **Logging Security** - Structured logging without sensitive data

### Validation Patterns Detected
```csharp
// SQL Injection
"'; DROP TABLE Users; --"
"UNION SELECT * FROM"

// XSS Attacks  
"<script>alert('xss')</script>"
"javascript:alert('xss')"
"<img src=x onerror=alert('xss')>"

// Command Injection
"../../etc/passwd"
"$(rm -rf /)"
```

## 📊 API Documentation

### Authentication Endpoints
- `GET /Auth/Login` - Login page
- `POST /Auth/Login` - Process login
- `POST /Auth/Logout` - End session
- `GET /Auth/Profile` - User profile management
- `POST /Auth/ChangePassword` - Change password securely

### Admin Endpoints
- `GET /Admin/Dashboard` - Admin overview
- `GET /Admin/UserManagement` - User management interface
- `POST /Admin/Register` - Create new users
- `GET /Admin/SystemLogs` - System activity logs

### Database Endpoints
- `GET /Database/Viewer` - View user data (Admin only)
- `GET /Database/PasswordHashInfo` - Security analysis (Admin only)
- `GET /Database/ExportData` - Export user data as CSV (Admin only)

## 🧪 Testing

### Run Input Validation Tests
```bash
dotnet test --filter "Category=InputValidation"
```

### Manual Security Testing
1. **Test XSS Prevention**
   - Try entering `<script>alert('xss')</script>` in forms
   - Verify it's blocked by validation

2. **Test SQL Injection Prevention**
   - Try entering `'; DROP TABLE Users; --` in login
   - Verify it's treated as literal text

3. **Test Authentication**
   - Verify unauthorized access is blocked
   - Test session timeout functionality

### Security Analysis
Review the comprehensive security analysis:
```bash
type SECURITY_ANALYSIS.md
```

## 🚀 Deployment

### Production Checklist
- [ ] Change default admin password
- [ ] Update connection strings
- [ ] Configure HTTPS certificates
- [ ] Set up proper logging
- [ ] Enable security headers middleware
- [ ] Configure backup strategies
- [ ] Set up monitoring and alerting

### Docker Deployment
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY . .
EXPOSE 80 443
ENTRYPOINT ["dotnet", "SafeVault.dll"]
```

### IIS Deployment
1. Publish the application: `dotnet publish -c Release`
2. Copy to IIS directory
3. Configure application pool for .NET 9.0
4. Set up HTTPS bindings

## 🏗️ Architecture

### Project Structure
```
SafeVault/
├── Controllers/         # MVC Controllers
├── Models/             # Data models and ViewModels
├── Views/              # Razor views
├── Services/           # Business logic services
├── Repositories/       # Data access layer
├── Data/               # Entity Framework context
├── Middleware/         # Custom middleware
├── Tests/              # Unit and integration tests
└── wwwroot/           # Static files
```

### Design Patterns
- **Repository Pattern** - Data access abstraction
- **Dependency Injection** - Service registration and resolution
- **Model-View-Controller** - Web application architecture
- **Service Layer** - Business logic separation

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Development Setup
1. Fork the repository
2. Create a feature branch: `git checkout -b feature/new-feature`
3. Make your changes with proper tests
4. Ensure all security tests pass
5. Submit a pull request

### Coding Standards
- Follow C# coding conventions
- Include comprehensive input validation
- Add security-focused unit tests
- Document security-related changes

## 📈 Roadmap

### Upcoming Features
- [ ] Two-Factor Authentication (2FA)
- [ ] OAuth2/OpenID Connect integration
- [ ] Advanced password policies
- [ ] Audit trail enhancements
- [ ] API rate limiting
- [ ] Advanced security monitoring

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **ASP.NET Core Team** - For the excellent framework
- **Entity Framework Team** - For secure ORM capabilities
- **Security Community** - For best practices and vulnerability research


**⚠️ Security Notice**: This application has undergone comprehensive security analysis and implements industry best practices. However, always perform your own security assessment before deploying to production.

**Built with ❤️ and 🔒 by developers who care about security**
