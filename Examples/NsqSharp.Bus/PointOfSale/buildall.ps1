# Set-ExecutionPolicy -Scope CurrentUser Unrestricted

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe PointOfSale.Handlers.Audit\PointOfSale.Handlers.Audit.csproj /p:Configuration=Release /p:OutputPath="../build/PointOfSale.Handlers.Audit"

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe PointOfSale.Handlers.CustomerHandlers\PointOfSale.Handlers.CustomerHandlers.csproj /p:Configuration=Release /p:OutputPath="../build/PointOfSale.Handlers.CustomerHandlers"

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe PointOfSale.Handlers.InvoiceHandlers\PointOfSale.Handlers.InvoiceHandlers.csproj /p:Configuration=Release /p:OutputPath="../build/PointOfSale.Handlers.InvoiceHandlers"

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe PointOfSale.Handlers.ProductHandlers\PointOfSale.Handlers.ProductHandlers.csproj /p:Configuration=Release /p:OutputPath="../build/PointOfSale.Handlers.ProductHandlers"
