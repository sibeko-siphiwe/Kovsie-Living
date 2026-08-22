const navbar = document.getElementById('navigation');

window.addEventListener('scroll', () => {
    if (window.scrollY > 50) {
        navbar.classList.add('scrolled');
    }
    else {
        navbar.classList.remove('scrolled');
    }
});

const mobileSidebar = document.getElementById('mobileSidebar');
const sidebarBackdrop = document.getElementById('sidebarBackdrop');
const menuToggleBtn = document.getElementById('menuToggleBtn');
const closeSidebarBtn = document.getElementById('closeSidebarBtn');

function openSidebar() {
    mobileSidebar.classList.add('show');
    sidebarBackdrop.classList.add('show');
    document.body.style.overflow = 'hidden';
}
 
function closeSidebar() {
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
 
document.addEventListener('click', function (e) {
    if (mobileSidebar && mobileSidebar.classList.contains('show') &&
        !mobileSidebar.contains(e.target) &&
        menuToggleBtn && !menuToggleBtn.contains(e.target)) {
        closeSidebar();
    }
});
 
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && mobileSidebar && mobileSidebar.classList.contains('show')) {
        closeSidebar();
    }
});
 
window.addEventListener('resize', function () {
    if (window.innerWidth >= 768 && mobileSidebar) {
        closeSidebar();
    }
});