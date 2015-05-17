/* Returns true if this content is running under Microsoft Help Viewer */
function hsIsMshv() {
    return (location.protocol == 'ms-xhelp:' || location.href.indexOf("/ms.help?") != -1);
}

/* Gets the MSHS base url for resources */
function hsResourceBaseUrl() {

    if (isDesignTime) {
        return '';
    }
    else {
        // Get the first script tag
        var script = document.getElementById('mshs_support_script');

        // Extract the src which is a full resource url to within our origin .mshc
        var scriptSrc = script.src;

        var scriptUrl = null;
        if (isMshv2()) {
            // HV 2
            var startIndex = scriptSrc.indexOf('&id=');
            var scriptUrl = scriptSrc.substring(0, startIndex);
            startIndex = scriptSrc.indexOf('&', startIndex + 1);
            scriptUrl = scriptUrl + scriptSrc.substring(startIndex) + "&id=";
        }
        else {
            // HV 1
            // Get the portion up to the ; (the base url for resource references)
            var startIndex = scriptSrc.indexOf(';') + 1;
            scriptUrl = scriptSrc.substring(0, startIndex);
        }

        return scriptUrl;
    }
}

function hsFixUrl(url) {
    if (hsIsMshv()) {
        var originalUrl;
        if (url.indexOf('127.0.0.1') != -1) {
            // Chrome - rule returned as full url
            originalUrl = url.substring(url.indexOf('/', url.indexOf('127.0.0.1')) + 5, url.length);
            originalUrl = originalUrl.replace("\"", "");
        }
        else if (url.indexOf('../') != -1) {
            // IE - rule returned as original, with a .. prefix
            originalUrl = url.substring(url.indexOf('../') + 2, url.lastIndexOf(')'));
            originalUrl = originalUrl.replace("\"", "");
        }
        else {
            // Relative url in MSHV 2.0
            originalUrl = url.replace(new RegExp("/", "g"), "\\");
        }
        if (originalUrl.indexOf("/help/") != -1) {
            originalUrl = originalUrl.substring(originalUrl.indexOf("/", originalUrl.indexOf("/help/") + 5), originalUrl.length);
        }
        var newUrl = hsResourceBaseUrl() + originalUrl;
        return newUrl;
    }
    else {
        return url
    }
}