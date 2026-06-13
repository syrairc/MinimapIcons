// ============================================================================
//  ArtAtlas.cs  -  Mapping for Icons_Art.png  (companion to SpriteIcon.cs)
// ----------------------------------------------------------------------------
//  Atlas : 2048 x 2944 px, 32-bit RGBA, transparent background.
//  Grid  : 16 columns x 23 rows = 368 cells, 356 used (row-major, 0-based).
//  Cell  : 128 x 128 px.
//
//  These are the DETAILED painted icons from the source sheet, extracted,
//  de-duplicated (one copy per unique art, colour variants collapsed),
//  desaturated to greyscale, and upscaled ~2x. Tint at runtime with a
//  multiply blend, exactly like the geometric set.
//
//  Because the art has no clean semantic names, icons are addressed by index.
//  Use the Icon Atlas.html preview as the visual index map (each cell shows
//  its number). Wrap the indices you actually use in your own named consts:
//
//      const int IconWolfHead = 268;   // <- from the preview
//      var uv = ArtAtlas.GetUV(IconWolfHead);
//      ImGui.Image(texId, new Vector2(32,32), uv.Uv0, uv.Uv1, tintRGBA);
// ============================================================================

using System.Numerics;

namespace MinimapIcons.IconsBuilder
{
    /// <summary>Indexed UV helper for the desaturated detailed-art atlas.</summary>
    public static class ArtAtlas
    {
        public const int AtlasWidth  = 2048;
        public const int AtlasHeight = 2944;
        public const int CellSize    = 128;
        public const int Columns     = 16;
        public const int Rows        = 23;
        public const int Count       = 356;   // valid indices: 0 .. 355

        public const string FileName = "Icons_Art.png";

        public static (int X, int Y) GetCell(int index)
            => ((index % Columns) * CellSize, (index / Columns) * CellSize);

        public static (int X, int Y, int W, int H) GetSourceRect(int index)
        {
            var (x, y) = GetCell(index);
            return (x, y, CellSize, CellSize);
        }

        public static (Vector2 Uv0, Vector2 Uv1) GetUVPair(int index)
        {
            var (x, y) = GetCell(index);
            var uv0 = new Vector2((float)x / AtlasWidth,  (float)y / AtlasHeight);
            var uv1 = new Vector2((float)(x + CellSize) / AtlasWidth,
                                  (float)(y + CellSize) / AtlasHeight);
            return (uv0, uv1);
        }

        public static UvRect GetUV(int index)
        {
            var (uv0, uv1) = GetUVPair(index);
            return new UvRect { Uv0 = uv0, Uv1 = uv1 };
        }
    }
}
