set "installPath=%SUBNAUTICA_PATH%"
set "buildPath=..\..\BuildTarget"
set "avsPath=%AVS_PATH%"

rmdir /Q /S "%installPath%\BepInEx\plugins\Archon"
mkdir "%installPath%\BepInEx\plugins\Archon"

copy /Y "%buildPath%\Subnautica Archon_Data\Managed\ArchonScripts.dll" "%installPath%\BepInEx\plugins\Archon"
copy /Y "..\ArchonUnity\Assets\AssetBundles\Windows\archon" "%installPath%\BepInEx\plugins\Archon"
copy /Y "..\ArchonUnity\Assets\AssetBundles\OSX\archon" "%installPath%\BepInEx\plugins\Archon\archon.osx"
copy /Y "..\ArchonPlugin\bin\Release\net4.7.2\Subnautica Archon.dll" "%installPath%\BepInEx\plugins\Archon"
mkdir "%installPath%\BepInEx\plugins\Archon\images"
copy /Y "..\images\*.*" "%installPath%\BepInEx\plugins\Archon\images"
mkdir "%installPath%\BepInEx\plugins\Archon\Localization"
copy /Y "..\Localization\*.*" "%installPath%\BepInEx\plugins\Archon\Localization"
copy /Y "%avsPath%\AVS\bin\Release\net4.7.2\AVS.dll" "%installPath%\BepInEx\plugins\Archon"
copy /Y "%avsPath%\AVS\bin\Release\net4.7.2\Assembly-CSharp_publicized.dll" "%installPath%\BepInEx\plugins\Archon"

rem "%installPath%\Subnautica.exe"