document.addEventListener('DOMContentLoaded', function () {

    const navbar = document.getElementById('navigation');
    const mobileSidebar = document.getElementById('mobileSidebar');
    const sidebarBackdrop = document.getElementById('sidebarBackdrop');
    const menuToggleBtn = document.getElementById('menuToggleBtn');
    const closeSidebarBtn = document.getElementById('closeSidebarBtn');

    function openSidebar() {
        if (!mobileSidebar || !sidebarBackdrop) return;
        mobileSidebar.classList.add('show');
        sidebarBackdrop.classList.add('show');
        document.body.style.overflow = 'hidden';
    }

    function closeSidebar() {
        if (!mobileSidebar || !sidebarBackdrop) return;
        mobileSidebar.classList.remove('show');
        sidebarBackdrop.classList.remove('show');
        document.body.style.overflow = '';
    }

    if (menuToggleBtn) {
        menuToggleBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            if (mobileSidebar.classList.contains('show')) {
                closeSidebar();
            } else {
                openSidebar();
            }
        });
    }

    if (closeSidebarBtn) {
        closeSidebarBtn.addEventListener('click', closeSidebar);
    }

    if (sidebarBackdrop) {
        sidebarBackdrop.addEventListener('click', closeSidebar);
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && mobileSidebar && mobileSidebar.classList.contains('show')) {
            closeSidebar();
        }
    });

    window.addEventListener('resize', function () {
        if (window.innerWidth >= 992 && mobileSidebar) {
            closeSidebar();
        }
    });

    const currentPath = window.location.pathname.toLowerCase().replace(/\/$/, '');
    const allSidebarLinks = document.querySelectorAll('.sidebar-link');

    let bestMatch = null;
    let bestMatchLength = 0;

    allSidebarLinks.forEach(link => {
        const href = link.getAttribute('href')?.toLowerCase().replace(/\/$/, '');
        if (!href) return;

        if (currentPath === href) {
            if (href.length > bestMatchLength) {
                bestMatch = link;
                bestMatchLength = href.length;
            }
        }
    });

    if (!bestMatch) {
        allSidebarLinks.forEach(link => {
            const href = link.getAttribute('href')?.toLowerCase().replace(/\/$/, '');
            if (!href || href === '/') return;

            if (currentPath.startsWith(href) && href.length > bestMatchLength) {
                bestMatch = link;
                bestMatchLength = href.length;
            }
        });
    }

    if (bestMatch) {
        bestMatch.classList.add('active');
    }

    if (navbar) {
        window.addEventListener('scroll', () => {
            if (window.scrollY > 50) {
                navbar.classList.add('scrolled');
            } else {
                navbar.classList.remove('scrolled');
            }
        });
    }

    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert.close();
        }, 5000);
    });

});