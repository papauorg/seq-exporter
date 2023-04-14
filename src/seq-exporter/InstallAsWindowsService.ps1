$Command = 'sc.exe create "{0}" binpath= "{1}" displayname= "{2}" start= "delayed-auto"' -f "SeqExporter","$(pwd)/seq_exporter.exe","Seq query metrics exporter"
Invoke-Expression $Command