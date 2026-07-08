; 视觉运控通讯协议测试工具 - Inno Setup 安装脚本

#define MyAppName "视觉运控通讯协议测试工具"
#define MyAppVersion "2.0"
#define MyAppPublisher "鼎茂"
#define MyAppExeName "通讯协议测试.exe"
#define MyAppSourceDir "bin\Release\net10.0-windows\win-x64\publish"

[Setup]
; 应用程序基本信息
AppId={{8F5A6B2C-3D1E-4F9A-B8C7-2E4D6A9F1B3C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; 输出设置
OutputDir=installer
OutputBaseFilename=视觉运控通讯协议测试工具_v{#MyAppVersion}_Setup
; 压缩设置
Compression=lzma2/ultra64
SolidCompression=yes
; Windows 版本要求
MinVersion=10.0
; 架构
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; UI设置
WizardStyle=modern
; 权限
PrivilegesRequired=admin
; 许可协议（可选）
; LicenseFile=LICENSE.txt
; 安装完成后的设置
DisableFinishedPage=no
DisableReadyPage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加图标:"; Flags: unchecked

[Files]
; 发布的所有文件
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; 注意: 不要在任何共享的系统文件上使用 "Flags: ignoreversion"

[Icons]
; 开始菜单图标
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
; 桌面图标（如果用户选择）
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; 安装完成后运行程序（可选）
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; 卸载时删除配置文件
Type: files; Name: "{app}\PortConfig.json"
