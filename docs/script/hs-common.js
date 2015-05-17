var isDesignTime = false;

/* Utility */

function getNextSibling(n)
{
    n=n.nextSibling;
    while(n)
    {
        if(n.nodeType == 1)
            return n;
        else
            n=n.nextSibling;
    }
}

/* End Utility */

/* Event wiring */

function hsAddLoadEvent(loadFunc) 
{
  var oldFunc = window.onload;
  if (typeof window.onload != 'function') 
  {
    window.onload = loadFunc;
  } 
  else {
    window.onload = function() 
    {
      if (oldFunc) {
        oldFunc();
      }
      loadFunc();
    }
  }
}

/* End Event wiring */

/* Returns true if this content is running under Microsoft Help Viewer */
function hsIsMshv() {
    return (location.href.indexOf("/ms.help?") != -1)
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

        // Get the portion up to the ; (the base url for resource references)
        var startIndex = scriptSrc.indexOf(';') + 1;
        var scriptUrl = scriptSrc.substring(0, startIndex);

        return scriptUrl;
    }
}

function hsFixUrl(url) {
    if (hsIsMshv()) {
        var originalUrl;
        if (url.indexOf('127.0.0.1') != -1) {
            // Chrome - rule returned as full url
            originalUrl = url.substring(url.indexOf('/', url.indexOf('127.0.0.1')) + 5, url.length);
        }
        else {
            // IE - rule returned as original, with a .. prefix
            originalUrl = url.substring(url.indexOf('../') + 2, url.lastIndexOf(')'));
        }
        if (originalUrl.indexOf("/help/") == -1) {
            originalUrl = originalUrl.substring(originalUrl.indexOf("/", originalUrl.indexOf("/help/") + 5), originalUrl.length);
        }
        originalUrl = originalUrl.replace("\"", "");
        var newUrl = hsResourceBaseUrl() + originalUrl;
        return newUrl;
    }
    else {
        return url
    }
}

function hsFixRelativeUrl(url) {
    if (hsIsMshv()) {
        return hsResourceBaseUrl() + url;
    }
    else {
        return url
    }
}