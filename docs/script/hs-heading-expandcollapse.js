/* Expand / Collapse Section support */

var collapsedSections = new Array();
var sectionsLoaded = false;

if (!toggleExpanded) {
    function toggleExpanded(img) {
        // Firefox passes in the event object from the event handler, so
        //  we check for that and set to null
        if (img) {
            if (img.tagName == null) {
                e = img;
                img = null;
            }
        }

        // Find the expand & collapse image
        if (!img) {
            if (window.event)
                e = window.event;

            var img = sourceElement(e)
            if (img) {
                while (img) {
                    if (img.className == "expandcollapse" && img.tagName == "SPAN")
                        break;
                    else
                        img = img.parentNode;
                }
                if (img)
                    img = findExpandCollapseImage(img);
            }
        }


        if (img) {
            if (isSectionCollapsed(img.id) == true) {
                img.src = hsFixRelativeUrl("images/hs-heading-expanded.gif");
                expandSection(img);
                removeCollapsedItem(img.id);
                if (img.id.indexOf("Family", 0) == 0) {
                    protectedMembers = "on";
                    configureMembersFilterCheckboxes();
                    changeMembersFilterLabel();
                }
            }
            else {
                img.src = hsFixRelativeUrl("images/hs-heading-collapsed.gif");
                collapseSection(img);
                addCollapsedSection(img.id);
            }

            saveSections();
        }
    }
}

function findExpandCollapseImage(sourceElement)
{
    var e;
    var elements;

    if(sourceElement.tagName == "IMG" && sourceElement.className == "toggle")
    {
        return(sourceElement);
    }
    else
    {
        if(sourceElement)
        {
            elements = sourceElement.getElementsByTagName("IMG");

            for(var i=0;i<elements.length;i++)
            {
                e = elements[i];
                if(e.className == "toggle")
                {
                    return(e);
                }
            }
        }
    }
}

if (!toggleExpandedOnKey) {
    function toggleExpandedOnKey(e) {
        if (window.event) {
            e = window.event;
        }

        if (e.keyCode == 13) {
            toggleExpanded(findExpandCollapseImage(e.srcElement));
        }
    }
}

function expandSection(imageItem)
{
    if(imageItem.id != "toggleExpandedAllImage")
    {
        getNextSibling(imageItem.parentNode.parentNode).style.display = "";
    }
}

function collapseSection(imageItem)
{
    if(imageItem.id != "toggleExpandedAllImage")
    {
        getNextSibling(imageItem.parentNode.parentNode).style.display = "none";
    }
}

function isSectionCollapsed(imageId)
{
    for(var i=0; i < collapsedSections.length; ++i)
    {
        if(imageId == collapsedSections[i])
        {
            return true;
        }
    }

    return false;
}

function addCollapsedSection(imageId)
{
    if(isSectionCollapsed(imageId) == false)
    {
        collapsedSections[collapsedSections.length] = imageId;
    }
}

function removeCollapsedItem(imageId)
{
    for(var i=0; i < collapsedSections.length; ++i)
    {
        if(imageId == collapsedSections[i])
        {
            collapsedSections.splice(i, 1);
        }
    }
}

function saveSections()
{
    var x = 0;

    cleanUserDataStore();
    for(var i=0; i < collapsedSections.length; ++i)
    {
        if(shouldSave(collapsedSections[i]) == true)
        {
            save("imageValue" + x, collapsedSections[i]);
            x++;
        }
    }
}

function loadSections()
{
    var i = 0;
    var imageId = load("imageValue" + i);

    if(!sectionsLoaded)
    {
        while(imageId != null)
        {
            var imageItem = document.getElementById(imageId);

            if(imageItem != null)
            {
                if(imageItem.id.indexOf("Family", 0) == 0)
                {
                    if(protectedMembers == "on")
                    {
                        toggleExpanded(imageItem);
                    }
                }
                else
                {
                    toggleExpanded(imageItem);
                }
            }
            else
            {
                addCollapsedSection(imageId);
            }

            i++;
            imageId = load("imageValue" + i);
        }
        sectionsLoaded = true;
    }
    try {
        setCollapseAll();
    }
    catch (e) { }
}

function shouldSave(imageId)
{
    var toggleName;

    if(imageId == "toggleExpandedAllImage")
    {
        return false;
    }

    return true;
}

function openSectionById(id)
{
    var e=documentElement(id);

    if(e)
    {
        if(isSectionCollapsed(e.id) == true)
        {
            toggleExpanded(e);
        }
    }
}

function cleanUserDataStore()
{
    var i = 0;
    var imageId = load("imageValue" + i);

    while(imageId != null)
    {
        removeAttribute("imageValue" + i);
        i++;
        imageId = load("imageValue" + i);
    }
}