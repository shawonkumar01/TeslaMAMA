
document.addEventListener("DOMContentLoaded", () => {
    const menuToggle = document.querySelector(".tm-menu-toggle");
    const navLinks = document.querySelector("#tm-nav-links");

    if (!menuToggle || !navLinks) {
        return;
    }

    menuToggle.addEventListener("click", () => {
        const isOpen = navLinks.classList.toggle("tm-nav-open");

        menuToggle.setAttribute("aria-expanded", String(isOpen));
        menuToggle.setAttribute(
            "aria-label",
            isOpen ? "Close navigation menu" : "Open navigation menu"
        );
    });

    navLinks.querySelectorAll("a").forEach((link) => {
        link.addEventListener("click", () => {
            navLinks.classList.remove("tm-nav-open");
            menuToggle.setAttribute("aria-expanded", "false");
            menuToggle.setAttribute("aria-label", "Open navigation menu");
        });
    });
});

// Notification system
function showNotification(title, message, type = 'info', duration = 5000) {
    const container = document.getElementById('tm-notification-container');
    if (!container) return;

    const notification = document.createElement('div');
    notification.className = `tm-notification tm-notification-${type}`;

    const icons = {
        success: '✓',
        error: '✕',
        info: 'ℹ'
    };

    notification.innerHTML = `
        <span class="tm-notification-icon">${icons[type] || icons.info}</span>
        <div class="tm-notification-content">
            <div class="tm-notification-title">${title}</div>
            <div class="tm-notification-message">${message}</div>
        </div>
        <button class="tm-notification-close" aria-label="Close notification">×</button>
    `;

    const closeButton = notification.querySelector('.tm-notification-close');
    closeButton.addEventListener('click', () => {
        notification.remove();
    });

    container.appendChild(notification);

    // Auto-remove after duration
    setTimeout(() => {
        if (notification.parentElement) {
            notification.style.animation = 'tm-slide-in 0.3s ease-out reverse';
            setTimeout(() => notification.remove(), 300);
        }
    }, duration);
}
