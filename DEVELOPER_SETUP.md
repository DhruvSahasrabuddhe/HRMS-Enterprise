# HRMS Enterprise - Developer Setup Guide

## Quick Start for Development

### 1. Clone the Repository
```bash
git clone <repository-url>
cd HRMS-Enterprise
```

### 2. Configure User Secrets for Encryption

The application requires encryption keys to be configured. **Never commit these to source control.**

```bash
cd src/HRMS.Web
dotnet user-secrets init  # Initialize user secrets (only needed once)
dotnet user-secrets set "Encryption:Key" "w40hGa4On6BuQt3NL/NwMmZnzTXIwPv8HiZT/dPRgII="
dotnet user-secrets set "Encryption:IV" "AabOjmbODg1xfqpnvgtJ/A=="
```

> **Note**: The keys above are example development keys. For production, generate your own keys using the script in SECURITY.md.

### 3. Build the Solution
```bash
dotnet build
```

### 4. Run Database Migrations
```bash
cd src/HRMS.Web
dotnet ef database update
```

Or simply run the application - it will auto-migrate on startup.

### 5. Run the Application
```bash
cd src/HRMS.Web
dotnet run
```

### 6. Get Default Admin Credentials

On first run, the application will create a default admin user and display the credentials in the console output:

```
=============================================================================
DEFAULT ADMIN CREDENTIALS CREATED - CHANGE IMMEDIATELY ON FIRST LOGIN!
Email: admin@hrms.com
Password: <randomly-generated-password>
=============================================================================
```

**Copy the password immediately** - it's only shown once!

### 7. Login and Change Password
1. Navigate to the login page
2. Login with the admin credentials
3. Change the password immediately

## Running Tests

### Unit Tests
```bash
dotnet test tests/HRMS.UnitTests/HRMS.UnitTests.csproj
```

### Integration Tests
```bash
dotnet test tests/HRMS.IntegrationTests/HRMS.IntegrationTests.csproj
```

### All Tests
```bash
dotnet test
```

## Security Features Implemented

✅ **Authentication & Authorization**
- Strong password policies (12+ chars, complexity requirements)
- Account lockout after 5 failed attempts
- Role-based access control (Admin, HR, Manager, Employee)
- Automatic admin user seeding

✅ **Data Protection**
- AES-256 encryption for sensitive PII
- Data masking for sensitive fields
- Secure key management via user secrets/environment variables

✅ **Security Headers**
- X-Frame-Options (clickjacking prevention)
- X-Content-Type-Options (MIME sniffing prevention)
- Content-Security-Policy (XSS mitigation)
- X-XSS-Protection
- Referrer-Policy

✅ **Input Validation**
- FluentValidation with XSS protection
- Script injection detection
- File upload validation (size, type, MIME)
- Path traversal detection

✅ **Logging & Monitoring**
- Request logging with IP tracking
- User activity logging
- Audit trail for all data changes

## Common Issues

### "Encryption key and IV must be configured"
You need to set up user secrets. Follow step 2 above.

### "Unable to create database"
Make sure SQL Server LocalDB is installed, or update the connection string in `appsettings.json`.

### Tests failing with encryption errors
Make sure you've run `dotnet user-secrets` commands in the Web project.

## Project Structure

```
HRMS-Enterprise/
├── src/
│   ├── HRMS.Core/           # Domain entities, interfaces, value objects
│   ├── HRMS.Infrastructure/ # Data access, repositories, services
│   ├── HRMS.Services/       # Application services, DTOs, validators
│   ├── HRMS.Shared/         # Shared constants and utilities
│   └── HRMS.Web/            # ASP.NET Core MVC application
├── tests/
│   ├── HRMS.UnitTests/      # Unit tests
│   └── HRMS.IntegrationTests/ # Integration tests
└── SECURITY.md              # Security documentation
```

## Learn More

- [Security Documentation](SECURITY.md) - Comprehensive security guide
- [Architecture](docs/architecture.md) - System architecture (if exists)
- [Contributing](CONTRIBUTING.md) - Contribution guidelines (if exists)

## Support

For issues or questions:
1. Check existing GitHub issues
2. Create a new issue with details
3. Contact the development team
