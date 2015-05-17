if (isPostMessageEnabled()) {
    addMessageListener(navigationHeaderMessageHandler);
}

/**
* @param {Object} event
**/
function navigationHeaderMessageHandler(event) {

    var message = null;
    try
    {
        message = getMessage(event.data);
    } catch (ex) {
        // event.data can throw an Invalid Pointer exception in CHM during
        //  page load
    }

    if (message) {
        switch (message.messageType) {
            case "insertNavigationHeader":
                insertNavigationHeader();
                break;
            case "searchHighlightComplete":
                if ($('a#removehighlighting').length) {
                    $('a#removehighlighting').css('display', 'inline');
                }
                break;
        }
    }

}

function insertNavigationHeader() {

    if ($('body > div.navigation-header').length == 0) {

        var header = $('<div class="navigation-header"><div class="inner-container"></div></div>');
        var innercontainer = header.find('div.inner-container').first();

        $('<a href="#" id="nav-previous"><i class="icon-arrow-left"/></a>').appendTo(innercontainer);
        $('<a href="#" id="nav-index"><i class="icon-list"/></a>').appendTo(innercontainer);
        $('<a href="#" id="nav-toc"><i class="icon-book"/></a>').appendTo(innercontainer);
        $('<a href="#" id="nav-search"><i class="icon-search"/></a>').appendTo(innercontainer);
        $('<a href="#" id="nav-next"><i class="icon-arrow-right"/></a>').appendTo(innercontainer);
        $('<a class="btn btn-warning" id="removehighlighting"><i class="icon-remove icon-white"></i></a>').appendTo(innercontainer)
            
        if ($('body span.InnovasysSearchHighlight').length) {
            // Highlighted search items have been added to the body so show the remove highlights button
            innercontainer.children('a#removehighlighting').css('display', 'inline');
        }

        innercontainer.children('a').click(function () {
            var webframe = window.parent;
            if (typeof webframe != "undefined") {

                switch ($(this).attr('id'))
                {
                    case "nav-previous":
                        window.parent.postMessage("navigate|getPreviousTopic", "*");
                        break;
                    case "nav-next":
                        window.parent.postMessage("navigate|getNextTopic", "*");
                        break;
                    case "nav-index":
                    case "nav-toc":
                    case "nav-search":
                        window.parent.postMessage("openNavigationPane|" + $(this).attr('id'), "*");
                        break;
                    case "removehighlighting":
                        removeAllHighlights(document);
                        $(this).css('display', 'none');
                        break;
                }
            }
        });    
        header.prependTo($('body'));
       
        if ($('html').data('responsive-load-complete') == true) {
            // Async load of responsive files already complete so make the body visible
            $('html').addClass('loaded');
            $('html').show(200);
            $('html').data('responsive-load-complete', null);
        }

    }

}