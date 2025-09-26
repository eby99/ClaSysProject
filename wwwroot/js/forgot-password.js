let currentUserId = null;
let resetToken = null;

function showMessage(message, type) {
    const messageDiv = $('#messageDiv');
    messageDiv.removeClass('success error').addClass(type).text(message).fadeIn();
}

function goToStep(stepNumber) {
    $('.step').removeClass('active');
    $(`#step${stepNumber}`).addClass('active');
    $('#messageDiv').fadeOut();
}

// Step 1: Find account
$('#usernameForm').on('submit', function(e) {
    e.preventDefault();
    const usernameEmail = $('#usernameEmail').val().trim();

    if (!usernameEmail) {
        showMessage('Please enter username or email', 'error');
        return;
    }

    $.post('/Account/GetSecurityQuestion', {
        usernameOrEmail: usernameEmail,
        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
    })
    .done(function(response) {
        if (response.success) {
            currentUserId = response.userId;
            $('#securityQuestion').text(response.securityQuestion);
            goToStep(2);
        } else {
            showMessage(response.message, 'error');
        }
    })
    .fail(function() {
        showMessage('An error occurred. Please try again.', 'error');
    });
});

// Step 2: Verify security answer
$('#securityForm').on('submit', function(e) {
    e.preventDefault();
    const securityAnswer = $('#securityAnswer').val().trim();

    if (!securityAnswer) {
        showMessage('Please enter your security answer', 'error');
        return;
    }

    $.post('/Account/VerifySecurityAnswer', {
        userId: currentUserId,
        securityAnswer: securityAnswer,
        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
    })
    .done(function(response) {
        if (response.success) {
            resetToken = response.resetToken;
            showMessage('Security answer verified! Please set your new password.', 'success');
            setTimeout(() => goToStep(3), 1500);
        } else {
            showMessage(response.message, 'error');
        }
    })
    .fail(function() {
        showMessage('An error occurred. Please try again.', 'error');
    });
});

// Step 3: Reset password
$('#resetForm').on('submit', function(e) {
    e.preventDefault();
    const newPassword = $('#newPassword').val();
    const confirmPassword = $('#confirmPassword').val();

    if (newPassword !== confirmPassword) {
        showMessage('Passwords do not match', 'error');
        return;
    }

    if (newPassword.length < 8) {
        showMessage('Password must be at least 8 characters long', 'error');
        return;
    }

    $.post('/Account/ResetPassword', {
        resetToken: resetToken,
        newPassword: newPassword,
        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
    })
    .done(function(response) {
        if (response.success) {
            showMessage('Password reset successfully! Redirecting to login...', 'success');
            setTimeout(() => {
                window.location.href = '/Account/Login';
            }, 2000);
        } else {
            showMessage(response.message, 'error');
        }
    })
    .fail(function() {
        showMessage('An error occurred. Please try again.', 'error');
    });
});