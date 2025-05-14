using System;
using System.Collections.Generic;
using System.Text;
using OdinSerializer;
using UnityEngine;

[Serializable]
public struct SerializeNodeInfo
{
    [OdinSerialize] public Type NodeType { get; set; }
    [OdinSerialize] public Vector2 NodePosition { get; set; }
    [OdinSerialize][field: SerializeReference] public object NodeAdditionalArgs { get; set; }
    [OdinSerialize] public Transition[] InTpState { get; set; }
    [OdinSerialize] public Transition[] OutTpState { get; set; }
    [OdinSerialize] public TransitionType[] InTpType { get; set; }
    [OdinSerialize] public TransitionType[] OutTpType { get; set; }
    [OdinSerialize] public bool[] StatePending { get; set; }
    [OdinSerialize] public TPConnectionIndexInfo[] InConnectionTargets { get; set; }
    [OdinSerialize] public TPConnectionIndexInfo[] OutConnectionTargets { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[SerializeNodeInfo]: {NodeType?.Name}");
        sb.AppendLine("{");
        sb.AppendLine($"    [NodeType]: {NodeType?.Name ?? "null"}\n");
        sb.AppendLine($"    [Position]: {NodePosition}\n");
        sb.AppendLine($"    [SerializableArgs]: {NodeAdditionalArgs?.ToString() ?? "null"}\n");

        sb.AppendLine($"    [InTpState] ({InTpState?.Length ?? 0})");
        sb.AppendLine("    {");
        if (InTpState != null)
        {
            for (int i = 0; i < InTpState.Length; i++)
            {
                sb.AppendLine($"        [{i}]: {InTpState[i]}");
            }
        }
        sb.AppendLine("    }\n");

        sb.AppendLine($"    [OutTpState] ({OutTpState?.Length ?? 0})");
        sb.AppendLine("    {");
        if (OutTpState != null)
        {
            for (int i = 0; i < OutTpState.Length; i++)
            {
                sb.AppendLine($"        [{i}]: {OutTpState[i]}");
            }
        }
        sb.AppendLine("    }\n");

        sb.AppendLine($"    [StatePending] ({StatePending?.Length ?? 0})");
        sb.AppendLine("    {");
        if (StatePending != null)
        {
            for (int i = 0; i < StatePending.Length; i++)
            {
                sb.AppendLine($"        [{i}]: {StatePending[i]}");
            }
        }
        sb.AppendLine("    }\n");

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

[Serializable]
public class TPConnectionIndexInfo
{
    [OdinSerialize] public int NodeIndex { get; set; } = -1;
    [OdinSerialize] public int TpIndex { get; set; } = -1;
    [OdinSerialize] public List<Vector2> Vertices { get; set; }
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[TPConnectionInfo]");
        sb.AppendLine("        {");
        sb.AppendLine($"            [NodeIndex]: {NodeIndex}");
        sb.AppendLine($"            [TpIndex]: {TpIndex}");
       
        if (Vertices != null)
        {
            sb.AppendLine($"            [Vertices] ({Vertices.Count})");
            sb.AppendLine("            {");
            foreach (var vertex in Vertices)
            {
                sb.AppendLine($"                {vertex}");
            }
            sb.AppendLine("            }");
        }
        else
        {
            sb.AppendLine("            [Vertices]: null");
        }
        sb.Append("        }");

        return sb.ToString();
    }
}