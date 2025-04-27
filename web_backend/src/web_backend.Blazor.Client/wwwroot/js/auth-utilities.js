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

/* Handle authentication management */
window.authHelpers = {
    // Check authentication status periodically
    startAuthenticationCheck: function() {
        console.log("Starting authentication status checker");
        
        // Check every 3 minutes (180000 ms)
        const intervalId = setInterval(function() {
            // Get the token expiry from storage
            const tokenExpiryStr = localStorage.getItem('oidc.expires_at');
            if (!tokenExpiryStr) {
                console.log("No token expiry found, user may not be logged in");
                return;
            }
            
            const tokenExpiry = new Date(parseInt(tokenExpiryStr));
            const now = new Date();
            
            // If token expires in less than 5 minutes (300 seconds)
            const expiresInSeconds = (tokenExpiry - now) / 1000;
            
            if (expiresInSeconds < 300 && expiresInSeconds > 0) {
                console.log(`Token expires in ${expiresInSeconds.toFixed(0)} seconds, preparing for refresh`);
                
                // If we're within 5 minutes of expiry but not already refreshing
                if (!localStorage.getItem('auth_refresh_in_progress')) {
                    // Mark that we're starting a refresh
                    localStorage.setItem('auth_refresh_in_progress', 'true');
                    
                    // Save the current URL to return after refresh
                    window.saveReturnUrl();
                    
                    console.log("Redirecting to login for token refresh");
                    
                    // Navigate to the authentication endpoint to force a new login
                    window.location.href = '/authentication/login?returnUrl=' + encodeURIComponent(window.location.pathname);
                }
            }
        }, 180000); // Check every 3 minutes
        
        return intervalId;
    },
    
    // Call this when the app initializes
    initializeAuthentication: function() {
        // Clear any previous refresh flags
        localStorage.removeItem('auth_refresh_in_progress');
        
        // Start the authentication checker
        this.authCheckInterval = this.startAuthenticationCheck();
        
        // Set the authentication status in the DOM for other scripts
        const isAuthenticated = this.checkIsAuthenticated();
        document.body.setAttribute('data-authenticated', isAuthenticated.toString());
        console.log("Authentication initialized, status:", isAuthenticated);
    },
    
    // Check if user is authenticated
    checkIsAuthenticated: function() {
        // Look for storage keys that indicate authentication
        const storagePrefix = 'oidc.user:https://vss-backend-api-fmbjgachhph9byce.westus2-01.azurewebsites.net:web_backend_Blazor';
        return localStorage.getItem(storagePrefix) !== null;
    }
};

// Initialize when the page loads
document.addEventListener('DOMContentLoaded', function() {
    window.authHelpers.initializeAuthentication();
});