using System;

namespace PreLuna.EnemyHealthBar.BFVFeedback.Core
{
    internal readonly struct BfvColor
    {
        internal BfvColor(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        internal byte R { get; }
        internal byte G { get; }
        internal byte B { get; }
        internal byte A { get; }

        internal static readonly BfvColor White = new(255, 255, 255, 255);
        internal static readonly BfvColor Weakspot = new(255, 140, 0, 255);
        internal static readonly BfvColor Armor = new(73, 173, 201, 255);
        internal static readonly BfvColor Kill = new(185, 40, 40, 255);

        internal static bool TryParse(string text, out BfvColor color)
        {
            color = White;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var value = text.Trim();
            if (value.Length > 0 && value[0] == '#') value = value.Substring(1);
            if (value.Length != 3 && value.Length != 6 && value.Length != 8) return false;

            if (value.Length == 3)
            {
                if (!TryNibble(value[0], out var r) || !TryNibble(value[1], out var g) ||
                    !TryNibble(value[2], out var b)) return false;
                color = new BfvColor((byte)(r * 17), (byte)(g * 17), (byte)(b * 17), 255);
                return true;
            }

            if (!TryByte(value, 0, out var red) || !TryByte(value, 2, out var green) ||
                !TryByte(value, 4, out var blue)) return false;
            var alpha = (byte)255;
            if (value.Length == 8 && !TryByte(value, 6, out alpha)) return false;
            color = new BfvColor(red, green, blue, alpha);
            return true;
        }

        internal static BfvColor ParseOrDefault(string text, BfvColor fallback)
            => TryParse(text, out var parsed) ? parsed : fallback;

        static bool TryByte(string text, int index, out byte value)
        {
            value = 0;
            if (index + 1 >= text.Length) return false;
            if (!TryNibble(text[index], out var high) || !TryNibble(text[index + 1], out var low))
                return false;
            value = (byte)((high << 4) | low);
            return true;
        }

        static bool TryNibble(char value, out int nibble)
        {
            if (value >= '0' && value <= '9') { nibble = value - '0'; return true; }
            if (value >= 'a' && value <= 'f') { nibble = value - 'a' + 10; return true; }
            if (value >= 'A' && value <= 'F') { nibble = value - 'A' + 10; return true; }
            nibble = 0;
            return false;
        }

        public override string ToString()
            => "#" + R.ToString("X2") + G.ToString("X2") + B.ToString("X2") + A.ToString("X2");
    }
}
