namespace PreLuna.EnemyHealthBar.DamageNumbers.Core
{
    public readonly struct DamageNumberStyle
    {
        public DamageNumberStyle(float sizeFactor, float r, float g, float b, float alpha, bool bold)
        {
            SizeFactor = sizeFactor;
            R = r;
            G = g;
            B = b;
            Alpha = alpha;
            Bold = bold;
        }

        public float SizeFactor { get; }
        public float R { get; }
        public float G { get; }
        public float B { get; }
        public float Alpha { get; }
        public bool Bold { get; }

        // Division 2-inspired palette adapted to GTFO. The kill underline is a
        // separate accent; the number itself keeps its hit-type color.
        public static DamageNumberStyle For(DamageNumberKind kind) => kind switch
        {
            DamageNumberKind.Weakspot => new DamageNumberStyle(1.22f, .910f, .639f, .239f, 1f, true),
            DamageNumberKind.Armor => new DamageNumberStyle(.85f, .310f, .722f, .910f, .85f, false),
            DamageNumberKind.Immune => new DamageNumberStyle(.92f, .588f, .980f, .910f, 1f, false),
            _ => new DamageNumberStyle(1f, .949f, .941f, .918f, 1f, false)
        };

        public static readonly DamageNumberStyle KillAccent = new(1f, .878f, .322f, .322f, 1f, false);
    }
}
