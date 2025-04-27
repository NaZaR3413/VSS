/**
 * Auth Token Manager - Enhanced for ABP Authentication
 */
window.authTokenManager = {
    storeAuthData: function (accessToken, refreshToken, expiresIn) {
        localStorage.setItem('accessToken', accessToken);
        localStorage.setItem('refreshToken', refreshToken);
        
        // Calculate and store expiration time
        const expirationTime = new Date().getTime() + expiresIn * 1000;
        localStorage.setItem('tokenExpiration', expirationTime.toString());
        
        // Mark as authenticated
        localStorage.setItem('isAuthenticated', 'true');
        localStorage.setItem('authTime', new Date().toISOString());
        
        console.log('Auth tokens stored successfully');
        return true;
    },
    
    clearAuthData: function () {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('tokenExpiration');
        localStorage.removeItem('isAuthenticated');
        localStorage.removeItem('authTime');
        localStorage.removeItem('currentUser');
        localStorage.removeItem('authCookies');
        
        // Clear cookies
        document.cookie.split(';').forEach(function(c) {
            document.cookie = c.replace(/^ +/, '').replace(/=.*/, '=;expires=' + new Date().toUTCString() + ';path=/');
        });
        
        console.log('Auth tokens and cookies cleared');
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
        // Check both localStorage flag and ABP cookie
        const hasAuthFlag = localStorage.getItem('isAuthenticated') === 'true';
        const hasAuthCookie = document.cookie.indexOf('.AspNetCore.Identity.Application') >= 0;
        
        if (hasAuthCookie) {
            // If cookie exists but flag doesn't, update the flag
            if (!hasAuthFlag) {
                localStorage.setItem('isAuthenticated', 'true');
                localStorage.setItem('authTime', new Date().toISOString());
            }
            return true;
        }
        
        if (hasAuthFlag) {
            // Check expiration if using token auth
            const token = localStorage.getItem('accessToken');
            const expiration = localStorage.getItem('tokenExpiration');
            
            if (token && expiration) {
                const currentTime = new Date().getTime();
                const expirationTime = parseInt(expiration);
                return currentTime < expirationTime;
            }
            
            return true; // If we have the flag but no token, assume cookie auth
        }
        
        return false;
    },
    
    checkAuthCookies: function() {
        // This function checks and updates authentication state based on cookies
        const hasAuthCookie = document.cookie.indexOf('.AspNetCore.Identity.Application') >= 0;
        
        if (hasAuthCookie && localStorage.getItem('isAuthenticated') !== 'true') {
            // Update auth state if cookie exists but localStorage doesn't reflect it
            localStorage.setItem('isAuthenticated', 'true');
            localStorage.setItem('authTime', new Date().toISOString());
            console.log('Updated auth state based on cookie presence');
            return true;
        } else if (!hasAuthCookie && localStorage.getItem('isAuthenticated') === 'true') {
            // Clear auth state if cookie is gone but localStorage still has it
            this.clearAuthData();
            console.log('Cleared auth state due to missing cookie');
            return false;
        }
        
        return hasAuthCookie;
    }
};

// Set up periodic check for auth cookies
setInterval(function() {
    window.authTokenManager.checkAuthCookies();
}, 30000); // Check every 30 seconds

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