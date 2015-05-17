/* GetSetting SaveSetting Alias */
if (!hsTabGetSetting) var hsTabGetSetting = function (key)
{
    return load(key);
}

if (!hsTabSaveSetting) var hsTabSaveSetting = function (key, value)
{
    save(key, value)
}
/* End GetSetting SaveSetting Alias */

/* Tab Strip */

if (!hsSetSelectedTabOnTabstrips) var hsSetSelectedTabOnTabstrips = function ()
{
    // Set the default tab on any tab strips
    var tabDivs = document.getElementsByTagName("DIV")
    for (var i = 0; i < tabDivs.length; i++)
    {
        var div = tabDivs[i];
        if (div.className == "HsTabStripContainer")
        {
            // Load the selected tab from the session cookie
            var defaultTabName = hsTabGetSetting(div.id + "_SelectedTab");
            if (defaultTabName)
                hsSetActiveTabById(div.id, defaultTabName);
            // Check that a tab is selected
            var selectedTabName = hsGetSelectedTabName(div);
            if (!selectedTabName)
            {
                var tabs = hsGetTabsFromTabContainer(div, true);
                if (tabs)
                    hsSetActiveTab(div, tabs[0]);
            }
        }
    }
}

if (!hsRefreshSelectedTabOnTabstrips) var hsRefreshSelectedTabOnTabstrips = function ()
{
    // Refresh the selected tab on any tab strips
    var tabDivs = document.getElementsByTagName("DIV")
    for (var i = 0; i < tabDivs.length; i++)
    {
        var div = tabDivs[i];
        if (div.className == "HsTabStripContainer")
        {
            // Get the currently selected tab
            var selectedTabName = hsGetSelectedTabName(div);
            hsSetActiveTabById(div.id, selectedTabName);
            // Now check we have a selection
            var selectedTabName = hsGetSelectedTabName(div);
            if (!selectedTabName)
            {
                var tabs = hsGetTabsFromTabContainer(div, true);
                if (tabs)
                    hsSetActiveTab(div, tabs[0]);
            }
        }
    }
}

if (!hsSaveSelectedTabs) var hsSaveSelectedTabs = function ()
{
    // Save the current tab on any tab strips
    var tabDivs = document.getElementsByTagName("DIV");
    for (var i = 0; i < tabDivs.length; i++)
    {
        var div = tabDivs[i];
        if (div.className == "HsTabStripContainer")
        {
            var selectedTabName = hsGetSelectedTabName(div);
            hsTabSaveSetting(div.id + "_SelectedTab", selectedTabName);
        }
    }
}

if (!hsSetActiveTabById) var hsSetActiveTabById = function (containerName, tabName)
{
    if (containerName && tabName)
    {
        var container = document.getElementById(containerName);
        var childNodes = container.childNodes;
        for (var i = 0; i < childNodes.length; i++)
        {
            var childNode = childNodes[i];
            if (childNode.id == tabName)
            {
                return hsSetActiveTab(container, childNode);
            }
        }
    }

    hsSaveSelectedTabs();
}

if (!hsGetNextDiv) var hsGetNextDiv = function (div)
{
    do
    {
        if (div.nextSibling)
        {
            div = div.nextSibling;
            if (div.tagName && div.tagName == "DIV" && div.style.display != "none")
                return div;
        }
    } while (div.nextSibling);
}

if (!hsGetPreviousDiv) var hsGetPreviousDiv = function (div)
{
    do
    {
        if (div.previousSibling)
        {
            div = div.previousSibling;
            if (div.tagName && div.tagName == "DIV" && div.style.display != "none")
                return div;
        }
    } while (div.previousSibling);
}

if (!hsSetActiveTab) var hsSetActiveTab = function (container, tab)
{
    if (container && tab)
    {
        var childNodes = container.childNodes;
        for (var i = 0; i < childNodes.length; i++)
        {
            var childNode = childNodes[i];
            if (childNode.tagName && childNode.tagName == "DIV" && (childNode.className == "HsTab" || childNode.className.indexOf(" HsTab") == childNode.className.length - 6))
            {
                var isActiveTab = (childNode == tab);
                if (isActiveTab)
                    childNode.className = "HsTabActive HsTab";
                else
                    childNode.className = "HsTab";
                // If this tab is visible, update the adjacent left / right end 
                if (childNode.style.display != 'none')
                {
                    if (hsGetNextDiv(childNode) && hsGetNextDiv(childNode).className && hsGetNextDiv(childNode).className.indexOf("HsTabRightEnd") != -1)
                    {
                        /* Last tab, update right end */
                        var rightEnd = hsGetNextDiv(childNode);
                        if (isActiveTab)
                            rightEnd.className = "HsTabRightEndActive HsTabRightEnd";
                        else
                            rightEnd.className = "HsTabRightEnd";
                    }
                    if (hsGetPreviousDiv(childNode) && hsGetPreviousDiv(childNode).className && hsGetPreviousDiv(childNode).className.indexOf("HsTabLeftEnd") != -1)
                    {
                        /* First tab, update right end */
                        var leftEnd = hsGetPreviousDiv(childNode);
                        if (isActiveTab)
                            leftEnd.className = "HsTabLeftEndActive HsTabLeftEnd";
                        else
                            leftEnd.className = "HsTabLeftEnd";
                    }
                }
                /* Find related content div and show/hide */
                hsShowOrHideTabContentSection(childNode.id + "_Content", isActiveTab);
            }
        }
    }
}

if (!hsShowOrHideTabContentSection) var hsShowOrHideTabContentSection = function (id, isVisible)
{
    var contentDiv = document.getElementById(id);
    if (contentDiv)
    {
        if (isVisible)
            contentDiv.style.display = "block";
        else
            contentDiv.style.display = "none";
    }
}

if (!hsGetSelectedTabName) var hsGetSelectedTabName = function (container)
{
    var tabs = hsGetTabsFromTabContainer(container, true);
    if (tabs)
    {
        for (var tabIndex = 0; tabIndex < tabs.length; tabIndex++)
        {
            var tab = tabs[tabIndex];
            if (tab.className.indexOf("HsTabActive") != -1)
                return tab.id;
        }
    }
}

if (!hsGetTabsFromTabContainer) var hsGetTabsFromTabContainer = function (container, visibleOnly)
{
    var tabs = [];
    var childNodes = container.childNodes;
    for (var i = 0; i < childNodes.length; i++)
    {
        var childNode = childNodes[i];
        if (childNode.tagName && childNode.tagName == "DIV" && (childNode.className == "HsTab" || (childNode.className.indexOf(" HsTab") == childNode.className.length - 6)))
        {
            if (!visibleOnly || childNode.style.display != "none")
                tabs[tabs.length] = childNode;
        }
    }
    return tabs;
}

/* End Tab Strip */