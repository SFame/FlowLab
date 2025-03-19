using OdinSerializer;
using System.Collections.Generic;
using System.Text;
using System;
using UnityEditor.Overlays;
using UnityEngine;
using Utils;
public class MapSaveLoad : MonoBehaviour
{
    private const string FILE_NAME = "PlayData.bin";

    //public void WriteData()
    //{
    //    _ = SaveData(FILE_NAME, _saveDatas);
    //}
}

[Serializable]
public struct PlayData
{
    [OdinSerialize] public Type NodeType { get; set; }
    [OdinSerialize] public Vector2 NodePosition { get; set; }
    [OdinSerialize][field: SerializeReference] public object NodeSerializableArgs { get; set; }
    [OdinSerialize] public TPConnectionIndexInfo[] InConnectionTargets { get; set; }
    [OdinSerialize] public TPConnectionIndexInfo[] OutConnectionTargets { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[SerializeNodeInfo]: {NodeType?.Name}");
        sb.AppendLine("{");
        sb.AppendLine($"    [NodeType]: {NodeType?.Name ?? "null"}\n");
        sb.AppendLine($"    [Position]: {NodePosition}\n");
        sb.AppendLine($"    [SerializableArgs]: {NodeSerializableArgs?.ToString() ?? "null"}\n");

        sb.AppendLine($"    [InConnections] ({InConnectionTargets?.Length ?? 0})");
        sb.AppendLine("    {");
        if (InConnectionTargets != null)
        {
            for (int i = 0; i < InConnectionTargets.Length; i++)
            {
                sb.AppendLine($"        [{i}]: {InConnectionTargets[i]?.ToString() ?? "null"}");
            }
        }
        sb.AppendLine("    }\n");

        sb.AppendLine($"    [OutConnections] ({OutConnectionTargets?.Length ?? 0})");
        sb.AppendLine("    {");
        if (OutConnectionTargets != null)
        {
            for (int i = 0; i < OutConnectionTargets.Length; i++)
            {
                sb.AppendLine($"        [{i}]: {OutConnectionTargets[i]?.ToString() ?? "null"}");
            }
        }
        sb.AppendLine("    }");
        sb.Append("}");

        return sb.ToString();
    }
}