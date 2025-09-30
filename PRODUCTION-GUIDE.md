# 🚀 Registration Portal - Production Deployment Guide

## ✅ Quick Start - After PC Restart

**Your system is configured for automatic startup!**

### Just One Click:
1. **Open browser** and go to: **http://registrationportal.local/**
2. That's it! No commands, no terminal, no manual startup needed!

**Or double-click:** `Open-Registration-Portal.bat`

---

## 📦 Initial Deployment

### One-Click Deployment:
```batch
Double-click: Deploy-To-Production.bat
```

This will:
1. Build the application in Release mode
2. Deploy to IIS automatically (requires admin approval)
3. Configure both Web App and API
4. Set up auto-startup on PC restart

### Manual Deployment Steps:
If you prefer manual control:

```powershell
# 1. Build and publish
dotnet publish -c Release -o "publish_temp" --no-self-contained

# 2. Deploy (requires admin PowerShell)
.\Deploy-Production.ps1
```

---

## 🌐 URLs and Access

| Service | URL | Description |
|---------|-----|-------------|
| **Web App** | http://registrationportal.local/ | Main user interface |
| **API** | http://api.registrationportal.local/ | Backend API service |
| **Local Web** | http://localhost/ | Alternative local access |

---

## 🔄 Auto-Startup Configuration

### What Starts Automatically:

✅ **IIS (W3SVC)**
- Windows Service
- Starts automatically with Windows
- No user interaction needed

✅ **Application Pool: RegistrationPortalPool**
- Managed by IIS
- Auto-start enabled
- Always running mode
- Idle timeout: Never

✅ **SQL Server Express**
- Windows Service
- Auto-starts with Windows
- Database ready immediately

✅ **Both Sites**
- Web App: registrationportal.local
- API: api.registrationportal.local
- Preload enabled (instant first request)

### Verify Auto-Startup Settings:

```powershell
# Check IIS service status
Get-Service W3SVC

# Check IIS sites (requires admin)
C:\Windows\system32\inetsrv\appcmd.exe list sites

# Check application pools
C:\Windows\system32\inetsrv\appcmd.exe list apppools
```

---

## 📁 File Locations

### Deployed Applications:
```
C:\inetpub\wwwroot\
├── RegistrationPortal\          # Web App files
│   ├── RegistrationPortal.dll
│   ├── appsettings.json
│   ├── wwwroot\
│   └── logs\
│
└── RegistrationPortal-API\      # API files
    ├── RegistrationPortal.dll
    ├── appsettings.json
    └── logs\
```

### Source Code:
```
e:\ClasySys\ClasysProj\RegPortalNEW\RegistrationPortal\
```

### Configuration Files:
- **Web App Config:** `C:\inetpub\wwwroot\RegistrationPortal\appsettings.json`
- **API Config:** `C:\inetpub\wwwroot\RegistrationPortal-API\appsettings.json`
- **Hosts File:** `C:\Windows\System32\drivers\etc\hosts`

---

## 🔧 Common Operations

### Update Code (After Changes):
```batch
# Option 1: One-click (easiest)
Deploy-To-Production.bat

# Option 2: Manual
dotnet publish -c Release -o "publish_temp"
.\Deploy-Production.ps1
```

### Restart IIS (After Config Changes):
```powershell
# Run as Administrator
iisreset
```

### Stop/Start Specific Site:
```powershell
# Run as Administrator
C:\Windows\system32\inetsrv\appcmd.exe stop site "RegistrationPortal"
C:\Windows\system32\inetsrv\appcmd.exe start site "RegistrationPortal"

C:\Windows\system32\inetsrv\appcmd.exe stop site "RegistrationPortal-API"
C:\Windows\system32\inetsrv\appcmd.exe start site "RegistrationPortal-API"
```

### View Application Logs:
```
Web App Logs: C:\inetpub\wwwroot\RegistrationPortal\logs\
API Logs: C:\inetpub\wwwroot\RegistrationPortal-API\logs\
```

---

## ⚙️ Configuration

### Web App Configuration:
**File:** `C:\inetpub\wwwroot\RegistrationPortal\appsettings.json`

```json
{
  "UseApiMode": true,
  "ApiBaseUrl": "http://api.registrationportal.local/",
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "ReCaptcha": {
    "SiteKey": "your-production-site-key",
    "SecretKey": "your-production-secret-key"
  }
}
```

### API Configuration:
**File:** `C:\inetpub\wwwroot\RegistrationPortal-API\appsettings.json`

```json
{
  "UseApiMode": true,
  "ApiBaseUrl": "http://api.registrationportal.local/",
  "ConnectionStrings": {
    "DefaultConnection": "..."
  }
}
```

**⚠️ Important:** After changing configuration files, restart IIS:
```powershell
iisreset
```

---

## 🐛 Troubleshooting

### Site Not Loading?

1. **Check IIS is running:**
   ```powershell
   Get-Service W3SVC
   # Should show "Running"
   ```

2. **Check sites are started:**
   ```powershell
   # Run as Administrator
   C:\Windows\system32\inetsrv\appcmd.exe list sites
   # Both should show "Started"
   ```

3. **Clear browser cache:**
   - Press `Ctrl + Shift + Delete`
   - Select "All time"
   - Clear cached images and cookies
   - Or use Incognito/Private mode

4. **Check hosts file:**
   ```
   C:\Windows\System32\drivers\etc\hosts

   Should contain:
   127.0.0.1    registrationportal.local
   127.0.0.1    api.registrationportal.local
   ```

### Database Connection Issues?

1. **Check SQL Server is running:**
   ```powershell
   Get-Service MSSQL*
   # Should show "Running"
   ```

2. **Verify connection string in appsettings.json**

3. **Check database exists:**
   ```sql
   -- In SQL Server Management Studio
   SELECT name FROM sys.databases WHERE name = 'RegistrationDB'
   ```

### Application Errors?

1. **Check application logs:**
   ```
   C:\inetpub\wwwroot\RegistrationPortal\logs\app-[date].log
   C:\inetpub\wwwroot\RegistrationPortal-API\logs\app-[date].log
   ```

2. **Check Windows Event Viewer:**
   - Open Event Viewer
   - Windows Logs > Application
   - Look for errors from "RegistrationPortal"

3. **Enable detailed errors (temporarily):**
   In `appsettings.json`:
   ```json
   "Logging": {
     "LogLevel": {
       "Default": "Debug",
       "Microsoft.AspNetCore": "Information"
     }
   }
   ```
   Then restart IIS: `iisreset`

### Performance Issues?

1. **Check application pool status:**
   ```powershell
   # Run as Administrator
   C:\Windows\system32\inetsrv\appcmd.exe list apppool "RegistrationPortalPool"
   ```

2. **Recycle application pool:**
   ```powershell
   # Run as Administrator
   C:\Windows\system32\inetsrv\appcmd.exe recycle apppool "RegistrationPortalPool"
   ```

3. **Check system resources:**
   - Open Task Manager
   - Check CPU, Memory, Disk usage
   - Look for `w3wp.exe` process

---

## 🔐 Security Best Practices

### Production Checklist:

- [ ] Update reCAPTCHA keys to production keys (not test keys)
- [ ] Use strong SQL Server passwords
- [ ] Keep `appsettings.json` secure (contains secrets)
- [ ] Enable HTTPS in production (requires SSL certificate)
- [ ] Regular Windows Updates
- [ ] Regular .NET runtime updates
- [ ] Enable firewall rules
- [ ] Regular database backups
- [ ] Monitor application logs for security events

### Backup Important Files:

```
📁 Configuration:
- C:\inetpub\wwwroot\RegistrationPortal\appsettings.json
- C:\inetpub\wwwroot\RegistrationPortal-API\appsettings.json

📁 Database:
- SQL Server backup of RegistrationDB

📁 Logs:
- C:\inetpub\wwwroot\RegistrationPortal\logs\
- C:\inetpub\wwwroot\RegistrationPortal-API\logs\
```

---

## 📊 Monitoring

### Health Checks:

```powershell
# IIS Status
Get-Service W3SVC

# SQL Server Status
Get-Service MSSQL*

# Sites Status (requires admin)
C:\Windows\system32\inetsrv\appcmd.exe list sites

# Application Pools Status (requires admin)
C:\Windows\system32\inetsrv\appcmd.exe list apppools

# Check URLs are responding
Invoke-WebRequest -Uri "http://registrationportal.local/" -UseBasicParsing
Invoke-WebRequest -Uri "http://api.registrationportal.local/health" -UseBasicParsing
```

### Log Monitoring:

Check logs regularly for:
- Failed login attempts
- Database errors
- API errors
- Security events
- CAPTCHA failures

---

## 🎯 Production Deployment Status

### ✅ Completed:

- [x] IIS configured and running
- [x] Application pool created with auto-start
- [x] Web App deployed to IIS
- [x] API deployed to IIS
- [x] Hosts file configured
- [x] Auto-startup on PC restart enabled
- [x] Database connection configured
- [x] Email service configured
- [x] reCAPTCHA integrated
- [x] Logging enabled
- [x] Security features enabled

### 🚀 Result:

**Single-click access:** http://registrationportal.local/

**Persistent:** Survives PC restart automatically

**Professional:** Enterprise-grade IIS hosting

**Full-featured:** All functionality working

---

## 📞 Quick Reference Commands

```powershell
# Restart everything (requires admin)
iisreset

# View sites
C:\Windows\system32\inetsrv\appcmd.exe list sites

# View application pools
C:\Windows\system32\inetsrv\appcmd.exe list apppools

# Stop/Start Web App
C:\Windows\system32\inetsrv\appcmd.exe stop site "RegistrationPortal"
C:\Windows\system32\inetsrv\appcmd.exe start site "RegistrationPortal"

# Stop/Start API
C:\Windows\system32\inetsrv\appcmd.exe stop site "RegistrationPortal-API"
C:\Windows\system32\inetsrv\appcmd.exe start site "RegistrationPortal-API"

# Redeploy after code changes
.\Deploy-To-Production.bat
```

---

## 🎉 Success!

Your Registration Portal is now:
- ✅ **Deployed to production IIS**
- ✅ **Accessible via clean URL**
- ✅ **Auto-starts with Windows**
- ✅ **Requires zero manual startup**
- ✅ **Professional enterprise hosting**

**Bookmark this:** http://registrationportal.local/

Enjoy your professional, production-ready application! 🚀