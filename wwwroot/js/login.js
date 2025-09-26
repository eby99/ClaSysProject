// Login page JavaScript functionality

// Create floating particles
function createParticles() {
    const particlesContainer = document.getElementById('particles');
    const particleCount = 30;

    for (let i = 0; i < particleCount; i++) {
        const particle = document.createElement('div');
        particle.className = 'particle';

        const size = 2 + Math.random() * 6;
        particle.style.width = size + 'px';
        particle.style.height = size + 'px';
        particle.style.left = Math.random() * 100 + '%';

        const duration = 20 + Math.random() * 15;
        particle.style.animationDuration = duration + 's';
        particle.style.animationDelay = Math.random() * 20 + 's';

        particlesContainer.appendChild(particle);
    }
}

function togglePassword() {
    const passwordField = document.getElementById('loginPassword');
    const toggleIcon = document.getElementById('toggleIcon');

    if (passwordField.type === 'password') {
        passwordField.type = 'text';
        toggleIcon.classList.remove('fa-eye');
        toggleIcon.classList.add('fa-eye-slash');
    } else {
        passwordField.type = 'password';
        toggleIcon.classList.remove('fa-eye-slash');
        toggleIcon.classList.add('fa-eye');
    }
}

function showLoading() {
    const submitBtn = document.querySelector('button[type="submit"]');
    submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Signing In...';
    submitBtn.disabled = true;
}

function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
}

document.addEventListener('DOMContentLoaded', function() {
    createParticles();

    // Auto-focus on username field
    const usernameField = document.querySelector('[name="UsernameEmail"]');
    if (usernameField) {
        usernameField.focus();
    }

    // Check for remember me cookie
    const rememberUser = getCookie('RememberUser');
    if (rememberUser && usernameField) {
        usernameField.value = rememberUser;
        const rememberCheckbox = document.querySelector('[name="RememberMe"]');
        if (rememberCheckbox) {
            rememberCheckbox.checked = true;
        }
    }

    // Add input animations and ensure white text color
    const inputs = document.querySelectorAll('.form-control');
    inputs.forEach(input => {
        // Force white text color immediately
        input.style.color = 'white';
        input.style.webkitTextFillColor = 'white';

        input.addEventListener('focus', function() {
            this.parentElement.style.transform = 'scale(1.02)';
            // Ensure white text on focus
            this.style.color = 'white';
            this.style.webkitTextFillColor = 'white';
        });

        input.addEventListener('blur', function() {
            // Ensure white text when losing focus and reset scale
            this.style.color = 'white';
            this.style.webkitTextFillColor = 'white';
            this.parentElement.style.transform = 'scale(1)';
        });

        input.addEventListener('input', function() {
            // Ensure white text while typing
            this.style.color = 'white';
            this.style.webkitTextFillColor = 'white';
        });
    });

    // Continuous monitoring to ensure white text color
    setInterval(function() {
        const allInputs = document.querySelectorAll('.form-control');
        allInputs.forEach(input => {
            if (input.style.color !== 'white' || input.style.webkitTextFillColor !== 'white') {
                input.style.color = 'white';
                input.style.webkitTextFillColor = 'white';
            }
        });
    }, 100);

    // Form submission enhancement
    const form = document.getElementById('loginForm');
    if (form) {
        form.addEventListener('submit', function(e) {
            const inputs = form.querySelectorAll('input[required]');
            let isValid = true;

            inputs.forEach(input => {
                if (!input.value.trim()) {
                    input.style.borderColor = 'var(--error-color)';
                    input.classList.add('shake');
                    isValid = false;
                } else {
                    input.style.borderColor = 'rgba(255, 255, 255, 0.2)';
                    input.classList.remove('shake');
                }
            });

            if (!isValid) {
                e.preventDefault();
                const errorMsg = document.getElementById('error-message');
                const errorText = document.getElementById('error-text');
                if (errorText) {
                    errorText.textContent = 'Please fill in all required fields.';
                }
                if (errorMsg) {
                    errorMsg.classList.remove('d-none');
                    errorMsg.classList.add('shake');
                }

                setTimeout(() => {
                    inputs.forEach(input => input.classList.remove('shake'));
                    if (errorMsg) {
                        errorMsg.classList.remove('shake');
                    }
                }, 500);
            }
        });
    }
});

// Function to handle model state errors (called from Razor)
function showModelErrors(errorMessages) {
    if (errorMessages && errorMessages.length > 0) {
        const errorText = document.getElementById('error-text');
        const errorMessage = document.getElementById('error-message');

        if (errorText) {
            errorText.textContent = errorMessages.join(' ');
        }
        if (errorMessage) {
            errorMessage.classList.remove('d-none');
            errorMessage.classList.add('shake');
        }
    }
}