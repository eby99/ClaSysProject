        $(document).ready(function() {
            // Generate floating particles
            function createParticles() {
                const particlesContainer = $('#particles');
                for (let i = 0; i < 20; i++) {
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
            createParticles();

            // Auto-hide success alert
            setTimeout(function() {
                $('.alert').fadeOut(500);
            }, 5000);

            // Add hover animations to buttons
            $('.btn').hover(
                function() {
                    $(this).addClass('animate__animated animate__pulse');
                },
                function() {
                    $(this).removeClass('animate__animated animate__pulse');
                }
            );
        });