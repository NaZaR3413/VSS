console.log("hlsPlayer.js Loaded Successfully!");

window.loadHlsStream = (videoUrl) => {
    console.log("Attempting to load video:", videoUrl);

    var video = document.getElementById("videoPlayer");
    if (!video) {
        console.error("Video element not found!");
        return;
    }

    if (Hls.isSupported()) {
        var hls = new Hls();
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
