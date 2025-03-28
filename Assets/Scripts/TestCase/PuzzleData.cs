using OdinSerializer;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PuzzleData
{
    [OdinSerialize] public List<TestCase> testCases;
}