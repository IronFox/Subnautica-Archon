using AVS.Util;

namespace Subnautica_Archon.Components
{
    public class ArchonArButton : HandTarget, IHandTarget
    {
        internal ArButton? arButton;

        public void OnHandClick(GUIHand hand)
        {
            arButton.SafeDo(x => x.OnTrigger());
        }

        public void OnHandHover(GUIHand hand)
        {
            if (arButton != null)
            {
                arButton.OnHandOver();
                HandReticle.main.SetText(HandReticle.TextType.Hand, Language.main.GetFormat($"AR.{arButton.function}", arButton.parameter + 1), translate: false, GameInput.Button.LeftHand);
                HandReticle.main.SetText(HandReticle.TextType.HandSubscript, string.Empty, translate: false);
                if (arButton.IsEnabled)
                {
                    HandReticle.main.SetIcon(HandReticle.IconType.Hand);
                }
            }

        }
    }
}
