using System.Collections.Generic;

public class SettingLinkInputManager : PUMPInputManager
{
    protected override void Initialize()
    {
        UpdateKeyMap(Setting.CurrentKeyMap);

        Setting.OnSettingUpdated += () =>
        {
            UpdateKeyMap(Setting.CurrentKeyMap);
        };
    }

    private void UpdateKeyMap(IEnumerable<BackgroundActionKeyMap> keyMaps)
    {
        KeyMap.Clear();
        KeyMap.AddRange(keyMaps);
    }
}