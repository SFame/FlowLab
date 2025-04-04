using System;
using UnityEngine;

public class LineConnectManager : MonoBehaviour
{
    #region On Inspector
    [SerializeField] private PUMPBackground m_Background;
    #endregion

    #region Privates
    private Action _lineRefreshAction;
    #endregion
    
    #region Interface
    public LineConnector AddLineConnector()
    {
        GameObject lineGo = new GameObject("LineConnector");
        lineGo.transform.SetParent(transform);
        
        LineConnector lc = lineGo.AddComponent<LineConnector>();
        //lc.LineRenderer.SetColor(Color.red);
        //lc.LineUpdated += lc.LineRenderer.SetAllDirty;
        lc.OnDragEnd += ((IChangeObserver)m_Background).ReportChanges;
        _lineRefreshAction += lc.RefreshPoints;
        lc.OnRemove += () => _lineRefreshAction -= lc.RefreshPoints;
        return lc;
    }

    /// <summary>
    /// 배경의 위치가 바뀌었을 때 호출
    /// </summary>
    public void LinesRefresh()
    {
        _lineRefreshAction?.Invoke();
    }
    #endregion
}
