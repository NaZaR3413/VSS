console.log("hlsPlayer.js Loaded Successfully!");

const PAYWALL_CONFIG = {
    FREE_PREVIEW_SECONDS: 300,
    STORAGE_KEY_PREFIX: 'vss_stream_access_',
    PAYWALL_OVERLAY_ID: 'vss-paywall-overlay'
};

window.PaywallManager = {
    checkAccess: function (streamId) {
        const storageKey = PAYWALL_CONFIG.STORAGE_KEY_PREFIX + streamId;
        const accessData = localStorage.getItem(storageKey);

        if (!accessData) {
            const newAccessData = {
                streamId: streamId,
                startTime: new Date().toISOString(),
                timeWatched: 0,
                paywallShown: false
            };
            localStorage.setItem(storageKey, JSON.stringify(newAccessData));
            return {
                hasAccess: true,
                remainingTime: PAYWALL_CONFIG.FREE_PREVIEW_SECONDS,
                isNewSession: true
            };
        } else {
            const data = JSON.parse(accessData);

            if (data.paywallShown) {
                return {
                    hasAccess: false,
                    remainingTime: 0,
                    isNewSession: false
                };
            }

            const remainingTime = Math.max(0, PAYWALL_CONFIG.FREE_PREVIEW_SECONDS - data.timeWatched);
            return {
                hasAccess: remainingTime > 0,
                remainingTime: remainingTime,
                isNewSession: false
            };
        }
    },

    updateWatchTime: function (streamId, secondsWatched) {
        const storageKey = PAYWALL_CONFIG.STORAGE_KEY_PREFIX + streamId;
        const accessData = localStorage.getItem(storageKey);

        if (accessData) {
            const data = JSON.parse(accessData);
            data.timeWatched += secondsWatched;

            if (data.timeWatched >= PAYWALL_CONFIG.FREE_PREVIEW_SECONDS) {
                data.paywallShown = true;
            }

            localStorage.setItem(storageKey, JSON.stringify(data));

            return {
                timeWatched: data.timeWatched,
                remainingTime: Math.max(0, PAYWALL_CONFIG.FREE_PREVIEW_SECONDS - data.timeWatched),
                hasAccess: data.timeWatched < PAYWALL_CONFIG.FREE_PREVIEW_SECONDS && !data.paywallShown
            };
        }
        return null;
    },

    showPaywall: function (streamId, containerSelector = '.vss-video-container') {
        const storageKey = PAYWALL_CONFIG.STORAGE_KEY_PREFIX + streamId;
        const accessData = localStorage.getItem(storageKey);

        if (accessData) {
            const data = JSON.parse(accessData);
            data.paywallShown = true;
            localStorage.setItem(storageKey, JSON.stringify(data));
        }

        let paywallOverlay = document.getElementById(PAYWALL_CONFIG.PAYWALL_OVERLAY_ID);

        if (!paywallOverlay) {
            paywallOverlay = document.createElement('div');
            paywallOverlay.id = PAYWALL_CONFIG.PAYWALL_OVERLAY_ID;
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

                window.pauseAndDisableVideo();
            }
        } else {
            paywallOverlay.style.display = 'flex';
        }
    },

    resetAccess: function (streamId) {
        const storageKey = PAYWALL_CONFIG.STORAGE_KEY_PREFIX + streamId;
        localStorage.removeItem(storageKey);
    }
};

window.loadHlsStream = (videoUrl, videoElementId = "videoPlayer", streamId = null) => {
    console.log("Attempting to load video:", videoUrl);

    // Don't automatically convert HTTP to HTTPS as the streaming server doesn't support SSL
    // Instead, maintain the original protocol to prevent SSL errors
    // If needed in the future, uncomment the code below:
    /*
    if (window.location.protocol === 'https:' && videoUrl.startsWith('http:')) {
        console.log("Converting HTTP stream URL to HTTPS to avoid mixed content blocking");
        videoUrl = videoUrl.replace('http:', 'https:');
    }
    */

    const video = document.getElementById(videoElementId);
    if (!video) {
        console.error(`Video element with ID '${videoElementId}' not found!`);
        return;
    }

    let isAuthenticated = document.body.hasAttribute('data-authenticated') &&
        document.body.getAttribute('data-authenticated') === 'true';

    if (streamId && !isAuthenticated) {
        const accessResult = window.PaywallManager.checkAccess(streamId);

        if (accessResult.hasAccess) {
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

            let remainingTime = accessResult.remainingTime;
            let timerId = null;

            const updateTimer = () => {
                if (remainingTime <= 0) {
                    clearInterval(timerId);
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
            timerId = setInterval(updateTimer, 1000);

            window._vssTimerId = timerId;
        } else {
            setTimeout(() => {
                window.PaywallManager.showPaywall(streamId);
            }, 500);
        }
    }

    if (Hls.isSupported()) {
        const hls = new Hls();
        hls.loadSource(videoUrl);
        hls.attachMedia(video);
        hls.on(Hls.Events.MANIFEST_PARSED, function () {
            console.log("HLS Stream Loaded Successfully");
            video.play();
        });
        hls.on(Hls.Events.ERROR, function (event, data) {
            console.error("HLS Error:", data);
        });
    } else if (video.canPlayType("application/vnd.apple.mpegurl")) {
        video.src = videoUrl;
        video.addEventListener("loadedmetadata", function () {
            console.log("Native HLS support detected, playing video.");
            video.play();
        });
    } else {
        console.error("HLS is not supported in this browser.");
    }
};

window.invokeDotNetAfterDelay = (dotnetHelper, methodName, delayMs) => {
    setTimeout(() => {
        dotnetHelper.invokeMethodAsync(methodName);
    }, delayMs);
};

window.pauseAndDisableVideo = (videoElementId = "videoPlayer") => {
    const video = document.getElementById(videoElementId);
    if (video) {
        video.pause();
        video.controls = false;
        video.style.pointerEvents = "none";
    }
};

window.setAuthenticationStatus = (isAuthenticated) => {
    document.body.setAttribute('data-authenticated', isAuthenticated ? 'true' : 'false');
};

window.isStreamLoaded = () => {
    return window.streamLoaded === true;
};

window.cleanupStreamTimers = () => {
    if (window._vssTimerId) {
        clearInterval(window._vssTimerId);
        window._vssTimerId = null;
    }
};
