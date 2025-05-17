using UnityEngine;

public class DestroyTarget : MonoBehaviour, IDestroyTarget
{
    [SerializeField] private GameObject m_TargetObject;

    public void Destroy(object sender)
    {
        Object.Destroy(m_TargetObject);
    }
}