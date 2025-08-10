set "installPath=%SUBNAUTICA_PATH%"
set "buildPath=..\..\BuildTarget"
set "avsPath=%AVS_PATH%"


del /Q Archon.zip
rmdir /Q /S .\Archon
mkdir .\Archon
mkdir .\Archon\images
mkdir .\Archon\Localization
copy /Y "%buildPath%\Subnautica Archon_Data\Managed\ArchonScripts.dll" .\Archon
copy /Y "..\ArchonUnity\Assets\AssetBundles\Windows\archon" .\Archon
copy /Y "..\ArchonUnity\Assets\AssetBundles\OSX\archon" .\Archon\archon.osx
copy /Y "..\ArchonPlugin\bin\Release\net4.7.2\Subnautica Archon.dll" .\Archon
copy /Y "..\images\*.*" .\Archon\images
copy /Y "..\Localization\*.*" ".\Archon\Localization"
copy /Y "%avsPath%\AVS\bin\Release\net4.7.2\AVS.dll" .\Archon


powershell Compress-Archive .\Archon archon.zip
