# Set-ExecutionPolicy -Scope CurrentUser Unrestricted
# Make sure msbuild.exe is in your path - C:\Windows\Microsoft.NET\Framework64\v4.0.30319
# Make sure nuget.exe is in your path
# nuget push NsqSharp.x.y.z.nupkg

### Build Tests

msbuild NsqSharp.Tests/NsqSharp.Tests.csproj /p:TargetFrameworkVersion=v4.6.2 /p:Configuration="Integration Tests" /t:Clean /t:Rebuild /p:OutputPath="../nuget-tests/net462"
if ($LastExitCode -ne 0) {
    echo ".NET 4.6.2 Tests Build failed. Process exited with error code $LastExitCode."
    Return
}

msbuild NsqSharp.Tests/NsqSharp.Tests.csproj /p:TargetFrameworkVersion=v4.6.1 /p:Configuration="Integration Tests" /t:Clean /t:Rebuild /p:OutputPath="../nuget-tests/net461"
if ($LastExitCode -ne 0) {
    echo ".NET 4.6.1 Tests Build failed. Process exited with error code $LastExitCode."
    Return
}

### Run Tests

.\packages\NUnit.Runners.2.6.4\tools\nunit-console.exe ./nuget-tests/net462/NsqSharp.Tests.dll
if ($LastExitCode -ne 0) {
    echo ".NET 4.6.2 Tests failed. Process exited with error code $LastExitCode."
    Return
}
echo "*** .NET 4.6.2 All tests passed."

.\packages\NUnit.Runners.2.6.4\tools\nunit-console.exe ./nuget-tests/net461/NsqSharp.Tests.dll
if ($LastExitCode -ne 0) {
    echo ".NET 4.6.1 Tests failed. Process exited with error code $LastExitCode."
    Return
}
echo "*** .NET 4.6.1 All tests passed."

### Build Nuget DLL's

msbuild NsqSharp/NsqSharp.csproj /p:TargetFrameworkVersion=v4.6.2 /p:Configuration="Integration Tests" /t:Clean /t:Rebuild /p:OutputPath="../nuget/lib/net462"
if ($LastExitCode -ne 0) {
    echo ".NET 4.6.2 Build failed. Process exited with error code $LastExitCode."
    Return
}

msbuild NsqSharp/NsqSharp.csproj /p:TargetFrameworkVersion=v4.6.1 /p:Configuration="Integration Tests" /t:Clean /t:Rebuild /p:OutputPath="../nuget/lib/net461"
if ($LastExitCode -ne 0) {
    echo ".NET 4.6.1 Build failed. Process exited with error code $LastExitCode."
    Return
}

### Nuget Pack

nuget pack nuget/NsqSharp.nuspec
if ($LastExitCode -ne 0) {
    echo "Nuget pack failed. Process exited with error code $LastExitCode."
    Return
}
