/*  hlsPlayer.js  – UPDATED WITH HTTPS→HTTP FALL-BACK & MIXED-CONTENT HANDLING  */

console.log("hlsPlayer.js Loaded Successfully!");

/* ------------------------------------------------------------ */
/*  PAY-WALL CONFIGURATION + HELPERS (unchanged)                */
/* ------------------------------------------------------------ */

const PAYWALL_CONFIG = {
    FREE_PREVIEW_SECONDS: 30,                 // 30 second preview window
    STORAGE_KEY_PREFIX: 'vss_stream_access_',  // localStorage key prefix
    PAYWALL_OVERLAY_ID: 'vss-paywall-overlay'
};

window.PaywallManager = {
    /* ---------- session / access management ---------- */
    checkAccess(streamId) {
        const k = PAYWALL_CONFIG.STORAGE_KEY_PREFIX + streamId;
        const stored = localStorage.getItem(k);

        if (!stored) {
            const fresh = {
                streamId,
                startTime: new Date().toISOString(),
                timeWatched: 0,
                paywallShown: false
            };
            localStorage.setItem(k, JSON.stringify(fresh));
            return { hasAccess: true, remainingTime: PAYWALL_CONFIG.FREE_PREVIEW_SECONDS, isNewSession: true };
        }

        const data = JSON.parse(stored);
        if (data.paywallShown) return { hasAccess: false, remainingTime: 0, isNewSession: false };

        const remaining = Math.max(0, PAYWALL_CONFIG.FREE_PREVIEW_SECONDS - data.timeWatched);
        return { hasAccess: remaining > 0, remainingTime: remaining, isNewSession: false };
    },

    updateWatchTime(streamId, seconds) {
        const k = PAYWALL_CONFIG.STORAGE_KEY_PREFIX + streamId;
        const stored = localStorage.getItem(k);
        if (!stored) return null;

        const data = JSON.parse(stored);
        data.timeWatched += seconds;
        if (data.timeWatched >= PAYWALL_CONFIG.FREE_PREVIEW_SECONDS) data.paywallShown = true;
        localStorage.setItem(k, JSON.stringify(data));

        return {
            timeWatched: data.timeWatched,
            remainingTime: Math.max(0, PAYWALL_CONFIG.FREE_PREVIEW_SECONDS - data.timeWatched),
            hasAccess: data.timeWatched < PAYWALL_CONFIG.FREE_PREVIEW_SECONDS && !data.paywallShown
        };
    },

    /* ---------- pay-wall overlay ---------- */
    showPaywall(streamId, containerSelector = '.vss-video-container') {
        const k = PAYWALL_CONFIG.STORAGE_KEY_PREFIX + streamId;
        const stored = localStorage.getItem(k);
        if (stored) {
            const d = JSON.parse(stored);
            d.paywallShown = true;
            localStorage.setItem(k, JSON.stringify(d));
        }

        let overlay = document.getElementById(PAYWALL_CONFIG.PAYWALL_OVERLAY_ID);
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.id = PAYWALL_CONFIG.PAYWALL_OVERLAY_ID;
            overlay.className = 'vss-paywall-overlay';

            /* inline style so overlay still looks right even if CSS fails */
            Object.assign(overlay.style, {
                position: 'absolute', top: 0, left: 0, width: '100%', height: '100%',
                backgroundColor: 'rgba(0,0,0,0.85)', color: '#fff', display: 'flex',
                flexDirection: 'column', justifyContent: 'center', alignItems: 'center',
                textAlign: 'center', padding: '20px', zIndex: 1000
            });

            overlay.innerHTML = `
                <h2>Free Preview Ended</h2>
                <p>Your free preview period has ended. Sign in or create an account to continue watching.</p>
                <div style="display:flex;gap:10px;margin-top:20px;">
                    <a href="/authentication/Login" class="btn btn-primary">Login</a>
                    <a href="/Account/Register" class="btn btn-success">Register</a>
                </div>
            `;

            const container = document.querySelector(containerSelector);
            if (container) {
                container.style.position = 'relative';
                container.appendChild(overlay);
                window.pauseAndDisableVideo();
            }
        } else {
            overlay.style.display = 'flex';
        }
    },

    resetAccess(streamId) {
        localStorage.removeItem(PAYWALL_CONFIG.STORAGE_KEY_PREFIX + streamId);
    }
};

/* ------------------------------------------------------------ */
/*  MAIN: LOAD & PLAY HLS STREAM                                */
/*  – tries HTTPS first, falls back to HTTP if certificate fails */
/* ------------------------------------------------------------ */

window.loadHlsStream = (videoUrl, videoElementId = "videoPlayer", streamId = null) => {
    console.log("Attempting to load video:", videoUrl);
    const originalUrl = videoUrl; // remember HTTP/HTTPS original

    /* upgrade http→https only when current page is https */
    if (location.protocol === 'https:' && videoUrl.startsWith('http:')) {
        videoUrl = videoUrl.replace('http:', 'https:');
        console.log("Upgraded stream URL to HTTPS:", videoUrl);
    }

    const video = document.getElementById(videoElementId);
    if (!video) {
        console.error(`Video element with ID '${videoElementId}' not found!`);
        return;
    }

    /* ------------ pay-wall logic (unchanged) ------------ */
    const isAuthenticated = document.body.dataset.authenticated === 'true';
    if (streamId && !isAuthenticated) {
        const access = window.PaywallManager.checkAccess(streamId);
        if (access.hasAccess) {
            /* show countdown badge */
            let badge = document.getElementById('vss-free-preview-timer');
            if (!badge) {
                badge = document.createElement('div');
                badge.id = 'vss-free-preview-timer';
                badge.className = 'vss-free-preview-timer';
                Object.assign(badge.style, {
                    position: 'absolute', top: '10px', right: '10px',
                    backgroundColor: 'rgba(0,0,0,.7)', color: '#fff',
                    padding: '5px 10px', borderRadius: '4px', fontSize: '14px',
                    zIndex: 999
                });
                video.parentElement.appendChild(badge);
            }

            let remaining = access.remainingTime;
            const tick = () => {
                if (remaining <= 0) { clearInterval(id); window.PaywallManager.showPaywall(streamId); return; }
                badge.textContent = `Free preview: ${Math.floor(remaining/60)}:${String(remaining%60).padStart(2,'0')}`;
                remaining--; window.PaywallManager.updateWatchTime(streamId, 1);
            };
            tick(); const id = setInterval(tick, 1000);
            window._vssTimerId = id;
        } else {
            setTimeout(() => window.PaywallManager.showPaywall(streamId), 500);
        }
    }

    /* ------------ HLS.JS with HTTPS→HTTP fallback ------------ */
    const startPlayback = (src) => {
        if (Hls.isSupported()) {
            const hls = new Hls();
            hls.on(Hls.Events.ERROR, (evt, data) => {
                console.error("HLS Error:", data);
                const isManifestErr = data?.fatal && data.type === 'networkError' && data.details === 'manifestLoadError';
                const triedHttps = src.startsWith('https:');
                if (isManifestErr && triedHttps && originalUrl.startsWith('http:')) {
                    console.warn("Manifest failed over HTTPS – retrying over HTTP");
                    hls.destroy();
                    startPlayback(originalUrl);
                }
            });
            hls.on(Hls.Events.MANIFEST_PARSED, () => {
                console.log("HLS manifest loaded");
                video.play().catch(e => console.warn('Autoplay prevented:', e));
            });
            hls.loadSource(src);
            hls.attachMedia(video);
            return;
        }

        /* iOS Safari – uses native HLS */
        if (video.canPlayType("application/vnd.apple.mpegurl")) {
            video.src = src;
            video.addEventListener("loadedmetadata", () => video.play());
            return;
        }

        console.error("HLS is not supported in this browser.");
    };

    startPlayback(videoUrl);
};

/* ------------------------------------------------------------ */
/*  SMALL UTILITY FUNCTIONS (unchanged)                         */
/* ------------------------------------------------------------ */

window.invokeDotNetAfterDelay = (dotnetHelper, methodName, delayMs) =>
    setTimeout(() => dotnetHelper.invokeMethodAsync(methodName), delayMs);

window.pauseAndDisableVideo = (videoElementId = "videoPlayer") => {
    const v = document.getElementById(videoElementId);
    if (v) { v.pause(); v.controls = false; v.style.pointerEvents = 'none'; }
};

window.setAuthenticationStatus = (isAuthenticated) =>
    document.body.setAttribute('data-authenticated', isAuthenticated ? 'true' : 'false');

window.isStreamLoaded = () => window.streamLoaded === true;

window.cleanupStreamTimers = () => {
    if (window._vssTimerId) { clearInterval(window._vssTimerId); window._vssTimerId = null; }
};
