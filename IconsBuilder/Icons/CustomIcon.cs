using ExileCore2.PoEMemory;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared;
using ExileCore2.Shared.Helpers;

namespace MinimapIcons.IconsBuilder.Icons;

public class CustomIcon : BaseIcon
{
    public CustomIcon(Entity entity, IconsBuilderSettings settings, CustomIconSettings customIconSettings)
        : base(entity)
    {
        Show = () => (!customIconSettings.OnlyShowAlive || entity.IsAlive) &&
                     (!customIconSettings.OnlyShowNotOpened || !entity.IsOpened);
        MainTexture = new HudTexture("Icons.png")
        {
            UV = SpriteHelper.GetUV(customIconSettings.Icon),
            Size = RemoteMemoryObject.TheGame.IngameState.IngameUi.Map.LargeMapZoom * RemoteMemoryObject.TheGame.IngameState.Camera.Height / 64 * customIconSettings.Size.Value,
            Color = customIconSettings.Tint,
        };
    }
}