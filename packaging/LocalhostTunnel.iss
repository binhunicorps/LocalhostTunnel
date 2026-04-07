[Setup]
AppName=Localhost Tunnel
AppVersion=1.0.15
DefaultDirName={autopf}\LocalhostTunnel
DefaultGroupName=Localhost Tunnel
OutputDir=..\artifacts\installer
OutputBaseFilename=LocalhostTunnel-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Localhost Tunnel"; Filename: "{app}\LocalhostTunnel.Desktop.exe"
Name: "{autodesktop}\Localhost Tunnel"; Filename: "{app}\LocalhostTunnel.Desktop.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\LocalhostTunnel.Desktop.exe"; Description: "Launch Localhost Tunnel"; Flags: nowait postinstall skipifsilent
