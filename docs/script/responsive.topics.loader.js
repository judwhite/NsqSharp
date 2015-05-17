yepnope.insertBeforeElement = document.getElementById('responsive-marker');
switch (getDeviceType()) {
    case "MOBILE":
        Modernizr.load([{
                        load: ['stylesheets/bootstrap.css',
                                'stylesheets/mobile.topics.2012.css',
                                'script/responsive.common.min.js',
                                'script/mobile.topics.2012.js'],
                        complete: function () {
                            onResponsiveFilesLoaded()
                        }
                        }]);          
        break;
    case "TABLET":
        Modernizr.load([{
                        load: ['stylesheets/bootstrap.css',
                                'stylesheets/tablet.topics.2012.css',
                                'script/responsive.common.min.js',
                                'script/tablet.topics.2012.js'],
                        complete: function () {
                            onResponsiveFilesLoaded()
                        }
                        }]);
        break;
}