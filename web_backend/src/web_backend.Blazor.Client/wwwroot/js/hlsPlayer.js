// hlsPlayer.js - Basic HLS video player implementation

// Check if Hls.js is supported on this browser
const isHlsSupported = () => {
    return Hls && Hls.isSupported();
};

// Initialize HLS player on a video element
const initializeHlsPlayer = (videoElementId, hlsUrl) => {
    const videoElement = document.getElementById(videoElementId);

    if (!videoElement) {
        console.error(`Video element with ID ${videoElementId} not found`);
        return false;
    }

    if (isHlsSupported()) {
        const hls = new Hls();
        hls.loadSource(hlsUrl);
        hls.attachMedia(videoElement);
        hls.on(Hls.Events.MANIFEST_PARSED, () => {
            console.log('HLS manifest loaded successfully');
            videoElement.play().catch(err => {
                console.warn('Auto-play failed:', err);
            });
        });

        hls.on(Hls.Events.ERROR, (event, data) => {
            console.error('HLS error:', data);
            if (data.fatal) {
                switch (data.type) {
                    case Hls.ErrorTypes.NETWORK_ERROR:
                        console.log('Network error, trying to recover...');
                        hls.startLoad();
                        break;
                    case Hls.ErrorTypes.MEDIA_ERROR:
                        console.log('Media error, trying to recover...');
                        hls.recoverMediaError();
                        break;
                    default:
                        console.error('Fatal error, cannot recover');
                        hls.destroy();
                        break;
                }
            }
        });

        return hls;
    } else if (videoElement.canPlayType('application/vnd.apple.mpegurl')) {
        // For Safari which has built-in HLS support
        videoElement.src = hlsUrl;
        videoElement.addEventListener('loadedmetadata', () => {
            videoElement.play().catch(err => {
                console.warn('Auto-play failed:', err);
            });
        });
        return true;
    } else {
        console.error('HLS is not supported in this browser');
        return false;
    }
};

// Expose functions to the global scope
window.hlsPlayer = {
    isHlsSupported,
    initializeHlsPlayer
};

console.log('HLS Player module loaded successfully');
