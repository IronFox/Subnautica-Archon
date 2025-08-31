using AVS.Assets;
using AVS.Log;
using AVS.Util;
using Subnautica_Archon.Util;
using UnityEngine;

namespace Subnautica_Archon
{
    public record StaticImages(Sprite DepthModule1Icon,
        Sprite DepthModule2Icon,
        Sprite DepthModule3Icon,
        Sprite DepthModuleNodeIcon,
        Sprite FabricatorIcon,
        Sprite ModulesBackground,
        Sprite ArchonCraftingSprite,
        Sprite ArchonPingSprite) : PatcherImages(
            DepthModule1Icon: DepthModule1Icon,
            DepthModule2Icon: DepthModule2Icon,
            DepthModule3Icon: DepthModule3Icon,
            ModulesBackground: ModulesBackground,
            FabricatorIcon: FabricatorIcon,
            DepthModuleNodeIcon: DepthModuleNodeIcon)
    {
        private static Sprite Load(ArchonModController amc, string filename)
        {
            using var log = SmartLog.For(amc);
            log.Write($"Loading sprite from {filename}");
            var rs = SpriteHelper.GetSpriteRaw(amc, filename);
            if (rs.IsNull())
                throw new System.IO.FileNotFoundException($"Sprite file not found: {filename}");
            return rs;
        }

        public StaticImages(ArchonModController amc) : this(
            DepthModule1Icon: Load(amc, "images/depth_module_1.png"),
            DepthModule2Icon: Load(amc, "images/depth_module_2.png"),
            DepthModule3Icon: Load(amc, "images/depth_module_3.png"),
            DepthModuleNodeIcon: Load(amc, "images/depth_module_node.png"),
            FabricatorIcon: Load(amc, "images/fabricator.png"),
            ModulesBackground: Load(amc, "images/archon_module_background.png"),
            ArchonCraftingSprite: Load(amc, "images/archon_crafting_sprite.png"),
            ArchonPingSprite: Load(amc, "images/archon_ping_sprite.png")
            )
        {
        }
    }
}