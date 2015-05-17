if (self != top && getDeviceType() != "DESKTOP") {
    // Loading in mobile / tablet mode in a webframe so hide the body until load is complete
    $('html').addClass('loading');
}

function getDeviceType() {

    var forcedDisplayMode = getForcedDisplayMode();
    if (forcedDisplayMode != null) {
        return forcedDisplayMode;
    }
    
    if (Modernizr.touch) {

        if (Modernizr.mq("screen and (orientation: portrait) and (max-device-width: 600px)")) {
            return "MOBILE";
        }
        else if (Modernizr.mq("screen and (orientation: landscape) and (max-device-width: 767px)")) {
            return "MOBILE";
        }
        else {
            return "TABLET";
        }

    }
        // Specific check for windows phone as Modernizr returns false for the touch property
    else if (navigator.userAgent.indexOf('Windows Phone OS') != -1) {
        return "MOBILE";
    }

    return "DESKTOP";

}

function getForcedDisplayMode() {

    if (window.location.hash == "#ForceDisplayDesktop") {
        return "DESKTOP";
    }
    else if (window.location.hash == "#ForceDisplayMobile") {
        return "MOBILE";
    }
    else if (window.location.hash == "#ForceDisplayTablet") {
        return "TABLET";
    }

    // Only check local storage here if we are in a frame - the parent frame sets the local storage
    // value for overriding the default behavior so we only need to check it if we are actually running
    // in a frame
    if (self != top) {
        var currentPath = window.location.pathname.substring(0, window.location.pathname.lastIndexOf('/'));
        var responsiveStorageId = 'innovasys-responsive-' + currentPath.replace(/[^a-zA-Z0-9_\-]/g, "");
        if (window.getLocalStorage().getAttribute(responsiveStorageId) != null) {
            return window.getLocalStorage().getAttribute(responsiveStorageId);
        }
    }

    return null;

}

function onResponsiveFilesLoaded() {

    if ($('body > div.navigation-header').length) {
        // Navigation header already loaded so make the body visible;
        $('html').addClass('loaded');
        $('html').show(200);
    }
    else {
        // Navigation header not loaded yet, add a flag here so that when the header has finished loaded it will make
        // the body visible
        $('html').data('responsive-load-complete', true);
    }

}