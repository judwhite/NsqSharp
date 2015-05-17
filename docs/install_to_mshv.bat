REM To ensure that installation will work on machines on which the Help Library Manager tool may not have been run
REM  you should add the /content argument specifying the default location of the Help Library
REM The default location of the Help Library differs on XP and Vista/Windows 7 so should be looked up in the registry 
REM  in the value HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Help\v1.0\LocalStore and added to this script by your deployment
REM  process at install time
REM There is a Help Library Manager bug that prevents use of quotes around the /content argument path
REM  so on XP you should use the short version of paths containing spaces so that the path does not contain any spaces
 "%ProgramFiles%\Microsoft Help Viewer\v1.0\helplibmanager" /silent /product "VS" /version 100 /locale en-US /sourceMedia "%CD%\helpcontentsetup.msha"