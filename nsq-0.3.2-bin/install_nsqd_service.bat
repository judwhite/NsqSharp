mkdir c:\nsq\data
copy /y nsqd.exe c:\nsq
sc create nsqd binpath= "c:\nsq\nsqd.exe -mem-queue-size=0 -lookupd-tcp-address=127.0.0.1:4160 -data-path=c:\nsq\data" start= auto DisplayName= "nsqd 0.3.2"
sc description nsqd "nsqd 0.3.2"
sc start nsqd
