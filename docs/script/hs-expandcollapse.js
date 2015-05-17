function HSToggleSection(id)
{
    var element;
    var img;

    // Find the element
    element = documentElement(id);
    img = documentElement(id+"_Image");
    if (element)
    {
        if (element.className=="hs-collapsed")
        {
            element.className="hs-expanded";
            if (img)
            {
                img.src = hsFixRelativeUrl("images/hs-expanded.gif");
            }
        }
        else
        {
            element.className="hs-collapsed";
            if (img)
            {
                img.src = hsFixRelativeUrl("images/hs-collapsed.gif");
            }
        };
    }
}

function HSHideOrShowAllCSections(show)
{
    var spans
    var divs

    spans = document.getElementsByTagName("SPAN");
    if (spans)
    {
        for (var spanindex = 0 ; spanindex < spans.length ; spanindex++)
        {
            if ((spans[spanindex].className == "hs-collapsed" && show) || (spans[spanindex].className == "hs-expanded" && !show))
            {
                HSToggleSection(spans[spanindex].id)
            }
        }
    }
    divs = document.getElementsByTagName("DIV")
    if (divs)
    {
        for (var divindex = 0 ; divindex < divs.length ; divindex++)
        {
            if ((divs[divindex].className == "hs-collapsed" && show) || (divs[divindex].className == "hs-expanded" && !show))
            {
                HSToggleSection(divs[divindex].id)
            }
        }
    }
}
function HSHideAllCSections()
{
    var HSHideAll = documentElement("HSHideAll");
    var HSShowAll = documentElement("HSShowAll");
    
    HSHideOrShowAllCSections(false) 
    if (HSHideAll)
    {
        HSHideAll.style.display="none";
        if (HSShowAll)
        {
            HSShowAll.style.display="block";
        }
    }
}
function HSShowAllCSections()
{
    var HSHideAll = documentElement("HSHideAll");
    var HSShowAll = documentElement("HSShowAll");
    
    HSHideOrShowAllCSections(true)
    if (HSShowAll)
    {
        HSShowAll.style.display="none";
        if (HSHideAll)
        {
            HSHideAll.style.display="block";
        }
    }   
}