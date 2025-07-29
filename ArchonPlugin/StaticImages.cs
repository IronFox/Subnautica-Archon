using AVS.Assets;
using AVS.Util;
using Subnautica_Archon.Util;
using System;
using System.IO;
using System.Reflection;

namespace Subnautica_Archon
{
    public class StaticImages : PatcherImages
    {
        private static Image Load(string filename)
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), filename);
            Log.Write($"Loading sprite from {path}");
            try
            {
                return new Image(SpriteHelper.GetSpriteRaw(path).OrThrow(() => new IOException($"File not found or could not be loaded: {path}")));
            }
            catch (Exception ex)
            {
                Log.Exception($"Error loading image from {path}", ex);
                throw;
            }
        }

        public StaticImages() : base(
            depthModule1Icon: Load("images/depth_module_1.png"),
            depthModule2Icon: Load("images/depth_module_2.png"),
            depthModule3Icon: Load("images/depth_module_3.png"),
            depthModuleNodeIcon: Load("images/depth_module_node.png"))
        {
            //ArchonCraftingSprite = Load("images/archon_crafting_sprite.png");
            //ArchonPingSprite = Load("images/archon_ping_sprite.png");
            ArchonModuleBackground = Load("images/archon_module_background.png");
        }
        public Image ArchonCraftingSprite { get; }
        public Image ArchonPingSprite { get; }
        public Image ArchonModuleBackground { get; }
    }
}