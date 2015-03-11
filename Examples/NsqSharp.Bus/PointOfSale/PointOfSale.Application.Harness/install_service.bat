if not exist "PointOfSale.Application.Harness.exe" (
  echo PointOfSale.Application.Harness.exe does not exist in this directory
  pause
)

if exist "PointOfSale.Application.Harness.exe" (
  sc stop nsqsharp-example
  sc delete nsqsharp-example

  mkdir c:\nsqsharp-example

  copy /y . c:\nsqsharp-example
  sc create nsqsharp-example binpath= "c:\nsqsharp-example\PointOfSale.Application.Harness.exe" start= auto DisplayName= "nsqsharp-example"
  sc description nsqsharp-example "NsqSharp Example - PointOfSale.Application.Harness"
  sc start nsqsharp-example
)
