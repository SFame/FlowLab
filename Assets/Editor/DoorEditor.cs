using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Door))]
public class DoorEditor : Editor
{
    Door door;
    private void OnEnable()
    {
        door = (Door)target;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }

    private void OnSceneGUI()
    {
        door.exitPosition = Handles.PositionHandle(door.exitPosition, Quaternion.identity);
    }
}
