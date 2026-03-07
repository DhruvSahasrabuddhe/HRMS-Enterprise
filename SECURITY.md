# Security Configuration Guide

## Overview
This document outlines the security enhancements implemented in the HRMS Enterprise application.

## 1. Authentication & Authorization

### Password Policy
The application enforces strong password requirements:
- Minimum length: 12 characters
- Must contain: uppercase, lowercase, digit, and special character
- Minimum unique characters: 4

### Account Lockout
To prevent brute-force attacks:
- Maximum failed login attempts: 5
- Lockout duration: 15 minutes
- Applies to all users including new accounts

### Default Credentials
A default admin account is created on first run with a randomly generated secure password:
- **Email**: admin@hrms.com
- **Password**: *Displayed in console logs on first run*
- ⚠️ **IMPORTANT**: The password is randomly generated and shown only once in the logs. Copy it immediately and change it after first login!

### Roles
The following roles are available:
- **Admin**: Full system access
- **HR**: Human resources operations
- **Manager**: Team management
- **Employee**: Basic employee access

## 2. Data Protection

### Field-Level Encryption
Sensitive fields are encrypted at rest using AES-256:
- National ID
- Passport Number
- Bank Account Number

### Encryption Key Management
Encryption keys should **NEVER** be committed to source control.

For **development**, use .NET User Secrets:
```bash
cd src/HRMS.Web
dotnet user-secrets set "Encryption:Key" "<your-32-byte-base64-key>"
dotnet user-secrets set "Encryption:IV" "<your-16-byte-base64-iv>"
```

For **production**, use one of these secure methods:

#### Option 1: Environment Variables
```bash
export Encryption__Key="<your-32-byte-base64-key>"
export Encryption__IV="<your-16-byte-base64-iv>"
```

#### Option 2: Azure Key Vault (recommended for production)
Configure Azure Key Vault in `Program.cs`:
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

### Generating Encryption Keys
Use this script to generate secure keys:
```bash
python3 -c "import base64; import os; print('Key:', base64.b64encode(os.urandom(32)).decode()); print('IV:', base64.b64encode(os.urandom(16)).decode())"
```

Or use PowerShell:
```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
[Convert]::ToBase64String((1..16 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

### Data Masking
Sensitive data is masked in API responses for non-admin users:
- Shows first 2 and last 2 characters
- Example: "1234567890" → "12******90"

## 3. Security Headers

The following security headers are automatically added to all responses:

- **X-Frame-Options**: DENY (prevents clickjacking)
- **X-Content-Type-Options**: nosniff (prevents MIME sniffing)
- **X-XSS-Protection**: 1; mode=block (enables XSS filter)
- **Content-Security-Policy**: Restricts resource loading
- **Referrer-Policy**: strict-origin-when-cross-origin
- **Permissions-Policy**: Restricts browser features

## 4. Request Logging

All HTTP requests are logged with:
- IP address
- User agent
- Request method and path
- Response status code
- Response time
- Authenticated user

Warnings are logged for:
- Slow requests (> 5 seconds)
- HTTP errors (4xx, 5xx)

## 5. Input Validation

FluentValidation is used for comprehensive input validation:
- Email format validation
- Phone number format validation
- Length constraints
- Required field validation
- Custom business rule validation

## 6. CSRF Protection

Anti-forgery tokens are validated on all state-changing operations:
- POST requests
- PUT requests
- DELETE requests

Enabled via `[ValidateAntiForgeryToken]` attribute on controllers.

## 7. SQL Injection Prevention

- Uses Entity Framework Core with parameterized queries
- No raw SQL execution
- LINQ-based data access

## 8. HTTPS/TLS Configuration

- HTTPS redirection enabled for all requests
- HSTS (HTTP Strict Transport Security) enabled in production
- Enforces secure connections

## 9. Allowed Hosts

Production configuration should restrict allowed hosts:
```json
"AllowedHosts": "yourdomain.com;www.yourdomain.com"
```

## 10. Security Best Practices

### For Developers
1. Never commit encryption keys to source control
2. Use user secrets for local development
3. Rotate encryption keys periodically
4. Review security logs regularly
5. Keep dependencies up to date

### For Administrators
1. Change default admin password immediately
2. Configure encryption keys in production
3. Set up proper allowed hosts
4. Enable HTTPS/TLS certificates
5. Monitor failed login attempts
6. Review audit logs regularly

## 11. Security Checklist

Before deploying to production:

- [ ] Change default admin password
- [ ] Configure production encryption keys (use Azure Key Vault)
- [ ] Set proper AllowedHosts
- [ ] Enable SSL/TLS certificates
- [ ] Configure production connection strings
- [ ] Review and test password policies
- [ ] Set up monitoring and alerting
- [ ] Configure backup and disaster recovery
- [ ] Perform security testing
- [ ] Review all user roles and permissions

## 12. Compliance Considerations

This implementation provides foundational security controls. For regulatory compliance (GDPR, HIPAA, etc.), additional measures may be required:
- Data retention policies
- Right to erasure implementation
- Consent management
- Data breach notification procedures
- Privacy impact assessments

## Support

For security-related issues or questions, contact the security team or raise an issue in the repository.
