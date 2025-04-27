window.loadHlsStream = function(streamUrl, videoElementId = 'videoPlayer', streamId = null) {
    console.log('Attempting to load video: ' + streamUrl);
    if (!streamUrl) {
        console.error('Stream URL is null or empty');
        return;
    }
    
    // Fix mixed content by converting HTTP to HTTPS for HLS URLs
    if (streamUrl.startsWith('http:') && streamUrl.includes('/hls/')) {
        streamUrl = streamUrl.replace('http:', 'https:');
        console.log('Fixed mixed content - converted to HTTPS URL: ' + streamUrl);
    }
    
    // Also fix port 8080 to 8443 if needed
    if (streamUrl.includes(':8080/hls/')) {
        streamUrl = streamUrl.replace(':8080/hls/', ':8443/hls/');
        console.log('Fixed port - converted to 8443: ' + streamUrl);
    }

    const video = document.getElementById(videoElementId);
    if (!video) {
        console.error('Video element not found');
        return;
    }

    // Check if user is authenticated from data attribute on body
    let isAuthenticated = document.body.hasAttribute('data-authenticated') && 
                         document.body.getAttribute('data-authenticated') === 'true';

    // For non-authenticated users, check paywall status BEFORE loading the stream
    if (streamId && !isAuthenticated && typeof PaywallManager !== 'undefined') {
        // Check if paywall is already shown
        if (PaywallManager.isPaywallShown && PaywallManager.isPaywallShown(streamId)) {
            console.log('Paywall active - not loading video content');
            return; // Don't load the video if paywall is active
        }
    }

    // Destroy previous instance if it exists
    if (window.hls) {
        window.hls.destroy();
        window.hls = null;
    }

    // Check if Hls.js is available
    if (typeof Hls === 'undefined') {
        console.error('HLS.js is not loaded. Make sure to include it in your page.');
        // Try to load it
        let script = document.createElement('script');
        script.src = 'https://cdn.jsdelivr.net/npm/hls.js@latest';
        script.onload = function() {
            console.log('HLS.js loaded dynamically');
            // Call ourselves again now that Hls is loaded
            loadHlsStream(streamUrl, videoElementId, streamId);
        };
        script.onerror = function() {
            console.error('Failed to load HLS.js dynamically');
        };
        document.head.appendChild(script);
        return;
    }

    // Check if Hls is supported
    if (Hls.isSupported()) {
        window.hls = new Hls({
            debug: false,
            maxBufferLength: 60,
            maxMaxBufferLength: 600,
            backBufferLength: 90,
            liveSyncDuration: 3,
            liveMaxLatencyDuration: Infinity,
            fragLoadingTimeOut: 20000,
            manifestLoadingTimeOut: 10000,
            levelLoadingTimeOut: 10000,
            liveDurationInfinity: true,
            startLevel: -1,
            autoStartLoad: true,
            enableWorker: true,
            lowLatencyMode: false,
            xhrSetup: function(xhr, url) {
                xhr.withCredentials = false;
            }
        });

        window.hls.loadSource(streamUrl);
        window.hls.attachMedia(video);

        // Add event listeners for detailed monitoring
        window.hls.on(Hls.Events.MANIFEST_PARSED, function() {
            console.log('HLS Manifest parsed, attempting to play video');

            // Extra check: If paywall is active, don't play
            if (!isAuthenticated && streamId && typeof PaywallManager !== 'undefined') {
                if (PaywallManager.isPaywallShown && PaywallManager.isPaywallShown(streamId)) {
                    console.log('Paywall detected during manifest parsing, stopping playback');
                    PaywallManager.showPaywall(streamId);
                    return;
                }
            }

            // Store the current time to track playback
            let lastTime = 0;
            let stuckCounter = 0;

            // Monitor playback to detect if stuck
            const playbackMonitor = setInterval(() => {
                if (video.currentTime === lastTime && !video.paused) {
                    stuckCounter++;
                    console.log(`Playback potentially stuck for ${stuckCounter} seconds`);

                    if (stuckCounter > 5) {
                        console.log('Attempting recovery for stuck playback');
                        video.currentTime += 0.1; // Try to advance playback slightly
                    }
                } else {
                    stuckCounter = 0;
                }
                lastTime = video.currentTime;
            }, 1000);

            video.play().then(() => {
                window.streamLoaded = true;
                console.log('Stream playback started successfully');
            }).catch(error => {
                console.error('Error starting playback:', error);
                window.streamLoaded = false;
            });
        });

        window.hls.on(Hls.Events.ERROR, function(event, data) {
            console.error('HLS Error:', data);

            if (data.fatal) {
                switch(data.type) {
                    case Hls.ErrorTypes.NETWORK_ERROR:
                        // Try to recover network error
                        console.log('Network error, trying to recover...');
                        window.hls.startLoad();
                        break;
                    case Hls.ErrorTypes.MEDIA_ERROR:
                        console.log('Media error, trying to recover...');
                        window.hls.recoverMediaError();
                        break;
                    default:
                        // Cannot recover
                        console.error('Fatal error, cannot recover', data);
                        // Wait a second before trying to reinitialize
                        setTimeout(() => {
                            window.hls.destroy();
                            window.hls = new Hls();
                            window.hls.loadSource(streamUrl);
                            window.hls.attachMedia(video);
                        }, 1000);
                        break;
                }
            }
        });
    }
    // For browsers that natively support HLS
    else if (video.canPlayType('application/vnd.apple.mpegurl')) {
        video.src = streamUrl;
        video.addEventListener('loadedmetadata', function() {
            // Extra check for paywall before playing
            if (!isAuthenticated && streamId && typeof PaywallManager !== 'undefined') {
                if (PaywallManager.isPaywallShown && PaywallManager.isPaywallShown(streamId)) {
                    console.log('Paywall detected before native playback, stopping');
                    video.pause();
                    PaywallManager.showPaywall(streamId);
                    return;
                }
            }

            video.play().then(() => {
                window.streamLoaded = true;
            }).catch(error => {
                console.error('Error starting playback:', error);
                window.streamLoaded = false;
            });
        });
        video.addEventListener('error', function(e) {
            window.streamLoaded = false;
            console.error('Error loading stream with native HLS support:', e);
        });
    } else {
        console.error('HLS not supported in this browser');
        window.streamLoaded = false;
    }
};

window.isStreamLoaded = function() {
    return window.streamLoaded === true;
};

// Add PaywallManager function if it doesn't exist
if (typeof window.PaywallManager === 'undefined') {
    window.PaywallManager = {
        // Key prefix for localStorage to identify specific streams
        keyPrefix: 'vss_stream_access_',

        // Default free access time in seconds (30 seconds)
        defaultFreeTime: 30,

        // Check if user has access to the stream
        checkAccess: function(streamId) {
            const key = this.keyPrefix + streamId;
            const accessData = localStorage.getItem(key);
            console.log('Checking access for stream:', streamId, 'Free time limit:', this.defaultFreeTime, 'seconds');

            if (!accessData) {
                // First time access, create new record
                const newAccessData = {
                    streamId: streamId,
                    startTime: new Date().toISOString(),
                    timeWatched: 0,
                    paywallShown: false
                };

                localStorage.setItem(key, JSON.stringify(newAccessData));
                return {
                    hasAccess: true,
                    remainingTime: this.defaultFreeTime,
                    isNewSession: true
                };
            }

            const data = JSON.parse(accessData);

            if (data.paywallShown) {
                return {
                    hasAccess: false,
                    remainingTime: 0,
                    isNewSession: false
                };
            }

            const remainingTime = Math.max(0, this.defaultFreeTime - data.timeWatched);
            return {
                hasAccess: remainingTime > 0,
                remainingTime: remainingTime,
                isNewSession: false
            };
        },

        // Update watched time for a stream
        updateWatchTime: function(streamId, secondsWatched) {
            const key = this.keyPrefix + streamId;
            const accessData = localStorage.getItem(key);

            if (accessData) {
                const data = JSON.parse(accessData);
                data.timeWatched += secondsWatched;

                if (data.timeWatched >= this.defaultFreeTime) {
                    data.paywallShown = true;
                }

                localStorage.setItem(key, JSON.stringify(data));

                return {
                    timeWatched: data.timeWatched,
                    remainingTime: Math.max(0, this.defaultFreeTime - data.timeWatched),
                    hasAccess: data.timeWatched < this.defaultFreeTime
                };
            }
            return null;
        },

        // Check if paywall is already shown for a stream
        isPaywallShown: function(streamId) {
            const key = this.keyPrefix + streamId;
            const accessData = localStorage.getItem(key);
            
            if (accessData) {
                const data = JSON.parse(accessData);
                return data.paywallShown;
            }
            return false;
        },

        // Show paywall overlay
        showPaywall: function(streamId, containerSelector = '.vss-video-container') {
            const key = this.keyPrefix + streamId;
            const accessData = localStorage.getItem(key);

            if (accessData) {
                const data = JSON.parse(accessData);
                data.paywallShown = true;
                localStorage.setItem(key, JSON.stringify(data));
            }

            let paywallOverlay = document.getElementById('vss-paywall-overlay');

            if (!paywallOverlay) {
                paywallOverlay = document.createElement('div');
                paywallOverlay.id = 'vss-paywall-overlay';
                paywallOverlay.className = 'vss-paywall-overlay';

                paywallOverlay.style.position = 'absolute';
                paywallOverlay.style.top = '0';
                paywallOverlay.style.left = '0';
                paywallOverlay.style.width = '100%';
                paywallOverlay.style.height = '100%';
                paywallOverlay.style.backgroundColor = 'rgba(0, 0, 0, 0.85)';
                paywallOverlay.style.color = 'white';
                paywallOverlay.style.display = 'flex';
                paywallOverlay.style.flexDirection = 'column';
                paywallOverlay.style.justifyContent = 'center';
                paywallOverlay.style.alignItems = 'center';
                paywallOverlay.style.textAlign = 'center';
                paywallOverlay.style.padding = '20px';
                paywallOverlay.style.zIndex = '1000';

                paywallOverlay.innerHTML = `
                    <h2>Free Preview Ended</h2>
                    <p>Your free preview period has ended. Sign in or create an account to continue watching.</p>
                    <div style="display: flex; gap: 10px; margin-top: 20px;">
                        <a href="/authentication/Login" class="btn btn-primary">Login</a>
                        <a href="/Account/Register" class="btn btn-success">Register</a>
                    </div>
                `;

                const videoContainer = document.querySelector(containerSelector);
                if (videoContainer) {
                    videoContainer.style.position = 'relative';
                    videoContainer.appendChild(paywallOverlay);

                    // Also pause and disable the video
                    const video = document.getElementById('videoPlayer');
                    if (video) {
                        window.pauseAndDisableVideo('videoPlayer');
                    }
                }
            } else {
                paywallOverlay.style.display = 'flex';

                // Make sure video stays paused
                const video = document.getElementById('videoPlayer');
                if (video) {
                    window.pauseAndDisableVideo('videoPlayer');
                }
            }
        },

        // Reset access for a stream
        resetAccess: function(streamId) {
            const key = this.keyPrefix + streamId;
            localStorage.removeItem(key);
        }
    };
}

// Function to pause and disable a video element
window.pauseAndDisableVideo = function(videoElementId) {
    console.log(`Pausing and disabling video: ${videoElementId}`);
    const video = document.getElementById(videoElementId);
    if (!video) {
        console.error(`Video element with ID ${videoElementId} not found`);
        return;
    }

    // Pause the video
    video.pause();
    
    // Disable controls and lower opacity to indicate it's disabled
    video.controls = false;
    video.style.opacity = '0.5';
    
    // Prevent video from being played again
    video.onplay = function() {
        video.pause();
    };
    
    // Additional measure to prevent playback with HLS.js
    if (window.hls) {
        try {
            window.hls.detachMedia();
            window.hls.stopLoad();
        } catch (error) {
            console.error('Error while disabling HLS stream:', error);
        }
    }
    
    console.log('Video successfully paused and disabled');
};

// Add function to call .NET methods after a delay
window.invokeDotNetAfterDelay = function(dotNetRef, methodName, delayMs) {
    console.log(`Setting up delayed callback to ${methodName} after ${delayMs}ms`);
    setTimeout(() => {
        console.log(`Invoking .NET method ${methodName}`);
        dotNetRef.invokeMethodAsync(methodName)
            .then(() => {
                console.log(`Successfully invoked ${methodName}`);
            })
            .catch(error => {
                console.error(`Error invoking ${methodName}:`, error);
            });
    }, delayMs);
};

// Function to create a free access timer display
window.createFreeAccessTimer = function(streamId, remainingTimeSeconds) {
    // Get the video element
    const video = document.getElementById('videoPlayer');
    if (!video) return;

    let timerElement = document.getElementById('vss-free-preview-timer');
    if (!timerElement) {
        timerElement = document.createElement('div');
        timerElement.id = 'vss-free-preview-timer';
        timerElement.className = 'vss-free-preview-timer';
        timerElement.style.position = 'absolute';
        timerElement.style.top = '10px';
        timerElement.style.right = '10px';
        timerElement.style.backgroundColor = 'rgba(0, 0, 0, 0.7)';
        timerElement.style.color = 'white';
        timerElement.style.padding = '5px 10px';
        timerElement.style.borderRadius = '4px';
        timerElement.style.fontSize = '14px';
        timerElement.style.zIndex = '999';

        video.parentElement.appendChild(timerElement);
    }

    let remainingTime = remainingTimeSeconds;
    window._vssTimerId = null;

    const updateTimer = () => {
        if (remainingTime <= 0) {
            clearInterval(window._vssTimerId);
            window.PaywallManager.showPaywall(streamId);
            return;
        }

        const minutes = Math.floor(remainingTime / 60);
        const seconds = remainingTime % 60;
        timerElement.textContent = `Free preview: ${minutes}:${seconds < 10 ? '0' : ''}${seconds}`;
        remainingTime--;

        window.PaywallManager.updateWatchTime(streamId, 1);
    };

    updateTimer();
    window._vssTimerId = setInterval(updateTimer, 1000);
};