$(function () {
    // JQuery drop down header
    $('p.hs-toggler a:first-child').addClass('btn');
    // Expandable text
    $('a[href*="HSToggleSection"]').addClass('btn');
    // In this Topic links
    $('div.hs-inthistopic-container a').addClass('btn');
    // Language filtered code box
    $('span.CopyCode').addClass('btn btn-mini');
    // Remove slimbox click handler from dynamic image widget thumbnails
    $('a.hs-thumbnail').unbind('click');
    // Show all / hide all
    $('div#HSShowAll a, div#HSHideAll a').addClass('btn');
});
$(function () {
    $('div.BreadcrumbsContainer a').addClass('btn btn-mini');
    $('div#AfterHeaderContent a.PageLink').addClass('btn btn-mini');
    $('div#AfterHeaderContent span.PopupLink').addClass('btn btn-mini');
    $('div#AfterHeaderContent span.FunctionLink').addClass('btn btn-mini');
    $('div#seealsoSectionContent a').addClass('btn');
});
$(function(){
    $('div.communityprivatecontentcommands a').addClass('btn');
    $('div.communitypubliccontentcommands a').addClass('btn');
    $('div.communityprivatecontentcontainer > a:last-of-type').addClass('btn btn-mini');
});
$(function () {
    $('p.hs-toggler a:first-child').css('display', 'inline');
});