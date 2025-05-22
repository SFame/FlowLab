using TMPro;
using UnityEngine;

public class SwitchSupport : MonoBehaviour
{
    [SerializeField] private RectTransform m_ARect;
    [SerializeField] private RectTransform m_BRect;

    private GameObject _aObject;
    private GameObject _bObject;
    private bool _transB;

    private GameObject AObject
    {
        get
        {
            _aObject ??= m_ARect.gameObject;
            return _aObject;
        }
    }

    private GameObject BObject
    {
        get
        {
            _bObject ??= m_BRect.gameObject;
            return _bObject;
        }
    }

    public bool TransB
    {
        get => _transB;
        set
        {
            _transB = value;
            SetTransVisible(_transB);
        }
    }

    public void SetYPositions(float aY, float bY)
    {
        Vector3 aPos = m_ARect.localPosition;
        Vector3 bPos = m_BRect.localPosition;
        m_ARect.localPosition = new Vector3(aPos.x, aY, aPos.z);
        m_BRect.localPosition = new Vector3(bPos.x, bY, bPos.z);
    }

    private void SetTransVisible(bool isB)
    {
        if (isB)
        {
            AObject.SetActive(false);
            BObject.SetActive(true);
            return;
        }

        AObject.SetActive(true);
        BObject.SetActive(false);
    }
}