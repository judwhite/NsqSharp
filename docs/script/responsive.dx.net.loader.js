yepnope.insertBeforeElement = document.getElementById('responsive-marker');
switch (getDeviceType()) {
    case "MOBILE":
        Modernizr.load([{
                        load: ['stylesheets/bootstrap.css',
                                'stylesheets/mobile.dx.net.2012.css',
                                'script/responsive.common.min.js',
                                'script/mobile.dx.net.2012.js'],
                        complete: function () {
                            onResponsiveFilesLoaded()
                        }
                        }]);
        break;
    case "TABLET":
        Modernizr.load([{
                        load: ['stylesheets/bootstrap.css',
                                'stylesheets/tablet.dx.net.2012.css',
                                'script/responsive.common.min.js',
                                'script/tablet.dx.net.2012.js'],
                        complete: function () {
                            onResponsiveFilesLoaded()
                        }
                        }]);
        break;
}