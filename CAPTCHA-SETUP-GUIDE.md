# 🔐 CAPTCHA Setup Guide - Registration Portal

## 🎯 Current Status

Your CAPTCHA implementation is **fully functional** but currently using **Google's test keys**. These test keys show a message: *"This reCAPTCHA is for testing purposes. Please report to admin if you are seeing this."*

To remove this message and make it professional, you need to register your own reCAPTCHA keys.

---

## 🚀 Quick Setup (5 Minutes)

### **Step 1: Register Your Site**

1. Visit: **https://www.google.com/recaptcha/admin/create**
2. Sign in with your Google account
3. Fill out the registration form:

```
Label: Registration Portal - ClasySys
reCAPTCHA type: ✅ reCAPTCHA v2 → "I'm not a robot" Checkbox
Domains:
  - registrationportal.local (for local testing)
  - localhost (for development)
  - yourdomain.com (your production domain)
Owners: your-email@gmail.com
Accept reCAPTCHA Terms of Service: ✅
```

4. Click **Submit**

### **Step 2: Copy Your Keys**

After registration, Google will display:
```
Site Key: 6Lc...ABC (40 characters)
Secret Key: 6Lc...XYZ (40 characters)
```

### **Step 3: Update Configuration Files**

#### **For Development** (`appsettings.Development.json`):
```json
{
  "ReCaptcha": {
    "SiteKey": "YOUR_SITE_KEY_FROM_GOOGLE",
    "SecretKey": "YOUR_SECRET_KEY_FROM_GOOGLE"
  }
}
```

#### **For Production** (`appsettings.json`):
```json
{
  "ReCaptcha": {
    "SiteKey": "YOUR_SITE_KEY_FROM_GOOGLE",
    "SecretKey": "YOUR_SECRET_KEY_FROM_GOOGLE"
  }
}
```

#### **For IIS Production** (`C:\inetpub\wwwroot\ClaysyProject\appsettings.json`):
```json
{
  "ReCaptcha": {
    "SiteKey": "YOUR_SITE_KEY_FROM_GOOGLE",
    "SecretKey": "YOUR_SECRET_KEY_FROM_GOOGLE"
  }
}
```

### **Step 4: Restart Application**

#### **Development**:
```bash
# Stop debugging in VS Code
# Press F5 to restart
```

#### **Production (IIS)**:
```bash
# Run as Administrator
net stop w3svc
net start w3svc
```

### **Step 5: Test**

1. Open your browser in **Incognito/Private mode** (to clear cache)
2. Navigate to: `http://registrationportal.local/Account/Login`
3. You should now see the professional CAPTCHA without any test message!

---

## 🎨 What Changes After Setup?

### **Before (Test Keys):**
```
┌─────────────────────────────────┐
│ ⚠️ This reCAPTCHA is for        │
│    testing purposes. Please     │
│    report to admin if you       │
│    are seeing this.             │
├─────────────────────────────────┤
│ ☐ I'm not a robot               │
└─────────────────────────────────┘
```

### **After (Production Keys):**
```
┌─────────────────────────────────┐
│ ☐ I'm not a robot               │
│   🔒                            │
│   reCAPTCHA                     │
│   Privacy - Terms               │
└─────────────────────────────────┘
```

Clean, professional, no warning messages! ✅

---

## 📊 Current Test Keys (For Reference)

**Site Key (Public):**
```
6LeIxAcTAAAAAJcZVRqyHh71UMIEGNQ_MXjiZKhI
```

**Secret Key (Private):**
```
6LeIxAcTAAAAAGG-vFI1TnRWxMZNFuojJ4WifJWe
```

⚠️ **Important**: These are Google's official test keys. They:
- ✅ Always pass validation (development friendly)
- ⚠️ Show "testing purposes" message
- ❌ Should NOT be used in production

---

## 🔍 Troubleshooting

### **Issue: Still seeing test message after updating keys**

**Solution**:
1. Clear browser cache (Ctrl+Shift+Delete)
2. Open in Incognito/Private mode
3. Verify keys are correctly pasted (no extra spaces)
4. Restart IIS or development server

### **Issue: CAPTCHA shows "ERROR for site owner: Invalid site key"**

**Solution**:
1. Verify domain is registered in Google reCAPTCHA admin
2. Check for typos in the Site Key
3. Ensure domain matches exactly (registrationportal.local vs localhost)

### **Issue: CAPTCHA validation fails on login**

**Solution**:
1. Check Secret Key in appsettings.json
2. Verify server can reach https://www.google.com/recaptcha/api/siteverify
3. Check debug console for error messages
4. Look in logs: `logs/app-{date}.log`

### **Issue: CAPTCHA not appearing at all**

**Solution**:
1. Check browser console for JavaScript errors (F12)
2. Verify reCAPTCHA script is loading: `https://www.google.com/recaptcha/api.js`
3. Check firewall isn't blocking Google reCAPTCHA
4. Ensure Site Key is present in configuration

---

## 🎯 Files That Use CAPTCHA

### **Views:**
- ✅ `Views/Account/Login.cshtml` - Login page
- ✅ `Views/Account/Register.cshtml` - Registration page (if implemented)
- ✅ `Views/Admin/Index.cshtml` - Admin login (if implemented)

### **Controllers:**
- ✅ `Controllers/AccountController.cs` - Login validation
- ✅ `Controllers/AccountController.cs` - Register validation
- ✅ `Controllers/AdminController.cs` - Admin login validation

### **Services:**
- ✅ `Services/CaptchaService.cs` - CAPTCHA validation logic

### **Configuration:**
- ✅ `appsettings.json` - Production settings
- ✅ `appsettings.Development.json` - Development settings
- ✅ `Program.cs` - Service registration

---

## 🔒 Security Best Practices

### **✅ DO:**
- Use different keys for development and production
- Keep Secret Key private (never commit to public repos)
- Register all domains you'll use (including localhost)
- Monitor failed CAPTCHA attempts in logs
- Update keys if compromised

### **❌ DON'T:**
- Share Secret Key publicly
- Use test keys in production
- Hardcode keys in code (use configuration files)
- Disable CAPTCHA validation in production
- Ignore failed CAPTCHA logs

---

## 📈 Monitoring CAPTCHA

### **Check Logs for CAPTCHA Events:**

```bash
# Development
tail -f logs/app-{today}.log | grep CAPTCHA

# Production
Get-Content C:\inetpub\wwwroot\ClaysyProject\logs\app-{today}.log | Select-String "CAPTCHA"
```

### **Log Messages:**
```
✅ CAPTCHA: Verification successful for IP: 192.168.1.100
⚠️ CAPTCHA: Verification failed. Errors: invalid-input-response
🔍 CAPTCHA: Verifying token from IP: 192.168.1.100
🐛 DEV CAPTCHA: Auto-accepting for development (IP: 127.0.0.1)
```

---

## 🎓 Advanced Configuration

### **Environment-Specific Settings:**

**Development (Auto-Accept):**
```csharp
// In Program.cs
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<ICaptchaService, DevelopmentCaptchaService>();
}
```

**Production (Real Validation):**
```csharp
// In Program.cs
else
{
    builder.Services.AddHttpClient<ICaptchaService, GoogleReCaptchaService>();
}
```

### **Custom Validation:**

You can adjust validation logic in `Services/CaptchaService.cs`:
```csharp
public async Task<bool> VerifyTokenAsync(string token, string? userIpAddress = null)
{
    // Custom validation logic here
    // Check IP addresses, rate limiting, etc.
}
```

---

## 📞 Support

### **Google reCAPTCHA Resources:**
- **Admin Console**: https://www.google.com/recaptcha/admin
- **Documentation**: https://developers.google.com/recaptcha/docs/display
- **Support**: https://support.google.com/recaptcha/

### **Registration Portal Help:**
- Check logs: `logs/app-{date}.log`
- Debug console: Press F12 in browser
- Event logs: Windows Event Viewer → Application

---

## ✅ Checklist

Before going to production, ensure:

- [ ] Registered reCAPTCHA keys from Google
- [ ] Updated `appsettings.json` with production keys
- [ ] Updated IIS production `appsettings.json`
- [ ] Registered production domain in reCAPTCHA admin
- [ ] Tested CAPTCHA in incognito mode
- [ ] Verified no "testing purposes" message appears
- [ ] Tested failed login with wrong CAPTCHA
- [ ] Checked logs for CAPTCHA events
- [ ] Documented keys securely (not in public repos)
- [ ] Configured monitoring for failed attempts

---

**🎉 Once completed, your CAPTCHA will look 100% professional with no test messages!**