/* Userdata support in CHMs */
(function () {
    var currentLocation = document.location + ".";
    if (currentLocation.indexOf("mk:@MSITStore") == 0) {
        currentLocation = "ms-its:" + currentLocation.substring(14, currentLocation.length - 1);
        document.location.replace(currentLocation);
    }
})();
/* End Userdata support in CHMs */

/* Global support functions - required by widgets */

var isDesignTime = false;
var isNew = false;
var isAnimationDisabled = false;

// Return a document element by id
function documentElement(id) {
    return document.getElementById(id);
}

// Returns the source element of an event
function sourceElement(e) {
    if (window.event) {
        e = window.event;
    }

    return e.srcElement ? e.srcElement : e.target;
}

//  Cancels an event, preventing further bubbling and returning false to cancel default behavior
function cancelEvent(e) {
    e.returnValue = false;
    e.cancelBubble = true;

    if (e.stopPropagation) {
        e.stopPropagation();
        e.preventDefault();
    }
}

// Return Microsoft Internet Explorer (major) version number, or 0 for others. */
function msieversion() {
    var ua = window.navigator.userAgent;
    var msie = ua.indexOf("MSIE ");

    if (msie > 0) // is Microsoft Internet Explorer; return version number
    {
        return parseInt(ua.substring(msie + 5, ua.indexOf(".", msie)));
    }
    else {
        return 0;    // is other browser
    }
}

// Returns an elements absolute position, allowing for the non-scrolling header
function getElementPosition(e) {
    var offsetLeft = 0;
    var offsetTop = 0;

    while (e) {
        // Allow for the scrolling body region in IE
        if (msieversion() > 4) {
            offsetLeft += (e.offsetLeft - e.scrollLeft);
            offsetTop += (e.offsetTop - e.scrollTop);
        }
        else {
            offsetLeft += e.offsetLeft;
            offsetTop += e.offsetTop;
        }

        e = e.offsetParent;
    }

    if (navigator.userAgent.indexOf('Mac') != -1 && typeof document.body.leftMargin != 'undefined') {
        offsetLeft += document.body.leftMargin;
        offsetTop += document.body.topMargin;
    }

    return { left: offsetLeft, top: offsetTop };
}

function findFrame(Name) {
    var frameObject = parent.frames[Name];
    try {
        if ((!frameObject) && parent.parent) {
            frameObject = parent.parent.frames[Name];
        }
    }
    catch (e) { }
    return frameObject;
}

// Prevent expand flickering when IE or CHM running in quirks mode
(function () {
    if (document.compatMode != 'CSS1Compat') {
        // Define overriding method.
        jQuery.fx.prototype.hide = function () {
            // Remember where we started, so that we can go back to it later
            this.options.orig[this.prop] = jQuery.style(this.elem, this.prop);
            this.options.hide = true;

            // Begin the animation
            this.custom(this.cur(), 1);
        }
    }
})();

/* End Global support functions */


/* Clipboard */

function getTextFromContainingTable() {
    var parentTable = $($(this).parents('table').get(0));
    var tableCell = parentTable.find('td').get(0);
    if (tableCell) {
        if (tableCell.textContent) {
            return tableCell.textContent;
        }
        else if (tableCell.innerText) {
            return tableCell.innerText;
        }
        else {
            return $(tableCell).text();
        }
    }
}

// Wire up any span.CopyCode elements to copy to clipboard
function initializeCopyCodeLinks() {
    // Wire up the copy code functionality
    if (location.protocol == 'mk:' || $.browser.msie || isDesignTime) {
        // In CHM or IE use the inbuilt IE clipboard support
        $('span.CopyCode').click(function () {
            var textValue = getTextFromContainingTable.call(this);
            window.clipboardData.setData('Text', textValue);
            alert("Copied text to clipboard:\n\n " + textValue);
        });
    } else if (location.protocol == "file:") {
        $('span.CopyCode').click(function () {
            // Cannot copy to clipboard from local content in browsers other than IE
            alert("Cannot copy to the clipboard as browser security restrictions prevent copying to the clipboard from local Html content");
        });
    } else {
        // Use zero clipboard for other scenarios		
        ZeroClipboard.config({ moviePath: 'script/ZeroClipboard.swf' });
        var zeroClipboardClient = new ZeroClipboard($('span.CopyCode'));
        zeroClipboardClient.on("dataRequested", function (client, args) {
            client.setText(getTextFromContainingTable.call(this));
        });
        zeroClipboardClient.on('complete', function (client, args) {
            var text = args.text;
            if (text.length > 500) {
                text = text.substr(0, 500) + "...\n\n(" + (text.length - 500) + " characters not shown)";
            }
            alert("Copied text to clipboard:\n\n " + text);
        });
    }
};

/* End Clipboard */


/* Section Toggle */

// Toggle a given section or sections
$.fn.toggleSection = function (method) {
    var result = this.each(function () {

        var sectionDiv = $(this).next('div.SectionContent');
        if (sectionDiv) {
            var isExpanded = $(this).hasClass('SectionHeadingCollapsed');
            if (method == 'immediate' || isAnimationDisabled) {
                if (sectionDiv.css('display') == 'none') {
                    sectionDiv.show();
                } else {
                    sectionDiv.hide();
                }
            }
            else {
                sectionDiv.slideToggle('fast');
            }
            $(this).toggleClass('SectionHeadingCollapsed', !isExpanded);
            if (isExpanded) {
                window.getLocalStorage().setAttribute('SectionHeadingCollapsed' + $(this).attr('id'), null);
            } else {
                window.getLocalStorage().setAttribute('SectionHeadingCollapsed' + $(this).attr('id'), 'true');
            }
        }

    });

    updateToggleAllSectionsLinkLabel();

    return result;
}

// Load initial state of sections
$.fn.loadToggleSectionState = function () {
    return this.each(function () {

        var attributeValue = window.getLocalStorage().getAttribute('SectionHeadingCollapsed' + $(this).attr('id'));
        if (attributeValue == 'true') {
            $(this).toggleSection('immediate');
        }

    });
}

$.fn.loadToggleCheckboxState = function () {
    return this.each(function () {

        var attributeValue = window.getLocalStorage().getAttribute('CheckboxIsUnchecked' + $(this).attr('id'));
        if (attributeValue == 'true') {
            $(this).prop('checked', false);
            $(this).toggleCheckbox('immediate');
        }

    });
}

$.fn.toggleCheckbox = function (method) {
    return this.each(function () {

        var toggleClassName = $(this).attr("data-toggleclass");
        if (toggleClassName) {
            $('.' + toggleClassName).toggleElement(method);
        }

        if (!$(this).is(":checked")) {
            window.getLocalStorage().setAttribute('CheckboxIsUnchecked' + $(this).attr('id'), 'true');
        } else {
            window.getLocalStorage().setAttribute('CheckboxIsUnchecked' + $(this).attr('id'), null);
        }

    });
}

$.fn.toggleElement = function (method) {
    return this.each(function () {
        if (this.tagName == 'LI' && $(this).parent().hasClass('ui-tabs-nav')) {
            // If we are hiding a tab, make sure it isn't the selected tab
            var tabContainer = $($(this).parents(".TabContainer").get(0));
            var selectedTab = tabContainer.find('.ui-tabs-selected');
            if (selectedTab.is(":hidden") || selectedTab.is(this)) {
                var firstVisibleTab = tabContainer.find('li:visible:not(.ui-tabs-selected):first');
                if (firstVisibleTab) {
                    tabContainer.tabs('select', firstVisibleTab.index());
                }
            }
        }
        if ($(this).is(":hidden")) {
            if ($(this).css("display") == "none") {
                // Element is currently not visible
                if (this.tagName == 'TR') {
                    $(this).css("display", "table-row");
                } else {
                    $(this).css("display", "block");
                }
            } else {
                $(this).css("display", "none");
            }
        } else {
            if (method == 'immediate' || isAnimationDisabled) {
                $(this).toggle();
            } else {
                $(this).slideToggle('fast');
            }
        }
    });
}

$(function () {
    $('#ToggleAllSectionsLink').click(function () {
        var desiredExpanded = !($('.SectionHeading.SectionHeadingCollapsed').length == 0);
        $('.SectionHeading').each(function () {
            var isExpanded = !$(this).hasClass('SectionHeadingCollapsed')
            if (isExpanded != desiredExpanded) {
                $(this).toggleSection();
            }
        });
    });
});

// Show all Drop down sections
function showAllDropDownSections() {
    $('.hs-toggler a:first-child').not('.open').each(function () {
        $(this).click();
    });
    HSShowAllCSections();
}

// Hide all Drop down sections
function hideAllDropDownSections() {
    $('.hs-toggler a:first-child.open').each(function () {
        $(this).click();
    });
    HSHideAllCSections();
}

function updateToggleAllSectionsLinkLabel() {
    var allSectionsExpanded = ($('.SectionHeading.SectionHeadingCollapsed').length == 0);
    $('#CollapseAllLabel').css('display', allSectionsExpanded ? 'inline' : 'none');
    $('#ExpandAllLabel').css('display', allSectionsExpanded ? 'none' : 'inline');
}

function setToggleAllSectionsVisibility() {
    if ($('.SectionHeading').length > 0) {
        // Sections - show
        $('#ToggleAllSectionsLink').show();
    } else {
        // No sections - hide
        $('#ToggleAllSectionsLink').hide();
    }
}

/* End Section Toggle */


/* Local Storage */

// LocalStorageHandler Type definition
var LocalStorageHandler = function () {

    var storageMethod = "native";
    var cookieValue = null;
    var storageElement = null;

    if (!window.localStorage) {
        if (location.protocol == "ms-its:") {
            // Cookies don't work in CHM so we use userdata behavior instead
            storageMethod = "userdata";
            var storageElement = $("<link />");
            storageElement.css("behavior", "url(#default#userdata)");
            storageElement.appendTo("body");
            storageElement = storageElement.get(0);
            storageElement.load("localStorage");
        } else {
            // If local storage isn't available, fall back to cookie storage
            storageMethod = "cookie";
            cookieValue = $.cookie("localStorage");
            if (cookieValue) {
                cookieData = JSON.parse(cookieValue);
            } else {
                cookieData = {};
            }
        }
    }

    return {

        load: function (name) {
            // local storage automatically saves
        },
        save: function (name) {
            // local storage automatically saves
        },
        setAttribute: function (key, value) {
            if (storageMethod == "native") {
                if (value == null || undefined == value) {
                    window.localStorage.removeItem(key);
                }
                else {
                    window.localStorage.setItem(key, value);
                }
            } else if (storageMethod == "cookie") {
                if (value == null) {
                    cookieData[key] = null;
                } else {
                    cookieData[key] = value + '';
                }
                $.cookie("localStorage", JSON.stringify(cookieData), { expires: 365, path: "/", domain: "" });
            } else if (storageMethod == "userdata") {
                storageElement.setAttribute(key, value + '');
                storageElement.save("localStorage");
            }
        },
        getAttribute: function (key) {
            if (storageMethod == "native") {
                return window.localStorage.getItem(key);
            } else if (storageMethod == "cookie") {
                if (cookieData[key] === undefined) {
                    return null;
                } else {
                    return cookieData[key];
                }
            } else if (storageMethod == "userdata") {
                return storageElement.getAttribute(key);
            }
        }

    }

};

// Get a local storage instance, initializing the first time it is called
window.getLocalStorage = function () {
    if (isDesignTime) {
        try {
            if (window.external.IsInnovasysDesigner) {
                this.localStorageInstance = window.external;
            }
        }
        catch (e) { }
    }
    if (!this.localStorageInstance) {
        this.localStorageInstance = new LocalStorageHandler();
    }
    return this.localStorageInstance;
}

// Legacy support (community)
function save(attributeName, attributeValue) {
    window.getLocalStorage().setAttribute(attributeName, attributeValue);
}

function load(attributeName) {
    return window.getLocalStorage().getAttribute(attributeName);
}

/* End Local Storage */


/* Popups */
function configurePopupLink(linkElement) {
    linkElement = $(linkElement);
    var content = null;
    var contentSource = linkElement.attr('data-popupcontentsource');
    if (contentSource) {
        // Get content from a jQuery selector
        content = $(contentSource);
    } else {
        // Content declared inline
        content = linkElement.attr('data-popupcontent');
        var r = /\\u([\d\w]{4})/gi;
        content = content.replace(r, function (match, group) {
            return String.fromCharCode(parseInt(group, 16));
        });
        content = content.replace(/\\n/g, '\n').replace(/\\r/g, '\r');
    }
    var showEvent = linkElement.attr('data-popupshowevent');
    if (!showEvent) {
        // Default show event to click
        showEvent = 'click';
    }
    var titleText = linkElement.attr('data-popuptitle');
    if (!titleText) {
        // Default title to link caption
        titleText = linkElement.text();
    }
    var classes = 'ui-tooltip-shadow';
    var customClasses = linkElement.attr('data-popupclasses');
    if (customClasses) {
        // Custom coloring or effect class
        classes = classes + ' ' + customClasses;
    }
    var adjustX = 0;
    if (linkElement.padding().left != 0) {
        // Adjust the padding of the tip by the same amount as the link element padding
        adjustX = linkElement.padding().left;
    }
    var showOptions = { event: showEvent };
    var hideOptions = { delay: 500, fixed: true };
    if (isAnimationDisabled) {
        showOptions.effect = false;
        hideOptions.effect = false;
    }
    linkElement.qtip({ content: { text: content,
        title: { text: titleText }
    },
        position: { my: 'top left',
            at: 'bottom left',
            adjust: { x: adjustX },
            viewport: $(window)
        },
        show: showOptions,
        hide: hideOptions,
        events: { render: function (event, api) {
            var tooltip = api.elements.tooltip;
            $(tooltip).find('.ToggleCheckbox').bind('click.toggle', function () {
                $(this).toggleCheckbox();
            });
            $(tooltip).find('.ToggleFilterCheckbox').bind('click.toggle', function () {
                $(this).toggleFilterCheckbox();
            });
            $(tooltip).find('.ToggleLanguageCheckbox').bind('click.toggle', function () {
                $(this).toggleLanguageCheckbox();
            });
        }
        },
        style: { classes: classes }
    });
};

/* End Popups */


/* Design time configuration */
$(function () {
    if (isDesignTime) {
        $('<div/>', { id: 'hsDesignTimeLoad',
                      click: function (e) {
                          var scrollPosition = window.getLocalStorage().getAttribute('scrollPosition');
                          if (scrollPosition) {
                              $(window).scrollTop(scrollPosition);
                          }
                      }
                  }).appendTo('body').css('display: none');
        $('<div/>', { id: 'hsDesignTimeSave',
                      click: function (e) {
                          window.getLocalStorage().setAttribute('scrollPosition', $(window).scrollTop());
                      }
                  }).appendTo('body').css('display: none');
        $('<div/>', { id: 'DesignTimeNewContentNotify',
                      click: function (e) {
                          
                      }
                  }).appendTo('body').css('display: none');

        isAnimationDisabled = true;
    }
});
/* End Design time configuration */

function tocDocument() {
    try {
        return findFrame("webnavbar").document.getElementById("cntNavtoc");
    } catch (e) { }
}
/* End Synchronize the web Table of Contents */


/* Common Messaging Support */
function isPostMessageEnabled() {
    return (window['postMessage'] != null);
}

function addMessageListener(receiver) {
    if (isPostMessageEnabled()) {
        if (window['addEventListener']) {
            window.addEventListener("message", receiver, false);
        }
        else {
            window.attachEvent("onmessage", receiver);
        }
    }
}

function Message(messageType, messageData) {
    this.messageType = messageType;
    this.messageData = messageData;
}

function getMessage(data) {
    var separator = data.indexOf("|");
    var messageType;
    var messageData;

    if (separator != -1) {
        messageType = data.substring(0, separator);
        messageData = data.substring(separator + 1);
    }
    else {
        messageType = data;
        messageData = "";
    }

    return new Message(messageType, messageData);
}

/* Message Handler */
function receiveMessage(event) {
    var message = null;
    try {
        message = getMessage(event.data);
    } catch (ex) {
        // Catch exceptions that can fire at design time
    }

    if (message) {
        switch (message.messageType) {
            case "print":
                printDocument();
                break;
            case "addtofavorites":
                addToFavorites();
                break;
            case "quicksearch":
                highlightText(document, message.messageData, "black", "yellow", true);
                break;
            case "resetquicksearch":
                removeAllHighlights(document);
                break;
            case "refresh":
                document.location.reload();
                break;
        }
    }
}

/* Print */
function printDocument() {
    window.print();
}

/* Add to favorites */
function addToFavorites() {
    var nav = findFrame("webnavbar");
    nav.postMessage("addtofavorites|" + location + "|" + document.title, "*");
}

/* Highlight text in a document */
function highlightText(targetDocument, text, color, backColor, clearBefore) {
    if (clearBefore) {
        firstMatch = null;
        removeAllHighlights(targetDocument);
    }

    if (text != "") {
        HighlightTextInElement(targetDocument, targetDocument.body, text, color, backColor, firstMatch);
        // Scroll to the first hit if it's not already visible
        if (firstMatch && clearBefore) {
            if (getElementPosition(firstMatch).top > targetDocument.documentElement.scrollTop + targetDocument.documentElement.clientHeight || getElementPosition(firstMatch).top < targetDocument.documentElement.scrollTop) {
                targetDocument.documentElement.scrollTop = firstMatch.offsetTop;
            }
        }
    }
}

/* Highlight text in a specific element */
function HighlightTextInElement(targetDocument, element, text, color, backColor) {
    var lowerCaseText = text.toLowerCase();
    var node = null;
    var nodeText = null;
    var lowerCaseNodeText = null;
    var highlightSpan = null;
    var remainingText = null;
    var textNode = null;
    var selection = null;

    // Traverse the document backwards otherwise the DOM returns stale objects as
    //  we make modifications
    for (var x = element.childNodes.length - 1; x >= 0; x--) {
        node = element.childNodes[x];

        // Text Node
        if (node.nodeType == 3) {
            nodeText = node.nodeValue;
            lowerCaseNodeText = nodeText.toLowerCase();
            for (pos = lowerCaseNodeText.indexOf(lowerCaseText); pos >= 0; pos = lowerCaseNodeText.indexOf(lowerCaseText)) {
                // Create a span to mark up the highlight
                highlightSpan = targetDocument.createElement("SPAN");
                highlightSpan.style.backgroundColor = backColor;
                highlightSpan.style.color = color;
                highlightSpan.className = "InnovasysSearchHighlight";
                highlightSpan.appendChild(targetDocument.createTextNode(nodeText.substring(pos, pos + text.length)));

                // Insert the span containing the term
                remainingText = targetDocument.createTextNode(nodeText.substring(pos + text.length, nodeText.length));
                node.nodeValue = nodeText.substring(0, pos);
                highlightSpan = node.parentNode.insertBefore(highlightSpan, node.nextSibling);
                remainingText = node.parentNode.insertBefore(remainingText, highlightSpan.nextSibling);

                // Store the first (last)hit so we can scroll to it
                firstMatch = highlightSpan;

                // Skip past the new nodes we've added
                node = node.nextSibling.nextSibling;
                nodeText = node.nodeValue;
                lowerCaseNodeText = nodeText.toLowerCase();
            }
        }
        // Element node
        else if (node.nodeType == 1) {
            // To ensure we don't modify script or go over
            //  highlights we have already applied
            if (node.nodeName != "SCRIPT" && !(node.nodeName == "SPAN" && node.className == "InnovasysSearchHighlight")) {
                HighlightTextInElement(targetDocument, node, text, color, backColor);
            }
        }
    }
}

/* Returns all highlight SPAN elements for a document */
function getHighlightSpans(targetDocument) {
    var spans = targetDocument.getElementsByTagName("SPAN");
    var highlightSpans = new Array();
    var span = null;
    var highlightSpanCount = 0;

    for (x = spans.length - 1; x >= 0; x--) {
        span = spans[x];
        if (span.className == "InnovasysSearchHighlight") {
            highlightSpans[highlightSpanCount] = span;
            highlightSpanCount++;
        }
    }

    return highlightSpans;
}

/* Merges any adjacent text node.s The IE DOM in particular has a habit of
splitting up text nodes, and also after highlighting and removing split
adjacent nodes can be left */
function cleanUpTextNodes(parentNode) {
    var node = null;
    var lastNode = null;
    var mergeCount = null;

    do {
        mergeCount = 0;
        for (var x = 1; x < parentNode.childNodes.length; x++) {
            node = parentNode.childNodes[x];
            lastNode = node.previousSibling;

            if (node.nodeType == 3 && lastNode.nodeType == 3) {
                node.nodeValue = lastNode.nodeValue + node.nodeValue;
                parentNode.removeChild(lastNode);
                mergeCount++;
            }
        }
    }
    while (mergeCount > 0)

    for (var x = 0; x < parentNode.childNodes.length; x++) {
        cleanUpTextNodes(parentNode.childNodes[x]);
    }
}

/* Removes any previously added highlight SPANs from the document */
function removeAllHighlights(targetDocument) {
    var spans = getHighlightSpans(targetDocument);
    var text = null;

    for (x = spans.length - 1; x >= 0; x--) {
        span = spans[x];
        text = targetDocument.createTextNode(span.innerHTML);
        span.parentNode.replaceChild(text, span);
    }

    // This process may have resulted in multiple contiguous text nodes
    //  which could cause problems with subsequent search highlight operations
    // So we join any continguous text nodes here
    cleanUpTextNodes(targetDocument.body);
}
/* End Common Messaging Support */


/* Microsoft Help Viewer Compatibility */
function removeExternalFile(filename, filetype) {
    var targetTagName = (filetype == "js") ? "script" : (filetype == "css") ? "link" : "none"
    var targetAttribute = (filetype == "js") ? "src" : (filetype == "css") ? "href" : "none"
    $(targetTagName).each(function (index) {
        if ($(this).attr(targetAttribute).match(filename))
            this.parentNode.removeChild(this);
    });
}

function isMshv() {
    return (location.protocol == 'ms-xhelp:' || location.href.indexOf('ms.help?') != -1 || location.href.indexOf('?method=page&') != -1);
}

function isMshv2() {
    var script = $('#mshs_support_script').get(0);
    var scriptSrc = script.src;
    return (scriptSrc.indexOf("method=asset") != -1)
}

/* Gets the MSHS base url for resources */
function mshvResourceBaseUrl() {

    if (isDesignTime) {
        return '';
    }
    else {
        // Get the first script tag
        var script = $('#mshs_support_script').get(0);

        // Extract the src which is a full resource url to within our origin .mshc
        var scriptSrc = script.src;

        var scriptUrl = null;
        if (isMshv2()) {
            // HV 2
            var startIndex = scriptSrc.indexOf('&id=');
            scriptUrl = scriptSrc.substring(0, startIndex);
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

if (isMshv()) {
    removeExternalFile(/branding.*\.css/g, "css");
    if (!isMshv2()) {
        // MSHV v1 flickers badly as the content is loaded and reparented, so 
        //  we hide the main content div until the page finishes initializing
        document.write('<style>#BodyContent { visibility: hidden }</style>');
    }
}
/* Microsoft Help Viewer Compatibility */

var mshvPendingStylesheets = new Array();
var mshvPendingStylesheetTimer = null;
function mshvFixUrls() {
	
	// Fix Javascript rules using urls
	var stylesheets = document.styleSheets;
	if (stylesheets && stylesheets.length > 0) {
	
		// Waiting on any stylesheets to load?
		if (mshvPendingStylesheets.length != 0) {
			for (var pendingStylesheetIndex = 0; pendingStylesheetIndex < mshvPendingStylesheets.length; pendingStylesheetIndex++) {
			
				var pendingStylesheetHref = mshvPendingStylesheets[pendingStylesheetIndex];
				var foundStylesheet = false;
				for (var stylesheetindex = 0; stylesheetindex < (stylesheets.length) ; stylesheetindex++) {
					var stylesheet = stylesheets[stylesheetindex];
					if (stylesheet.href != null && stylesheet.href == pendingStylesheetHref) {
						// Found the pending stylesheet - check that the rules have loaded
						var rules = null;
						try {
							if (stylesheet.rules) {
								rules = stylesheet.rules;
							}
							else {
								rules = stylesheet.cssRules;
							}
						} catch (ex) { };
						if (rules != null && rules.length > 0) {
							foundStylesheet = true;
						}
						break;
					}
				}
				if (!foundStylesheet) {
					// Could not locate the stylesheet, try again in a bit
					if (mshvPendingStylesheetTimer == null) {
						mshvPendingStylesheetTimer = window.setInterval(mshvFixUrls, 50);
					}
					return;
				}
			
			}
			if (mshvPendingStylesheetTimer != null) {
				clearInterval(mshvPendingStylesheetTimer);
				mshvPendingStylesheetTimer = null;
			}
		}
	
		for (var stylesheetindex = 0; stylesheetindex < (stylesheets.length) ; stylesheetindex++) {
			var stylesheet = stylesheets[stylesheetindex];
			var rules;
			try {
				if (stylesheet.rules) {
					rules = stylesheet.rules;
				}
				else {
					rules = stylesheet.cssRules;
				}
			} catch (ex) { };
			if (rules) {
				for (var ruleindex = 0; ruleindex < rules.length; ruleindex++) {
					var rule = rules[ruleindex];
					if (rule.style.backgroundImage) {
						if (rule.style.backgroundImage.substring(0, 4) == 'url(') {
							var backgroundText = rule.style.backgroundImage;
							var originalUrl = null;
							if (rule.style.backgroundImage.indexOf('127.0.0.1') != -1) {
								// Chrome - rule returned as full url
								originalUrl = backgroundText.substring(backgroundText.indexOf('/', backgroundText.indexOf('127.0.0.1')) + 5, backgroundText.lastIndexOf(')'));
							}
							else if (backgroundText.indexOf('../') != -1) {
									// IE - rule returned as original, with a .. prefix
								originalUrl = backgroundText.substring(backgroundText.indexOf('../') + 2, backgroundText.lastIndexOf(')'));
							} else {
								// Relative url
								originalUrl = backgroundText.substring(0, backgroundText.lastIndexOf(')'));
							}
							originalUrl = originalUrl.replace("\"", "");
							var newUrl = 'url(\"' + mshvResourceBaseUrl() + originalUrl + '\")';
							backgroundText = newUrl + backgroundText.substring(backgroundText.indexOf(')') + 1);
							rule.style.backgroundImage = backgroundText;
						}
					}
				}
			}
		}
	}
	
}

/* Common Page Initialization */
function initializePageContent() {

    // Microsoft Help Viewer (Visual Studio 2012) patches
    if (isMshv() && isMshv2()) {
        $('body').hide();

        // Standard url() references in stylesheets don't work in MSHV 2 so we have to reference
        //  an additional stylesheet with alternate syntax
        $('link').each(function () {
            var mshvStylesheet = $(this).attr('data-mshv2-stylesheet');
            if (mshvStylesheet) {
                var newStylesheetHref = 'ms-xhelp:///?;' + mshvStylesheet;
                mshvPendingStylesheets.push(newStylesheetHref);
                $('head').append('<link rel="stylesheet" href="' + newStylesheetHref + '" type="text/css" />');
            }
        });

        // Fix any id links to work around bug in VS 2012 RC Help Viewer
        $('a').each(function () {
            var href = $(this).attr('href');
            if (href && href.indexOf('ms-xhelp:///?id=') != -1) {
                $(this).attr('href', href.replace('ms-xhelp:///?id=', 'ms-xhelp:///?method=page&id='));
            }
        });
    }

    // Microsoft Help Viewer 1 (Visual Studio 2010) patches
    if (isMshv() && !isMshv2()) {
        // Standard url() references in stylesheets don't work in MSHV 2 so we have to reference
        //  an additional stylesheet with alternate syntax for those references to work in MSHV 1
        $('link').each(function () {
            var mshvStylesheet = $(this).attr('data-mshv1-stylesheet');
            if (mshvStylesheet) {
                var newStylesheetHref = mshvResourceBaseUrl() + mshvStylesheet;
                mshvPendingStylesheets.push(newStylesheetHref);
				
                $('head').append('<link rel="stylesheet" href="' + newStylesheetHref + '" type="text/css" />');
            }
        });
    }

    // Mark as new
    if (isNew) {
        $('body').addClass('IsNew');
    }

    // Wire up toggle sections
    $('.SectionHeading').loadToggleSectionState().click(function () {
        $(this).toggleSection();
    });
    setToggleAllSectionsVisibility();

    // Configure any popup links
    $('.PopupLink').each(function () {
        configurePopupLink(this);
    });

    var selectedTabIndex = window.getLocalStorage().getAttribute('TabContainerSelectedTabIndex');
    if (!selectedTabIndex) {
        selectedTabIndex = 0;
    }

    // Change default duration on the tabs
    if (!isDesignTime) {
        $('.TabContainer').tabs({
            fx: { opacity: 'toggle', duration: 'fast' },
            selected: selectedTabIndex,
            select: function (event, ui) {
                window.getLocalStorage().setAttribute('TabContainerSelectedTabIndex', ui.index);
            }
        });
    } else {
        $('.TabContainer').tabs({
            selected: selectedTabIndex,
            select: function (event, ui) {
                window.getLocalStorage().setAttribute('TabContainerSelectedTabIndex', ui.index);
            }
        });
    }

    // Wire up the copy code functionality
    initializeCopyCodeLinks();

    // If running in a frame, set up a message listener and let
    //  the parent frame know we have loaded
    if (parent) {
        /* Running in a frame - listen for commands */
        if (isPostMessageEnabled()) {
            addMessageListener(receiveMessage);
            parent.postMessage("loaded|" + location.href, "*");
            parent.postMessage("updatePageTitle|" + document.title, "*");
        }
        else {
            parent.loaded = true;
        }
    }

    // Fix quirks mode rendering issues
    if (document.compatMode != 'CSS1Compat') {

        var ContentSections = $('.SectionContent,.DescriptionContent,.ReturnsContent,.DescriptionCell,td.hs-box-content,td.hs-box-content span#Content')
        ContentSections.each(function (index) {
            $(this).children().first().addClass('FirstChild');
            $(this).children().last().addClass('LastChild');
        });
        
        $('table.SyntaxTable th:first-child').addClass('FirstChild');
        $('table.SyntaxTable th:last-child').addClass('LastChild');
        
        $('p+p').addClass('AdjacentParagraph');
        $('h4+.ReturnsContent').addClass('ReturnsContentAfterHeading');
        $('exampleSectionContent p+div').addClass('ExampleAfterParagraph');

    } else if ($.browser.msie && parseInt($.browser.version, 10) <= 8) {

        $('table.SyntaxTable th:last-child').addClass('LastChild');
        var ContentSections = $('.SectionContent,.DescriptionContent,.ReturnsContent,.DescriptionCell,td.hs-box-content,td.hs-box-content span#Content')
        ContentSections.each(function (index) {
            $(this).children().last().addClass('LastChild');
        });

    }

    // If running in Microsoft Help Viewer, execute some workarounds
    if (isMshv()) {

        if (!isMshv2()) {

            // Fix double line breaks
            $('BR').filter(function () { return $(this).next().is('BR') }).remove();

            // Fix bookmark links
            $('A').each(function () {
                // Check for bookmark links - currently prefixed with the full page url
                var anchorHref = $(this).attr('href');
                if (anchorHref && anchorHref.indexOf('#') != -1) {
                    var bookmark = anchorHref.substring(anchorHref.indexOf('#'));
                    if (anchorHref.substring(0, anchorHref.indexOf('#')) == location.href) {
                        // Bookmark in this document
                        anchor.attr('target', '_self');
                    }
                }
            });

            mshvFixUrls();

            $('#BodyContent').css('visibility', 'visible');

        }
        else {
            $('body').show();
        }

    }

}
/* End Common Page Initialization */

/* Language Filtering */
$.fn.toggleLanguageCheckbox = function (method) {

    // Wrapper for toggleCheckBox that hides the consolidated VB section
    //  if both VB and VBUsage are hidden

    if (this.attr('data-toggleclass') == 'FilteredContentVBUsage'
        || this.attr('data-toggleclass') == 'FilteredContentVB') {
        var allVbHidden = false;
        if (this.attr('data-toggleclass') == 'FilteredContentVBUsage') {
            allVbHidden = !($('.FilteredContentVBUsage').css("display") == "none") && ($('.FilteredContentVB').length == 0 || ($('.FilteredContentVB').css("display") == "none"));
        } else if (this.attr('data-toggleclass') == 'FilteredContentVB') {
            allVbHidden = !($('.FilteredContentVB').css("display") == "none") && ($('.FilteredContentVBUsage').length == 0 || ($('.FilteredContentVBUsage').css("display") == "none"));
        }

        if (allVbHidden != ($('.FilteredContentVBAll').css("display") == "none")) {
            $('.FilteredContentVBAll').toggleElement(method);
        }
    }

    var result = $(this).toggleCheckbox(method);

    updateLanguageFilterPopupLinkLabel();

    return result;
}

function updateLanguageFilterPopupLinkLabel() {
    // Set caption of language filter to reflect current set
    var targetLabel = null
    var allCheckboxes = $('.ToggleLanguageCheckbox')
    var allCheckedCheckboxes = allCheckboxes.filter(':checked');
    var allLabels = $('#LanguageFilterPopupLink label');
    if (allCheckedCheckboxes.length == allCheckboxes.length) {
        // All languages	
        targetLabel = $('#LanguageFilterPopupLink label#ShowAllLabel')
    } else if (allCheckedCheckboxes.length == 0) {
        // No languages

    } else if (allCheckedCheckboxes.length == 1) {
        // Single language
        var languageName = allCheckedCheckboxes.attr('data-languagename');
        targetLabel = $('#LanguageFilterPopupLink label#' + languageName + 'Label')
    } else {
        // Multiple languages
        if (allCheckedCheckboxes.length == 2
            && allCheckedCheckboxes.filter('[data-languagename^="VB"]').length == 2) {
            // 2 languages, both VB
            targetLabel = $('#LanguageFilterPopupLink label#VBAllLabel')
        }
        else {
            targetLabel = $('#LanguageFilterPopupLink label#MultipleLabel')
        }
    }
    allLabels.css("display", "none");
    targetLabel.css("display", "inline");
}

$.fn.loadToggleLanguageCheckboxState = function () {
    return this.each(function () {

        var attributeValue = window.getLocalStorage().getAttribute('CheckboxIsUnchecked' + $(this).attr('id'));
        if (attributeValue == 'true') {
            $(this).prop('checked', false);
            $(this).toggleLanguageCheckbox('immediate');
        }

    });
}
/* End Language Filtering */

/* .NET Framework Help Topic Resolution */

// This function is Copyright 2006 Innovasys Limited. No reproduction or usage
//  allowed other than in documentation generated by licensed Innovasys products
function resolveHelp2Keyword(Keyword, OnlineKeyword) {

    var URL = "";

    try {
        // Try the current namespace
        URL = findHelp2Keyword(getCurrentHelp2Namespace(), Keyword);
        if (URL == "") {
            // Try the likely namespaces first, most recent first
            URL = findHelp2Keyword("MS.VSCC.v80", Keyword);
            if (URL == "") {
                URL = findHelp2Keyword("MS.VSCC.2003", Keyword);
                if (URL == "") {
                    URL = findHelp2Keyword("MS.VSCC", Keyword);
                }
            }
        }

        // URL found in one of the known VSCC namespaces
        if (URL != "") {
            return URL;
        }
            // For future proofing, try other VSCC namespaces
        else {
            var RegistryWalker = new ActiveXObject("HxDs.HxRegistryWalker");
            var Namespaces = RegistryWalker.RegisteredNamespaceList("MS.VSCC");
            var Namespace, NamespaceName, Session, Topics, Topic;

            if (Namespaces.Count > 0) {
                for (n = 1; n <= Namespaces.Count; n++) {
                    Namespace = Namespaces.Item(n);
                    NamespaceName = Namespace.Name;
                    if (NamespaceName.substring(0, 7) == "MS.VSCC") {
                        switch (NamespaceName) {
                            case "MS.VSCC.v80":
                                break;
                            case "MS.VSCC.2003":
                                break;
                            case "MS.VSCC":
                                break;
                            default:
                                URL = findHelp2Keyword(NamespaceName);
                                if (URL != "") {
                                    return Topics(1).URL;
                                }
                        }
                    }
                }
            }
        }
    }
    catch (e) { }

    // No match found in any applicable namespace
    // Msdn doesn't support links to individual overloads, only to the master page
    //  so we trim off the brackets when directing to Msdn
    var BracketPosition = OnlineKeyword.indexOf("(");
    if (BracketPosition != -1) {
        OnlineKeyword = OnlineKeyword.substring(0, BracketPosition);
    }
    return "http://msdn.microsoft.com/query/dev10.query?appId=Dev10IDEF1&l=EN-US&k=k(" + OnlineKeyword + ")&rd=true"
}

function findHelp2Keyword(NamespaceName, Keyword) {
    var Session, Topics;

    if (NamespaceName.length > 0) {
        try {
            Session = new ActiveXObject("HxDs.HxSession");
            Session.Initialize("ms-help://" + NamespaceName, 0);
            Topics = Session.Query(Keyword, "!DefaultAssociativeIndex", 0, "");
            if (Topics.Count > 0) {
                return Topics(1).URL;
            }
        }
        catch (e) { }
    }
    return "";
}

function navigateToHelp2Keyword(Keyword, OnlineKeyword, ReplacePage) {
    window.status = "Resolving link. Please wait a moment...";
    var URL = resolveHelp2Keyword(Keyword, OnlineKeyword);
    window.status = "";
    if (URL.substring(0, 25) === "http://msdn.microsoft.com" && window.parent != null) {
        // MSDN no longer support hosting in an IFRAME so open in new browser window 
        window.open(URL, "_blank");
    } else if (ReplacePage == true) {
        location.replace(URL);
    } else {
        location.href = URL;
    }
}

function getCurrentHelp2Namespace() {
    var namespace = "";
    var location = window.location;

    if (location.protocol == "ms-help:") {
        namespace = location.hostname;
        if (namespace.substring(0, 2) == "//")
            namespace = namespace.substring(2);
    }

    return namespace;
}

/* End .NET Framework Help Topic Resolution */

/* Initialize Page */
$(function () {

    initializePageContent();

    // Wire up any toggle check boxes
    $('.ToggleCheckbox').bind('click.toggle', function () { $(this).toggleCheckbox(); }).loadToggleCheckboxState().unbind('click.toggle');
    $('.ToggleLanguageCheckbox').bind('click.toggle', function () { $(this).toggleLanguageCheckbox(); }).loadToggleLanguageCheckboxState().unbind('click.toggle');
    
});
