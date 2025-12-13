// Mobile menu functionality
document.addEventListener('DOMContentLoaded', function() {
    const hamburger = document.querySelector('.hamburger');
    const nav = document.querySelector('nav');
    const navItems = document.querySelectorAll('.nav-item');

    // Toggle mobile menu
    if (hamburger) {
        hamburger.addEventListener('click', function(e) {
            e.stopPropagation();
            this.classList.toggle('active');
            nav.classList.toggle('mobile-menu-open');
        });
    }

    // Handle dropdown clicks on mobile
    navItems.forEach(item => {
        const link = item.querySelector('a');
        if (link) {
            link.addEventListener('click', function(e) {
                // Only handle dropdown toggle on mobile
                if (window.innerWidth <= 768) {
                    e.preventDefault();
                    e.stopPropagation();

                    // Close other dropdowns
                    navItems.forEach(otherItem => {
                        if (otherItem !== item) {
                            otherItem.classList.remove('mobile-dropdown-open');
                        }
                    });

                    // Toggle current dropdown
                    item.classList.toggle('mobile-dropdown-open');
                }
            });
        }
    });

    // Close mobile menu when clicking outside
    document.addEventListener('click', function(e) {
        if (window.innerWidth <= 768 && nav && hamburger) {
            if (!nav.contains(e.target) && !hamburger.contains(e.target)) {
                nav.classList.remove('mobile-menu-open');
                hamburger.classList.remove('active');
                navItems.forEach(item => {
                    item.classList.remove('mobile-dropdown-open');
                });
            }
        }
    });

    // Close mobile menu on window resize if switching to desktop
    window.addEventListener('resize', function() {
        if (window.innerWidth > 768) {
            if (nav) nav.classList.remove('mobile-menu-open');
            if (hamburger) hamburger.classList.remove('active');
            navItems.forEach(item => {
                item.classList.remove('mobile-dropdown-open');
            });
        }
    });
});
