// cors-proxy.js - A JavaScript proxy for handling CORS issues in the browser

// Store the original fetch function
const originalFetch = window.fetch;

// Override the fetch function to add CORS headers and handle CORS errors
window.fetch = function(...args) {
    // Get the request URL and options
    const url = args[0];
    const options = args[1] || {};
    
    // Only intercept API calls to our backend
    if (typeof url === 'string' && url.includes('azurewebsites.net')) {
        console.log(`CORS Proxy: Intercepting request to ${url}`);
        
        // Set up headers if they don't exist
        options.headers = options.headers || {};
        
        // Set credentials mode to include cookies
        options.credentials = 'include';
        
        // Add necessary CORS headers
        options.headers['X-Requested-With'] = 'XMLHttpRequest';
        
        // Add Content-Type if not present
        if (!options.headers['Content-Type'] && !options.headers['content-type']) {
            options.headers['Content-Type'] = 'application/json';
        }
        
        // Add auth token if available (from auth-state-monitoring.js)
        if (window.authState && window.authState.isAuthenticated && window.authState.token) {
            options.headers['Authorization'] = `Bearer ${window.authState.token}`;
        }
        
        console.log('CORS Proxy: Added headers to request');
    }
    
    // Call the original fetch with our modified options
    return originalFetch.apply(this, [url, options])
        .catch(error => {
            if (error.message.includes('CORS') || error.name === 'TypeError') {
                console.error(`CORS Proxy: CORS error detected for ${url}`, error);
                
                // If it's a CORS error, we can try to work around it
                if (typeof url === 'string' && url.includes('azurewebsites.net')) {
                    console.log('CORS Proxy: Attempting workaround for CORS error');
                    
                    // Create a new options object for our retry
                    const newOptions = {...options};
                    newOptions.mode = 'cors';
                    newOptions.credentials = 'include';
                    
                    // Try again with modified options
                    return originalFetch.apply(this, [url, newOptions]);
                }
            }
            
            // Rethrow the error if we can't handle it
            throw error;
        });
};

// Override XMLHttpRequest to handle CORS issues
const originalXHROpen = XMLHttpRequest.prototype.open;
XMLHttpRequest.prototype.open = function(method, url, async, user, password) {
    // Check if this is a request to our API
    if (typeof url === 'string' && url.includes('azurewebsites.net')) {
        console.log(`CORS Proxy: Intercepting XMLHttpRequest to ${url}`);
        
        // Store the URL for later use in setRequestHeader
        this._corsProxyUrl = url;
    }
    
    // Call the original open method
    return originalXHROpen.apply(this, arguments);
};

// Override setRequestHeader to add CORS headers
const originalXHRSetRequestHeader = XMLHttpRequest.prototype.setRequestHeader;
XMLHttpRequest.prototype.setRequestHeader = function(header, value) {
    // Call the original method
    originalXHRSetRequestHeader.apply(this, arguments);
    
    // If this is a request to our API, add CORS headers
    if (this._corsProxyUrl) {
        if (header !== 'X-Requested-With') {
            this.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
        }
        
        // Add auth token if available
        if (window.authState && window.authState.isAuthenticated && window.authState.token &&
            header !== 'Authorization') {
            this.setRequestHeader('Authorization', `Bearer ${window.authState.token}`);
        }
    }
};

// Wait for the window to load before ensuring CORS settings are applied
window.addEventListener('load', function() {
    console.log('CORS Proxy: Initializing');
    
    // Check if auth state is available
    if (window.checkAuthState) {
        window.checkAuthState();
    }
    
    console.log('CORS Proxy: Initialization complete');
});

console.log('CORS Proxy loaded');