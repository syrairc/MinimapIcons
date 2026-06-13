// ============================================================================
//  SpriteIcon.cs  -  Mapping for Icons_Geo_Grey.png
// ----------------------------------------------------------------------------
//  Atlas : 1024 x 512 px, 32-bit RGBA, transparent background.
//  Grid  : 8 columns x 4 rows = 32 cells, each 128 x 128 px.
//  Rendered at 2x and downscaled, so edges stay crisp when scaled in ImGui.
//
//  Icons are GREYSCALE with a baked glossy 3D bevel (orb/gem look). Tint at
//  runtime with a multiply blend: the white highlights take your colour while
//  the dark shading and rim are preserved -> believable coloured icon.
//
//  Tinting recipe (ImGui.NET):
//      var uv = SpriteAtlas.GetUV(SpriteIcon.Hexagon);
//      ImGui.Image(textureId, new Vector2(24, 24), uv.Uv0, uv.Uv1, tintRGBA);
//
//  Draw-list:
//      var (uv0, uv1) = SpriteAtlas.GetUVPair(SpriteIcon.Star5);
//      drawList.AddImage(textureId, pMin, pMax, uv0, uv1,
//                        ImGui.ColorConvertFloat4ToU32(tint));
// ============================================================================

using System.Numerics;

namespace MinimapIcons.IconsBuilder
{
    /// <summary>Index of each icon in Icons_Geo_Grey.png (row-major, 0-based).</summary>
    public enum SpriteIcon
    {
        // ---- Row 0 : filled solid shapes ----
        Circle                 = 0,
        Square                 = 1,
        Hexagon                = 2,   // flat-top
        Pentagon               = 3,
        Triangle               = 4,   // point up
        Diamond                = 5,   // rhombus
        DiamondCluster         = 6,   // 4 small diamonds
        Star4                  = 7,   // plump 4-point sparkle

        // ---- Row 1 : filled stars / specials ----
        Star4Thin              = 8,   // thin 4-point sparkle
        Star5                  = 9,
        Star6                  = 10,
        Crescent               = 11,
        Teardrop               = 12,  // point up, round bottom
        GemDown                = 13,  // rounded top, point down
        Lozenge                = 14,  // tall sharp diamond
        Shield                 = 15,

        // ---- Row 2 : outline shapes ----
        CircleOutline          = 16,
        SquareOutline          = 17,
        HexagonOutline         = 18,
        PentagonOutline        = 19,
        TriangleOutline        = 20,
        DiamondOutline         = 21,
        DiamondClusterOutline  = 22,
        Star4Outline           = 23,

        // ---- Row 3 : outline stars / specials ----
        Star5Outline           = 24,
        Star6Outline           = 25,
        CrescentOutline        = 26,
        TeardropOutline        = 27,
        GemDownOutline         = 28,
        LozengeOutline         = 29,
        ShieldOutline          = 30,
        RingThin               = 31,  // thin outline circle
    }

    /// <summary>UV / source-rect helpers for the greyscale geometric atlas.</summary>
    public static class SpriteAtlas
    {
        public const int AtlasWidth  = 1024;
        public const int AtlasHeight = 512;
        public const int CellSize    = 128;
        public const int Columns     = 8;
        public const int Rows        = 4;
        public const int Count       = Columns * Rows; // 32

        public const string FileName = "Icons_Geo_Grey.png";

        /// <summary>Top-left pixel of the icon's cell.</summary>
        public static (int X, int Y) GetCell(SpriteIcon icon)
        {
            int i = (int)icon;
            return ((i % Columns) * CellSize, (i / Columns) * CellSize);
        }

        /// <summary>Pixel source rectangle (x, y, w, h) inside the atlas.</summary>
        public static (int X, int Y, int W, int H) GetSourceRect(SpriteIcon icon)
        {
            var (x, y) = GetCell(icon);
            return (x, y, CellSize, CellSize);
        }

        /// <summary>Normalised UVs as (Uv0, Uv1) corner pair for ImGui.</summary>
        public static (Vector2 Uv0, Vector2 Uv1) GetUVPair(SpriteIcon icon)
        {
            var (x, y) = GetCell(icon);
            var uv0 = new Vector2((float)x / AtlasWidth,  (float)y / AtlasHeight);
            var uv1 = new Vector2((float)(x + CellSize) / AtlasWidth,
                                  (float)(y + CellSize) / AtlasHeight);
            return (uv0, uv1);
        }

        /// <summary>Convenience struct accessor for GetUVPair.</summary>
        public static UvRect GetUV(SpriteIcon icon)
        {
            var (uv0, uv1) = GetUVPair(icon);
            return new UvRect { Uv0 = uv0, Uv1 = uv1 };
        }
    }

    public struct UvRect
    {
        public Vector2 Uv0;
        public Vector2 Uv1;
    }
}
