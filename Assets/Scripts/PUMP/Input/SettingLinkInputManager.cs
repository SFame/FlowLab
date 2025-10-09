using System.Collections.Generic;
using UnityEngine;

public class SettingLinkInputManager : PUMPInputManager
{
    protected override void Initialize()
    {
        UpdateKeyMap(Setting.CurrentKeyMap);

        Setting.OnSettingUpdated += () =>
        {
            UpdateKeyMap(Setting.CurrentKeyMap);
            Refresh();
        };
    }

    private void UpdateKeyMap(IEnumerable<BackgroundActionKeyMap> keyMaps)
    {
        KeyMap.Clear();
        KeyMap.AddRange(keyMaps);
    }
}