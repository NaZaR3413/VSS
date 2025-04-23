// Existing toast notification function
window.showNotification = function (message) {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'position-fixed bottom-0 end-0 p-3';
        container.style.zIndex = '11';
        document.body.appendChild(container);
    }

    const toastId = 'toast-' + new Date().getTime();
    const toast = document.createElement('div');
    toast.id = toastId;
    toast.className = 'toast';
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');

    toast.innerHTML = `
        <div class="toast-header bg-primary text-white">
            <strong class="me-auto">Livestream Information</strong>
            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
        <div class="toast-body">
            ${message}
        </div>
    `;

    container.appendChild(toast);

    const bsToast = new bootstrap.Toast(toast, {
        autohide: true,
        delay: 5000
    });
    bsToast.show();

    toast.addEventListener('hidden.bs.toast', function () {
        toast.remove();
        if (container.children.length === 0) {
            container.remove();
        }
    });
};

// New banner notification function
window.showBannerNotification = function (message) {
    // Get the banner element
    const banner = document.getElementById('livestream-banner');
    if (!banner) return;

    // Set the message
    const messageElement = document.getElementById('livestream-banner-message');
    if (messageElement) {
        messageElement.textContent = message;
    }

    // Show the banner with a slide-down animation
    banner.style.display = 'block';

    // Add animation
    banner.style.animation = 'none';
    banner.offsetHeight; // Trigger reflow
    banner.style.animation = 'slideInDown 0.5s ease-in-out';

    // Auto-hide after 15 seconds if user doesn't close it manually
    setTimeout(() => {
        if (banner.style.display !== 'none') {
            banner.style.animation = 'fadeOut 0.5s ease-in-out forwards';
            setTimeout(() => {
                banner.style.display = 'none';
            }, 500);
        }
    }, 15000);
};

// Add these CSS animations to document
(function () {
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideInDown {
            from { transform: translateY(-100%); opacity: 0; }
            to { transform: translateY(0); opacity: 1; }
        }
        
        @keyframes fadeOut {
            from { opacity: 1; }
            to { opacity: 0; }
        }
    `;
    document.head.appendChild(style);
})();
