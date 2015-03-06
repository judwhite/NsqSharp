sc stop nsqd
sc stop nsqlookupd

sc delete nsqd
sc delete nsqlookupd

mkdir c:\nsq\data

copy /y nsqlookupd.exe c:\nsq
sc create nsqlookupd binpath= "c:\nsq\nsqlookupd.exe" start= auto DisplayName= "nsqlookupd"
sc description nsqlookupd "nsqlookupd 0.3.2"
sc start nsqlookupd

copy /y nsqd.exe c:\nsq
sc create nsqd binpath= "c:\nsq\nsqd.exe -mem-queue-size=0 -lookupd-tcp-address=127.0.0.1:4160 -data-path=c:\nsq\data" start= auto DisplayName= "nsqd"
sc description nsqd "nsqd 0.3.2"
sc start nsqd
