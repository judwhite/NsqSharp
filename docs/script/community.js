/* Community Features Filtering */
var ic_communityShowPrivate = false;
var ic_communityShowPublic = false;
var ic_communitySignedIn = false;
var ic_clientBehavior = new Object();
var ic_ajax;
var oldOnLoad;

/* Hook window load */
oldOnLoad = (window.onload) ? window.onload : function () {};
window.onload = function () {oldOnLoad(); ic_loadCommunityFeatureStates()};

function ic_loadCommunityFeatureStates()
{
    if (typeof communityItemId === 'undefined' || !communityItemId) {
        // Community id not defined
        return;
    }

    ic_communityShowPrivate = loadValueWithDefault("community_showPrivate",true);
    ic_communityShowPublic = loadValueWithDefault("community_showPublic", true);
    var privateCheckBox = documentElement("CommunityShowPrivateCheckbox");
    if (privateCheckBox) {
        privateCheckBox.checked = ic_communityShowPrivate;
    }
    var publicCheckBox = documentElement("CommunityShowPublicCheckbox");
    if (publicCheckBox) {
        publicCheckBox.checked = ic_communityShowPublic;
    }
    
    ic_updateCommunityFilterLabel();
    ic_populateCommunitySections();    
}

function ic_saveCommunityFeatureStates()
{
    save("community_showPrivate",ic_communityShowPrivate);
    save("community_showPublic",ic_communityShowPublic);
}

function ic_setCommunityFeatureVisibility(checkBox)
{
    if (checkBox.id == "CommunityShowPrivateCheckbox")
    {
        ic_communityShowPrivate = !ic_communityShowPrivate;
        checkBox.checked = ic_communityShowPrivate;
    }
    else if (checkBox.id == "CommunityShowPublicCheckbox")
    {
        ic_communityShowPublic = !ic_communityShowPublic;
        checkBox.checked = ic_communityShowPublic;
    }
        
    ic_updateCommunityFilterLabel();
    ic_populateCommunitySections();
    ic_saveCommunityFeatureStates();
}

function ic_updateCommunityFilterLabel()
{
    if(!document.getElementById("showAllCommunityContentLabel"))
    {
        return;
    }
    
    document.getElementById("showNoCommunityContentLabel").style.display = ((!ic_communityShowPrivate && !ic_communityShowPublic)?"inline":"none");
    document.getElementById("showAllCommunityContentLabel").style.display = ((ic_communityShowPrivate && ic_communityShowPublic)?"inline":"none");
    document.getElementById("showPrivateCommunityContentLabel").style.display = ((ic_communityShowPrivate && !ic_communityShowPublic)?"inline":"none");
    document.getElementById("showPublicCommunityContentLabel").style.display = ((!ic_communityShowPrivate && ic_communityShowPublic)?"inline":"none");
}

function loadValueWithDefault(valueName,defaultValue)
{
    var value = load(valueName);
        
    if(value == null)
        return defaultValue;
    else
    {
        if (value == "true")
            return true;
        else if (value == "false")
            return false;
        else
            return value;
    }
}

/* End Community Features Filtering */

function ic_GetPhrase(name)
{
    var span = document.getElementById("ic_" + name);
    if (span)
    {
        return span.innerHTML;
    }
}

function ic_populateCommunitySections()
{
    if (typeof communityItemId === 'undefined' || !communityItemId) {
        // Community id not defined
        return;
    }

    var publicDisplayStyle = (ic_communityShowPublic?"block":"none");
    var privateDisplayStyle = (ic_communityShowPrivate?"block":"none");
    
    ic_ajax = new InnovasysAjax();
    ic_ajax.setNodeHandler("privateitems",ic_privateItemsHandler);
    ic_ajax.setNodeHandler("publicitems",ic_publicItemsHandler);
    ic_ajax.setNodeHandler("newpagecontent",ic_newPageContentHandler);
    ic_ajax.setNodeHandler("newscript",ic_newScriptHandler);
    ic_ajax.setNodeHandler("refresh",ic_refreshHandler);
    ic_ajax.setNodeHandler("errormessage",ic_errorMessageHandler);
    ic_ajax.setNodeHandler("signedin",ic_signedInHandler);
    ic_ajax.setNodeHandler("newstyles",ic_newStylesHandler);

    // Show the footer if any community features enabled
    var footerElement = documentElement("communityfooter");
    if (footerElement) {
        footerElement.style.display = ((ic_communityShowPrivate || ic_communityShowPublic) ? "block" : "none");
    }
    
    var communitydivs = document.getElementsByTagName("DIV");
    for (var x=0;x<communitydivs.length;x++)
    {
        var div = communitydivs[x];
        if (div.className == "communityprivatecontainer")
        {
            div.style.display = privateDisplayStyle;
            div.getElementsByTagName("DIV").item(0).innerHTML = ic_GetPhrase("Loading");
        }
        else if (div.className == "communitypubliccontainer" || div.className == "communityratingcontainer")
        {
            div.style.display = publicDisplayStyle;
            div.getElementsByTagName("DIV").item(0).innerHTML  = ic_GetPhrase("Loading");
        }
    }
    
    // Fire off the Ajax request for community content
    ic_ajax.sendRequest(communityBaseUrl + "ic_community.aspx?action=getcontent&language=" + communityPhraseLanguage + "&itemid=" + encodeURIComponent(communityItemId) + "&getprivate=" + (ic_communityShowPrivate?"true":"false") + "&getpublic=" + (ic_communityShowPublic?"true":"false") + "&projectkey=" + communityProjectKey)
}

function ic_addPrivateContent(contentType)
{
    // Fire off the request to add a new item
    ic_ajax.sendRequest(communityBaseUrl + "ic_community.aspx?action=createitem&language=" + communityPhraseLanguage + "&contenttype=" + contentType + "&itemid=" + encodeURIComponent(communityItemId) + "&isprivate=true&returnurl=" + location.href + "&projectkey=" + communityProjectKey);
}

function ic_addPublicContent(contentType)
{
    // Fire off the request to add a new item
    ic_ajax.sendRequest(communityBaseUrl + "ic_community.aspx?action=createitem&language=" + communityPhraseLanguage + "&contenttype=" + contentType + "&itemid=" + encodeURIComponent(communityItemId) + "&isprivate=false&returnurl=" + location.href + "&projectkey=" + communityProjectKey);
}

function ic_postPrivateContent(button,contentType)
{
    var txt = button.parentNode.getElementsByTagName("TEXTAREA").item(0);
    ic_ajax.sendRequest(communityBaseUrl + "ic_community.aspx?action=postnewitem&language=" + communityPhraseLanguage + "&itemid=" + encodeURIComponent(communityItemId) + "&contenttype=" + contentType + "&isprivate=true&projectkey=" + communityProjectKey,"POST",txt.value);
}

function ic_postPublicContent(button,contentType)
{
    var txt = button.parentNode.getElementsByTagName("TEXTAREA").item(0);
    ic_ajax.sendRequest(communityBaseUrl + "ic_community.aspx?action=postnewitem&language=" + communityPhraseLanguage + "&itemid=" + encodeURIComponent(communityItemId) + "&contenttype=" + contentType + "&isprivate=false&projectkey=" + communityProjectKey,"POST",txt.value);
}

function ic_postRating(value)
{
    ic_ajax.sendRequest(communityBaseUrl + "ic_community.aspx?action=postrating&language=" + communityPhraseLanguage + "&itemid=" + encodeURIComponent(communityItemId) + "&value=" + value + "&projectkey=" + communityProjectKey);
}

function ic_editPrivateItem(contentId)
{
    ic_ajax.sendRequest(communityBaseUrl + "ic_community.aspx?action=edititem&language=" + communityPhraseLanguage + "&contentid=" + contentId + "&projectkey=" + communityProjectKey);  
}

function ic_editPublicItem(contentId)
{
    ic_ajax.sendRequest(communityBaseUrl + "ic_community.aspx?action=edititem&language=" + communityPhraseLanguage + "&contentid=" + contentId + "&projectkey=" + communityProjectKey);  
}

function ic_deletePrivateItem(contentId)
{
    if (window.confirm(ic_GetPhrase("ConfirmDelete")))
        ic_ajax.sendRequest(communityBaseUrl + "ic_community.aspx?action=deleteitem&language=" + communityPhraseLanguage + "&contentid=" + contentId + "&projectkey=" + communityProjectKey);    
}

function ic_deletePublicItem(contentId)
{
    if (window.confirm(ic_GetPhrase("ConfirmDelete")))
        ic_ajax.sendRequest(communityBaseUrl + "ic_community.aspx?action=deleteitem&language=" + communityPhraseLanguage + "&contentid=" + contentId + "&projectkey=" + communityProjectKey);    
}

function ic_postEditPrivateContent(button,contentId)
{
    var txt = button.parentNode.getElementsByTagName("TEXTAREA").item(0);
    ic_ajax.sendRequest(communityBaseUrl + "ic_community.aspx?action=postedititem&language=" + communityPhraseLanguage + "&contentid=" + contentId + "&projectkey=" + communityProjectKey,"POST",txt.value);
}

function ic_postEditPublicContent(button,contentId)
{
    var txt = button.parentNode.getElementsByTagName("TEXTAREA").item(0);
    ic_ajax.sendRequest(communityBaseUrl + "ic_community.aspx?action=postedititem&language=" + communityPhraseLanguage + "&contentid=" + contentId + "&projectkey=" + communityProjectKey,"POST",txt.value);
}

function ic_communitySignIn()
{
    ic_ajax.sendRequest(communityBaseUrl + "ic_community.aspx?action=signin&language=" + communityPhraseLanguage + "&returnurl=" + location.href + "&projectkey=" + communityProjectKey);
}

function ic_communityCancelEdit(button)
{
    var container = button.parentNode;
    container.style.display = "none";
}

function ic_privateItemsHandler(element)
{
    ic_commonItemsHandler(element,"communityprivatecontent");
}

function ic_publicItemsHandler(element)
{
    ic_commonItemsHandler(element,"communitypubliccontent");
}

function ic_refreshHandler()
{
    ic_populateCommunitySections();
}

function ic_errorMessageHandler(element)
{
    alert(element.text);
}

function ic_signedInHandler(element)
{
    ic_communitySignedIn = (element.text == "true");
}

function ic_commonItemsHandler(element,prefix)
{
    // The returned XML is a collection of child nodes, one per content type
    for (x=0;x<element.childNodes.length;x++)
    {
        var contentNode = element.childNodes[x];
        var itemId = contentNode .getAttribute("itemId");
        var contentType = contentNode .getAttribute("contentType");
        
        var contentElement = document.getElementById(prefix + "|" + itemId + "|" + contentType);
        if (contentElement != null)
        {
            contentElement.innerHTML = contentNode.text;
        }
    }
    
    // Identify any sections without content
    var communitydivs = document.getElementsByTagName("DIV");
    for (var x=0;x<communitydivs.length;x++)
    {
        var div = communitydivs[x];
        if (div.className == "communityprivatecontainer")
        {
            if (div.getElementsByTagName("DIV").item(0).innerHTML == ic_GetPhrase("Loading"))
            {
                if (ic_communitySignedIn)
                {
                    div.getElementsByTagName("DIV").item(0).innerHTML = ic_GetPhrase("NoNotes");
                }
                else
                    div.getElementsByTagName("DIV").item(0).innerHTML = ic_GetPhrase("SignInToView");
            }
        }
        else if (div.className == "communitypubliccontainer")
        {
            if (div.getElementsByTagName("DIV").item(0).innerHTML == ic_GetPhrase("Loading"))
                div.getElementsByTagName("DIV").item(0).innerHTML  = ic_GetPhrase("NoComments");
        }
    }
    
}

function ic_newPageContentHandler(element)
{
        var elementId = element.getAttribute("clientElementId");
        
        var contentElement = document.getElementById(elementId);
        if (contentElement == null)
        {
            contentElement = document.createElement("DIV");
            contentElement.id = elementId;
            document.body.appendChild(contentElement);
        }
        contentElement.innerHTML = element.text;
}

function ic_newStylesHandler(element)
{
    var styles = element.text;
    var newSS=document.createElement('link');
    newSS.rel='stylesheet';
    newSS.href=styles;
    document.getElementsByTagName("head")[0].appendChild(newSS);
}

function ic_newScriptHandler(element)
{
    if (element.text.length > 0)
    {
        var ua = window.navigator.userAgent;
        var msie = ua.indexOf ( "MSIE " );
        var safari = ua.indexOf ( "Safari");
        var globalObj = this;
        if ( safari != -1)
            setTimeout(element.text,0);                        
        else if ( msie != -1 )
            window.execScript(element.text);
        else
        {
            if (globalObj.eval)
                globalObj.eval(element.text);
            else
                eval(element.text);
        }
    }
}

function ic_communityNavigate(url)
{
    if (isDesignTime)
        window.external.communityLogin(url);
    else
        location.href = url;
}


/**** Ajax Support ****/

/* Prototypes to make cross browser DOM functionality simpler */

if (typeof XMLDocument !== "undefined" && XMLDocument != null) {
  // select the first node that matches the XPath expression
  // xPath: the XPath expression to use
  XMLDocument.prototype.selectSingleNode = function(xPath) {
    var doc = this;
    if (doc.nodeType != 9)
      doc = doc.ownerDocument;
    if (doc.nsResolver == null) doc.nsResolver = function(prefix) { return(null); };
    var node = doc.evaluate(xPath, this, doc.nsResolver, XPathResult.ANY_UNORDERED_NODE_TYPE, null);
    if (node != null) node = node.singleNodeValue;
    return(node);
  }; // selectSingleNode

  Node.prototype.__defineGetter__("text", function () {
    return(this.textContent);
  }); // text
}

if(typeof HTMLElement!="undefined" && !HTMLElement.prototype.insertAdjacentElement){
    HTMLElement.prototype.insertAdjacentElement = function(where,parsedNode)
    {
        switch (where){
        case 'beforeBegin':
            this.parentNode.insertBefore(parsedNode,this)
            break;
        case 'afterBegin':
            this.insertBefore(parsedNode,this.firstChild);
            break;
        case 'beforeEnd':
            this.appendChild(parsedNode);
            break;
        case 'afterEnd':
            if (this.nextSibling) this.parentNode.insertBefore(parsedNode,this.nextSibling);
            else this.parentNode.appendChild(parsedNode);
            break;
        }
    }

    HTMLElement.prototype.insertAdjacentHTML = function(where,htmlStr)
    {
        var r = this.ownerDocument.createRange();
        r.setStartBefore(this);
        var parsedHTML = r.createContextualFragment(htmlStr);
        this.insertAdjacentElement(where,parsedHTML)
    }


    HTMLElement.prototype.insertAdjacentText = function(where,txtStr)
    {
        var parsedText = document.createTextNode(txtStr)
        this.insertAdjacentElement(where,parsedText)
    }
}

/* End cross browser DOM Prototypes */

/* Hashtable implementation */

function InnovasysHashtable()
{
   this.data = new Object();
   this.keys = new Array();
} // end constructor


function InnovasysHashtable_getKey(raw)
{
   // avoids conflict with actual attributes
   return '__'+ raw +'__';
}

function InnovasysHashtable_get(nam)
{
   var key = this.getKey(nam);
   // retreive value if key exists, otherwise null
   var val = (this.data[key]) ? this.data[key] : null;
   return val;
}

function InnovasysHashtable_put(nam, val)
{
   // Check that name isn't missing
   if (!nam) return false;
   
   var key = this.getKey(nam);
   
   // Add if doesn't already exist
   var exists = true;
   if (!this.data[key])
   {
      exists = false;
      this.keys[this.keys.length] = key;
   }
   
   // return old value if set, otherwise null
   var oldval = exists ? this.data[key] : null;
   
   this.data[key] = val;
    
   return oldval;
}


function InnovasysHashtable_keys()
{
   // return a copy of the array so that it isn't
   // accidentally modified by the caller
   return keys.slice(0);
}

function InnovasysHashtable_containsKey(nam)
{
   // Return true if key found
   return (this.get(nam) != null);
}

InnovasysHashtable.prototype.getKey = InnovasysHashtable_getKey;
InnovasysHashtable.prototype.get = InnovasysHashtable_get;
InnovasysHashtable.prototype.put = InnovasysHashtable_put;
InnovasysHashtable.prototype.keys = InnovasysHashtable_keys;
InnovasysHashtable.prototype.containsKey = InnovasysHashtable_containsKey;

/* End Hashtable implementation */


var HTTP_STATUS_OK = 200;
var READYSTATE_COMPLETED = 4;
var NODE_TYPE_ELEMENT = 1;

function InnovasysAjax(recurseOnChildren)
{
    // request objects
    this.aRequests = new Array();
    this.aRequests[0] = null;
    // maps handlers to element names
    this.hRespHandlers = new InnovasysHashtable();
    // records if a wildcard handler has been set
    this.isWildcardSet = false;
    // whether or not to recurse on children of a 
    // matched element that has already been passed
    // to an appropriate handler function.
    this.recurseOnChildren = 
      recurseOnChildren ? true : false;
    
    this.sendRequest = function(url, method, requestxml)
    {
        // set defaults for omitted optional arguments
        if (!method) method = "GET";
        if (!requestxml) requestxml = null;

        // request object
        var req = null;

        // look for an empty spot in requests 
        // array due to a deleted request. 

        // default spot is at end
        var openIndex = this.aRequests.length;
        // look for closer spot
        for (var i=0; i<this.aRequests.length; i++)
        {
            if (this.aRequests[i] == null)
            {
                openIndex = i;
                break;
            }
        }

    // now make the request, if possible
        if (window.ActiveXObject)
        {
            try {req = window.external.GetXMLHTTP();} catch (ex) { }
            if (!req)
                req = new ActiveXObject("Microsoft.XMLHTTP");
            if (req)
            {
                // this might look odd, but it is
                // necessary because ‘this’ in event 
                // handlers refers to the owner of the 
                // fired event. See: 
                // www.quirksmode.org/js/this.html
                var self = this;
                req.onreadystatechange = 
                  function() {self.handle()};
                // add the element to the array before 
                // doing anything that will fire 
                // readyStateChange event. If we didn’t
                // do this now, we could be getting event 
                // firings from request objects that we 
                // can’t find in our requsts array, when
                // we go to handle the readyStateChange.
                this.aRequests[openIndex] = req;
                req.open(method, url, true);
                req.send(requestxml);
            }
            else
            {
                return false;
            }
        }
        else if (window.XMLHttpRequest)
        {
            // this might look odd, but it is
            // necessary because ‘this’ in event 
            // handlers refers to the owner of the 
            // fired event. See: 
            // www.quirksmode.org/js/this.html
            var self = this;
            req = new XMLHttpRequest();
            req.onreadystatechange = 
               function() {self.handle()};
            // add the element to the array before 
            // doing anything that will fire 
            // readyStateChange event. If we didn’t
            // do this now, we could be getting event 
            // firings from request objects that we 
            // can’t find in our requsts array, when we 
            // go to handle the readyStateChange.
            this.aRequests[openIndex] = req;
            req.open(method, url, true);
            req.setRequestHeader("Content-Type", "text/xml");
            req.send(requestxml);
        }
        else
        {
            // no support
            return false; 
        } 

        return true; // indicate no errors
    }; 

    this.handle = function()
    {
        // cycle through request objects to see 
        // if any are ready with a response.
        for (var i=0; i<this.aRequests.length; i++)
        {
            // if state is "complete"
            if (this.aRequests[i] != null && this.aRequests[i].readyState == 4)
            {
               if (this.aRequests[i].status == 0 || this.aRequests[i].status == HTTP_STATUS_OK)
               {
                  // pass this off to the xml parser
                  this.parseResponse(this.aRequests[i].responseXML);
                  this.aRequests[i] = null;
               }
            }
        }
    };
    
    this.setNodeHandler = function(sElementName, funcHandler)
    {
        // flip flag if wildcard
        if (sElementName == '*')
            this.isWildcardSet = true;

        // add the element handler to the hashtable
        return this.hRespHandlers.put(sElementName, funcHandler);
    };
    
    
    this.parseResponse = function(oNode)
    {
        if (!oNode) return;
        
        // base case (oNode is a leaf element)
        if (!oNode.hasChildNodes()) return;

        // otherwise recurse through children
        var children = oNode.childNodes;

        for (var i=0; i<children.length; i++)
        {
            // only act on elements
            if (children[i].nodeType == NODE_TYPE_ELEMENT)
            {
                // check for specific handler
                var elementName = children[i].nodeName;
                if (this.hRespHandlers.containsKey(elementName))
                {
                    // get the handler
                    var funcHandler = this.hRespHandlers.get(elementName);
                    // fire the handler
                    funcHandler(children[i]);
                           
                    // recurse if necessary
                    if (this.recurseOnChildren) 
                        this.parseResponse(children[i]);
                }
                else if (this.isWildcardSet)
                {
                    // retreive the handler
                    var funcHandler = this.hRespHandlers.get('*');
                    // fire the handler
                    funcHandler(children[i]);

                    // recurse if necessary
                    if (this.recurseOnChildren) 
                        this.parseResponse(children[i]);
                }
                else
                {
                    // no match on this tree, search children
                    this.parseResponse(children[i]);
                }
            }
        }
    };

}
