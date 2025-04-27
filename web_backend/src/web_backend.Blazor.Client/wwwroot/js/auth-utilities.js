/* Authentication utilities */
window.saveReturnUrl = function() {
    // Save the current URL (minus any authentication parameters) to return to after login
    const currentUrl = window.location.href.split('?')[0]; // Remove query params
    localStorage.setItem('vss_auth_return_url', currentUrl);
    console.log('Saved return URL:', currentUrl);
    return currentUrl;
};

/* Handle login required errors automatically */
window.handleLoginRequired = function() {
    window.saveReturnUrl();
    // Redirect to the login page
    window.location.href = '/authentication/login';
    return true;
};