# 🚀 Registration Portal - START HERE

## ✅ Production Deployment Complete!

Your Registration Portal is now deployed to IIS and configured for automatic startup.

---

## 🌐 Access Your Application

### **Single-Click Access:**

**Option 1:** Open browser and go to:
```
http://registrationportal.local/
```

**Option 2:** Double-click this file:
```
Open-Registration-Portal.bat
```

### **That's it!** No commands, no terminal, no manual startup needed! 🎉

---

## 🔄 After PC Restart

**Your system is configured for AUTO-STARTUP!**

When you restart your PC:

✅ **IIS starts automatically** - Windows Service
✅ **Application pool starts** - Always running mode
✅ **Both sites load immediately** - Preload enabled
✅ **Database ready** - SQL Server auto-starts

**Just open the browser and go to:** `http://registrationportal.local/`

**No manual commands required!**

---

## 📦 Deployment Files

| File | Purpose |
|------|---------|
| `Open-Registration-Portal.bat` | Quick shortcut to open the app in browser |
| `Deploy-To-Production.bat` | One-click deployment (after code changes) |
| `Deploy-Production.ps1` | Detailed deployment script |
| `PRODUCTION-GUIDE.md` | Complete production guide |
| `CAPTCHA-SETUP-GUIDE.md` | reCAPTCHA configuration |
| `DEBUGGING-GUIDE.md` | VS Code debugging guide |

---

## 🔧 After Making Code Changes

When you update the code and need to redeploy:

```batch
# Double-click this file:
Deploy-To-Production.bat
```

This will:
1. Build your application
2. Deploy to IIS
3. Restart services
4. Done!

---

## 📁 Production Locations

### Deployed Applications:
```
C:\inetpub\wwwroot\
├── RegistrationPortal\          # Web App
└── RegistrationPortal-API\      # API
```

### URLs:
- **Web App:** http://registrationportal.local/
- **API:** http://api.registrationportal.local/

### Configuration Files:
- **Web App:** `C:\inetpub\wwwroot\RegistrationPortal\appsettings.json`
- **API:** `C:\inetpub\wwwroot\RegistrationPortal-API\appsettings.json`

### Logs:
- **Web App:** `C:\inetpub\wwwroot\RegistrationPortal\logs\`
- **API:** `C:\inetpub\wwwroot\RegistrationPortal-API\logs\`

---

## 🎯 Quick Commands

### Restart IIS (if needed):
```powershell
# Run PowerShell as Administrator
iisreset
```

### View Site Status:
```powershell
# Run PowerShell as Administrator
C:\Windows\system32\inetsrv\appcmd.exe list sites
```

### Check Services:
```powershell
Get-Service W3SVC     # IIS
Get-Service MSSQL*    # SQL Server
```

---

## ❓ Troubleshooting

### Site not loading?

1. **Clear browser cache:**
   - Press `Ctrl + Shift + Delete`
   - Select "All time"
   - Clear cached images and cookies
   - Restart browser

2. **Or use Incognito/Private mode:**
   - `Ctrl + Shift + N` (Chrome)
   - `Ctrl + Shift + P` (Edge/Firefox)

3. **Check IIS is running:**
   ```powershell
   Get-Service W3SVC
   # Should show "Running"
   ```

4. **Restart IIS:**
   ```powershell
   # Run PowerShell as Administrator
   iisreset
   ```

### Need more help?

See detailed troubleshooting in: **[PRODUCTION-GUIDE.md](PRODUCTION-GUIDE.md)**

---

## 🎉 You're All Set!

Your application is:
- ✅ **Deployed to production IIS**
- ✅ **Accessible via clean URL**
- ✅ **Auto-starts with Windows**
- ✅ **Requires zero manual startup**
- ✅ **Professional enterprise hosting**

### **Bookmark this URL:**
# http://registrationportal.local/

**Enjoy your production application! 🚀**

---

## 📚 Additional Documentation

- **[PRODUCTION-GUIDE.md](PRODUCTION-GUIDE.md)** - Complete production operations guide
- **[DEBUGGING-GUIDE.md](DEBUGGING-GUIDE.md)** - VS Code debugging setup
- **[CAPTCHA-SETUP-GUIDE.md](CAPTCHA-SETUP-GUIDE.md)** - reCAPTCHA configuration
- **[TESTING_INSTRUCTIONS.md](TESTING_INSTRUCTIONS.md)** - Testing workflows