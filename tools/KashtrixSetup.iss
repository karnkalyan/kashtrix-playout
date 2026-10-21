; =====================================================================
; Kashtrix Broadcast Playout Suite - Inno Setup 7 Script
; =====================================================================

#define MyAppName "Kashtrix Broadcast Playout Suite"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Kashtrix Broadcast Systems"
#define MyAppURL "https://kashtrix.com"
#define BuildBinDir GetEnv('LOCALAPPDATA') + "\KashtrixPlayout\BuildSessions\active\bin"
#define WorkspaceRoot "c:\Users\karnk\Downloads\Kashtrix-COMPLETE-PROJECT-FIX49H-FULL-BUILD-DEBUG-RECOVERY-FINAL\Kashtrix-FIX49H"
#define BrandingDir WorkspaceRoot + "\src\BroadcastPlayout.App\Branding"

[Setup]
AppId={{6B5A10A9-C59F-491C-9E2B-834415C29A64}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\Kashtrix\PlayoutSuite
DefaultGroupName=Kashtrix Broadcast
DisableProgramGroupPage=yes
OutputDir={#WorkspaceRoot}\installer-output
OutputBaseFilename=KashtrixPlayoutSuite_Setup_x64
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
PrivilegesRequired=admin

; --- DARK MODE & MODERN DIALOG STYLING ---
WizardStyle=modern dark includetitlebar

; --- CUSTOM LOGO & ICONS ---
SetupIconFile={#BrandingDir}\kashtrix-playout.ico
UninstallDisplayIcon={app}\kashtrix-playout.ico

; =====================================================================
; APPLICATION SELECTION (COMPONENTS)
; =====================================================================
[Types]
Name: "full";        Description: "Full Broadcast Playout Suite (All Applications & Engines)"
Name: "compact";     Description: "Core Playout Master Only"
Name: "custom";      Description: "Custom Application Selection"; Flags: iscustom

[Components]
Name: "core";        Description: "Kashtrix Playout Master Engine (Playout.app)"; Types: full compact custom; Flags: fixed
Name: "output";      Description: "Kashtrix.OutputEngine (SDI, NDI, RTMP, UDP, SRT, Virtual)"; Types: full custom
Name: "input";       Description: "Kashtrix.InputEngine (Capture, SDI, NDI, Feeds)"; Types: full custom
Name: "ha";          Description: "Kashtrix.HAController (1+1 Redundant Mirror Failover)"; Types: full custom
Name: "nrcs";        Description: "Kashtrix.NRCS (Newsroom Computer System & MOS)"; Types: full custom
Name: "apigateway";  Description: "Kashtrix.API Gateway (REST / WebSocket / Web MCR)"; Types: full custom
Name: "cgctrl";      Description: "Kashtrix.CGController (Real-Time Graphics Compositor)"; Types: full custom
Name: "cgeditor";    Description: "Kashtrix.CGEditor (CG Designer & Template Studio)"; Types: full custom
Name: "channelctrl"; Description: "Kashtrix.ChannelController (Transmission Supervision)"; Types: full custom
Name: "ingest";      Description: "Kashtrix.Ingest (Automated Multi-Channel Ingest Server)"; Types: full custom
Name: "mam";         Description: "Kashtrix.MAM (Media Asset Management Catalog)"; Types: full custom
Name: "multiview";   Description: "Kashtrix.Multiviewer (Multi-Window Wall & Audio Meters)"; Types: full custom
Name: "playlist";    Description: "Kashtrix.PlaylistEditor (Rundown Sequence Editor)"; Types: full custom
Name: "scheduler";   Description: "Kashtrix.PlaylistScheduler (24/7 Daypart Timetable)"; Types: full custom
Name: "prompter";    Description: "Kashtrix.Prompter (Studio News Teleprompter)"; Types: full custom
Name: "qc";          Description: "Kashtrix.QCController (Quality Control & Compliance)"; Types: full custom
Name: "filemgr";     Description: "Kashtrix.FileManager (Media File Preparation)"; Types: full custom
Name: "settings";    Description: "Kashtrix.Settings (Suite Configuration)"; Types: full custom
Name: "licmgr";      Description: "Kashtrix.LicenseManager (Central License Authority)"; Types: full custom

; =====================================================================
; DESKTOP SHORTCUT TASKS
; =====================================================================
[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "desktopicon\playout";     Description: "Playout.app (Kashtrix.Playout)";       Components: core
Name: "desktopicon\output";      Description: "Kashtrix.OutputEngine";               Components: output
Name: "desktopicon\input";       Description: "Kashtrix.InputEngine";                Components: input
Name: "desktopicon\ha";          Description: "Kashtrix.HAController";               Components: ha
Name: "desktopicon\nrcs";        Description: "Kashtrix.NRCS Newsroom";              Components: nrcs
Name: "desktopicon\apigateway";  Description: "Kashtrix.API Gateway & Web MCR";      Components: apigateway
Name: "desktopicon\cgctrl";      Description: "Kashtrix.CGController";               Components: cgctrl
Name: "desktopicon\cgeditor";    Description: "Kashtrix.CGEditor (CG Designer)";     Components: cgeditor
Name: "desktopicon\channelctrl"; Description: "Kashtrix.ChannelController";          Components: channelctrl
Name: "desktopicon\ingest";      Description: "Kashtrix.Ingest (Ingest Server)";     Components: ingest
Name: "desktopicon\mam";         Description: "Kashtrix.MAM (Media Asset Mgmt)";     Components: mam
Name: "desktopicon\multiview";   Description: "Kashtrix.Multiviewer";                Components: multiview
Name: "desktopicon\playlist";    Description: "Kashtrix.PlaylistEditor";             Components: playlist
Name: "desktopicon\scheduler";   Description: "Kashtrix.PlaylistScheduler";          Components: scheduler
Name: "desktopicon\prompter";    Description: "Kashtrix.Prompter (Teleprompter)";    Components: prompter
Name: "desktopicon\qc";          Description: "Kashtrix.QCController";               Components: qc
Name: "desktopicon\filemgr";     Description: "Kashtrix.FileManager";                Components: filemgr
Name: "desktopicon\settings";    Description: "Kashtrix.Settings";                   Components: settings
Name: "desktopicon\licmgr";      Description: "Kashtrix.LicenseManager";             Components: licmgr

; =====================================================================
; RELEASE BINARIES INSTALLATION
; =====================================================================
[Files]
; 1. Core Playout Master Runtime (Release)
Source: "{#BuildBinDir}\BroadcastPlayout.App\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: core

; 2. All Standalone Applications (Release)
Source: "{#BuildBinDir}\Kashtrix.OutputEngine\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\OutputEngine"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: output
Source: "{#BuildBinDir}\Kashtrix.InputEngine\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\InputEngine"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: input
Source: "{#BuildBinDir}\Kashtrix.HAController\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\HAController"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: ha
Source: "{#BuildBinDir}\Kashtrix.NRCS\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\NRCS"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: nrcs
Source: "{#BuildBinDir}\Kashtrix.ApiGateway\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\ApiGateway"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: apigateway
Source: "{#BuildBinDir}\Kashtrix.CGController\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\CGController"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: cgctrl
Source: "{#BuildBinDir}\Kashtrix.CGEditor\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\CGEditor"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: cgeditor
Source: "{#BuildBinDir}\Kashtrix.ChannelController\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\ChannelController"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: channelctrl
Source: "{#BuildBinDir}\Kashtrix.IngestServer\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\IngestServer"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: ingest
Source: "{#BuildBinDir}\Kashtrix.MAM\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\MAM"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: mam
Source: "{#BuildBinDir}\Kashtrix.Multiview\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\Multiview"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: multiview
Source: "{#BuildBinDir}\Kashtrix.PlaylistEditor\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\PlaylistEditor"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: playlist
Source: "{#BuildBinDir}\Kashtrix.Scheduler\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\Scheduler"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: scheduler
Source: "{#BuildBinDir}\Kashtrix.Prompter\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\Prompter"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: prompter
Source: "{#BuildBinDir}\Kashtrix.QCController\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\QCController"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: qc
Source: "{#BuildBinDir}\Kashtrix.FileManager\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\FileManager"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: filemgr
Source: "{#BuildBinDir}\Kashtrix.Settings\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\Settings"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: settings
Source: "{#BuildBinDir}\Kashtrix.LicenseManager\x64\Release\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}\LicenseManager"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: licmgr

; 3. Branding, Templates & Native Engine Assets
Source: "{#BrandingDir}\*"; DestDir: "{app}\Branding"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BrandingDir}\kashtrix-playout.ico"; DestDir: "{app}"; DestName: "kashtrix-playout.ico"; Flags: ignoreversion
Source: "{#WorkspaceRoot}\cg-demo\*"; DestDir: "{app}\cg-demo"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#WorkspaceRoot}\native\*"; DestDir: "{app}\native"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#WorkspaceRoot}\src\BroadcastPlayout.App\native\*"; DestDir: "{app}\native"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#WorkspaceRoot}\src\BroadcastPlayout.App\native\ffmpeg\*"; DestDir: "{app}\ffmpeg"; Flags: ignoreversion recursesubdirs createallsubdirs

; =====================================================================
; START MENU SHORTCUTS
; =====================================================================
[Icons]
Name: "{group}\Kashtrix Playout"; Filename: "{app}\Kashtrix.Playout.exe"; Components: core
Name: "{group}\Kashtrix Output Engine"; Filename: "{app}\OutputEngine\Kashtrix.OutputEngine.exe"; Components: output
Name: "{group}\Kashtrix Input Engine"; Filename: "{app}\InputEngine\Kashtrix.InputEngine.exe"; Components: input
Name: "{group}\Kashtrix HA Controller"; Filename: "{app}\HAController\Kashtrix.HAController.exe"; Components: ha
Name: "{group}\Kashtrix NRCS Newsroom"; Filename: "{app}\NRCS\Kashtrix.NRCS.exe"; Components: nrcs
Name: "{group}\Kashtrix API Gateway"; Filename: "{app}\ApiGateway\Kashtrix.ApiGateway.exe"; Components: apigateway
Name: "{group}\Kashtrix CG Controller"; Filename: "{app}\CGController\Kashtrix.CGController.exe"; Components: cgctrl
Name: "{group}\Kashtrix CG Designer"; Filename: "{app}\CGEditor\Kashtrix.CGEditor.exe"; Components: cgeditor
Name: "{group}\Kashtrix Channel Controller"; Filename: "{app}\ChannelController\Kashtrix.ChannelController.exe"; Components: channelctrl
Name: "{group}\Kashtrix Ingest Server"; Filename: "{app}\IngestServer\Kashtrix.IngestServer.exe"; Components: ingest
Name: "{group}\Kashtrix Media Asset Management"; Filename: "{app}\MAM\Kashtrix.MAM.exe"; Components: mam
Name: "{group}\Kashtrix Multiviewer"; Filename: "{app}\Multiview\Kashtrix.Multiview.exe"; Components: multiview
Name: "{group}\Kashtrix Playlist Editor"; Filename: "{app}\PlaylistEditor\Kashtrix.PlaylistEditor.exe"; Components: playlist
Name: "{group}\Kashtrix Broadcast Scheduler"; Filename: "{app}\Scheduler\Kashtrix.Scheduler.exe"; Components: scheduler
Name: "{group}\Kashtrix Teleprompter"; Filename: "{app}\Prompter\Kashtrix.Prompter.exe"; Components: prompter
Name: "{group}\Kashtrix QC Controller"; Filename: "{app}\QCController\Kashtrix.QCController.exe"; Components: qc
Name: "{group}\Kashtrix File Manager"; Filename: "{app}\FileManager\Kashtrix.FileManager.exe"; Components: filemgr
Name: "{group}\Kashtrix Settings"; Filename: "{app}\Settings\Kashtrix.Settings.exe"; Components: settings
Name: "{group}\Kashtrix License Manager"; Filename: "{app}\LicenseManager\Kashtrix.LicenseManager.exe"; Components: licmgr
Name: "{group}\Uninstall Kashtrix Suite"; Filename: "{uninstallexe}"

; =====================================================================
; DESKTOP SHORTCUTS
; =====================================================================
Name: "{autodesktop}\Kashtrix Playout"; Filename: "{app}\Kashtrix.Playout.exe"; Tasks: desktopicon\playout; Components: core
Name: "{autodesktop}\Kashtrix Output Engine"; Filename: "{app}\OutputEngine\Kashtrix.OutputEngine.exe"; Tasks: desktopicon\output; Components: output
Name: "{autodesktop}\Kashtrix Input Engine"; Filename: "{app}\InputEngine\Kashtrix.InputEngine.exe"; Tasks: desktopicon\input; Components: input
Name: "{autodesktop}\Kashtrix HA Controller"; Filename: "{app}\HAController\Kashtrix.HAController.exe"; Tasks: desktopicon\ha; Components: ha
Name: "{autodesktop}\Kashtrix NRCS Newsroom"; Filename: "{app}\NRCS\Kashtrix.NRCS.exe"; Tasks: desktopicon\nrcs; Components: nrcs
Name: "{autodesktop}\Kashtrix API Gateway"; Filename: "{app}\ApiGateway\Kashtrix.ApiGateway.exe"; Tasks: desktopicon\apigateway; Components: apigateway
Name: "{autodesktop}\Kashtrix CG Controller"; Filename: "{app}\CGController\Kashtrix.CGController.exe"; Tasks: desktopicon\cgctrl; Components: cgctrl
Name: "{autodesktop}\Kashtrix CG Designer"; Filename: "{app}\CGEditor\Kashtrix.CGEditor.exe"; Tasks: desktopicon\cgeditor; Components: cgeditor
Name: "{autodesktop}\Kashtrix Channel Controller"; Filename: "{app}\ChannelController\Kashtrix.ChannelController.exe"; Tasks: desktopicon\channelctrl; Components: channelctrl
Name: "{autodesktop}\Kashtrix Ingest Server"; Filename: "{app}\IngestServer\Kashtrix.IngestServer.exe"; Tasks: desktopicon\ingest; Components: ingest
Name: "{autodesktop}\Kashtrix MAM"; Filename: "{app}\MAM\Kashtrix.MAM.exe"; Tasks: desktopicon\mam; Components: mam
Name: "{autodesktop}\Kashtrix Multiviewer"; Filename: "{app}\Multiview\Kashtrix.Multiview.exe"; Tasks: desktopicon\multiview; Components: multiview
Name: "{autodesktop}\Kashtrix Playlist Editor"; Filename: "{app}\PlaylistEditor\Kashtrix.PlaylistEditor.exe"; Tasks: desktopicon\playlist; Components: playlist
Name: "{autodesktop}\Kashtrix Scheduler"; Filename: "{app}\Scheduler\Kashtrix.Scheduler.exe"; Tasks: desktopicon\scheduler; Components: scheduler
Name: "{autodesktop}\Kashtrix Teleprompter"; Filename: "{app}\Prompter\Kashtrix.Prompter.exe"; Tasks: desktopicon\prompter; Components: prompter
Name: "{autodesktop}\Kashtrix QC Controller"; Filename: "{app}\QCController\Kashtrix.QCController.exe"; Tasks: desktopicon\qc; Components: qc
Name: "{autodesktop}\Kashtrix File Manager"; Filename: "{app}\FileManager\Kashtrix.FileManager.exe"; Tasks: desktopicon\filemgr; Components: filemgr
Name: "{autodesktop}\Kashtrix Settings"; Filename: "{app}\Settings\Kashtrix.Settings.exe"; Tasks: desktopicon\settings; Components: settings
Name: "{autodesktop}\Kashtrix License Manager"; Filename: "{app}\LicenseManager\Kashtrix.LicenseManager.exe"; Tasks: desktopicon\licmgr; Components: licmgr

; =====================================================================
; POST-INSTALLATION RUN
; =====================================================================
[Run]
Filename: "{app}\Kashtrix.Playout.exe"; Description: "{cm:LaunchProgram,Kashtrix Playout}"; Flags: nowait postinstall skipifsilent; Components: core
