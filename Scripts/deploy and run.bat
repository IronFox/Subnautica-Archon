set "installPath=G:\SteamLibrary\steamapps\common\Subnautica"
set "buildPath=..\..\BuildTarget"

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

rem "%installPath%\Subnautica.exe"