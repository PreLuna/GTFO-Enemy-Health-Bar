using System.Text;

namespace PreLuna.EnemyHealthBar.DamageNumbers.Core
{
    // IL2CPP-safe formatting: no format providers, no float.ToString(format).
    public static class DamageNumberFormat
    {
        public const string ImmuneText = "IMMUNE";

        public static int FromApplied(float applied)
        {
            if (!float.IsFinite(applied) || applied <= 0f) return 0;
            int rounded = (int)(applied + .5f);
            return rounded < 1 ? 1 : rounded;
        }

        public static string Number(int damage)
        {
            if (damage <= 0) return "0";
            string raw = damage.ToString();
            if (raw.Length <= 3) return raw;
            var text = new StringBuilder(raw.Length + raw.Length / 3);
            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && (raw.Length - i) % 3 == 0) text.Append(',');
                text.Append(raw[i]);
            }
            return text.ToString();
        }
    }
}
