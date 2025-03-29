using NUnit.Framework;
using OdinSerializer;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct TestCase
{
    [OdinSerialize] public List<bool> ExternalInputStates;
    [OdinSerialize] public List<bool> ExternalOutputStates;
}