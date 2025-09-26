document.addEventListener('DOMContentLoaded', function() {
    // Auto-focus on first name field
    const firstNameField = document.querySelector('input[name="FirstName"]');
    if (firstNameField) {
        firstNameField.focus();
    }

    // Name field validation - only alphabets
    const nameFields = document.querySelectorAll('input[name="FirstName"], input[name="LastName"]');
    nameFields.forEach(field => {
        field.addEventListener('input', function(e) {
            // Remove any non-alphabetic characters
            let value = e.target.value;
            let cleanValue = '';
            for (let i = 0; i < value.length; i++) {
                let char = value.charAt(i);
                if ((char >= 'A' && char <= 'Z') || (char >= 'a' && char <= 'z')) {
                    cleanValue += char;
                }
            }
            if (value !== cleanValue) {
                e.target.value = cleanValue;
            }
        });
    });

    // Phone number validation - only 10 digits
    const phoneField = document.querySelector('input[name="PhoneNumber"]');
    if (phoneField) {
        phoneField.addEventListener('input', function(e) {
            // Remove any non-numeric characters
            let value = e.target.value;
            let cleanValue = '';
            for (let i = 0; i < value.length; i++) {
                let char = value.charAt(i);
                if (char >= '0' && char <= '9') {
                    cleanValue += char;
                }
            }
            // Limit to 10 digits
            if (cleanValue.length > 10) {
                cleanValue = cleanValue.substring(0, 10);
            }
            if (value !== cleanValue) {
                e.target.value = cleanValue;
            }
        });

        // Validation on blur
        phoneField.addEventListener('blur', function(e) {
            const value = e.target.value;
            const errorSpan = e.target.parentNode.querySelector('.text-danger');
            if (errorSpan) {
                if (value.length > 0 && value.length !== 10) {
                    errorSpan.textContent = 'Phone number must be exactly 10 digits.';
                    e.target.classList.add('error');
                } else {
                    errorSpan.textContent = '';
                    e.target.classList.remove('error');
                }
            }
        });
    }

    // State validation - alphabets and spaces only
    const stateField = document.querySelector('input[name="State"]');
    if (stateField) {
        stateField.addEventListener('input', function(e) {
            let value = e.target.value;
            let cleanValue = '';
            for (let i = 0; i < value.length; i++) {
                let char = value.charAt(i);
                if ((char >= 'A' && char <= 'Z') || (char >= 'a' && char <= 'z') || char === ' ') {
                    cleanValue += char;
                }
            }
            if (value !== cleanValue) {
                e.target.value = cleanValue;
            }
        });
    }

    // City validation - alphabets and spaces only
    const cityField = document.querySelector('input[name="City"]');
    if (cityField) {
        cityField.addEventListener('input', function(e) {
            let value = e.target.value;
            let cleanValue = '';
            for (let i = 0; i < value.length; i++) {
                let char = value.charAt(i);
                if ((char >= 'A' && char <= 'Z') || (char >= 'a' && char <= 'z') || char === ' ') {
                    cleanValue += char;
                }
            }
            if (value !== cleanValue) {
                e.target.value = cleanValue;
            }
        });
    }

    // ZIP code validation - exactly 6 digits
    const zipField = document.querySelector('input[name="ZipCode"]');
    if (zipField) {
        zipField.addEventListener('input', function(e) {
            let value = e.target.value;
            let cleanValue = '';
            for (let i = 0; i < value.length; i++) {
                let char = value.charAt(i);
                if (char >= '0' && char <= '9') {
                    cleanValue += char;
                }
            }
            // Limit to 6 digits
            if (cleanValue.length > 6) {
                cleanValue = cleanValue.substring(0, 6);
            }
            if (value !== cleanValue) {
                e.target.value = cleanValue;
            }
        });

        // Validation on blur
        zipField.addEventListener('blur', function(e) {
            const value = e.target.value;
            const errorSpan = e.target.parentNode.querySelector('.text-danger');
            if (errorSpan) {
                if (value.length > 0 && value.length !== 6) {
                    errorSpan.textContent = 'ZIP/Postal code must be exactly 6 digits.';
                    e.target.classList.add('error');
                } else {
                    errorSpan.textContent = '';
                    e.target.classList.remove('error');
                }
            }
        });
    }

    // Date of Birth validation and restrictions
    const dobField = document.querySelector('input[name="DateOfBirth"]');
    if (dobField) {
        // Set min and max dates (18-80 years old)
        const today = new Date();
        // Max date: Latest date for someone to be at least 18 years old
        // Subtract 18 years and 1 day to ensure they are definitely 18+
        const maxDate = new Date(today.getFullYear() - 18, today.getMonth(), today.getDate() - 1);
        // Min date: Earliest date for someone to be at most 80 years old
        const minDate = new Date(today.getFullYear() - 80, today.getMonth(), today.getDate());

        // For HTML5 date input: max prevents selecting dates after this (too young)
        // min prevents selecting dates before this (too old)
        dobField.max = maxDate.toISOString().split('T')[0];
        dobField.min = minDate.toISOString().split('T')[0];


        // Add input event listener for real-time validation
        dobField.addEventListener('input', function(e) {
            validateAge(e.target);
        });

        dobField.addEventListener('blur', function(e) {
            validateAge(e.target);
        });

        function validateAge(field) {
            const value = field.value;
            const errorSpan = field.parentNode.querySelector('.text-danger');
            if (errorSpan && value) {
                const birthDate = new Date(value);
                const currentDate = new Date();

                // Calculate precise age
                let age = currentDate.getFullYear() - birthDate.getFullYear();
                const monthDiff = currentDate.getMonth() - birthDate.getMonth();

                // Adjust age if birthday hasn't occurred this year
                if (monthDiff < 0 || (monthDiff === 0 && currentDate.getDate() < birthDate.getDate())) {
                    age--;
                }

                // Check age range (18-80 years)
                if (age < 18) {
                    errorSpan.textContent = 'User must be at least 18 years old.';
                    field.classList.add('error');
                } else if (age > 80) {
                    errorSpan.textContent = 'User cannot be older than 80 years.';
                    field.classList.add('error');
                } else {
                    errorSpan.textContent = '';
                    field.classList.remove('error');
                }
            } else if (errorSpan && !value) {
                errorSpan.textContent = '';
                field.classList.remove('error');
            }
        }
    }

    // Real-time uniqueness validation with AJAX
    const currentUserId = document.querySelector('input[name="UserID"]').value;

    async function checkUniqueness(field, value, fieldElement) {
        const errorSpan = fieldElement.parentNode.querySelector('.text-danger');

        if (!value || value.trim() === '') {
            errorSpan.textContent = '';
            fieldElement.classList.remove('error');
            return true;
        }

        try {
            const response = await fetch('/Admin/CheckUniqueness', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: `field=${field}&value=${encodeURIComponent(value)}&currentUserId=${currentUserId}`
            });

            const result = await response.json();

            if (result.success) {
                if (!result.isUnique) {
                    const fieldName = field === 'phonenumber' ? 'phone number' : field.toLowerCase();
                    errorSpan.textContent = `This ${fieldName} is already taken by another user.`;
                    fieldElement.classList.add('error');
                    fieldElement.setAttribute('data-unique', 'false');
                    return false;
                } else {
                    errorSpan.textContent = '';
                    fieldElement.classList.remove('error');
                    fieldElement.setAttribute('data-unique', 'true');
                    return true;
                }
            } else {
                const fieldName = field === 'phonenumber' ? 'phone number' : field.toLowerCase();
                errorSpan.textContent = `Error checking ${fieldName} availability.`;
                fieldElement.classList.add('error');
                fieldElement.setAttribute('data-unique', 'false');
                return false;
            }
        } catch (error) {
            console.error('Uniqueness check error:', error);
            const fieldName = field === 'phonenumber' ? 'phone number' : field.toLowerCase();
            errorSpan.textContent = `Unable to verify ${fieldName} availability.`;
            fieldElement.classList.add('error');
            fieldElement.setAttribute('data-unique', 'false');
            return false;
        }
    }

    // Username field is readonly in edit mode, no uniqueness check needed
    const usernameField = document.querySelector('input[name="Username"]');
    if (usernameField && !usernameField.readOnly) {
        let usernameTimeout;
        usernameField.addEventListener('input', function(e) {
            clearTimeout(usernameTimeout);
            const value = e.target.value.trim();
            const errorSpan = e.target.parentNode.querySelector('.text-danger');

            if (value.length >= 3) {
                errorSpan.textContent = 'Checking username...';
                e.target.classList.remove('error');

                usernameTimeout = setTimeout(() => {
                    checkUniqueness('username', value, e.target);
                }, 800);
            } else {
                errorSpan.textContent = '';
                e.target.classList.remove('error');
            }
        });
    }

    // Email uniqueness check
    const emailField = document.querySelector('input[name="Email"]');
    if (emailField) {
        let emailTimeout;
        emailField.addEventListener('input', function(e) {
            clearTimeout(emailTimeout);
            const value = e.target.value.trim();
            const errorSpan = e.target.parentNode.querySelector('.text-danger');

            // Basic email format validation - using simple includes check
            const hasAt = value.includes('@');
            const hasDot = value.includes('.');
            const atIndex = value.indexOf('@');
            const dotIndex = value.lastIndexOf('.');
            const isValidFormat = hasAt && hasDot && atIndex > 0 && dotIndex > atIndex && dotIndex < value.length - 1;
            if (value && isValidFormat) {
                errorSpan.textContent = 'Checking email availability...';
                e.target.classList.remove('error');

                emailTimeout = setTimeout(() => {
                    checkUniqueness('email', value, e.target);
                }, 800);
            } else if (value && !isValidFormat) {
                errorSpan.textContent = 'Please enter a valid email address.';
                e.target.classList.add('error');
            } else {
                errorSpan.textContent = '';
                e.target.classList.remove('error');
            }
        });

        // Also validate on blur
        emailField.addEventListener('blur', function(e) {
            clearTimeout(emailTimeout);
            const value = e.target.value.trim();
            const errorSpan = e.target.parentNode.querySelector('.text-danger');

            if (value) {
                // Basic email format validation
                const hasAt = value.includes('@');
                const hasDot = value.includes('.');
                const atIndex = value.indexOf('@');
                const dotIndex = value.lastIndexOf('.');
                const isValidFormat = hasAt && hasDot && atIndex > 0 && dotIndex > atIndex && dotIndex < value.length - 1;
                if (isValidFormat) {
                    checkUniqueness('email', value, e.target);
                } else {
                    errorSpan.textContent = 'Please enter a valid email address.';
                    e.target.classList.add('error');
                }
            }
        });
    }

    // Enhanced phone uniqueness check
    if (phoneField) {
        let phoneTimeout;
        phoneField.addEventListener('input', function(e) {
            // First apply the existing digit-only validation
            let value = e.target.value;
            let cleanValue = '';
            for (let i = 0; i < value.length; i++) {
                let char = value.charAt(i);
                if (char >= '0' && char <= '9') {
                    cleanValue += char;
                }
            }
            if (cleanValue.length > 10) {
                cleanValue = cleanValue.substring(0, 10);
            }
            if (value !== cleanValue) {
                e.target.value = cleanValue;
            }

            // Then check uniqueness
            clearTimeout(phoneTimeout);
            const errorSpan = e.target.parentNode.querySelector('.text-danger');

            if (cleanValue.length === 10) {
                errorSpan.textContent = 'Checking phone number availability...';
                e.target.classList.remove('error');

                phoneTimeout = setTimeout(() => {
                    checkUniqueness('phonenumber', cleanValue, e.target);
                }, 800);
            } else if (cleanValue.length > 0 && cleanValue.length < 10) {
                errorSpan.textContent = 'Phone number must be exactly 10 digits.';
                e.target.classList.add('error');
            } else {
                errorSpan.textContent = '';
                e.target.classList.remove('error');
            }
        });

        // Also check on blur for complete validation
        phoneField.addEventListener('blur', function(e) {
            clearTimeout(phoneTimeout);
            const value = e.target.value.trim();
            const errorSpan = e.target.parentNode.querySelector('.text-danger');

            if (value.length === 10) {
                checkUniqueness('phonenumber', value, e.target);
            } else if (value.length > 0 && value.length !== 10) {
                errorSpan.textContent = 'Phone number must be exactly 10 digits.';
                e.target.classList.add('error');
            }
        });
    }

    // Disable communication preferences for admin
    const newsletterCheckbox = document.querySelector('input[name="ReceiveNewsletter"]');
    const smsCheckbox = document.querySelector('input[name="ReceiveSMS"]');
    if (newsletterCheckbox) {
        newsletterCheckbox.disabled = true;
        newsletterCheckbox.style.opacity = '0.5';
    }
    if (smsCheckbox) {
        smsCheckbox.disabled = true;
        smsCheckbox.style.opacity = '0.5';
    }

    // Form submission validation
    const form = document.querySelector('form');
    if (form) {
        form.addEventListener('submit', function(e) {
            let hasErrors = false;
            const errorFields = [];

            // Check for validation errors
            const errorElements = form.querySelectorAll('.form-control.error');
            if (errorElements.length > 0) {
                hasErrors = true;
                errorElements.forEach(el => {
                    const fieldName = el.getAttribute('name');
                    if (fieldName) {
                        errorFields.push(fieldName);
                    }
                });
            }

            // Check for uniqueness validation in progress or failed
            const emailField = form.querySelector('input[name="Email"]');
            const phoneField = form.querySelector('input[name="PhoneNumber"]');

            if (emailField && emailField.value.trim()) {
                const emailUnique = emailField.getAttribute('data-unique');
                if (emailUnique === 'false') {
                    hasErrors = true;
                    errorFields.push('Email');
                }
            }

            if (phoneField && phoneField.value.trim()) {
                const phoneUnique = phoneField.getAttribute('data-unique');
                if (phoneUnique === 'false') {
                    hasErrors = true;
                    errorFields.push('PhoneNumber');
                }
            }

            if (hasErrors) {
                e.preventDefault();

                // Show a consolidated error message
                let alertDiv = document.querySelector('.validation-alert');
                if (!alertDiv) {
                    alertDiv = document.createElement('div');
                    alertDiv.className = 'alert alert-error validation-alert';
                    alertDiv.innerHTML = '<i class="fas fa-exclamation-triangle"></i> <span class="alert-message"></span>';
                    form.insertBefore(alertDiv, form.firstChild);
                }

                const messageSpan = alertDiv.querySelector('.alert-message');
                if (errorFields.length > 0) {
                    messageSpan.textContent = `Please fix the following fields before saving: ${errorFields.join(', ')}`;
                } else {
                    messageSpan.textContent = 'Please fix all validation errors before saving.';
                }

                // Scroll to the top to show the error
                alertDiv.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        });
    }
});