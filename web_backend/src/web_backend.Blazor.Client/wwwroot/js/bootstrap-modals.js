/**
 * Bootstrap modal helper functions for Blazor components
 */

// Show a Bootstrap modal by its ID
function showModal(modalId) {
    const modalElement = document.getElementById(modalId);
    if (modalElement) {
        const modal = new bootstrap.Modal(modalElement);
        modal.show();
    }
}

// Hide a Bootstrap modal by its ID
function hideModal(modalId) {
    const modalElement = document.getElementById(modalId);
    if (modalElement) {
        const modal = bootstrap.Modal.getInstance(modalElement);
        if (modal) {
            modal.hide();
        }
    }
}

// Export functions to make them available to Blazor
window.showModal = showModal;
window.hideModal = hideModal;
