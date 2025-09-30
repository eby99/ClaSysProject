# 🔧 CAPTCHA Not Showing in Production - Fix Guide

## ❌ Problem:
CAPTCHA widget appears in development (localhost) but **NOT in production** (registrationportal.local)

## ✅ Root Cause:
Google reCAPTCHA is **blocking the domain** because `registrationportal.local` is not registered in your reCAPTCHA configuration.

---

## 🔧 Solution: Add Domain to Google reCAPTCHA

### Step 1: Open Google reCAPTCHA Admin

1. Go to: **https://www.google.com/recaptcha/admin**
2. Log in with your Google account
3. Find your reCAPTCHA site: **"RegPortal"**
4. Click on the **Settings gear icon** or click the site name to edit

### Step 2: Check Current Domains

You'll see a **"Domains"** section showing your current domains:
```
localhost
127.0.0.1
```

### Step 3: Add Production Domain

**Add this line** to the domains list:
```
registrationportal.local
```

**Final domains list should be:**
```
registrationportal.local
localhost
127.0.0.1
```

**Important:**
- Add **each domain on a separate line**
- Do NOT use `http://` or `https://`
- Just the domain name: `registrationportal.local`

### Step 4: Save Changes

Click the **"Save"** button at the bottom.

### Step 5: Clear Browser Cache

After saving in Google reCAPTCHA:

**Option 1: Clear Cache**
1. Press `Ctrl + Shift + Delete`
2. Select "All time" or "Everything"
3. Check: "Cached images and files" + "Cookies and site data"
4. Click "Clear data"
5. Close and reopen browser

**Option 2: Use Incognito Mode (Faster)**
1. Press `Ctrl + Shift + N` (Chrome) or `Ctrl + Shift + P` (Edge/Firefox)
2. Go to: `http://registrationportal.local/`
3. Navigate to Login page

### Step 6: Test CAPTCHA

1. Open: http://registrationportal.local/
2. Click "Login" or go to login page
3. **CAPTCHA widget should now appear!**
4. You should see the "I'm not a robot" checkbox

---

## 🎯 Why This Happens

Google reCAPTCHA has **domain validation** for security:
- It checks which domain is requesting the CAPTCHA
- If domain is not in the registered list, it **blocks the widget**
- This prevents unauthorized use of your reCAPTCHA keys

**Your current registration had:**
- ✅ `localhost` - Works in development
- ✅ `127.0.0.1` - Works with IP
- ❌ `registrationportal.local` - **MISSING** - Blocks production

---

## 📸 Visual Guide - Google reCAPTCHA Admin

When you open the reCAPTCHA admin page, look for:

```
┌────────────────────────────────────────────────────┐
│ RegPortal                                          │
│ ┌────────────────────────────────────────────┐   │
│ │ Label: RegPortal                           │   │
│ │ Type: reCAPTCHA v2 (Checkbox)             │   │
│ │                                            │   │
│ │ Domains:                                   │   │
│ │ ┌────────────────────────────────────┐    │   │
│ │ │ registrationportal.local           │    │   │  ← ADD THIS
│ │ │ localhost                          │    │   │
│ │ │ 127.0.0.1                         │    │   │
│ │ └────────────────────────────────────┘    │   │
│ │                                            │   │
│ │ Site Key: 6LdOfNkrAAAAAEDk0jlBIRLFE8... │   │
│ │ Secret Key: 6LdOfNkrAAAAAAy90XhCgJVUQ...│   │
│ │                                            │   │
│ │ [Save]                                     │   │  ← CLICK SAVE
│ └────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────┘
```

---

## ✅ Verification

After adding the domain and clearing cache:

**Test Checklist:**
- [ ] Go to http://registrationportal.local/
- [ ] Navigate to Login page
- [ ] CAPTCHA widget appears (checkbox visible)
- [ ] Can check "I'm not a robot"
- [ ] Submit button enables after CAPTCHA completion
- [ ] Login works successfully

---

## 🐛 Still Not Working?

### Check Browser Console (F12):

1. Press `F12` to open Developer Tools
2. Go to **"Console"** tab
3. Reload the page
4. Look for reCAPTCHA errors

**Common errors:**

**Error 1:**
```
ERROR for site owner: Invalid domain for site key
```
**Fix:** Domain not added yet - follow steps above

**Error 2:**
```
reCAPTCHA placeholder element must be an element or id
```
**Fix:** View file issue - shouldn't happen, views are compiled correctly

**Error 3:**
```
Failed to load resource: net::ERR_NAME_NOT_RESOLVED
```
**Fix:** DNS issue - check hosts file has `127.0.0.1    registrationportal.local`

### Verify Hosts File:

```powershell
# Run in PowerShell
Get-Content C:\Windows\System32\drivers\etc\hosts
```

Should contain:
```
127.0.0.1    registrationportal.local
127.0.0.1    api.registrationportal.local
```

### Restart IIS (if needed):

```powershell
# Run PowerShell as Administrator
iisreset
```

---

## 📞 Quick Reference

| Item | Value |
|------|-------|
| **reCAPTCHA Admin** | https://www.google.com/recaptcha/admin |
| **Site Name** | RegPortal |
| **Production Domain** | registrationportal.local |
| **Site Key** | 6LdOfNkrAAAAAEDk0jlBIRLFE8uiXmYpIW9pIuTj |
| **Production URL** | http://registrationportal.local/ |

---

## 🎉 After Fix

Once you add `registrationportal.local` to Google reCAPTCHA:

✅ CAPTCHA will work in **production** (registrationportal.local)
✅ CAPTCHA will still work in **development** (localhost)
✅ Same keys work for **all registered domains**
✅ No code changes needed
✅ No redeployment needed

**Just add the domain in Google reCAPTCHA admin and clear browser cache!**

---

## 💡 Pro Tip

Bookmark the Google reCAPTCHA admin page for easy access:
**https://www.google.com/recaptcha/admin**

This way you can quickly add new domains if you deploy to additional environments later.