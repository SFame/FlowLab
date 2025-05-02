using UnityEngine;

public class DebugObject : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float m_SimulationSpeed;
    [SerializeField] private ConnectionAwait m_ConnectionAwait;

    private void Update()
    {
        TPConnection.AwaitType = m_ConnectionAwait;
        TPConnection.WaitTime = m_SimulationSpeed;
    }
}
