window.authHelpers = {
    clearAuthTokens: function () {
        localStorage.removeItem('authToken');
        localStorage.removeItem('abpAuthToken');

        document.cookie.split(';').forEach(function (c) {
            document.cookie = c.replace(/^ +/, '').replace(/=.*/, '=;expires=' + new Date().toUTCString() + ';path=/');
        });

        console.log('Auth tokens cleared');
        return true;
    }
}
