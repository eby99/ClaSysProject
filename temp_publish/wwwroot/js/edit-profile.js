// Edit Profile page JavaScript functionality
$(document).ready(function() {
    let validationStatus = {};
    let uniqueValidationCache = {};
    let isSubmitting = false;
    const currentUserId = $('#UserID').val();

    // Initialize
    initializeValidation();
    initializeAgeRestrictions();
    
    // Hide all validation messages on page load - FIX for premature display
    hideAllValidationMessages();

    // Bio character counter
    $('#Bio').on('input', function() {
        const count = $(this).val().length;
        const maxLength = 1000;
        $('.bio-counter').text(`${count}/${maxLength} characters`);

        if (count >= maxLength) {
            $('.bio-counter').css('color', '#ff6b6b');
        } else {
            $('.bio-counter').css('color', 'var(--text-muted)');
        }
    });

    // Initialize bio counter
    $('#Bio').trigger('input');

    // Mark fields as touched when user starts interacting
    $('input, select, textarea').on('focus', function() {
        $(this).data('touched', true);
    });

    // Field validation handlers
    $('input, select, textarea').on('blur change paste keyup', function() {
        const $field = $(this);
        const fieldId = $field.attr('id') || $field.attr('name');
        
        // Only validate if field has been touched
        if ($field.data('touched')) {
            console.log('Field validation triggered for:', fieldId, 'Value:', $field.val());

            setTimeout(function() {
                validateField($field);
                updateSubmitButton();
            }, 100);
        }
    });

    // Real-time input handlers for formatting
    $('input[data-validation]').on('input', function() {
        const $field = $(this);
        const validationType = $field.data('validation');

        if (validationType === 'phone') {
            let value = $field.val().replace(/\D/g, '');
            if (value.length > 10) value = value.slice(0, 10);
            $field.val(value);
        } else if (validationType === 'zipcode') {
            let value = $field.val().replace(/\D/g, '');
            if (value.length > 6) value = value.slice(0, 6);
            $field.val(value);
        } else if (validationType === 'alphabets') {
            $field.val($field.val().replace(/[^a-zA-Z]/g, ''));
        } else if (validationType === 'alphabets-spaces') {
            $field.val($field.val().replace(/[^a-zA-Z\s]/g, ''));
        }
    });

    // Form submission
    $('#editProfileForm').submit(function(e) {
        e.preventDefault();

        if (isSubmitting) return;

        if (validateAllFields()) {
            isSubmitting = true;
            showLoading();
            submitForm();
        } else {
            showError('Please fix all validation errors before submitting.');
        }
    });

    // Hide all validation messages on initial load - FIX
    function hideAllValidationMessages() {
        $('.validation-message').removeClass('show').hide();
        $('.input-group-custom').removeClass('field-valid field-error');
        $('.inline-validation span').addClass('d-none');
    }

    function initializeValidation() {
        $('input, select, textarea').each(function() {
            const $field = $(this);
            const fieldId = $field.attr('id') || $field.attr('name');
            if (fieldId && fieldId !== 'UserID') {
                const isRequired = $field.prop('required');
                const hasValue = $field.val() && $field.val().trim();

                if (isRequired) {
                    validationStatus[fieldId] = hasValue ? true : false;
                } else {
                    // Non-required fields are valid by default if empty or have valid value
                    validationStatus[fieldId] = hasValue ? true : true;
                }

                // DON'T trigger validation for fields with existing values on page load
                // Only validate when user interacts with the field
            }
        });
    }

    function initializeAgeRestrictions() {
        const today = new Date();
        const minDate = new Date(today.getFullYear() - 80, today.getMonth(), today.getDate());
        const maxDate = new Date(today.getFullYear() - 18, today.getMonth(), today.getDate());

        $('#DateOfBirth').attr('min', minDate.toISOString().split('T')[0]);
        $('#DateOfBirth').attr('max', maxDate.toISOString().split('T')[0]);
    }

    function validateField($field) {
        const fieldId = $field.attr('id') || $field.attr('name');
        const validationType = $field.data('validation');
        const value = $field.val().trim();
        const isRequired = $field.prop('required');
        const $container = $field.closest('.input-group-custom');
        const $errorMsg = $container.find('.error-message');
        const $successMsg = $container.find('.success-message');

        let isValid = true;
        let errorMessage = '';

        // Clear previous states
        $container.removeClass('field-valid field-error');
        $errorMsg.removeClass('show').hide();
        $successMsg.removeClass('show').hide();

        // Required field validation
        if (isRequired && !value && $field.data('touched')) {
            isValid = false;
            errorMessage = 'This field is required';
        } else if (value) {
            // Field-specific validations
            switch (validationType) {
                case 'alphabets':
                    if (!/^[a-zA-Z]+$/.test(value)) {
                        isValid = false;
                        errorMessage = 'Only alphabets are allowed, no spaces';
                    }
                    break;
                case 'alphabets-spaces':
                    if (!/^[a-zA-Z\s]+$/.test(value)) {
                        isValid = false;
                        errorMessage = 'Only alphabets and spaces are allowed';
                    }
                    break;
                case 'username':
                    if (!/^[a-zA-Z0-9_]{3,30}$/.test(value)) {
                        isValid = false;
                        errorMessage = '3-30 characters, letters, numbers and underscore only';
                    } else {
                        checkUniqueness('username', value, $field, currentUserId);
                    }
                    break;
                case 'email':
                    // Very strict email validation
                    const emailRegex = /^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?@[a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?\.[a-zA-Z]{2,4}$/;

                    // Additional validation rules
                    const hasValidLength = value.length >= 5 && value.length <= 254;
                    const hasValidDomain = value.includes('@') && value.split('@').length === 2;
                    const domainPart = value.includes('@') ? value.split('@')[1] : '';
                    const domainParts = domainPart.split('.');
                    const hasValidDomainExtension = domainPart.includes('.') &&
                                                   domainParts.length >= 2 &&
                                                   domainParts[domainParts.length - 1].length >= 2 &&
                                                   domainParts[domainParts.length - 1].length <= 4;

                    // Check for invalid patterns like multiple consecutive dots or ending with invalid characters
                    const hasInvalidPatterns = value.includes('..') ||
                                              value.endsWith('.') ||
                                              value.includes('@.') ||
                                              /[^a-zA-Z0-9._@-]/.test(value) ||
                                              domainPart.startsWith('.') ||
                                              domainPart.endsWith('.');

                    const emailValid = emailRegex.test(value) &&
                                      hasValidLength &&
                                      hasValidDomain &&
                                      hasValidDomainExtension &&
                                      !hasInvalidPatterns;

                    if (!emailValid) {
                        isValid = false;
                        if (!hasValidDomain) {
                            errorMessage = 'Email must contain exactly one @ symbol';
                        } else if (!hasValidDomainExtension) {
                            errorMessage = 'Email must have a valid domain extension (2-4 letters)';
                        } else if (hasInvalidPatterns) {
                            errorMessage = 'Email contains invalid characters or patterns';
                        } else {
                            errorMessage = 'Please enter a valid email address (e.g., user@domain.com)';
                        }
                    } else {
                        checkUniqueness('email', value, $field, currentUserId);
                    }
                    break;
                case 'phone':
                    if (!/^\d{10}$/.test(value)) {
                        isValid = false;
                        errorMessage = 'Phone number must be exactly 10 digits';
                    } else {
                        checkUniqueness('phone', value, $field, currentUserId);
                    }
                    break;
                case 'zipcode':
                    if (!/^\d{6}$/.test(value)) {
                        isValid = false;
                        errorMessage = 'ZIP code must be exactly 6 digits';
                    }
                    break;
                case 'age':
                    const birthDate = new Date(value);
                    const today = new Date();
                    let age = today.getFullYear() - birthDate.getFullYear();
                    const monthDiff = today.getMonth() - birthDate.getMonth();

                    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
                        age--;
                    }

                    if (age < 18 || age > 80) {
                        isValid = false;
                        errorMessage = 'You must be between 18 and 80 years old';
                    }
                    break;
                case 'password':
                    const passwordResult = validatePasswordStrength(value);
                    isValid = passwordResult.isValid;
                    if (!isValid) {
                        errorMessage = passwordResult.message;
                    }
                    break;
                case 'confirm-password':
                    const newPassword = $('#NewPassword').val();
                    if (newPassword && value !== newPassword) {
                        isValid = false;
                        errorMessage = 'Password confirmation does not match';
                    } else if (newPassword && value === newPassword) {
                        // Both passwords match, but check if the main password is strong
                        const passwordStrengthResult = validatePasswordStrength(newPassword);
                        if (!passwordStrengthResult.isValid) {
                            isValid = false;
                            errorMessage = 'Password must meet all strength requirements first';
                        }
                    }
                    break;
            }
        }

        // Update validation status
        validationStatus[fieldId] = isValid;

        // Show validation result ONLY if field has been touched and has validation errors or success
        if ($field.data('touched')) {
            if (!isValid && errorMessage) {
                $container.addClass('field-error');
                $errorMsg.text(errorMessage).addClass('show').show();
            } else if (isValid && value && !['username', 'email', 'phone'].includes(validationType)) {
                $container.addClass('field-valid');
                $successMsg.addClass('show').show();
            }
        }

        return isValid;
    }

    function checkUniqueness(type, value, $field, excludeUserId) {
        const cacheKey = `${type}_${value}_${excludeUserId}`;
        const $container = $field.closest('.input-group-custom');
        const $inlineValidation = $container.find('.inline-validation');

        console.log('Checking uniqueness for:', type, 'Value:', value, 'ExcludeUserId:', excludeUserId);
        console.log('Inline validation element found:', $inlineValidation.length);

        // Check cache first
        if (uniqueValidationCache[cacheKey] !== undefined) {
            console.log('Using cached result:', uniqueValidationCache[cacheKey]);
            showUniqueValidationResult(type, $container, uniqueValidationCache[cacheKey], $field);
            return;
        }

        // Show loading state in inline validation
        $inlineValidation.find('.validation-spinner, .validation-check, .validation-cross').addClass('d-none');
        $inlineValidation.find('.validation-spinner').removeClass('d-none');
        console.log('Showing spinner for uniqueness check');

        // Make AJAX call to server
        let url;
        let data = { excludeUserId: excludeUserId };

        switch (type) {
            case 'username':
                url = '/Account/CheckUsernameAvailability';
                data.username = value;
                break;
            case 'email':
                url = '/Account/CheckEmailAvailability';
                data.email = value;
                break;
            case 'phone':
                url = '/Account/CheckPhoneAvailability';
                data.phoneNumber = value;
                break;
            default:
                return;
        }

        $.ajax({
            url: url,
            type: 'GET',
            data: data,
            success: function(response) {
                uniqueValidationCache[cacheKey] = response.available;
                showUniqueValidationResult(type, $container, response.available, $field);
                updateSubmitButton();
            },
            error: function() {
                uniqueValidationCache[cacheKey] = false;
                showUniqueValidationResult(type, $container, false, $field);
                updateSubmitButton();
            },
            complete: function() {
                // Remove loading state - keep the inline validation visible
            }
        });
    }

    function getIconClass(fieldId) {
        const iconMap = {
            'FirstName': 'fa-user',
            'LastName': 'fa-user',
            'Username': 'fa-at',
            'Email': 'fa-envelope',
            'PhoneNumber': 'fa-phone',
            'DateOfBirth': 'fa-calendar',
            'Gender': 'fa-venus-mars',
            'Country': 'fa-globe',
            'StreetAddress': 'fa-home',
            'City': 'fa-map-marker-alt',
            'State': 'fa-map',
            'ZipCode': 'fa-mail-bulk',
            'Bio': 'fa-user-edit'
        };
        return iconMap[fieldId] || 'fa-user';
    }

    function showUniqueValidationResult(type, $container, isUnique, $field) {
        const fieldId = $field.attr('id') || $field.attr('name');
        const $errorMsg = $container.find('.error-message');
        const $successMsg = $container.find('.success-message');
        const $inlineValidation = $container.find('.inline-validation');

        console.log('Showing validation result for:', fieldId, 'isUnique:', isUnique);
        console.log('Inline validation container found:', $inlineValidation.length);

        $container.removeClass('field-valid field-error');
        $errorMsg.removeClass('show').hide();
        $successMsg.removeClass('show').hide();

        // Hide all inline validation indicators first
        $inlineValidation.find('.validation-spinner, .validation-check, .validation-cross').addClass('d-none');

        // Only show validation results if field has been touched
        if ($field.data('touched')) {
            if (isUnique) {
                $container.addClass('field-valid');
                $successMsg.addClass('show').show();
                $inlineValidation.find('.validation-check').removeClass('d-none');
                validationStatus[fieldId] = true;
                console.log('Showing success check for:', fieldId);
            } else {
                $container.addClass('field-error');
                $errorMsg.text(`This ${type} is already taken by another user`).addClass('show').show();
                $inlineValidation.find('.validation-cross').removeClass('d-none');
                validationStatus[fieldId] = false;
                console.log('Showing error cross for:', fieldId);
            }
        }
    }

    function validateAllFields() {
        let allValid = true;

        $('input[required], select[required], textarea[required]').each(function() {
            const $field = $(this);
            $field.data('touched', true);
            if (!validateField($field)) {
                allValid = false;
            }
        });

        // Also validate non-required fields that have values
        $('input:not([required]), select:not([required]), textarea:not([required])').each(function() {
            const $field = $(this);
            if ($field.val().trim()) {
                $field.data('touched', true);
                if (!validateField($field)) {
                    allValid = false;
                }
            }
        });

        return allValid;
    }

    function updateSubmitButton() {
        const $submitBtn = $('#submitBtn');

        // Check only required fields
        const requiredFields = ['FirstName', 'LastName', 'Username', 'Email', 'DateOfBirth', 'Country', 'PhoneNumber'];
        let allRequiredValid = true;

        console.log('Checking required fields validation status:', validationStatus);

        for (const fieldId of requiredFields) {
            if (validationStatus[fieldId] === false || validationStatus[fieldId] === undefined) {
                const $field = $(`#${fieldId}`);
                if ($field.length && $field.prop('required') && !$field.val().trim()) {
                    allRequiredValid = false;
                    console.log(`Required field ${fieldId} is empty or invalid`);
                    break;
                }
                if (validationStatus[fieldId] === false) {
                    allRequiredValid = false;
                    console.log(`Required field ${fieldId} failed validation`);
                    break;
                }
            }
        }

        console.log('All required fields valid:', allRequiredValid);

        $submitBtn.prop('disabled', !allRequiredValid);

        if (allRequiredValid) {
            $submitBtn.removeClass('btn-secondary').addClass('btn-primary');
        } else {
            $submitBtn.removeClass('btn-primary').addClass('btn-secondary');
        }
    }

    function submitForm() {
        const formData = new FormData($('#editProfileForm')[0]);

        $.ajax({
            url: $('#editProfileForm').attr('action'),
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: {
                'Accept': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            success: function(response) {
                hideLoading();
                isSubmitting = false;

                if (response.success || response.Success) {
                    showSuccess('Profile updated successfully!');
                    setTimeout(function() {
                        window.location.href = '/Account/Profile';
                    }, 2000);
                } else {
                    showError(response.message || response.Message || 'An error occurred while updating your profile.');
                }
            },
            error: function(xhr) {
                hideLoading();
                isSubmitting = false;

                let errorMessage = 'An error occurred while updating your profile.';

                if (xhr.responseJSON) {
                    if (xhr.responseJSON.errors) {
                        // Handle validation errors
                        const errors = [];
                        for (const field in xhr.responseJSON.errors) {
                            errors.push(...xhr.responseJSON.errors[field]);
                        }
                        errorMessage = errors.join(' ');
                    } else if (xhr.responseJSON.message || xhr.responseJSON.Message) {
                        errorMessage = xhr.responseJSON.message || xhr.responseJSON.Message;
                    }
                }

                showError(errorMessage);
            }
        });
    }

    function showLoading() {
        const $submitBtn = $('#submitBtn');
        $submitBtn.addClass('loading').prop('disabled', true);
        $('.btn-secondary').prop('disabled', true);
    }

    function hideLoading() {
        const $submitBtn = $('#submitBtn');
        $submitBtn.removeClass('loading');
        $('.btn-secondary').prop('disabled', false);
        updateSubmitButton();
    }

    function showSuccess(message) {
        hideAllMessages();
        $('#success-message').removeClass('d-none').find('#success-text').text(message);
        $('#success-message')[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    function showError(message) {
        hideAllMessages();
        $('#error-message').removeClass('d-none').find('#error-text').text(message);
        $('#error-message')[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    function hideAllMessages() {
        $('#success-message, #error-message').addClass('d-none');
    }

    // Password field specific handlers
    $('#NewPassword').on('input', function() {
        const password = $(this).val();
        const $requirements = $('#password-requirements');

        if (password.length > 0) {
            $requirements.show();
            validatePasswordStrength(password);
        } else {
            $requirements.hide();
        }

        // Also revalidate confirm password if it has a value
        const confirmPassword = $('#ConfirmNewPassword').val();
        if (confirmPassword) {
            validateField($('#ConfirmNewPassword'));
        }
    });

    $('#ConfirmNewPassword').on('input', function() {
        if ($(this).val() || $('#NewPassword').val()) {
            validateField($(this));
        }
    });

    // Initial validation state update
    updateSubmitButton();
});

// Global functions for server-side handling
function showModelErrors(errorMessages) {
    if (errorMessages && errorMessages.length > 0) {
        const message = Array.isArray(errorMessages) ? errorMessages.join(' ') : errorMessages;
        showError(message);
    }
}

function showSuccess(message) {
    $('#success-message').removeClass('d-none').find('#success-text').text(message || 'Profile updated successfully!');
    $('#success-message')[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function validatePasswordStrength(password) {
    const requirements = {
        length: password.length >= 8,
        uppercase: /[A-Z]/.test(password),
        lowercase: /[a-z]/.test(password),
        number: /\d/.test(password),
        special: /[!@#$%^&*(),.?\":{}|<>]/.test(password)
    };

    // Update requirement indicators
    Object.keys(requirements).forEach(req => {
        const $requirement = $(`#req-${req}`);
        const $icon = $requirement.find('i');

        if (requirements[req]) {
            $requirement.addClass('met');
            $icon.removeClass('fa-times').addClass('fa-check');
        } else {
            $requirement.removeClass('met');
            $icon.removeClass('fa-check').addClass('fa-times');
        }
    });

    // Calculate strength
    const metCount = Object.values(requirements).filter(Boolean).length;
    const isValid = metCount >= 4; // At least 4 out of 5 requirements

    let message = '';
    if (password.length > 0 && !isValid) {
        const missing = [];
        if (!requirements.length) missing.push('8 characters');
        if (!requirements.uppercase) missing.push('uppercase letter');
        if (!requirements.lowercase) missing.push('lowercase letter');
        if (!requirements.number) missing.push('number');
        if (!requirements.special) missing.push('special character');
        message = `Password needs: ${missing.join(', ')}`;
    }

    return {
        isValid,
        message
    };
}