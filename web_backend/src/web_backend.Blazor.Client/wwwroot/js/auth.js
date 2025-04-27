/**
 * Auth Token Manager - Handles authentication token storage and operations
 */
// Token management functions for authentication
window.authTokenManager = {
    storeAuthData: function (accessToken, refreshToken, expiresIn) {
        localStorage.setItem('accessToken', accessToken);
        localStorage.setItem('refreshToken', refreshToken);
        
        // Calculate and store expiration time
        const expirationTime = new Date().getTime() + expiresIn * 1000;
        localStorage.setItem('tokenExpiration', expirationTime.toString());
        
        console.log('Auth tokens stored successfully');
        return true;
    },
    
    clearAuthData: function () {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('tokenExpiration');
        console.log('Auth tokens cleared');
        return true;
    },
    
    getToken: function () {
        return localStorage.getItem('accessToken');
    },
    
    getAccessToken: function () {
        return localStorage.getItem('accessToken');
    },
    
    getRefreshToken: function () {
        return localStorage.getItem('refreshToken');
    },
    
    isAuthenticated: function () {
        const token = localStorage.getItem('accessToken');
        const expiration = localStorage.getItem('tokenExpiration');
        
        if (!token || !expiration) {
            return false;
        }
        
        // Check if token is expired
        const currentTime = new Date().getTime();
        const expirationTime = parseInt(expiration);
        
        return token && currentTime < expirationTime;
    }
};

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