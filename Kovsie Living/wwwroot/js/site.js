document.addEventListener('DOMContentLoaded', function () {

    const navbar = document.getElementById('navigation');

    const currentPath = window.location.pathname.toLowerCase().replace(/\/$/, '');
    const allLinks = document.querySelectorAll('.sidebar-menu a');

    let bestMatch = null;
    let bestMatchLength = 0;

    allLinks.forEach(link => {
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
        allLinks.forEach(link => {
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
        window.addEventListener('scroll', function () {
            if (window.scrollY > 50) {
                navbar.classList.add('scrolled');
            } else {
                navbar.classList.remove('scrolled');
            }
        });
    }

    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(alert => {
        setTimeout(function () {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert.close();
        }, 5000);
    });

});