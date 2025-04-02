# Subnautica-Archon
A mod that adds the Archon submarine to the Subnautica game

## Requirements
- Tobey's BepInEx Pack for Subnautica (https://www.nexusmods.com/subnautica/mods/1108)
- Vehicle Framework (https://www.nexusmods.com/subnautica/mods/859)
- Unity Editor version 2019.4.36f
- Visual Studio 2019+ (2022 is current at the time of writing) Community+
- .NET 4.7.2 developer pack (https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472)

## Project Composition
The project is split in two: There is a unity project in the [clone]\ArchonUnity subdirectory. If should be opened with the correct Unity editor. The second directory, [clone]\ArchonPlugin, contains the actual plugin which is loaded by BepInEx.
While the Unity project should build (and run), the Plugin likely will not.
See dependencies below. Since the plugin references the DLL produced by Unity, you will need to build those at least once before fixing the dependency issues.

## Building via Unity
1) Build assets via Unity: Menu -> Assets -> Build AssetBundles
2) Build DLLs via Unity: Menu -> File -> Build Settings ... -> Build 
(Then pick a folder that is not in the clone directory.  This will be called [build] from here)

## Plugin/Subnautica Echelon Project Dependencies and Building
If you do not want to jump through all the hoops necessary to build this project, the compiled dll has now been added to the repo. So this step can be skipped if necessary.
Otherwise read on:

The plugin needs the following DLLs to be referenced in the ArchonPlugin project:
- [build]\Subnautica Archon_Data\Managed\ArchonScripts.dll
- [Subnautica]\BepInEx\core\0Harmony.dll
- [Subnautica]\BepInEx\core\BepInEx.dll
- [Subnautica]\BepInEx\plugins\Nautilus\Nautilus.dll
- [Subnautica]\BepInEx\plugins\VehicleFramework\VehicleFramework.dll
- [Subnautica]\Subnautica_Data\Managed\Assembly-CSharp.dll
- [Subnautica]\Subnautica_Data\Managed\Assembly-CSharp-firstpass.dll
- [Subnautica]\Subnautica_Data\Managed\FMODUnity.dll
- [Subnautica]\Subnautica_Data\Managed\UnityEngine.dll
- [Subnautica]\Subnautica_Data\Managed\UnityEngine.AssetBundleModule.dll
- [Subnautica]\Subnautica_Data\Managed\UnityEngine.AudioModule.dll
- [Subnautica]\Subnautica_Data\Managed\UnityEngine.CoreModule.dll
- [Subnautica]\Subnautica_Data\Managed\UnityEngine.InputLegacyModule.dll
- [Subnautica]\Subnautica_Data\Managed\UnityEngine.PhysicsModule.dll

Once set up, the project should build.
Compile the ArchonPlugin project for **release**. It cannot be run outside Subnautica. That should produce the DLL we need in [clone]\ArchonPlugin\bin\Release\net4.7.2\Subnautica Archon.dll

## Assembly
The target mod directory should be in [Subnautica]\BepInEx\plugins\Archon.
In order to run the mod, you need to copy the following files directly into that directory (no subdirectories):
1) [clone]\Unity\Assets\AssetBundles\OSX\archon -> (rename to) archon.osx
2) [clone]\Unity\Assets\AssetBundles\Windows\archon
3) [clone]\Plugin\bin\Release\net4.7.2\Subnautica Archon.dll
4) [build]\Subnautica Archon_Data\Managed\ArchonScripts.dll

Also copy these entire directories:
1) [clone]\images
2) [clone]\Localization

If you intend to frequently change things, you should probably adapt the scripts in [clone]\Scripts to your needs.

## Notes about the recipes
The current version of vehicle framework (1.7.0) does not register changes in the build recipe of the craft.
A workaround has been implemented by the plugin itself which deletes Archon recipes on launch if it recognizes them.
Otherwise, you may have to manually delete [Subnautica]\BepInEx\plugins\VehicleFramework\recipes\Archon_recipe.json before the next game start

## Start
Once copied, the game should pick up the mod automatically. To check if everything went fine, check 
[Subnautica]\BepInEx\LogOutput.log
It should contain the following messages:
- [Info   :VehicleFramework] The Archon is beginning Registration.
- [Info   :VehicleFramework] Finished Archon registration.

It should be possible to build the craft via a Mobile Vehicle Bay. Alternatively 'spawn archon' should also do the trick.


