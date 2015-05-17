yepnope.insertBeforeElement = document.getElementById('responsive-marker');
switch (getDeviceType()) {
    case "MOBILE":
        Modernizr.load(['stylesheets/bootstrap.css',
                        'stylesheets/mobile.glossary.2012.css',
                        'script/responsive.common.min.js',
                        'script/mobile.glossary.2012.js']);
        break;
    case "TABLET":
        Modernizr.load(['stylesheets/bootstrap.css',
                        'stylesheets/tablet.glossary.2012.css',
                        'script/responsive.common.min.js',
                        'script/tablet.glossary.2012.js']);
        break;
}