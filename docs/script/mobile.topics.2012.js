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
    $('td.LinkCell>a').addClass('btn');
    $('td.MembersLinkCell a').addClass('btn');
});
$(function () {
    $('div.communityratingcontainer').css('display', 'none');
    $('div#communityfooter').css('display', 'none');
});
$(function () {
    // drop down section
    $('div.SectionHeading').addClass('btn');
});