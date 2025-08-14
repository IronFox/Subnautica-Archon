using AVS.Assets;
using Subnautica_Archon.Util;

namespace Subnautica_Archon
{
    public class StaticImages : PatcherImages
    {
        private static Image Load(string filename)
        {
            Log.Write($"Loading sprite from {filename}");
            return SpriteHelper.RequireImage(filename);
        }

        public StaticImages() : base(
            depthModule1Icon: Load("images/depth_module_1.png"),
            depthModule2Icon: Load("images/depth_module_2.png"),
            depthModule3Icon: Load("images/depth_module_3.png"),
            depthModuleNodeIcon: Load("images/depth_module_node.png"),
            fabricatorIcon: Load("images/fabricator.png"),
            modulesBackground: Load("images/archon_module_background.png")

            )
        {
            ArchonCraftingSprite = Load("images/archon_crafting_sprite.png");
            ArchonPingSprite = Load("images/archon_ping_sprite.png");
        }
        public Image ArchonCraftingSprite { get; }
        public Image ArchonPingSprite { get; }
    }
}