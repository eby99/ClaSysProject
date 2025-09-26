// Three.js Enhanced Scene
let scene, camera, renderer, composer;
let mouseX = 0, mouseY = 0;
let targetMouseX = 0, targetMouseY = 0;
let time = 0;

function init() {
    // Scene setup
    scene = new THREE.Scene();
    scene.fog = new THREE.Fog(0x000000, 10, 50);

    // Camera with better positioning
    camera = new THREE.PerspectiveCamera(60, window.innerWidth / window.innerHeight, 0.1, 100);
    camera.position.set(0, 0, 8);

    // Enhanced renderer
    renderer = new THREE.WebGLRenderer({
        alpha: true,
        antialias: true,
        powerPreference: "high-performance"
    });
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.setClearColor(0x000000, 0);
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;

    document.getElementById('three-container').appendChild(renderer.domElement);

    // Create enhanced floating elements
    createFloatingElements();

    // Event listeners
    document.addEventListener('mousemove', onMouseMove, { passive: true });
    window.addEventListener('resize', onWindowResize, { passive: true });

    animate();
}

function createFloatingElements() {
    const elements = [];
    const geometries = [
        new THREE.IcosahedronGeometry(0.8, 1),
        new THREE.OctahedronGeometry(0.6),
        new THREE.TetrahedronGeometry(0.7),
        new THREE.DodecahedronGeometry(0.5),
        new THREE.SphereGeometry(0.4, 16, 12),
        new THREE.TorusGeometry(0.5, 0.2, 8, 16)
    ];

    // Create main floating elements
    for (let i = 0; i < 20; i++) {
        const geometry = geometries[Math.floor(Math.random() * geometries.length)];

        const material = new THREE.MeshPhongMaterial({
            color: new THREE.Color().setHSL(
                0.6 + Math.random() * 0.4,
                0.7 + Math.random() * 0.3,
                0.6 + Math.random() * 0.4
            ),
            transparent: true,
            opacity: 0.3 + Math.random() * 0.4,
            shininess: 100,
            wireframe: Math.random() > 0.7,
            emissive: new THREE.Color().setHSL(
                0.6 + Math.random() * 0.4,
                0.3,
                0.1
            )
        });

        const element = new THREE.Mesh(geometry, material);

        // Positioning in 3D space
        const radius = 15 + Math.random() * 10;
        const theta = Math.random() * Math.PI * 2;
        const phi = Math.random() * Math.PI;

        element.position.x = radius * Math.sin(phi) * Math.cos(theta);
        element.position.y = radius * Math.sin(phi) * Math.sin(theta);
        element.position.z = radius * Math.cos(phi) - 5;

        element.rotation.set(
            Math.random() * Math.PI,
            Math.random() * Math.PI,
            Math.random() * Math.PI
        );

        // Animation properties
        element.userData = {
            originalPosition: element.position.clone(),
            rotationSpeed: {
                x: (Math.random() - 0.5) * 0.02,
                y: (Math.random() - 0.5) * 0.02,
                z: (Math.random() - 0.5) * 0.02
            },
            floatSpeed: 0.5 + Math.random() * 0.5,
            floatAmplitude: 0.5 + Math.random() * 1.5,
            phase: Math.random() * Math.PI * 2
        };

        scene.add(element);
        elements.push(element);
    }

    // Add ambient and directional lighting
    const ambientLight = new THREE.AmbientLight(0x6366f1, 0.4);
    scene.add(ambientLight);

    const directionalLight1 = new THREE.DirectionalLight(0x8b5cf6, 0.6);
    directionalLight1.position.set(5, 5, 5);
    scene.add(directionalLight1);

    const directionalLight2 = new THREE.DirectionalLight(0xff6b9d, 0.4);
    directionalLight2.position.set(-5, -5, 3);
    scene.add(directionalLight2);

    // Add point lights for dynamic lighting
    const pointLight1 = new THREE.PointLight(0x667eea, 0.8, 20);
    pointLight1.position.set(10, 10, 5);
    scene.add(pointLight1);

    const pointLight2 = new THREE.PointLight(0xf093fb, 0.6, 25);
    pointLight2.position.set(-8, -8, 3);
    scene.add(pointLight2);

    window.floatingElements = elements;
}

function onMouseMove(event) {
    targetMouseX = (event.clientX / window.innerWidth) * 2 - 1;
    targetMouseY = -(event.clientY / window.innerHeight) * 2 + 1;
}

function onWindowResize() {
    camera.aspect = window.innerWidth / window.innerHeight;
    camera.updateProjectionMatrix();
    renderer.setSize(window.innerWidth, window.innerHeight);
}

function animate() {
    requestAnimationFrame(animate);

    time += 0.01;

    // Smooth mouse interpolation
    mouseX += (targetMouseX - mouseX) * 0.02;
    mouseY += (targetMouseY - mouseY) * 0.02;

    // Animate floating elements
    if (window.floatingElements) {
        window.floatingElements.forEach((element, index) => {
            const userData = element.userData;

            // Complex floating motion
            element.position.x = userData.originalPosition.x +
                Math.sin(time * userData.floatSpeed + userData.phase) * userData.floatAmplitude +
                mouseX * 2;
            element.position.y = userData.originalPosition.y +
                Math.cos(time * userData.floatSpeed * 0.7 + userData.phase) * userData.floatAmplitude * 0.8 +
                mouseY * 1.5;
            element.position.z = userData.originalPosition.z +
                Math.sin(time * userData.floatSpeed * 0.5 + userData.phase) * 0.5;

            // Smooth rotation
            element.rotation.x += userData.rotationSpeed.x + mouseY * 0.001;
            element.rotation.y += userData.rotationSpeed.y + mouseX * 0.001;
            element.rotation.z += userData.rotationSpeed.z;

            // Dynamic opacity based on distance
            const distance = camera.position.distanceTo(element.position);
            element.material.opacity = Math.max(0.1, Math.min(0.7, 10 / distance));
        });
    }

    // Smooth camera movement
    camera.position.x += (mouseX * 1.5 - camera.position.x) * 0.02;
    camera.position.y += (mouseY * 1.2 - camera.position.y) * 0.02;

    // Camera breathing effect
    camera.position.z = 8 + Math.sin(time * 0.5) * 0.5;

    renderer.render(scene, camera);
}

function createAdvancedParticles() {
    const particlesContainer = document.getElementById('particles');
    const particleTypes = ['particle-1', 'particle-2', 'particle-3'];
    const particleCount = 50;

    for (let i = 0; i < particleCount; i++) {
        const particle = document.createElement('div');
        particle.className = `particle ${particleTypes[Math.floor(Math.random() * particleTypes.length)]}`;

        const size = 2 + Math.random() * 8;
        particle.style.width = size + 'px';
        particle.style.height = size + 'px';
        particle.style.left = Math.random() * 100 + '%';

        const duration = 15 + Math.random() * 20;
        particle.style.animationDuration = duration + 's';
        particle.style.animationDelay = Math.random() * 20 + 's';

        particlesContainer.appendChild(particle);
    }
}

// Enhanced button interactions
function initButtonEffects() {
    const buttons = document.querySelectorAll('.hero-button');

    buttons.forEach(button => {
        button.addEventListener('mouseenter', (e) => {
            e.target.style.transform = 'translateY(-8px) scale(1.05)';
        });

        button.addEventListener('mouseleave', (e) => {
            e.target.style.transform = 'translateY(0) scale(1)';
        });

        button.addEventListener('click', (e) => {
            const href = e.target.getAttribute('href') || e.target.closest('a')?.getAttribute('href');

            if (href) {
                e.preventDefault();

                // Create ripple effect
                const ripple = document.createElement('span');
                const rect = e.target.getBoundingClientRect();
                const size = Math.max(rect.width, rect.height);
                const x = e.clientX - rect.left - size / 2;
                const y = e.clientY - rect.top - size / 2;

                ripple.style.position = 'absolute';
                ripple.style.borderRadius = '50%';
                ripple.style.background = 'rgba(255, 255, 255, 0.6)';
                ripple.style.transform = 'scale(0)';
                ripple.style.animation = 'ripple 0.6s linear';
                ripple.style.left = x + 'px';
                ripple.style.top = y + 'px';
                ripple.style.width = size + 'px';
                ripple.style.height = size + 'px';
                ripple.style.pointerEvents = 'none';

                e.target.appendChild(ripple);

                // Navigate after ripple effect
                setTimeout(() => {
                    window.location.href = href;
                }, 300);
            }
        });
    });
}

// Parallax scroll effect for mobile
function initParallaxEffects() {
    let ticking = false;

    function updateParallax() {
        const scrolled = window.pageYOffset;
        const parallaxElements = document.querySelectorAll('.hero-content, .features-preview');

        parallaxElements.forEach(el => {
            const speed = el.getAttribute('data-speed') || 0.5;
            const yPos = -(scrolled * speed);
            el.style.transform = `translateY(${yPos}px)`;
        });

        ticking = false;
    }

    function requestTick() {
        if (!ticking) {
            requestAnimationFrame(updateParallax);
            ticking = true;
        }
    }

    window.addEventListener('scroll', requestTick, { passive: true });
}

// Advanced cursor trail effect
function createCursorTrail() {
    const trail = [];
    const trailLength = 20;

    for (let i = 0; i < trailLength; i++) {
        const dot = document.createElement('div');
        dot.style.position = 'fixed';
        dot.style.width = '4px';
        dot.style.height = '4px';
        dot.style.background = `rgba(255, 255, 255, ${0.8 - (i * 0.04)})`;
        dot.style.borderRadius = '50%';
        dot.style.pointerEvents = 'none';
        dot.style.zIndex = '9999';
        dot.style.transition = 'all 0.1s ease-out';
        document.body.appendChild(dot);
        trail.push(dot);
    }

    let mouseX = 0, mouseY = 0;

    document.addEventListener('mousemove', (e) => {
        mouseX = e.clientX;
        mouseY = e.clientY;
    });

    function animateTrail() {
        let x = mouseX, y = mouseY;

        trail.forEach((dot, index) => {
            dot.style.left = x + 'px';
            dot.style.top = y + 'px';
            dot.style.transform = `scale(${1 - index * 0.05})`;

            const nextDot = trail[index + 1] || trail[0];
            x += (parseFloat(nextDot.style.left) - x) * 0.3;
            y += (parseFloat(nextDot.style.top) - y) * 0.3;
        });

        requestAnimationFrame(animateTrail);
    }

    animateTrail();
}

// Initialize everything with enhanced loading
document.addEventListener('DOMContentLoaded', function() {
    // Add CSS for ripple animation
    const style = document.createElement('style');
    style.textContent = `
        @keyframes ripple {
            to {
                transform: scale(4);
                opacity: 0;
            }
        }

        @keyframes float-in {
            0% {
                opacity: 0;
                transform: translateY(100px) rotate(180deg);
            }
            100% {
                opacity: 1;
                transform: translateY(0) rotate(0deg);
            }
        }

        .cursor-trail {
            position: fixed;
            width: 10px;
            height: 10px;
            background: radial-gradient(circle, rgba(255,255,255,0.8) 0%, transparent 70%);
            border-radius: 50%;
            pointer-events: none;
            z-index: 9999;
            mix-blend-mode: difference;
        }

        /* Enhanced mobile optimizations */
        @media (max-width: 768px) {
            .cursor-trail { display: none; }

            .hero-button {
                transform: none !important;
                transition: background-color 0.3s ease, border-color 0.3s ease;
            }

            .hero-button:hover {
                transform: none !important;
            }

            .hero-button:active {
                transform: scale(0.98) !important;
            }
        }

        /* Accessibility improvements */
       @media (prefers-reduced-motion: reduce) {
            *, *::before, *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
            }

            .floating-particles {
                display: none;
            }
        }

        /* High contrast mode support */
        @media (prefers-contrast: high) {
            .hero-content {
                background: rgba(0, 0, 0, 0.8);
                border: 2px solid white;
            }

            .hero-title {
                color: white;
                text-shadow: none;
            }

            .hero-subtitle {
                color: white;
            }
        }

        /* Focus styles for accessibility */
        .hero-button:focus {
            outline: 3px solid rgba(255, 255, 255, 0.8);
            outline-offset: 3px;
        }

        /* Loading shimmer effect */
        .shimmer {
            background: linear-gradient(90deg,
                rgba(255,255,255,0.1) 0%,
                rgba(255,255,255,0.3) 50%,
                rgba(255,255,255,0.1) 100%);
            background-size: 200% 100%;
            animation: shimmer 2s infinite;
        }

        @keyframes shimmer {
            0% { background-position: -200% 0; }
            100% { background-position: 200% 0; }
        }
    `;
    document.head.appendChild(style);

    // Initialize all components
    setTimeout(() => {
        init();
        createAdvancedParticles();
        initButtonEffects();
        initParallaxEffects();

        // Only add cursor trail on desktop
        if (window.innerWidth > 768) {
            createCursorTrail();
        }

        // Add shimmer effect to buttons initially
        document.querySelectorAll('.hero-button').forEach(button => {
            button.classList.add('shimmer');
            setTimeout(() => {
                button.classList.remove('shimmer');
            }, 3000);
        });

        // Performance monitoring
        if ('requestIdleCallback' in window) {
        }

    }, 100);

    // Auto-hide logout message after 5 seconds
    const logoutMessage = document.querySelector('[style*="rgba(40, 167, 69, 0.9)"]');
    if (logoutMessage) {
        setTimeout(() => {
            logoutMessage.style.opacity = '0';
            logoutMessage.style.transform = 'translateX(-50%) translateY(-100%)';
            logoutMessage.style.transition = 'all 0.5s ease-out';
            setTimeout(() => {
                logoutMessage.style.display = 'none';
            }, 500);
        }, 5000);
    }

    // Service worker for performance (if needed)
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', () => {
            navigator.serviceWorker.register('/sw.js')
                .catch(err => {});
        });
    }
});

// Enhanced performance optimizations
const observerOptions = {
    root: null,
    rootMargin: '0px',
    threshold: 0.1
};

const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            entry.target.style.animationPlayState = 'running';
        } else {
            entry.target.style.animationPlayState = 'paused';
        }
    });
}, observerOptions);

// Observe animated elements for performance
document.querySelectorAll('.particle, .hero-content, .floating-particles').forEach(el => {
    observer.observe(el);
});

// Cleanup function for memory management
window.addEventListener('beforeunload', () => {
    if (window.floatingElements) {
        window.floatingElements.forEach(element => {
            scene.remove(element);
            element.geometry.dispose();
            element.material.dispose();
        });
    }

    if (renderer) {
        renderer.dispose();
    }
});

// Add touch gestures for mobile
let touchStartX = 0;
let touchStartY = 0;

document.addEventListener('touchstart', (e) => {
    touchStartX = e.touches[0].clientX;
    touchStartY = e.touches[0].clientY;
}, { passive: true });

document.addEventListener('touchmove', (e) => {
    if (!touchStartX || !touchStartY) return;

    const touchEndX = e.touches[0].clientX;
    const touchEndY = e.touches[0].clientY;

    const deltaX = (touchEndX - touchStartX) / window.innerWidth;
    const deltaY = (touchEndY - touchStartY) / window.innerHeight;

    // Update mouse position for mobile interactions
    targetMouseX = deltaX * 2;
    targetMouseY = -deltaY * 2;
}, { passive: true });