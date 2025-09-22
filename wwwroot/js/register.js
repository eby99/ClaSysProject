$(document).ready(function() {
    // Set dynamic max date for DOB to ensure minimum age of 18
    function setDOBMaxDate() {
        const today = new Date();
        const maxDate = new Date(today.getFullYear() - 18, today.getMonth(), today.getDate());
        const maxDateString = maxDate.toISOString().split('T')[0];
        $('#dob').attr('max', maxDateString);

        // Set default value to the maximum allowed date (exactly 18 years ago)
        if (!$('#dob').val() || $('#dob').val() === '0001-01-01') {
            $('#dob').val(maxDateString);
            // Trigger change event to update progress and validation
            $('#dob').trigger('change');
        }
    }
    setDOBMaxDate();

    // Generate floating particles
    function createParticles() {
        const particlesContainer = $('#particles');
        for (let i = 0; i < 15; i++) {
            const particle = $('<div class="particle"></div>');
            const size = Math.random() * 5 + 2;
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
    createParticles();


    // Initialize password requirements tracking
    if (!window.passwordRequirements) {
        window.passwordRequirements = {
            length: false,
            uppercase: false,
            lowercase: false,
            number: false,
            special: false
        };
    }

    // Enhanced logical progress tracking
    function updateProgress() {
        // Personal Info Fields (required)
        const personalRequired = ['#firstname', '#lastname', '#username', '#dob', '#country'];
        const personalFilled = personalRequired.filter(field => $(field).val() && $(field).val().trim() !== '').length;
        const personalValid = personalRequired.filter(field => $(field).val() && $(field)[0].checkValidity()).length;
        
        // Contact Info Fields (required)
        const contactRequired = ['#email', '#phone'];
        const contactFilled = contactRequired.filter(field => $(field).val() && $(field).val().trim() !== '').length;
        const contactValid = contactRequired.filter(field => $(field).val() && $(field)[0].checkValidity()).length;
        
        // Security Fields (required)
        const securityRequired = ['#password', '#confirm-password', '#security-question', '#security-answer'];
        const securityFilled = securityRequired.filter(field => $(field).val() && $(field).val().trim() !== '').length;
        const securityValid = securityRequired.filter(field => $(field).val() && $(field)[0].checkValidity()).length;
        
        // Terms acceptance
        const termsAccepted = $('#terms').is(':checked');
        
        // Function to update circular progress
        function updateCircularProgress(stepNumber, percentage) {
            const circle = $(`.step[data-step="${stepNumber}"] .progress-ring-fill`);
            const circumference = 157.08; // 2 * Math.PI * 25
            const offset = circumference - (percentage / 100) * circumference;
            circle.css('stroke-dashoffset', offset);
        }

        // Personal Info Section - only progress on valid fields
        const personalProgress = (personalValid / personalRequired.length) * 100;
        if (personalValid === personalRequired.length) {
            $('.step[data-step="1"]').addClass('completed').addClass('active');
            updateCircularProgress(1, 100);
        } else if (personalValid > 0) {
            $('.step[data-step="1"]').addClass('active').removeClass('completed');
            updateCircularProgress(1, personalProgress);
        } else {
            $('.step[data-step="1"]').removeClass('completed').removeClass('active');
            updateCircularProgress(1, 0);
        }

        // Contact Info Section - only progress on valid fields
        const contactProgress = (contactValid / contactRequired.length) * 100;
        if (contactValid === contactRequired.length) {
            $('.step[data-step="2"]').addClass('completed').addClass('active');
            updateCircularProgress(2, 100);
        } else if (contactValid > 0) {
            $('.step[data-step="2"]').addClass('active').removeClass('completed');
            updateCircularProgress(2, contactProgress);
        } else {
            $('.step[data-step="2"]').removeClass('completed').removeClass('active');
            updateCircularProgress(2, 0);
        }

        // Security Section - only progress on valid fields
        const passwordsMatch = $('#password').val() === $('#confirm-password').val() && $('#password').val() !== '';
        const passwordRequirementsMet = Object.values(window.passwordRequirements || {}).every(Boolean);
        const securityProgress = (securityValid / securityRequired.length) * 100;

        if (securityValid === securityRequired.length && passwordsMatch && passwordRequirementsMet && termsAccepted) {
            $('.step[data-step="3"]').addClass('completed').addClass('active');
            updateCircularProgress(3, 100);
        } else if (securityValid > 0) {
            $('.step[data-step="3"]').addClass('active').removeClass('completed');
            updateCircularProgress(3, securityProgress);
        } else {
            $('.step[data-step="3"]').removeClass('completed').removeClass('active');
            updateCircularProgress(3, 0);
        }
    }

    // Inline validation functions
    function checkUsernameAvailability() {
        const username = $('#username').val().trim();
        if (username.length >= 3) {
            $.get('/Account/CheckUsernameAvailability', { username: username })
                .done(function(response) {
                    const messageDiv = $('#username-error');
                    if (response.available) {
                        messageDiv.removeClass('show').addClass('success-message').text('✓ Username is available').fadeIn(300);
                        $('#username').removeClass('error').addClass('success');
                    } else {
                        messageDiv.removeClass('success-message').addClass('show').text(response.message).fadeIn(300);
                        $('#username').removeClass('success').addClass('error');
                    }
                });
        }
    }

    function checkEmailAvailability() {
        const email = $('#email').val().trim();
        if (email && email.includes('@')) {
            $.get('/Account/CheckEmailAvailability', { email: email })
                .done(function(response) {
                    const messageDiv = $('#email-error');
                    if (response.available) {
                        messageDiv.removeClass('show').addClass('success-message').text('✓ Email is available').fadeIn(300);
                        $('#email').removeClass('error').addClass('success');
                    } else {
                        messageDiv.removeClass('success-message').addClass('show').text(response.message).fadeIn(300);
                        $('#email').removeClass('success').addClass('error');
                    }
                });
        }
    }

    function checkPhoneAvailability() {
        const phone = $('#phone').val().trim();
        if (phone.length >= 10) {
            $.get('/Account/CheckPhoneAvailability', { phoneNumber: phone })
                .done(function(response) {
                    const messageDiv = $('#phone-error');
                    if (response.available) {
                        messageDiv.removeClass('show').addClass('success-message').text('✓ Phone number is available').fadeIn(300);
                        $('#phone').removeClass('error').addClass('success');
                    } else {
                        messageDiv.removeClass('success-message').addClass('show').text(response.message).fadeIn(300);
                        $('#phone').removeClass('success').addClass('error');
                    }
                });
        }
    }

    // Track form field changes for each section
    $('.personal-field').on('input change', function() {
        updateProgress();
        $(this).removeClass('error');
        $(this).closest('.form-group').find('.error-message').fadeOut(300);
    });

    $('.contact-field').on('input change', function() {
        updateProgress();
        $(this).removeClass('error');
        $(this).closest('.form-group').find('.error-message').fadeOut(300);
    });

    // Add specific validation for username, email, and phone
    let usernameTimeout, emailTimeout, phoneTimeout;

    $('#username').on('input', function() {
        clearTimeout(usernameTimeout);
        usernameTimeout = setTimeout(checkUsernameAvailability, 500);
    });

    $('#email').on('input', function() {
        clearTimeout(emailTimeout);
        emailTimeout = setTimeout(checkEmailAvailability, 500);
    });

    $('#phone').on('input', function() {
        clearTimeout(phoneTimeout);
        phoneTimeout = setTimeout(checkPhoneAvailability, 500);
    });

    $('.security-field').on('input change', function() {
        updateProgress();
        $(this).removeClass('error');
        $(this).closest('.form-group').find('.error-message').fadeOut(300);
    });

    $('#terms').on('change', function() {
        updateProgress();
    });

    // Enhanced password validation with requirements tracking
    $('#password').on('input focus', function() {
        const password = $(this).val();
        $('#passwordRequirements').addClass('show');
        
        // Check requirements
        window.passwordRequirements.length = password.length >= 8;
        window.passwordRequirements.uppercase = /[A-Z]/.test(password);
        window.passwordRequirements.lowercase = /[a-z]/.test(password);
        window.passwordRequirements.number = /[0-9]/.test(password);
        window.passwordRequirements.special = /[@$!%*?&]/.test(password);
        
        // Update UI with animations
        $('#length-req').toggleClass('valid', window.passwordRequirements.length);
        $('#uppercase-req').toggleClass('valid', window.passwordRequirements.uppercase);
        $('#lowercase-req').toggleClass('valid', window.passwordRequirements.lowercase);
        $('#number-req').toggleClass('valid', window.passwordRequirements.number);
        $('#special-req').toggleClass('valid', window.passwordRequirements.special);
        
        // Update strength meter
        const strength = calculatePasswordStrength(password);
        updatePasswordStrengthIndicator(strength);
    });

    $('#password').on('blur', function() {
        if ($(this).val()) {
            setTimeout(function() {
                $('#passwordRequirements').removeClass('show');
            }, 500);
        }
    });

    // Real-time password confirmation check
    $('#confirm-password').on('input', function() {
        const password = $('#password').val();
        const confirmPassword = $(this).val();
        
        if (confirmPassword && password !== confirmPassword) {
            $('#confirm-password-error').fadeIn(300);
            $(this).addClass('error');
        } else {
            $('#confirm-password-error').fadeOut(300);
            $(this).removeClass('error');
        }
        updateProgress();
    });

    // Character counter for textarea
    $('#bio').on('input', function() {
        const length = $(this).val().length;
        $('#bio-count').text(length);
        
        if (length > 450) {
            $('#bio-count').css('color', '#ed8936');
        } else if (length > 400) {
            $('#bio-count').css('color', '#ecc94b');
        } else {
            $('#bio-count').css('color', '#a0aec0');
        }
    });

    // Username validation
    $('#username').on('input', function() {
        let value = $(this).val();
        value = value.replace(/[^a-zA-Z0-9_]/g, '');
        
        if (value.length > 0 && !/^[a-zA-Z]/.test(value)) {
            $(this).addClass('error');
            $('#username-error').text('Username must start with a letter').fadeIn(300);
            while (value.length > 0 && !/^[a-zA-Z]/.test(value)) {
                value = value.substring(1);
            }
        } else if (value.length === 0) {
            $(this).addClass('error');
            $('#username-error').text('Username is required').fadeIn(300);
        } else if (value.length < 3) {
            $(this).addClass('error');
            $('#username-error').text('Username must be at least 3 characters').fadeIn(300);
        } else if (value.length > 30) {
            $(this).addClass('error');
            $('#username-error').text('Username must not exceed 30 characters').fadeIn(300);
        } else {
            $(this).removeClass('error');
            $('#username-error').fadeOut(300);
        }
        
        $(this).val(value);
    });

    // Age validation
    $('#dob').on('change', function() {
        const dob = new Date($(this).val());
        const today = new Date();
        const age = Math.floor((today - dob) / (365.25 * 24 * 60 * 60 * 1000));
        
        if (age < 18) {
            $('#dob-error').fadeIn(300);
            $(this).addClass('error');
        } else {
            $('#dob-error').fadeOut(300);
            $(this).removeClass('error');
        }
    });

    // Enhanced form validation
    function validateForm() {
        let isValid = true;
        const errors = [];
        
        $('input[required], select[required]').each(function() {
            if (!$(this).val() || !this.checkValidity()) {
                $(this).addClass('error');
                const errorMsg = $(this).closest('.form-group').find('.error-message');
                errorMsg.fadeIn(300);
                isValid = false;
                
                const label = $(this).closest('.form-group').find('label').first().text().replace('*', '').trim();
                errors.push(label);
            }
        });
        
        if ($('#password').val() !== $('#confirm-password').val()) {
            $('#confirm-password-error').fadeIn(300);
            $('#confirm-password').addClass('error');
            isValid = false;
            errors.push('Password confirmation');
        }
        
        const allRequirementsMet = Object.values(window.passwordRequirements).every(Boolean);
        if (!allRequirementsMet && $('#password').val()) {
            isValid = false;
            errors.push('Password requirements');
        }
        
        if (!$('#terms').is(':checked')) {
            $('#terms-error').fadeIn(300);
            isValid = false;
            errors.push('Terms and conditions');
        }

        return { isValid: isValid, errors: errors };
    }

    // Form submission
    $('#registrationForm').on('submit', function(e) {
        const validation = validateForm();
        
        if (!validation.isValid) {
            e.preventDefault();
            
            $('#error-alert').html(
                '<i class="fas fa-exclamation-circle"></i> Please correct the following: ' + 
                validation.errors.join(', ')
            ).fadeIn(300);
            
            const firstError = $('.error').first();
            if (firstError.length) {
                $('html, body').animate({
                    scrollTop: firstError.offset().top - 100
                }, 500, 'swing');
            }
            
            setTimeout(function() {
                $('#error-alert').fadeOut(300);
            }, 5000);
            
            return false;
        } else {
            $('#loadingOverlay').fadeIn(300).css('display', 'flex');
        }
    });
    
    // Reset button functionality with modern modal
    $('#resetBtn').on('click', function(e) {
        e.preventDefault();
        $('#resetModal').addClass('show');
    });

    // Modal event handlers
    $('#cancelReset').on('click', function() {
        $('#resetModal').removeClass('show');
    });

    $('#confirmReset').on('click', function() {
        // Reset the form
        $('#registrationForm')[0].reset();
        $('.error-message').fadeOut(300);
        $('.error, .success').removeClass('error success');
        $('.success-message').fadeOut(300);

        // Reset progress bars
        $('.step').removeClass('active completed');
        $('.progress-ring-fill').css('stroke-dashoffset', '157.08');

        // Reset password requirements
        window.passwordRequirements = {
            length: false,
            uppercase: false,
            lowercase: false,
            number: false,
            special: false
        };
        $('#passwordStrength').css('width', '0%').removeClass('weak medium strong');
        $('#passwordRequirements li').removeClass('met');

        // Close modal
        $('#resetModal').removeClass('show');
    });

    // Close modal when clicking outside
    $('#resetModal').on('click', function(e) {
        if (e.target === this) {
            $('#resetModal').removeClass('show');
        }
    });

    // Password strength calculation
    function calculatePasswordStrength(password) {
        if (!password) return 0;
        let strength = 0;
        strength += Math.min(password.length, 8) / 8 * 25;
        if (/[A-Z]/.test(password)) strength += 18.75;
        if (/[a-z]/.test(password)) strength += 18.75;
        if (/[0-9]/.test(password)) strength += 18.75;
        if (/[^A-Za-z0-9]/.test(password)) strength += 18.75;
        return Math.min(strength, 100);
    }

    function updatePasswordStrengthIndicator(strength) {
        const strengthBar = $('#passwordStrength');
        let color;
        if (strength === 0) color = '#ffffff';
        else if (strength < 25) color = '#ff4444';
        else if (strength < 50) color = '#ffbb33';
        else if (strength < 75) color = '#ffeb3b';
        else color = '#00C851';
        
        strengthBar.css({
            'width': strength + '%',
            'background-color': color,
            'transition': 'all 0.3s ease'
        });
    }

    // Input validation for names
    $('#firstname, #lastname').on('input', function() {
        let value = $(this).val().replace(/[^A-Za-z]/g, '');
        $(this).val(value);
    });

    $('#city, #state').on('input', function() {
        let value = $(this).val().replace(/[^a-zA-Z\s]/g, '');
        $(this).val(value);
    });

    $('#address').on('input', function() {
        let value = $(this).val().replace(/[^a-zA-Z0-9,;\-\(\)\s]/g, '');
        $(this).val(value);
    });

    // Phone number validation
    $('#phone').on('input', function() {
        let value = $(this).val().replace(/\D/g, '');
        if (value.length > 10) value = value.substring(0, 10);
        $(this).val(value);

        if (value && value.length !== 10) {
            $(this).addClass('error');
            $('#phone-error').fadeIn(300);
        } else {
            $(this).removeClass('error');
            $('#phone-error').fadeOut(300);
        }
    });

    // Email validation
    $('input[type="email"]').on('blur', function() {
        const email = $(this).val();
        const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
        
        if (email && !emailRegex.test(email)) {
            $(this).addClass('error');
            $('#email-error').fadeIn(300);
        }
    });

    // Modal functionality for terms and privacy
    $('.terms-link, .privacy-link').on('click', function(e) {
        e.preventDefault();
        const modal = $('<div class="modal" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.8); backdrop-filter: blur(10px); z-index: 10000; display: flex; align-items: center; justify-content: center; padding: 2rem;"></div>').html(
            '<div style="background: rgba(255,255,255,0.1); backdrop-filter: blur(25px); border-radius: 20px; padding: 2rem; max-width: 500px; width: 100%; border: 1px solid rgba(255,255,255,0.2); color: rgba(255,255,255,0.95);">' +
            '<h3 style="margin-bottom: 1rem; color: rgba(255,255,255,0.95);">Terms and Privacy Policy</h3>' +
            '<p>Terms and conditions content would be displayed here...</p>' +
            '<button class="close-modal" style="background: linear-gradient(45deg, #ff6b6b, #ee5a52, #ff8a80); color: white; border: none; padding: 0.75rem 1.5rem; border-radius: 12px; font-weight: 600; cursor: pointer; margin-top: 1rem; transition: all 0.3s ease;">Close</button>' +
            '</div>'
        );
        
        $('body').append(modal);
        modal.fadeIn(300);
        
        $('.close-modal').on('click', function() {
            modal.fadeOut(300, function() {
                $(this).remove();
            });
        });
    });

    // Add hover effects
    $('.btn-primary, .btn-secondary').hover(
        function() {
            $(this).addClass('animate__animated animate__pulse');
        },
        function() {
            $(this).removeClass('animate__animated animate__pulse');
        }
    );

    // Initialize tooltips
    $('.tooltip').hover(
        function() {
            $(this).find('.tooltip-content').stop().fadeIn(200);
        },
        function() {
            $(this).find('.tooltip-content').stop().fadeOut(200);
        }
    );

});