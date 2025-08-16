using AVS.Assets;
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
        private static Sprite Load(string filename)
        {
            Log.Write($"Loading sprite from {filename}");
            var rs = SpriteHelper.GetSpriteRaw(filename);
            if (rs == null)
                throw new System.IO.FileNotFoundException($"Sprite file not found: {filename}");
            return rs;
        }

        public StaticImages() : this(
            DepthModule1Icon: Load("images/depth_module_1.png"),
            DepthModule2Icon: Load("images/depth_module_2.png"),
            DepthModule3Icon: Load("images/depth_module_3.png"),
            DepthModuleNodeIcon: Load("images/depth_module_node.png"),
            FabricatorIcon: Load("images/fabricator.png"),
            ModulesBackground: Load("images/archon_module_background.png"),
            ArchonCraftingSprite: Load("images/archon_crafting_sprite.png"),
            ArchonPingSprite: Load("images/archon_ping_sprite.png")
            )
        {
        }
    }
}