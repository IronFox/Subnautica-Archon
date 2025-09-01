namespace Assets.Behavior.Debugging
{
    public class DebugArButton : DebugHandTarget
    {

        public override void OnTrigger(ArchonControl archon, FpsTest player)
        {
            button.OnTrigger();
        }

        public override void OnHandOver(ArchonControl archon, FpsTest player)
        {
            button.OnHandOver();
        }

        private ArButton button;

        // Start is called before the first frame update
        void Start()
        {
            button = GetComponent<ArButton>();
        }

    }
}