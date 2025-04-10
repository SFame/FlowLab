using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Utils;

public class PUMPSeparator : MonoBehaviour
{
    #region On Inspector
    [SerializeField] private RectTransform m_BaseSector;
    [SerializeField] private RectTransform m_OverSector;
    [SerializeField] private GameObject m_PumpSectorPrefab;

    [Space(15)]

    [SerializeField] private bool m_OpenBackgroundOnStart;
    [SerializeField] private bool m_ShowWhenStart;
    [SerializeField] private bool m_ShowWhenGet;
    #endregion

    #region Privates
    private PUMPBackground _currentBackground;
    private HashSet<ISeparatorSectorable> _separatorSectorables = new();
    private bool _isVisible = false;
    private bool _isVisibleAnyChange = false;

    private PUMPBackground CurrentBackground
    {
        get
        {
            if (_currentBackground == null)
            {
                RemoveNullSeparatorSectorables();

                GameObject go = Instantiate(m_PumpSectorPrefab, m_BaseSector);
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.SetRectFull();
                _currentBackground = rect.GetComponentInChildren<PUMPBackground>();

                if (_currentBackground is ISeparatorSectorable settable)
                {
                    settable.SetSeparator(this);
                    settable.SetVisible(_isVisible);
                    _separatorSectorables.Add(settable);
                }
                else
                {
                    Debug.LogError($"{name}: Can't GetComponent PUMPBackground");
                    return null;
                }

                _currentBackground.RecordOnInitialize = true;
            }
            return _currentBackground;
        }
    }

    private void RemoveNullSeparatorSectorables()
    {
        _separatorSectorables.RemoveWhere(sector => sector.IsUnityNull());
    }

    private void Start()
    {
        if (!_isVisibleAnyChange)
        {
            _isVisible = m_ShowWhenStart;
        }

        if (m_OpenBackgroundOnStart)
        {
            PUMPBackground background = GetBackground();
            if (background != null)
            {
                background.Open();
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            PUMPBackground background = GetBackground();
            if (background != null)
            {
                background.Open();
            }
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            PUMPBackground background = GetBackground();
            if (background != null)
            {
                background.Close();
            }
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            SetVisible(true);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            SetVisible(false);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            PUMPBackground.Current.Destroy();
        }
    }
    #endregion
    Loading.Progress p;
    #region Interface
    /// <summary>
    /// PUMPBackground Get
    /// </summary>
    /// <returns></returns>
    public PUMPBackground GetBackground()
    {
        if (m_ShowWhenGet)
        {
            SetVisible(true);
        }

        return CurrentBackground;
    }

    public void SetVisible(bool visible)
    {
        _isVisibleAnyChange = true;
        _isVisible = visible;
        bool isNull = false;

        foreach (ISeparatorSectorable sectorable in _separatorSectorables)
        {
            if (sectorable.IsUnityNull())
            {
                isNull = true;
                continue;
            }
            sectorable.SetVisible(visible);
        }

        if (isNull)
        {
            RemoveNullSeparatorSectorables();
        }
    }

    public void SetOver(RectTransform rect, ISeparatorSectorable separatorSectorable = null)
    {
        rect.SetParent(m_OverSector);
        separatorSectorable?.SetSeparator(this);
        separatorSectorable.SetVisible(_isVisible);
        _separatorSectorables.Add(separatorSectorable);
    }

    public void SetOverFull(RectTransform rect, ISeparatorSectorable separatorSectorable = null)
    {
        rect.SetParent(m_OverSector);
        rect.SetRectFull();
        separatorSectorable?.SetSeparator(this);
        separatorSectorable.SetVisible(_isVisible);
        _separatorSectorables.Add(separatorSectorable);
    }

    public T GetComponentInOver<T>()
    {
        return m_OverSector.GetComponentInChildren<T>();
    }
    #endregion
}

public interface ISeparatorSectorable
{
    void SetSeparator(PUMPSeparator separator);
    PUMPSeparator GetSeparator();
    void SetVisible(bool visible);
}