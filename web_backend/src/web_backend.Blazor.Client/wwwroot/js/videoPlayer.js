function initializeVideoPlayer(videoElementId) {
    const videoElement = document.getElementById(videoElementId);

    if (!videoElement) {
        console.error('Video element not found:', videoElementId);
        return;
    }

    if (window.Hls && Hls.isSupported()) {
        const hls = new Hls();
        hls.loadSource(videoElement.querySelector('source').src);
        hls.attachMedia(videoElement);
        hls.on(Hls.Events.MANIFEST_PARSED, function () {
            videoElement.play();
        });
    }
    else if (videoElement.canPlayType('application/vnd.apple.mpegurl')) {
        videoElement.src = videoElement.querySelector('source').src;
    }

    videoElement.addEventListener('error', function (e) {
        console.error('Video playback error:', e);
    });
}

window.initializeVideoPlayer = initializeVideoPlayer;
