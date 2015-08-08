if not exist "LogProcessCrash.exe" (
  echo LogProcessCrash.exe does not exist in this directory
  pause
)

if exist "LogProcessCrash.exe" (
  sc stop nsqsharp-crash-example
  sc delete nsqsharp-crash-example

  mkdir c:\nsqsharp-crash-example

  copy /y . c:\nsqsharp-crash-example
  sc create nsqsharp-crash-example binpath= "c:\nsqsharp-crash-example\LogProcessCrash.exe" start= auto DisplayName= "nsqsharp-crash-example"
  sc description nsqsharp-crash-example "NsqSharp Example - LogProcessCrash"
  sc start nsqsharp-crash-example
)
