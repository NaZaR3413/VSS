/**
 * Auth Token Manager - Handles authentication token storage and operations
 */
window.authTokenManager = (function() {
    const ACCESS_TOKEN_KEY = 'auth_access_token';
    const REFRESH_TOKEN_KEY = 'auth_refresh_token';
    const TOKEN_EXPIRY_KEY = 'auth_token_expiry';

    return {
        /**
         * Store authentication data in local storage
         * @param {string} accessToken - JWT access token
         * @param {string} refreshToken - Refresh token for getting new access tokens
         * @param {number} expiresIn - Token expiry in seconds
         */
        storeAuthData: function(accessToken, refreshToken, expiresIn) {
            if (!accessToken) {
                console.error('Cannot store null or empty access token');
                return;
            }

            // Calculate expiry time in milliseconds since epoch
            const expiryTime = Date.now() + (expiresIn * 1000);
            
            // Store in local storage
            localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
            
            if (refreshToken) {
                localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
            }
            
            localStorage.setItem(TOKEN_EXPIRY_KEY, expiryTime.toString());
            
            console.log('Authentication data stored successfully');
        },

        /**
         * Clear all authentication data from local storage
         */
        clearAuthData: function() {
            localStorage.removeItem(ACCESS_TOKEN_KEY);
            localStorage.removeItem(REFRESH_TOKEN_KEY);
            localStorage.removeItem(TOKEN_EXPIRY_KEY);
            console.log('Authentication data cleared');
        },

        /**
         * Check if user is authenticated with a valid, non-expired token
         * @returns {boolean} True if authenticated with valid token
         */
        isAuthenticated: function() {
            const token = localStorage.getItem(ACCESS_TOKEN_KEY);
            const expiry = localStorage.getItem(TOKEN_EXPIRY_KEY);
            
            if (!token || !expiry) {
                return false;
            }
            
            // Check if token is expired
            const expiryTime = parseInt(expiry, 10);
            const currentTime = Date.now();
            
            if (currentTime >= expiryTime) {
                console.log('Token expired, clearing auth data');
                this.clearAuthData();
                return false;
            }
            
            return true;
        },

        /**
         * Get the access token
         * @returns {string|null} Access token or null if not authenticated
         */
        getAccessToken: function() {
            if (!this.isAuthenticated()) {
                return null;
            }
            
            return localStorage.getItem(ACCESS_TOKEN_KEY);
        },

        /**
         * Get refresh token
         * @returns {string|null} Refresh token or null if not available
         */
        getRefreshToken: function() {
            return localStorage.getItem(REFRESH_TOKEN_KEY);
        },

        /**
         * Parse the token and get claims information from it
         * @returns {string} JSON string with token claims
         */
        getTokenInfo: function() {
            const token = this.getAccessToken();
            
            if (!token) {
                return null;
            }
            
            try {
                // Get the payload part of the JWT (second part)
                const payload = token.split('.')[1];
                
                // Base64url decode the payload
                const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
                const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
                    return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
                }).join(''));
                
                return jsonPayload;
            } catch (error) {
                console.error('Error parsing token:', error);
                return null;
            }
        }
    };
})();

/**
 * Add authentication token to HTTP requests
 * @param {object} config - HTTP request configuration object
 * @returns {object} Updated configuration object with Authorization header
 */
function addAuthHeader(config) {
    if (window.authTokenManager.isAuthenticated()) {
        const token = window.authTokenManager.getAccessToken();
        if (token) {
            return {
                ...config,
                headers: {
                    ...config.headers,
                    'Authorization': `Bearer ${token}`
                }
            };
        }
    }
    return config;
}