// Registration page JavaScript functionality
$(document).ready(function() {
    let currentStep = 1;
    const totalSteps = 3;
    let stepValidationStatus = { 1: {}, 2: {}, 3: {} };
    let uniqueValidationCache = {};

    // Initialize
    updateStepIndicators();
    createParticles();
    initializeAgeRestrictions();
    updateNavigation();

    // Bio character counter
    $('#Bio').on('input', function() {
        const count = $(this).val().length;
        $('#bio-count').text(count);
    });

    // Mark fields as touched when user starts interacting
    $('input, select, textarea').on('focus', function() {
        $(this).data('touched', true);
    });

    // Field validation handlers - only validate on blur and specific events
    $('input, select, textarea').on('blur change paste', function() {
        const $field = $(this);
        $field.data('touched', true); // Mark as touched on blur
        // Small delay to ensure paste/autocomplete data is processed
        setTimeout(function() {
            validateField($field);
            updateStepProgress();
            updateNavigationState();
        }, 50);
    });

    // Real-time input handlers
    $('input[data-validation]').on('input', function() {
        const $field = $(this);
        const validationType = $field.data('validation');

        if (validationType === 'phone') {
            // Only allow digits
            let value = $field.val().replace(/\D/g, '');
            if (value.length > 10) value = value.slice(0, 10);
            $field.val(value);
        } else if (validationType === 'zipcode') {
            // Only allow digits
            let value = $field.val().replace(/\D/g, '');
            if (value.length > 6) value = value.slice(0, 6);
            $field.val(value);
        } else if (validationType === 'alphabets') {
            // Only allow alphabets
            $field.val($field.val().replace(/[^a-zA-Z]/g, ''));
        } else if (validationType === 'alphabets-spaces') {
            // Only allow alphabets and spaces
            $field.val($field.val().replace(/[^a-zA-Z\s]/g, ''));
        }
    });

    // Password strength validation - show strength while typing but don't update progress
    $('#password-field').on('input', function() {
        validatePasswordStrength($(this).val());
        // Also recheck password confirmation when main password changes
        if ($('#confirm-password-field').val()) {
            validatePasswordConfirmation();
        }
    });

    // Password field validation - only update progress on blur
    $('#password-field').on('blur', function() {
        $(this).data('touched', true);
        validateField($(this));
        updateStepProgress();
        updateNavigationState();
    });

    // Confirm password validation - show validation while typing but don't update progress
    $('#confirm-password-field').on('input', function() {
        validatePasswordConfirmation();
    });

    // Confirm password field validation - only update progress on blur
    $('#confirm-password-field').on('blur', function() {
        $(this).data('touched', true);
        validatePasswordConfirmation();
        updateStepProgress();
        updateNavigationState();
    });

    // Terms checkbox
    $('#terms').on('change', function() {
        updateNavigationState();
    });

    // Real-time duplicate checking event handlers
    let usernameTimeout, emailTimeout, phoneTimeout;

    $('#username').on('input', function() {
        const $this = $(this);
        const username = $this.val();

        // Clear previous timeout
        clearTimeout(usernameTimeout);

        // Clear any existing messages
        $('#username-error').removeClass('show');
        $('#username-success').removeClass('show');
        $this.removeClass('error');

        if (username.length >= 3) {
            // Add a short delay to avoid too many API calls
            usernameTimeout = setTimeout(function() {
                checkUsernameAvailability();
            }, 500);
        }
    });

    $('#email').on('input', function() {
        const $this = $(this);
        const email = $this.val();

        // Clear previous timeout
        clearTimeout(emailTimeout);

        // Clear any existing messages
        $('#email-error').removeClass('show');
        $('#email-success').removeClass('show');
        $this.removeClass('error');

        if (email.includes('@') && email.length >= 5) {
            emailTimeout = setTimeout(function() {
                checkEmailAvailability();
            }, 500);
        }
    });

    $('#phoneNumber').on('input', function() {
        const $this = $(this);
        const phone = $this.val();

        // Clear previous timeout
        clearTimeout(phoneTimeout);

        // Clear any existing messages
        $('#phone-error').removeClass('show');
        $('#phone-success').removeClass('show');
        $this.removeClass('error');

        if (phone.length >= 8) {
            phoneTimeout = setTimeout(function() {
                checkPhoneAvailability();
            }, 500);
        }
    });

    // Step navigation
    $('#nextBtn').click(function() {
        if (canProceedToNextStep()) {
            if (currentStep < totalSteps) {
                currentStep++;
                showStep(currentStep);
                updateStepIndicators();
                updateNavigation();
            }
        }
    });

    $('#prevBtn').click(function() {
        if (currentStep > 1) {
            currentStep--;
            showStep(currentStep);
            updateStepIndicators();
            updateNavigation();
        }
    });

    // Form submission
    $('#registrationForm').submit(function(e) {
        e.preventDefault();
        if (canSubmitForm()) {
            showLoading();
            // Submit the form using AJAX
            submitForm();
        }
    });

    function initializeAgeRestrictions() {
        const today = new Date();
        const minDate = new Date(today.getFullYear() - 80, today.getMonth(), today.getDate());
        const maxDate = new Date(today.getFullYear() - 18, today.getMonth(), today.getDate());
        const defaultDate = new Date(today.getFullYear() - 18, today.getMonth(), today.getDate());

        $('#DateOfBirth').attr('min', minDate.toISOString().split('T')[0]);
        $('#DateOfBirth').attr('max', maxDate.toISOString().split('T')[0]);
        $('#DateOfBirth').val(defaultDate.toISOString().split('T')[0]);
    }

    function validateField($field) {
        const fieldId = $field.attr('id') || $field.attr('name');
        const validationType = $field.data('validation');
        const value = $field.val().trim();
        const isRequired = $field.prop('required');
        const $errorMsg = $field.closest('.form-group').find('.error-message');
        const $successMsg = $field.closest('.form-group').find('.success-message-field');

        let isValid = true;
        let errorMessage = '';

        // Clear previous states
        $errorMsg.removeClass('show');
        $successMsg.removeClass('show');

        // For required fields, only show error if the field has been touched and is empty
        // Don't show required error while user is actively typing
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
                    } else if (value.length > 50) {
                        isValid = false;
                        errorMessage = 'Maximum 50 characters allowed';
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
                        checkUniqueness('username', value, $field);
                    }
                    break;
                case 'email':
                    // Use HTML5 email validation
                    const emailInput = document.createElement('input');
                    emailInput.type = 'email';
                    emailInput.value = value;
                    const emailValid = emailInput.validity.valid && value.length > 0;
                    if (!emailValid) {
                        isValid = false;
                        errorMessage = 'Please enter a valid email address';
                    } else if (value.length > 100) {
                        isValid = false;
                        errorMessage = 'Email must be less than 100 characters';
                    } else {
                        checkUniqueness('email', value, $field);
                    }
                    break;
                case 'phone':
                    if (!/^\d{10}$/.test(value)) {
                        isValid = false;
                        errorMessage = 'Phone number must be exactly 10 digits';
                    } else if (value.length > 20) {
                        isValid = false;
                        errorMessage = 'Phone number must be less than 20 characters';
                    } else {
                        checkUniqueness('phone', value, $field);
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
                    const age = today.getFullYear() - birthDate.getFullYear();
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
                    // Use the existing validatePasswordStrength function
                    const strengthResult = validatePasswordStrength(value);
                    if (!strengthResult.isStrong) {
                        isValid = false;
                        errorMessage = 'Password must be at least 8 characters with uppercase, lowercase, number, and special character';
                    }
                    break;
                case 'confirm-password':
                    // Check if password confirmation matches and main password is strong
                    const mainPassword = $('#password-field').val();
                    const passwordIsStrong = stepValidationStatus[3] && stepValidationStatus[3]['Password'] === true;
                    if (!mainPassword) {
                        isValid = false;
                        errorMessage = 'Please enter password first';
                    } else if (value !== mainPassword) {
                        isValid = false;
                        errorMessage = 'Passwords do not match';
                    } else if (!passwordIsStrong) {
                        isValid = false;
                        errorMessage = 'Please ensure password meets strength requirements first';
                    }
                    break;
                case 'country':
                    if (value.length > 50) {
                        isValid = false;
                        errorMessage = 'Country name must be less than 50 characters';
                    }
                    break;
                case 'security-question':
                    if (value.length > 200) {
                        isValid = false;
                        errorMessage = 'Security question must be less than 200 characters';
                    }
                    break;
                case 'security-answer':
                    if (value.length > 100) {
                        isValid = false;
                        errorMessage = 'Security answer must be less than 100 characters';
                    }
                    break;
            }
        }

        // Update field validation status
        const stepNum = getStepForField(fieldId);
        if (stepNum) {
            stepValidationStatus[stepNum][fieldId] = isValid;
        }

        // Show error or success message
        if (!isValid && errorMessage) {
            $errorMsg.text(errorMessage).addClass('show');
        } else if (isValid && value && !['username', 'email', 'phone'].includes(validationType)) {
            $successMsg.addClass('show');
        }

        return isValid;
    }

    function checkUniqueness(type, value, $field) {
        const cacheKey = `${type}_${value}`;

        // Check cache first
        if (uniqueValidationCache[cacheKey] !== undefined) {
            showUniqueValidationResult(type, $field, uniqueValidationCache[cacheKey]);
            return;
        }

        // Make AJAX call to server
        let url;
        let paramName;

        switch (type) {
            case 'username':
                url = '/Account/CheckUsernameAvailability';
                paramName = 'username';
                break;
            case 'email':
                url = '/Account/CheckEmailAvailability';
                paramName = 'email';
                break;
            case 'phone':
                url = '/Account/CheckPhoneAvailability';
                paramName = 'phoneNumber';
                break;
            default:
                return;
        }

        $.ajax({
            url: url,
            type: 'GET',
            data: (function() { var obj = {}; obj[paramName] = value; return obj; })(),
            success: function(response) {
                uniqueValidationCache[cacheKey] = response.available;
                showUniqueValidationResult(type, $field, response.available);
                updateStepProgress();
                updateNavigationState();
            },
            error: function() {
                // On error, assume not unique to be safe
                uniqueValidationCache[cacheKey] = false;
                showUniqueValidationResult(type, $field, false);
                updateStepProgress();
                updateNavigationState();
            }
        });
    }

    function showUniqueValidationResult(type, $field, isUnique) {
        const $errorMsg = $field.closest('.form-group').find('.error-message');
        const $successMsg = $field.closest('.form-group').find('.success-message-field');
        const fieldId = $field.attr('id') || $field.attr('name');
        const stepNum = getStepForField(fieldId);

        if (isUnique) {
            $errorMsg.removeClass('show');
            $successMsg.addClass('show');
            if (stepNum) stepValidationStatus[stepNum][fieldId] = true;
        } else {
            $successMsg.removeClass('show');
            $errorMsg.text(`This ${type} is already taken`).addClass('show');
            if (stepNum) stepValidationStatus[stepNum][fieldId] = false;
        }
    }

    function validatePasswordStrength(password) {
        const requirements = {
            length: password.length >= 8,
            uppercase: /[A-Z]/.test(password),
            lowercase: /[a-z]/.test(password),
            number: /\d/.test(password),
            special: /[!@@#$%^&*(),.?\":{}|<>]/.test(password)
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
        let strength = 0;
        let strengthClass = '';
        let strengthText = '';

        if (metCount >= 5) {
            strength = 100;
            strengthClass = 'strength-strong';
            strengthText = 'Strong';
        } else if (metCount >= 4) {
            strength = 75;
            strengthClass = 'strength-good';
            strengthText = 'Good';
        } else if (metCount >= 2) {
            strength = 50;
            strengthClass = 'strength-fair';
            strengthText = 'Fair';
        } else if (password.length > 0) {
            strength = 25;
            strengthClass = 'strength-weak';
            strengthText = 'Weak';
        }

        // Update strength bar
        const $strengthBar = $('#strength-bar');
        $strengthBar.css('width', strength + '%')
                   .removeClass('strength-weak strength-fair strength-good strength-strong')
                   .addClass(strengthClass);

        const isStrong = metCount >= 5;
        stepValidationStatus[3]['Password'] = isStrong;

        const $errorMsg = $('#password-error');
        const $successMsg = $('#password-success');

        if (password && !isStrong) {
            $errorMsg.addClass('show');
            $successMsg.removeClass('show');
        } else if (isStrong) {
            $errorMsg.removeClass('show');
            $successMsg.addClass('show');
        } else {
            $errorMsg.removeClass('show');
            $successMsg.removeClass('show');
        }

        return isStrong;
    }

    function validatePasswordConfirmation() {
        const password = $('#password-field').val();
        const confirmPassword = $('#confirm-password-field').val();
        const $errorMsg = $('#confirm-password-error');
        const $successMsg = $('#confirm-password-success');

        // Only consider it a match if both fields have values AND they match AND password is strong
        const passwordIsStrong = stepValidationStatus[3]['Password'] === true;
        const isMatch = password && confirmPassword && password === confirmPassword && passwordIsStrong;
        stepValidationStatus[3]['ConfirmPassword'] = isMatch;

        if (confirmPassword.length > 0) {
            if (password !== confirmPassword) {
                // Passwords don't match
                $errorMsg.text('Passwords do not match').addClass('show');
                $successMsg.removeClass('show');
            } else if (password === confirmPassword && password.length > 0) {
                if (passwordIsStrong) {
                    // Passwords match and password is strong
                    $errorMsg.removeClass('show');
                    $successMsg.addClass('show');
                } else {
                    // Passwords match but password is not strong enough
                    $errorMsg.text('Password must meet all requirements first').addClass('show');
                    $successMsg.removeClass('show');
                }
            }
        } else {
            // No confirm password entered yet
            $errorMsg.removeClass('show');
            $successMsg.removeClass('show');
        }

        return isMatch;
    }

    function getStepForField(fieldId) {
        const step1Fields = ['FirstName', 'LastName', 'Username', 'DateOfBirth', 'Country'];
        const step2Fields = ['Email', 'PhoneNumber', 'City', 'State', 'ZipCode'];
        const step3Fields = ['Password', 'ConfirmPassword', 'SecurityQuestion', 'SecurityAnswer'];

        if (step1Fields.includes(fieldId)) return 1;
        if (step2Fields.includes(fieldId)) return 2;
        if (step3Fields.includes(fieldId)) return 3;
        return null;
    }

    function updateStepProgress() {
        for (let step = 1; step <= totalSteps; step++) {
            const $step = $(`.step[data-step="${step}"]`);
            const $progress = $step.find('.step-progress');

            const requiredFields = getRequiredFieldsForStep(step);
            const validatedFields = Object.keys(stepValidationStatus[step]).filter(field =>
                stepValidationStatus[step][field] === true && requiredFields.includes(field)
            );

            const progress = requiredFields.length > 0 ?
                (validatedFields.length / requiredFields.length) * 100 : 0;

            // Only show progress bar if there's actual progress
            if (progress > 0) {
                $step.addClass('has-progress');
                $progress.css('--progress', progress + '%');
            } else {
                $step.removeClass('has-progress');
                $progress.css('--progress', '0%');
            }

            // Add visual feedback for different progress levels
            if (progress >= 100) {
                $step.addClass('step-complete');
            } else {
                $step.removeClass('step-complete');
            }

        }
    }

    function getRequiredFieldsForStep(step) {
        switch (step) {
            case 1: return ['FirstName', 'LastName', 'Username', 'DateOfBirth', 'Country'];
            case 2: return ['Email', 'PhoneNumber'];
            case 3: return ['Password', 'ConfirmPassword', 'SecurityQuestion', 'SecurityAnswer'];
            default: return [];
        }
    }

    function canProceedToNextStep() {
        const requiredFields = getRequiredFieldsForStep(currentStep);
        return requiredFields.every(field => stepValidationStatus[currentStep][field] === true);
    }

    function canSubmitForm() {
        const termsAccepted = $('#terms').is(':checked');
        const allStepsValid = [1, 2, 3].every(step => {
            const requiredFields = getRequiredFieldsForStep(step);
            return requiredFields.every(field => stepValidationStatus[step][field] === true);
        });

        return allStepsValid && termsAccepted;
    }

    function updateNavigationState() {
        const canProceed = canProceedToNextStep();
        const canSubmit = canSubmitForm();

        $('#nextBtn').prop('disabled', !canProceed);
        $('#submitBtn').prop('disabled', !canSubmit);
    }

    function showStep(step) {
        $('.form-section').removeClass('active');
        $(`#step${step}`).addClass('active');
    }

    function updateStepIndicators() {
        // Only remove active class, keep completed
        $('.step').removeClass('active');

        for (let i = 1; i <= totalSteps; i++) {
            const $step = $(`.step[data-step="${i}"]`);
            if (i < currentStep) {
                $step.addClass('completed').css('opacity', '1');
            } else if (i === currentStep) {
                $step.removeClass('completed').addClass('active').css('opacity', '1');
            } else {
                // Future steps - make them visible but dimmed
                $step.removeClass('completed').css('opacity', '0.5');
            }
        }
    }

    function updateNavigation() {
        $('#prevBtn').toggle(currentStep > 1);
        $('#nextBtn').toggle(currentStep < totalSteps);
        $('#submitBtn').toggle(currentStep === totalSteps);
        updateNavigationState();
    }

    function submitForm() {
        const formData = new FormData($('#registrationForm')[0]);

        $.ajax({
            url: $('#registrationForm').attr('action'),
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            },
            success: function(response) {
                hideLoading();
                if (response.success) {
                    // Update success message with server response
                    $('#successMessage h2').text('Account Created Successfully!');
                    $('#successMessage p').text(response.message);
                    showSuccessAndRedirect();
                } else {
                    // Handle validation errors
                    if (response.errors) {
                        displayValidationErrors(response.errors);
                    } else if (response.message) {
                        // General error message
                        alert(response.message);
                    }
                }
            },
            error: function(xhr, status, error) {
                hideLoading();
                let errorMessage = 'An error occurred while creating your account. Please try again.';

                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                }

                alert(errorMessage);
            }
        });
    }

    function showLoading() {
        $('#loadingOverlay').fadeIn();
    }

    function hideLoading() {
        $('#loadingOverlay').fadeOut();
    }

    function showSuccessAndRedirect() {
        $('.registration-card').fadeOut(500, function() {
            $('#successMessage').css('display', 'block').fadeIn(500);
            // Show success message for 4 seconds before redirecting
            setTimeout(function() {
                window.location.href = '/Account/Login';
            }, 4000);
        });
    }

    function displayValidationErrors(errors) {
        // Clear all existing error messages first
        $('.error-message').removeClass('show').text('');
        $('.success-message-field').removeClass('show');

        // Display field-specific errors
        Object.keys(errors).forEach(function(fieldName) {
            const errorMessage = errors[fieldName];

            // Map field names to error element IDs
            const fieldMap = {
                'Username': 'username-error',
                'Email': 'email-error',
                'PhoneNumber': 'phone-error',
                'FirstName': 'firstname-error',
                'LastName': 'lastname-error',
                'Password': 'password-error',
                'ConfirmPassword': 'confirm-password-error'
            };

            const errorElementId = fieldMap[fieldName];
            if (errorElementId) {
                const $errorElement = $('#' + errorElementId);
                if ($errorElement.length) {
                    $errorElement.text(errorMessage).addClass('show');

                    // Also highlight the input field
                    const $input = $('input[name="' + fieldName + '"]');
                    if ($input.length) {
                        $input.addClass('error').focus();

                        // Remove error styling when user starts typing
                        $input.one('input', function() {
                            $(this).removeClass('error');
                            $errorElement.removeClass('show');
                        });
                    }
                }
            }
        });
    }

    // Real-time duplicate checking functions
    function checkUsernameAvailability() {
        const username = $('#username').val();
        if (username && username.length >= 3) {
            $.post('/Account/CheckUsername', { username: username })
                .done(function(response) {
                    const $errorElement = $('#username-error');
                    const $successElement = $('#username-success');

                    if (response.available) {
                        $errorElement.removeClass('show');
                        $successElement.text('✓ Username available').addClass('show');
                        $('#username').removeClass('error');
                    } else {
                        $successElement.removeClass('show');
                        $errorElement.text('Username already taken').addClass('show');
                        $('#username').addClass('error');
                    }
                })
                .fail(function() {
                    $('#username-error').text('Unable to verify username availability').addClass('show');
                });
        }
    }

    function checkEmailAvailability() {
        const email = $('#email').val();
        if (email && email.includes('@')) {
            $.post('/Account/CheckEmail', { email: email })
                .done(function(response) {
                    const $errorElement = $('#email-error');
                    const $successElement = $('#email-success');

                    if (response.available) {
                        $errorElement.removeClass('show');
                        $successElement.text('✓ Email available').addClass('show');
                        $('#email').removeClass('error');
                    } else {
                        $successElement.removeClass('show');
                        $errorElement.text('Email already registered').addClass('show');
                        $('#email').addClass('error');
                    }
                })
                .fail(function() {
                    $('#email-error').text('Unable to verify email availability').addClass('show');
                });
        }
    }

    function checkPhoneAvailability() {
        const phone = $('#phoneNumber').val();
        if (phone && phone.length >= 8) {
            $.post('/Account/CheckPhoneNumber', { phoneNumber: phone })
                .done(function(response) {
                    const $errorElement = $('#phone-error');
                    const $successElement = $('#phone-success');

                    if (response.available) {
                        $errorElement.removeClass('show');
                        $successElement.text('✓ Phone number available').addClass('show');
                        $('#phoneNumber').removeClass('error');
                    } else {
                        $successElement.removeClass('show');
                        $errorElement.text('Phone number already registered').addClass('show');
                        $('#phoneNumber').addClass('error');
                    }
                })
                .fail(function() {
                    $('#phone-error').text('Unable to verify phone availability').addClass('show');
                });
        }
    }

    function createParticles() {
        const particlesContainer = $('#particles');
        for (let i = 0; i < 30; i++) {
            const particle = $('<div class="particle"></div>');
            const size = Math.random() * 4 + 2;
            particle.css({
                width: size + 'px',
                height: size + 'px',
                left: Math.random() * 100 + '%',
                animationDelay: Math.random() * 20 + 's',
                animationDuration: (Math.random() * 20 + 20) + 's'
            });
            particlesContainer.append(particle);
        }
    }

    // Don't auto-validate pre-filled fields on page load
    // Progress should start at 0 until user actually interacts with fields

    // Don't auto-validate pre-filled fields - wait for user interaction
    // Progress should start at 0% with no initialization call to updateStepProgress()

    // Enhanced detection for autofilled or bulk-updated fields
    function checkForAutoFilledFields() {
        let needsUpdate = false;
        $('input[required], select[required], input[data-validation]').each(function() {
            const $field = $(this);
            const fieldId = $field.attr('id') || $field.attr('name');
            const stepNum = getStepForField(fieldId);

            if (stepNum && $field.val()) {
                // Check if field validation status doesn't match current value
                const currentStatus = stepValidationStatus[stepNum][fieldId];
                if (currentStatus === undefined) {
                    validateField($field);
                    needsUpdate = true;
                }
            }
        });

        if (needsUpdate) {
            updateStepProgress();
            updateNavigationState();
        }
    }

    // Check every 500ms for faster response to auto-fill
    setInterval(checkForAutoFilledFields, 500);

    // Also listen for browser auto-fill events
    $(document).on('focusout', 'input, select', function() {
        setTimeout(checkForAutoFilledFields, 100);
    });

    // Listen for form changes that might indicate auto-fill
    $(document).on('DOMSubtreeModified input propertychange', 'form', function() {
        setTimeout(checkForAutoFilledFields, 100);
    });
});

// Global functions for server-side validation handling
function showModelErrors(errorMessages) {
    if (errorMessages && errorMessages.length > 0) {
        // Show errors
        console.log('Model errors:', errorMessages);
    }
}

function showSuccess() {
    $('.registration-card').fadeOut(function() {
        $('#successMessage').fadeIn();
    });
}