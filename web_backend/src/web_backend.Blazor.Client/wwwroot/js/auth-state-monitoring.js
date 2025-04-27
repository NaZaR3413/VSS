// auth-state-monitoring.js - A JavaScript module for tracking authentication state

// Initialize the global authentication state
window.authState = {
    isAuthenticated: false,
    token: null,
    username: null,
    roles: []
};

// Function to check the authentication state from various sources
window.checkAuthState = async function() {
    console.log('Auth Monitor: Checking authentication state');
    
    try {
        // Check local storage for auth token
        const token = localStorage.getItem('auth_token');
        const tokenExpiry = localStorage.getItem('auth_token_expiry');
        
        if (token && tokenExpiry && new Date(tokenExpiry) > new Date()) {
            console.log('Auth Monitor: Found valid token in local storage');
            window.authState.isAuthenticated = true;
            window.authState.token = token;
            
            // Try to parse the token to get user info
            try {
                const tokenParts = token.split('.');
                if (tokenParts.length === 3) {
                    const tokenPayload = JSON.parse(atob(tokenParts[1]));
                    window.authState.username = tokenPayload.sub || tokenPayload.name;
                    window.authState.roles = tokenPayload.role || [];
                }
            } catch (e) {
                console.error('Auth Monitor: Failed to parse token', e);
            }
            
            return;
        }
        
        // Check cookies for auth information
        const cookies = document.cookie.split(';');
        let authCookie = cookies.find(cookie => cookie.trim().startsWith('auth='));
        
        if (authCookie) {
            console.log('Auth Monitor: Found auth cookie');
            authCookie = authCookie.trim().substring(5); // Remove 'auth=' prefix
            
            try {
                const authData = JSON.parse(decodeURIComponent(authCookie));
                if (authData.token) {
                    window.authState.isAuthenticated = true;
                    window.authState.token = authData.token;
                    window.authState.username = authData.username;
                    window.authState.roles = authData.roles || [];
                    return;
                }
            } catch (e) {
                console.error('Auth Monitor: Failed to parse auth cookie', e);
            }
        }
        
        // If we're still not authenticated, try to silently check with the server
        await checkServerAuth();
        
    } catch (error) {
        console.error('Auth Monitor: Error checking authentication state', error);
    }
    
    console.log('Auth Monitor: Authentication state', 
        window.authState.isAuthenticated ? 'Authenticated' : 'Not authenticated');
};

// Function to check authentication with the server
async function checkServerAuth() {
    try {
        // Try to call a lightweight endpoint that requires authentication
        const response = await fetch('/api/account/my-profile', {
            method: 'GET',
            credentials: 'include',
            headers: {
                'Accept': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            }
        });
        
        if (response.ok) {
            console.log('Auth Monitor: Successfully authenticated with server');
            const data = await response.json();
            window.authState.isAuthenticated = true;
            window.authState.username = data.userName;
            window.authState.roles = data.roles || [];
            return;
        } else {
            console.log('Auth Monitor: Not authenticated with server');
            window.authState.isAuthenticated = false;
        }
    } catch (error) {
        console.error('Auth Monitor: Error checking server authentication', error);
    }
}

// Listen for storage events to detect changes in authentication
window.addEventListener('storage', function(event) {
    if (event.key === 'auth_token' || event.key === 'auth_token_expiry') {
        console.log('Auth Monitor: Auth storage changed, rechecking state');
        window.checkAuthState();
    }
});

// Set up a timer to periodically check authentication state
setInterval(window.checkAuthState, 300000); // Check every 5 minutes

// Check auth state immediately when the script loads
window.checkAuthState();

console.log('Auth Monitor loaded');