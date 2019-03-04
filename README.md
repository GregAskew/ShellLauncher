# ShellLauncher
Launches an alternate shell application.
Users with the alternate shell are logged off when the application is closed.
Builtin Administrator account and Domain Admins are excluded from alternate shell and load Explorer.exe.

To use, copy ShellLauncher.exe and ShellLauncher.exe.config to %systemroot%, and set the following registry value:  

Key: HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon  
Value: Shell  
Value Type: REG_SZ  
Value Data: %systemroot%\ShellLauncher.exe  
