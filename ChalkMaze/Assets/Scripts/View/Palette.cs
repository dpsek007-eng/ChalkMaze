using UnityEngine;

namespace ChalkMaze
{
    public static class Palette
    {
        public static readonly Color Void     = Hex("0B0A0C");
        public static readonly Color Floor    = Hex("171520");
        public static readonly Color StoneLit = Hex("3A3542");
        public static readonly Color Chalk    = Hex("E8E3D6");
        public static readonly Color Ash      = Hex("6E6875");
        public static readonly Color Ember    = Hex("FF7A3D");
        public static readonly Color Fire     = Hex("F2A33C");
        public static readonly Color Moss     = Hex("4ADE9A");
        public static readonly Color Danger   = Hex("FF4D4D");

        public static Color Hex(string hex)
        {
            int r = System.Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = System.Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = System.Convert.ToInt32(hex.Substring(4, 2), 16);
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}
