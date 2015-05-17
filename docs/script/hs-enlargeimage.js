// This function retained for legacy and custom Dynamic Image Widgets - current standard Widget uses hsToggleImage below
//  for localization support
function hsEnlargeImage(img, link, inplace) {
    var newsrc;
    var newlinktext;

    if (img) {
        if (!img.src)
            img = documentElement(img);

        if (img) {
            if (img.src.substring(img.src.length - 9, img.src.length - 4).toLowerCase() == 'thumb') {
                newsrc = img.src.substring(0, img.src.length - 10) + img.src.substring(img.src.length - 4);
                newlinktext = link.innerHTML.replace(/enlarge/gi, "shrink");
            }
            else {
                newsrc = img.src.substring(0, img.src.length - 4) + '_thumb' + img.src.substring(img.src.length - 4);
                newlinktext = link.innerHTML.replace(/shrink/gi, "enlarge");
            }
            if (!inplace) {
                var newimage = new Image();
                newimage.src = newsrc;
                hsOpenWindow(newimage.src, newimage.width + 20, newimage.height + 25);
            }
            else {
                img.src = newsrc;
                link.innerHTML = newlinktext;
            }
        }
    }
}

function hsToggleImage(img, link, inplace) {
    var newsrc;
    var newlinktext;
    var newlinkimgsrc;

    if (img) {
        if (!img.src)
            img = documentElement(img);

        if (img) {
            var imgId = img.id;
            var expandDiv = document.getElementById(imgId + "_expand");
            var shrinkDiv = document.getElementById(imgId + "_shrink");
            if (img.src.substring(img.src.length - 9, img.src.length - 4).toLowerCase() == 'thumb') {
                // Currently collapsed - expand
                expandDiv.style.display = "none";
                shrinkDiv.style.display = "block";
                // New img src
                newsrc = img.src.substring(0, img.src.length - 10) + img.src.substring(img.src.length - 4);
            }
            else {
                // Currently expanded - collapse
                expandDiv.style.display = "block";
                shrinkDiv.style.display = "none";
                // New img src
                newsrc = img.src.substring(0, img.src.length - 4) + '_thumb' + img.src.substring(img.src.length - 4);
            }
            // Update the img with the new src
            if (!inplace) {
                var newimage = new Image();
                newimage.src = newsrc;
                hsOpenWindow(newimage.src, newimage.width + 20, newimage.height + 25);
            }
            else {
                img.src = newsrc;
            }
        }
    }
}

function hsOpenWindow(strURL,strWidth,strHeight)
{
    /* open a new browser window based on info passed to the function */
    window.open(strURL,"","Width=" + strWidth + ",Height=" + strHeight,0);
}
