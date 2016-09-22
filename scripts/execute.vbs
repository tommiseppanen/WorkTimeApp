command = "powershell.exe -ExecutionPolicy ByPass -File C:\add-time.ps1 -WindowStyle Hidden"
set shell = CreateObject("WScript.Shell")
shell.Run command,0