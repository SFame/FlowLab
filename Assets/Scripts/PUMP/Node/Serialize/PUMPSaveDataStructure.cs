using OdinSerializer;
using System.Collections.Generic;
using System;

public class PUMPSaveDataStructure
{
    public PUMPSaveDataStructure() { }

    public PUMPSaveDataStructure(List<SerializeNodeInfo> nodeInfos, string name, object tag = null)
    {
        NodeInfos = nodeInfos;
        Name = name;
        Tag = tag;
    }

    #region Serialize Data
    [OdinSerialize] public List<SerializeNodeInfo> NodeInfos { get; set; }
    [OdinSerialize] public string Name { get; set; }
    [OdinSerialize] public object Tag { get; set; } // Optional
    #endregion

    #region Automatic generation
    [OdinSerialize] public DateTime LastUpdate { get; set; }
    #endregion

    [field: NonSerialized] private event Action<PUMPSaveDataStructure> DeleteRequest;
    [field: NonSerialized] private event Action<PUMPSaveDataStructure> UpdateNotification;

    public void SubscribeDeleteRequest(Action<PUMPSaveDataStructure> action)
    {
        if (DeleteRequest != null)
        {
            foreach (Delegate d in DeleteRequest.GetInvocationList())
            {
                if (d.Equals(action))
                    return;
            }
        }
        DeleteRequest += action;
    }

    public void SubscribeUpdateNotification(Action<PUMPSaveDataStructure> action)
    {
        if (UpdateNotification != null)
        {
            foreach (Delegate d in UpdateNotification.GetInvocationList())
            {
                if (d.Equals(action))
                    return;
            }
        }
        UpdateNotification += action;
    }

    public void UnsubscribeDeleteRequest(Action<PUMPSaveDataStructure> action)
    {
        DeleteRequest -= action;
    }

    public void UnsubscribeUpdateNotification(Action<PUMPSaveDataStructure> action)
    {
        UpdateNotification -= action;
    }

    public void Paste(PUMPSaveDataStructure structure)
    {
        NodeInfos = structure.NodeInfos;
        Name = structure.Name;
        Tag = structure.Tag;
    }

    public void Delete() => DeleteRequest?.Invoke(this);
    public void NotifyDataChanged() => UpdateNotification?.Invoke(this);
}