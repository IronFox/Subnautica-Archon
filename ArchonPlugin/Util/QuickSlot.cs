namespace Subnautica_Archon.Util
{
    public readonly struct QuickSlot
    {
        public int Index { get; }
        public string ID { get; }

        public QuickSlot(int index, string iD)
        {
            Index = index;
            ID = iD;
        }

        public override string ToString()
            => $"#{Index}/'{ID}'";
    }
}