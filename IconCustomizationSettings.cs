using System;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared.Helpers;
using ExileCore2.Shared.Nodes;
using ImGuiNET;
using MinimapIcons.IconsBuilder;
using MinimapIcons.IconsBuilder.Icons;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using RectangleF = ExileCore2.Shared.RectangleF;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace MinimapIcons;

/// <summary>Which sprite sheet an override icon comes from.</summary>
public enum IconSource
{
    GameIcons, // default ExileCore2 Icons.png, indexed by MapIconsIndex
    Geo,       // Icons_Geo_Grey.png — 32 geometric orb/gem icons (SpriteIcon)
    Art,       // Icons_Art.png — 356 detailed painted icons (by index)
}

/// <summary>
/// User-facing categories that can have their icon sprite, size, and tint overridden.
/// PoE2 set: monster rarities + minion, the standalone categories, and the 5 PoE2 chest subtypes.
/// Resolved from a built <see cref="BaseIcon"/> by <see cref="IconCustomizationSettings.ResolveIconType"/>
/// without touching the icon classes (uses public Rarity / ChestIcon.CType / Entity.IsHostile).
///
/// Adding a value here is non-breaking — <see cref="IconCustomizationSettings.EnsureAllKeys"/> seeds any
/// missing entry on load, so new rows appear automatically and old saves keep working.
/// </summary>
public enum IconType
{
    MonsterWhite,
    MonsterMagic,
    MonsterRare,
    MonsterUnique,
    Minion,
    Npc,
    Player,
    Shrine,
    MissionMarker,
    Misc,
    ChestBreach,
    ChestStrongbox,
    ChestExpedition,
    ChestSanctum,
    ChestSmall,
}

/// <summary>
/// Per-type override. Single toggle: when <see cref="Customize"/> is on, the icon's sprite, size, and
/// tint are fully replaced by this row at render time (see MinimapIcons.Render). The sprite is addressed
/// by (<see cref="Source"/>, <see cref="SpriteId"/>) so it can come from the game's Icons.png, the
/// geometric atlas, or the detailed-art atlas.
/// </summary>
public class IconTypeOverride
{
    public ToggleNode Customize { get; set; } = new ToggleNode(false);

    // When on, this type is always drawn by the plugin even if the entity has a native game minimap
    // icon (bypasses the native-icon deferral, and the replacer's "defer while in range" Show()).
    public ToggleNode AlwaysDraw { get; set; } = new ToggleNode(false);

    // When on, draws the entity's render name as the icon label (overriding whatever text the icon
    // class set, if any).
    public ToggleNode ShowName { get; set; } = new ToggleNode(false);

    [JsonConverter(typeof(StringEnumConverter))]
    public IconSource Source = IconSource.GameIcons;

    // Meaning depends on Source: GameIcons -> (int)MapIconsIndex; Geo -> SpriteIcon; Art -> art index.
    public int SpriteId = (int)MapIconsIndex.QuestObject;

    public RangeNode<float> Size { get; set; } = new RangeNode<float>(10, 1, 60);
    public ColorNode Tint { get; set; } = new ColorNode(Color.White);

    // Transient picker state. Not persisted.
    [JsonIgnore] public string Filter = "";
    [JsonIgnore] public int PickerTab = -1;

    public IconTypeOverride() { }

    public IconTypeOverride(MapIconsIndex icon, float size, Color tint)
    {
        Source = IconSource.GameIcons;
        SpriteId = (int)icon;
        Size = new RangeNode<float>(size, 1, 60);
        Tint = new ColorNode(tint);
    }

    /// <summary>Resolves the override to the texture file name and normalized UV rect to draw.</summary>
    public (string FileName, RectangleF Uv) ResolveSprite()
    {
        switch (Source)
        {
            case IconSource.Geo:
                return (SpriteAtlas.FileName, ToRect(SpriteAtlas.GetUVPair((SpriteIcon)SpriteId)));
            case IconSource.Art:
                return (ArtAtlas.FileName, ToRect(ArtAtlas.GetUVPair(ClampArt(SpriteId))));
            default:
                return ("Icons.png", SpriteHelper.GetUV((MapIconsIndex)SpriteId));
        }
    }

    private static int ClampArt(int i) => i < 0 ? 0 : i >= ArtAtlas.Count ? ArtAtlas.Count - 1 : i;

    private static RectangleF ToRect((Vector2 Uv0, Vector2 Uv1) p) =>
        new RectangleF(p.Uv0.X, p.Uv0.Y, p.Uv1.X - p.Uv0.X, p.Uv1.Y - p.Uv0.Y);
}

[Submenu(RenderMethod = nameof(Render))]
public class IconCustomizationSettings
{
    /// <summary>ImGui texture ids for the custom atlases, set by the plugin during Initialise.</summary>
    public static IntPtr GeoTextureId;
    public static IntPtr ArtTextureId;

    /// <summary>
    /// Persisted per-type overrides. Newtonsoft serializes the enum key as its name. Seeded in the
    /// constructor; <see cref="EnsureAllKeys"/> repairs partial/old saves.
    /// </summary>
    public Dictionary<IconType, IconTypeOverride> Overrides { get; set; } = BuildDefaults();

    public IconCustomizationSettings()
    {
        EnsureAllKeys();
    }

    /// <summary>
    /// Default seed per type — approximates the plugin's current hardcoded look so toggling Customize on
    /// is not a surprise. Defaults only take effect once a row's Customize is enabled.
    /// </summary>
    private static Dictionary<IconType, IconTypeOverride> BuildDefaults()
    {
        return new Dictionary<IconType, IconTypeOverride>
        {
            [IconType.MonsterWhite]    = new(MapIconsIndex.LootFilterLargeRedCircle, 10, Color.White),
            [IconType.MonsterMagic]    = new(MapIconsIndex.LootFilterLargeBlueCircle, 10, Color.White),
            [IconType.MonsterRare]     = new(MapIconsIndex.LootFilterLargeYellowCircle, 10, Color.White),
            [IconType.MonsterUnique]   = new(MapIconsIndex.LootFilterLargeWhiteHexagon, 10, Color.DarkOrange),
            [IconType.Minion]          = new(MapIconsIndex.LootFilterSmallGreenCircle, 10, Color.White),
            [IconType.Npc]             = new(MapIconsIndex.NPC, 10, Color.White),
            [IconType.Player]          = new(MapIconsIndex.OtherPlayer, 13, Color.White),
            [IconType.Shrine]          = new(MapIconsIndex.Shrine, 10, Color.White),
            [IconType.MissionMarker]   = new(MapIconsIndex.QuestObject, 10, Color.White),
            [IconType.Misc]            = new(MapIconsIndex.QuestObject, 10, Color.White),
            [IconType.ChestBreach]     = new(MapIconsIndex.RewardChestGeneric, 10, Color.White),
            [IconType.ChestStrongbox]  = new(MapIconsIndex.RewardChestGeneric, 10, Color.White),
            [IconType.ChestExpedition] = new(MapIconsIndex.ExpeditionChest2, 30, Color.White),
            [IconType.ChestSanctum]    = new(MapIconsIndex.HeistPathChest, 30, Color.White),
            [IconType.ChestSmall]      = new(MapIconsIndex.LootFilterSmallCyanSquare, 10, Color.White),
        };
    }

    /// <summary>
    /// Inserts a seeded default for any <see cref="IconType"/> missing from <see cref="Overrides"/>.
    /// Makes adding new IconTypes (and loading older saves) seamless — call from plugin Initialise.
    /// </summary>
    public void EnsureAllKeys()
    {
        Overrides ??= new Dictionary<IconType, IconTypeOverride>();
        foreach (var (type, def) in BuildDefaults())
        {
            if (!Overrides.TryGetValue(type, out var existing) || existing == null)
                Overrides[type] = def;
        }
    }

    /// <summary>
    /// Maps a built icon to its <see cref="IconType"/>, or null when it should not be overridden.
    /// <see cref="CustomIcon"/> is excluded (user controls it fully). The native-game-icon replacers
    /// are classified by their underlying entity (e.g. a shrine that became an
    /// <see cref="IngameIconReplacerIcon"/> still resolves to <see cref="IconType.Shrine"/>) so the
    /// per-type overrides — including Always Draw — can target them. No icon-class edits required.
    /// </summary>
    public static IconType? ResolveIconType(BaseIcon icon)
    {
        switch (icon)
        {
            case CustomIcon:
                return null;
            case IngameIconReplacerIcon:
            case IngameItemReplacerIcon:
                return ResolveByEntity(icon.Entity);
            case ChestIcon chest:
                return chest.CType switch
                {
                    ChestType.Breach => IconType.ChestBreach,
                    ChestType.Strongbox => IconType.ChestStrongbox,
                    ChestType.Expedition => IconType.ChestExpedition,
                    ChestType.Sanctum => IconType.ChestSanctum,
                    ChestType.SmallChest => IconType.ChestSmall,
                    _ => IconType.ChestSmall,
                };
            case MonsterIcon:
            case DeliriumIcon:
                return icon.Entity is { IsHostile: false } ? IconType.Minion : RarityToType(icon.Rarity);
            case NpcIcon:
                return IconType.Npc;
            case PlayerIcon:
                return IconType.Player;
            case ShrineIcon:
                return IconType.Shrine;
            case MissionMarkerIcon:
                return IconType.MissionMarker;
            case MiscIcon:
                return IconType.Misc;
            default:
                return null;
        }
    }

    private static IconType RarityToType(MonsterRarity rarity) => rarity switch
    {
        MonsterRarity.White => IconType.MonsterWhite,
        MonsterRarity.Magic => IconType.MonsterMagic,
        MonsterRarity.Rare => IconType.MonsterRare,
        MonsterRarity.Unique => IconType.MonsterUnique,
        _ => IconType.MonsterWhite,
    };

    /// <summary>
    /// Classifies a native-icon-replacer's underlying entity into an <see cref="IconType"/> so its
    /// per-type override applies. Covers the common replacer-caught categories; returns null otherwise
    /// (e.g. world items), which simply means "not customizable".
    /// </summary>
    private static IconType? ResolveByEntity(Entity e)
    {
        if (e == null) return null;
        if (e.HasComponent<Shrine>()) return IconType.Shrine;
        return e.Type switch
        {
            EntityType.Monster => e is { IsHostile: false } ? IconType.Minion : RarityToType(e.Rarity),
            EntityType.Npc => IconType.Npc,
            EntityType.Player => IconType.Player,
            EntityType.AreaTransition => IconType.Misc,
            _ => null,
        };
    }

    // ---- UI ----

    private static readonly MapIconsIndex[] AllGameIcons = Enum.GetValues<MapIconsIndex>();

    // Display order + labels, grouped. Group header (null Type) renders a SeparatorText row.
    private static readonly (IconType? Type, string Label)[] Rows =
    [
        (null, "Monsters"),
        (IconType.MonsterWhite, "Monster (White)"),
        (IconType.MonsterMagic, "Monster (Magic)"),
        (IconType.MonsterRare, "Monster (Rare)"),
        (IconType.MonsterUnique, "Monster (Unique)"),
        (IconType.Minion, "Minion"),
        (null, "Other"),
        (IconType.Npc, "NPC"),
        (IconType.Player, "Player"),
        (IconType.Shrine, "Shrine"),
        (IconType.MissionMarker, "Mission Marker"),
        (IconType.Misc, "Misc"),
        (null, "Chests"),
        (IconType.ChestBreach, "Chest: Breach"),
        (IconType.ChestStrongbox, "Chest: Strongbox"),
        (IconType.ChestExpedition, "Chest: Expedition"),
        (IconType.ChestSanctum, "Chest: Sanctum"),
        (IconType.ChestSmall, "Chest: Small"),
    ];

    public void Render()
    {
        EnsureAllKeys();

        ImGui.TextWrapped("'Always Draw' forces this type to be drawn by the plugin even when the game " +
                          "already has a native minimap icon for it (use this if e.g. shrines aren't " +
                          "showing). 'Show Name' labels the icon with the entity's name. 'Customize' " +
                          "fully replaces the type's icon, size, and tint — the " +
                          "picker offers three sources: Game (Icons.png), Geo (geometric), and Art " +
                          "(detailed); greyscale Geo/Art take your tint via multiply. Changes apply live. " +
                          "Note: a custom monster icon also replaces mod-alert (danger) sprites for that " +
                          "rarity.");
        ImGui.Separator();

        if (!ImGui.BeginTable("icon_customization", 7,
                ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            return;

        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 140);
        ImGui.TableSetupColumn("Always Draw", ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Show Name", ImGuiTableColumnFlags.WidthFixed, 75);
        ImGui.TableSetupColumn("Customize", ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, 140);
        ImGui.TableSetupColumn("Tint", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableHeadersRow();

        foreach (var (type, label) in Rows)
        {
            if (type == null)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.SeparatorText(label);
                continue;
            }

            if (!Overrides.TryGetValue(type.Value, out var o) || o == null)
                continue;

            var id = type.Value.ToString();
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);

            ImGui.TableSetColumnIndex(1);
            var alwaysDraw = o.AlwaysDraw.Value;
            if (ImGui.Checkbox($"##always_{id}", ref alwaysDraw))
                o.AlwaysDraw.Value = alwaysDraw;

            ImGui.TableSetColumnIndex(2);
            var showName = o.ShowName.Value;
            if (ImGui.Checkbox($"##name_{id}", ref showName))
                o.ShowName.Value = showName;

            ImGui.TableSetColumnIndex(3);
            var customize = o.Customize.Value;
            if (ImGui.Checkbox($"##cust_{id}", ref customize))
                o.Customize.Value = customize;

            ImGui.TableSetColumnIndex(4);
            IconPicker(id, o);

            ImGui.TableSetColumnIndex(5);
            var size = o.Size.Value;
            ImGui.SetNextItemWidth(130);
            if (ImGui.SliderFloat($"##size_{id}", ref size, 1f, 60f))
                o.Size.Value = size;

            ImGui.TableSetColumnIndex(6);
            var tint = o.Tint.Value.ToImguiVec4();
            if (ImGui.ColorEdit4($"##tint_{id}", ref tint,
                    ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoInputs))
                o.Tint.Value = Color.FromArgb(
                    (int)(tint.W * 255), (int)(tint.X * 255), (int)(tint.Y * 255), (int)(tint.Z * 255));
        }

        ImGui.EndTable();
    }

    // ---- Icon picker (3 sources) ----

    private static void IconPicker(string id, IconTypeOverride o)
    {
        var tint = o.Tint.Value.ToImguiVec4();
        var popupId = $"iconpick_{id}";

        if (SpriteButton($"##btn_{id}", o.Source, o.SpriteId, Vector2.One * 20, tint))
        {
            o.PickerTab = (int)o.Source;
            ImGui.OpenPopup(popupId);
        }

        if (!ImGui.BeginPopup(popupId))
            return;

        if (o.PickerTab < 0) o.PickerTab = (int)o.Source;

        if (ImGui.Button($"Game##{id}")) o.PickerTab = (int)IconSource.GameIcons;
        ImGui.SameLine();
        if (ImGui.Button($"Geo##{id}")) o.PickerTab = (int)IconSource.Geo;
        ImGui.SameLine();
        if (ImGui.Button($"Art##{id}")) o.PickerTab = (int)IconSource.Art;
        ImGui.Separator();

        switch ((IconSource)o.PickerTab)
        {
            case IconSource.GameIcons:
                GameIconGrid(id, o, tint);
                break;
            case IconSource.Geo:
                AtlasGrid(id, o, tint, IconSource.Geo, SpriteAtlas.Count);
                break;
            case IconSource.Art:
                AtlasGrid(id, o, tint, IconSource.Art, ArtAtlas.Count);
                break;
        }

        ImGui.EndPopup();
    }

    private static void GameIconGrid(string id, IconTypeOverride o, Vector4 tint)
    {
        ImGui.SetNextItemWidth(240);
        ImGui.InputText($"##filter_{id}", ref o.Filter, 64);
        ImGui.SameLine();
        ImGui.TextDisabled("filter");

        ImGui.BeginChild($"##grid_{id}", new Vector2(8 * 34 + 16, 300), ImGuiChildFlags.Border);
        var f = o.Filter ?? "";
        var col = 0;
        foreach (var candidate in AllGameIcons)
        {
            if (f.Length > 0 && candidate.ToString().IndexOf(f, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (ImGuiHelpers.IconButton($"##g_{id}_{(int)candidate}", Vector2.One * 28, candidate, tint))
            {
                o.Source = IconSource.GameIcons;
                o.SpriteId = (int)candidate;
                ImGui.CloseCurrentPopup();
            }

            if (++col % 8 != 0) ImGui.SameLine();
        }
        ImGui.EndChild();
    }

    private static void AtlasGrid(string id, IconTypeOverride o, Vector4 tint, IconSource src, int count)
    {
        ImGui.BeginChild($"##grid_{id}_{src}", new Vector2(8 * 40 + 16, 320), ImGuiChildFlags.Border);
        var col = 0;
        for (var i = 0; i < count; i++)
        {
            if (SpriteButton($"##a_{id}_{src}_{i}", src, i, Vector2.One * 32, tint))
            {
                o.Source = src;
                o.SpriteId = i;
                ImGui.CloseCurrentPopup();
            }

            if (++col % 8 != 0) ImGui.SameLine();
        }
        ImGui.EndChild();
    }

    /// <summary>Draws a clickable icon button for any source; returns true on click.</summary>
    private static bool SpriteButton(string btnId, IconSource src, int spriteId, Vector2 size, Vector4 tint)
    {
        switch (src)
        {
            case IconSource.Geo:
                if (GeoTextureId == IntPtr.Zero) return ImGui.Button($"{spriteId}{btnId}", size);
                var g = SpriteAtlas.GetUVPair((SpriteIcon)spriteId);
                return ImGui.ImageButton(btnId, GeoTextureId, size, g.Uv0, g.Uv1, Vector4.Zero, tint);
            case IconSource.Art:
                if (ArtTextureId == IntPtr.Zero) return ImGui.Button($"{spriteId}{btnId}", size);
                var a = ArtAtlas.GetUVPair(spriteId);
                return ImGui.ImageButton(btnId, ArtTextureId, size, a.Uv0, a.Uv1, Vector4.Zero, tint);
            default:
                return ImGuiHelpers.IconButton(btnId, size, (MapIconsIndex)spriteId, tint);
        }
    }
}
