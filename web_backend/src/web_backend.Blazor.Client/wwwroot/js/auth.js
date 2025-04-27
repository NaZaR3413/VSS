// Authentication token management
window.authTokenManager = {
    // Get the access token from localStorage
    getAccessToken: function() {
        return localStorage.getItem('access_token');
    },
    
    // Check if the user is authenticated
    isAuthenticated: function() {
        const token = this.getAccessToken();
        if (!token) {
            return false;
        }
        
        // Check if token has expired
        const expiryStr = localStorage.getItem('token_expiry');
        if (expiryStr) {
            const expiry = new Date(expiryStr);
            if (expiry < new Date()) {
                console.log('Token has expired');
                return false;
            }
        }
        
        return true;
    },
    
    // Add the authorization header to fetch requests
    applyAuthHeader: function(headers) {
        const token = this.getAccessToken();
        if (token) {
            headers.Authorization = `Bearer ${token}`;
        }
        return headers;
    },
    
    // Clear all auth data (for logout)
    clearAuthData: function() {
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
        localStorage.removeItem('token_expiry');
    }
};

// Configure fetch API to include auth tokens in requests
const originalFetch = window.fetch;
window.fetch = function(url, options = {}) {
    // Create headers object if it doesn't exist
    options.headers = options.headers || {};
    
    // Don't add auth header for token endpoint
    if (!url.includes('/connect/token')) {
        // Add the Authorization header if we have a token
        const token = window.authTokenManager.getAccessToken();
        if (token) {
            options.headers.Authorization = `Bearer ${token}`;
        }
    }
    
    // Make the fetch request with the modified options
    return originalFetch(url, options);
};

// Initialize on page load
window.addEventListener('DOMContentLoaded', function() {
    console.log('Auth token manager initialized');
    // Check if authenticated
    const isAuthenticated = window.authTokenManager.isAuthenticated();
    console.log('Is authenticated:', isAuthenticated);
});