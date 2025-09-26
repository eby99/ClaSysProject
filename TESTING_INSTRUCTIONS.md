# Registration Portal - Testing & Verification Guide

## 🔍 Complete Testing Instructions for All Features

This guide provides comprehensive testing instructions for verifying all implemented features of the Registration Portal, including the new enhancements and fixes.

---

## 📋 Prerequisites

Before testing, ensure:

1. **Both servers are running:**
   ```bash
   # Terminal 1 - API Server
   set ASPNETCORE_ENVIRONMENT=api && dotnet run --urls "http://localhost:8085"

   # Terminal 2 - Web App
   dotnet run --urls "http://localhost:5500"
   ```

2. **Database is accessible:**
   - SQL Server Express running
   - RegistrationDB database exists with users and admin data

3. **Email Configuration (Optional):**
   - Configure SMTP settings in appsettings.json for notification testing

---

## 1. 🔐 EditProfile.cshtml Testing

### 1.1 Password Update Functionality

**Test Steps:**
1. Login as any user (e.g., ebymathew619@gmail.com)
2. Navigate to Profile page → Edit Profile
3. **Test Password Visibility Toggle:**
   - Enter new password in "New Password" field
   - Click the eye icon → Password should become visible
   - Click again → Password should be hidden
   - Repeat for "Confirm New Password" field

4. **Test Password Strength Validation:**
   - Enter weak password (e.g., "123") → Should show requirements list with red X marks
   - Enter stronger password progressively:
     - "Password1" → Some requirements should turn green with checkmarks
     - "Password123!" → All requirements should be green

5. **Test Password Confirmation:**
   - Enter different passwords in both fields → Should show error
   - Enter matching strong passwords → Should show success message

6. **Test Password Update:**
   - Fill in strong, matching passwords
   - Click "Update Profile" (should be enabled only when all validations pass)
   - Should show success message
   - Logout and login with new password → Should work

### 1.2 Inline Validation Testing

**Test Steps:**
1. In Edit Profile form, test each field:
   - **First Name:** Enter numbers → Should show inline error
   - **Email:** Enter invalid email → Should show inline error with specific message
   - **Phone:** Enter letters → Should show error, only numbers allowed
   - **Username:** Enter special characters → Should show error

2. **Button State Testing:**
   - With invalid fields → "Update Profile" button should be disabled/grayed
   - Fix all fields → Button should become enabled and blue

**Expected Results:**
- ✅ All validation messages appear inline immediately as user types
- ✅ Password requirements update in real-time
- ✅ Password visibility toggles work correctly
- ✅ Button only enables when all fields are valid
- ✅ Password actually changes in database and allows login

---

## 2. 📝 Register.cshtml Testing

### 2.1 Confirm Password Validation

**Test Steps:**
1. Navigate to Registration page
2. Fill out Step 1 fields (Name, Username, etc.)
3. Proceed to Step 2 (Email, Phone, etc.)
4. Proceed to Step 3 (Password & Security)

5. **Test Password Confirmation:**
   - Enter password in first field → Requirements should appear and update
   - Enter different password in confirm field → Should show "Passwords do not match"
   - Enter same password → Should show "Passwords match!" when both are strong
   - **Critical:** Should NOT show "password matches" incorrectly when clicking away

### 2.2 Password Visibility Toggle

**Test Steps:**
1. In password fields, click eye icons
2. Both password fields should toggle visibility independently
3. Icons should change between eye and eye-slash

### 2.3 Button State Management

**Test Steps:**
1. Navigate through registration steps
2. **Next/Create Account button should only be enabled when:**
   - All required fields in current step are filled correctly
   - No warning/error messages are present
   - Password requirements are met (Step 3)

### 2.4 Registration Success Message

**Test Steps:**
1. Complete full registration process
2. **After account creation, should display:**
   - ✅ Confirmation message that account was created successfully
   - ✅ Message indicating account will wait for admin approval
   - ✅ UI background should remain (no blank screen)
   - ✅ Footer should be hidden during success message display

**Expected Results:**
- ✅ Password confirmation works inline immediately
- ✅ No false "password matches" messages
- ✅ Password visibility toggles work
- ✅ Next/Create buttons only enable when all validations pass
- ✅ Success message displays properly with admin approval notice

---

## 3. 👨‍💼 Admin Dashboard Testing

### 3.1 Button Styling and Layout

**Test Steps:**
1. Login as admin (username: admin, password: Admin@123)
2. Navigate to Admin Dashboard
3. **Check filter buttons:**
   - Active Users, Inactive Users, All Users, Pending Approval
   - All buttons should have identical styling
   - Only selected button should be highlighted
   - Buttons should be arranged in same row, or 2 per row on smaller screens

4. **Test Responsive Layout:**
   - Resize browser window
   - On smaller screens, buttons should stack properly (2 per row)
   - Pending Approval button should not have positioning issues

### 3.2 User Display Logic

**Test Steps:**
1. **Test each filter:**
   - **Active Users:** Should show only active AND approved users
   - **Inactive Users:** Should show only inactive users (regardless of approval)
   - **All Users:** Should show all users regardless of status
   - **Pending Approval:** Should show ONLY unapproved users (not in "All Users")

2. **Verify Pending Users Display:**
   - Click "Pending Approval" → Should show only users with "Pending" status
   - Click "All Users" → Should NOT include pending approval users
   - Pending users should only appear in "Pending Approval" section

### 3.3 Permanent User Deletion

**Test Steps:**
1. In Admin Dashboard, find any user
2. Click "Delete" button
3. **Professional confirmation modal should appear with:**
   - ✅ Clear title: "Delete User Account"
   - ✅ Warning message about permanent deletion
   - ✅ User's name displayed
   - ✅ "Delete Permanently" button (red)
   - ✅ "Cancel" button
   - ✅ Professional styling similar to activate/deactivate modals

4. **Test deletion process:**
   - Click "Delete Permanently"
   - Should show loading state
   - User should be permanently removed from database
   - Should show success message
   - User should no longer appear in any list

**Expected Results:**
- ✅ All filter buttons have identical, professional styling
- ✅ Selected button is properly highlighted
- ✅ Responsive layout works correctly
- ✅ Pending users only appear in "Pending Approval" section
- ✅ Delete confirmation is professional and clear
- ✅ Users are permanently deleted (not just deactivated)

---

## 4. 🌐 IIS Hosting Testing

### 4.1 IIS Deployment Verification

**Test Steps:**
1. **Run IIS deployment script:**
   ```powershell
   # Run as Administrator
   .\Deploy-IIS.ps1 -CreateAppPools -CreateSites
   ```

2. **Verify IIS Configuration:**
   - Open IIS Manager
   - Check sites:
     - **ClaysyProject** (Web App) - Port 8081
     - **ClaysyProjectAPI** (API) - Port 8085
   - Check Application Pools:
     - **ClasysProjectPool** - Running
     - **ClasysProjectAPIPool** - Running
   - Both pools should have **.NET Version: (empty)** for .NET Core

3. **Test Web App Access:**
   - **http://localhost:8081** → Should load Registration Portal
   - **http://registrationportal.local** → Should work (if hosts file configured)

4. **Test API Access:**
   - **http://localhost:8085/api/UsersApi** → Should return JSON user data
   - **http://localhost:8085/api/UsersApi/dashboard-stats** → Should return stats

### 4.2 API Endpoint Testing (Without VS Code)

**Test using curl or PowerShell:**

```bash
# Test API connectivity
curl "http://localhost:8085/api/UsersApi"

# Test admin authentication
curl -X POST "http://localhost:8085/api/UsersApi/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"UsernameOrEmail":"admin","Password":"Admin@123"}'

# Test dashboard stats
curl "http://localhost:8085/api/UsersApi/dashboard-stats"

# Test user creation (POST)
curl -X POST "http://localhost:8085/api/UsersApi" \
  -H "Content-Type: application/json" \
  -d '{"FirstName":"Test","LastName":"User","Username":"testuser","Email":"test@example.com","Password":"Test123!","SecurityQuestion":"pet","SecurityAnswer":"fluffy","PhoneNumber":"1234567890","DateOfBirth":"1990-01-01","Country":"USA"}'

# Test forgot password
curl -X POST "http://localhost:8085/api/UsersApi/forgot-password/security-question" \
  -H "Content-Type: application/json" \
  -d '{"UsernameOrEmail":"ebymathew619@gmail.com"}'
```

**PowerShell Alternative:**
```powershell
# Test API endpoints
Invoke-RestMethod -Uri "http://localhost:8085/api/UsersApi" -Method GET
Invoke-RestMethod -Uri "http://localhost:8085/api/UsersApi/dashboard-stats" -Method GET
```

**Expected Results:**
- ✅ Both Web App and API are hosted separately in IIS
- ✅ Sites are accessible without VS Code
- ✅ All API endpoints respond correctly
- ✅ Application pools are running and configured properly

---

## 5. 📧 Windows Notification Service Testing

### 5.1 Email Configuration

**Test Steps:**
1. **Configure email settings in appsettings.json:**
   ```json
   "EmailSettings": {
     "SmtpHost": "smtp.gmail.com",
     "SmtpPort": "587",
     "EnableSsl": "true",
     "Username": "your-email@gmail.com",
     "Password": "your-app-password",
     "FromEmail": "your-email@gmail.com",
     "FromName": "Registration Portal System"
   }
   ```

2. **Configure notification service:**
   ```json
   "NotificationService": {
     "Enabled": true,
     "AdminEmail": "ebymathew142@gmail.com",
     "CheckIntervalMinutes": 1,
     "NotificationThresholdHours": 0.1,
     "NotificationIntervalHours": 1
   }
   ```

### 5.2 Service Testing

**Test Steps:**
1. **Start the application** → Background service should start automatically
2. **Create test pending users:**
   - Register new users but don't approve them as admin
   - Wait for threshold time (configured above as 6 minutes for testing)

3. **Check logs for service activity:**
   ```bash
   tail -f logs/app-<date>.log
   ```

4. **Expected email notification should contain:**
   - ✅ Professional HTML formatting
   - ✅ Number of pending users
   - ✅ Oldest pending registration date
   - ✅ Time waiting information
   - ✅ Direct link to admin dashboard
   - ✅ System information (server name, timestamp)

5. **Test admin email configuration:**
   - Should be configurable in appsettings.json
   - Should default to ebymathew142@gmail.com if not configured

### 5.3 Notification Time Frame Testing

**Test Steps:**
1. **Set short intervals for testing:**
   - CheckIntervalMinutes: 2 (check every 2 minutes)
   - NotificationThresholdHours: 0.1 (6 minutes)

2. **Create pending user and wait**
3. **Should receive email after threshold time**
4. **Verify no spam** - should not send duplicate emails within interval

**Expected Results:**
- ✅ Windows service runs automatically when application starts
- ✅ Emails sent to configurable admin email address
- ✅ Time frame is configurable and respected
- ✅ Professional email formatting with all required information
- ✅ No duplicate/spam emails

---

## 6. 🧪 Complete Integration Testing

### 6.1 End-to-End User Registration Flow

**Test Steps:**
1. **Complete Registration:**
   - Register new user with all validations
   - Verify inline validations work
   - Verify password strength and confirmation
   - Verify success message with approval notice

2. **Admin Review:**
   - Login as admin
   - Verify user appears in "Pending Approval" only
   - Test approve/reject functionality

3. **User Login:**
   - Approved user should be able to login
   - User should appear in "Active Users" after approval

### 6.2 Password Management Flow

**Test Steps:**
1. **User Profile Update:**
   - Login as approved user
   - Update password with proper validation
   - Verify password actually changes

2. **Forgot Password Flow:**
   - Use forgot password feature
   - Verify security question/answer flow

### 6.3 Admin Management Flow

**Test Steps:**
1. **User Management:**
   - Test all filter buttons and user display logic
   - Test activate/deactivate functionality
   - Test permanent user deletion

2. **System Monitoring:**
   - Verify notification service is running
   - Check logs for proper event logging
   - Verify email notifications work

**Expected Results:**
- ✅ Complete user lifecycle works end-to-end
- ✅ All validation and security features function properly
- ✅ Admin management tools work correctly
- ✅ Notification system operates as configured

---

## 🚨 Critical Test Points

### Must-Pass Tests:
1. **Password updates actually change in database**
2. **Pending users only appear in "Pending Approval", not "All Users"**
3. **Registration success message displays correctly**
4. **Delete confirmation is professional and permanent**
5. **Both Web App and API work in IIS without VS Code**
6. **Email notifications contain all required information**

### Performance Tests:
1. **Button enable/disable responds immediately to validation changes**
2. **Inline validation appears without page refresh**
3. **Password strength indicators update in real-time**

### Security Tests:
1. **Password visibility toggles don't compromise security**
2. **User deletion is truly permanent**
3. **Email notifications don't expose sensitive information**

---

## 📊 Test Results Template

Use this template to document test results:

```markdown
## Test Results - [Date]

### ✅ PASSED TESTS
- [ ] EditProfile password update functionality
- [ ] Register.cshtml password confirmation validation
- [ ] Admin Dashboard button styling and layout
- [ ] Pending Approval users display logic
- [ ] Permanent user deletion with confirmation
- [ ] IIS hosting for both Web App and API
- [ ] Windows notification service functionality

### ❌ FAILED TESTS
- Issue: [Description]
- Steps to reproduce: [Steps]
- Expected: [Expected behavior]
- Actual: [Actual behavior]

### 🔧 FIXES REQUIRED
- [List any issues that need to be addressed]

### 📝 NOTES
- [Any additional observations or recommendations]
```

---

## 🔧 Troubleshooting

### Common Issues:

1. **Email not sending:**
   - Check SMTP credentials and settings
   - Verify Gmail app password (not regular password)
   - Check firewall settings

2. **IIS sites not working:**
   - Run PowerShell as Administrator
   - Install .NET Core Hosting Bundle
   - Check Application Pool settings

3. **Inline validation not working:**
   - Check browser console for JavaScript errors
   - Verify jQuery is loaded
   - Check network tab for AJAX validation calls

4. **Password update not persisting:**
   - Check database connection string
   - Verify user service is properly routing calls
   - Check for API/service errors in logs

This comprehensive testing guide ensures all features work correctly and meet the specified requirements with absolute precision.