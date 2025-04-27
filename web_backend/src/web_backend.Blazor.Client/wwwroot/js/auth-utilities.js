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

/* Handle silent token refresh */
window.authHelpers = {
    // Check authentication status periodically
    startAuthenticationCheck: function() {
        console.log("Starting authentication status checker");
        
        // Check every 3 minutes (180000 ms)
        const intervalId = setInterval(function() {
            const tokenExpiryStr = localStorage.getItem('oidc.expires_at');
            if (!tokenExpiryStr) {
                console.log("No token expiry found, user may not be logged in");
                return;
            }
            
            const tokenExpiry = new Date(parseInt(tokenExpiryStr));
            const now = new Date();
            
            // If token expires in less than 5 minutes (300 seconds)
            const expiresInSeconds = (tokenExpiry - now) / 1000;
            console.log(`Token expires in ${expiresInSeconds.toFixed(0)} seconds`);
            
            if (expiresInSeconds < 300 && expiresInSeconds > 0) {
                console.log("Token expiring soon, redirecting to login for refresh");
                // Save the current URL to return after refresh
                window.saveReturnUrl();
                // Redirect to authentication endpoint which will refresh the token
                window.location.href = '/authentication/login?returnUrl=' + encodeURIComponent(window.location.pathname);
            }
        }, 180000); // Check every 3 minutes
        
        return intervalId;
    },
    
    // Call this when the app initializes
    initializeAuthentication: function() {
        // Start the authentication checker
        this.authCheckInterval = this.startAuthenticationCheck();
        
        // Set the authentication status in the DOM for other scripts
        const isAuthenticated = localStorage.getItem('oidc.user:https://vss-backend-api-fmbjgachhph9byce.westus2-01.azurewebsites.net:web_backend_Blazor') !== null;
        document.body.setAttribute('data-authenticated', isAuthenticated.toString());
        console.log("Authentication initialized, status:", isAuthenticated);
    }
};

// Initialize when the page loads
document.addEventListener('DOMContentLoaded', function() {
    window.authHelpers.initializeAuthentication();
});