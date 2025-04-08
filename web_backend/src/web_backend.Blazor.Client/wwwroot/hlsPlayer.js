console.log("hlsPlayer.js Loaded Successfully!");

window.loadHlsStream = (videoUrl, videoElementId = "videoPlayer") => {
    console.log("Attempting to load video:", videoUrl);

    const video = document.getElementById(videoElementId);
    if (!video) {
        console.error(`Video element with ID '${videoElementId}' not found!`);
        return;
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
