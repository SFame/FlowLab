using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using OdinSerializer;
using System.Text;
using System;
using System.Linq;

[Serializable]
public class Room : MonoBehaviour
{
    [SerializeField]private string roomName;
    [SerializeField]private List<GameObject> roomObjects;

    public RoomData GetRoomData()
    {
        return new RoomData(roomName, roomObjects);
    }
    public void SetRoomData(RoomData roomData)
    {
        for (int i = 0; i < roomObjects.Count; i++)
        {
            roomObjects[i].GetComponent<ISaveLoad>().objectData = roomData.ObjectDatas[i];
        }
    }
}

[Serializable]
public class RoomData 
{ 
    [OdinSerialize][SerializeField]private string _name;
    [OdinSerialize][SerializeField]private List<ObjectData> _objectDatas;

    public String Name
    {
        get { return _name; }
        set { _name = value; }
    }
    public List<ObjectData> ObjectDatas
    {
        get { 
            _objectDatas ??= new List<ObjectData>();
            return _objectDatas; 
        }
        set { _objectDatas = value; }
    }

    public RoomData(string name, List<GameObject> roomObjects)
    {
        Name = name;
        for (int i = 0; i < roomObjects.Count; i++)
        {
            ObjectDatas.Add(roomObjects[i].GetComponent<ISaveLoad>().objectData);
        }
    }
}
