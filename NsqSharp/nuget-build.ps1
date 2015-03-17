# Set-ExecutionPolicy -Scope CurrentUser Unrestricted

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe NsqSharp.csproj /p:Configuration=Release /p:OutputPath="nuget/lib/net45"
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe NsqSharp.Net40.csproj /p:Configuration=Release /p:OutputPath="nuget/lib/net40"
nuget pack nuget/NsqSharp.nuspec
