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

        lc.OnDragEnd += ((IChangeObserver)m_Background).ReportChanges;
        lc.OnEdgeRemoved += _ => ((IChangeObserver)m_Background).ReportChanges();
        lc.OnRemove += () => _lineRefreshAction -= lc.RefreshPoints;
        lc.OnEdgeAdded += edge => m_Background.JoinDraggable(edge);

        _lineRefreshAction += lc.RefreshPoints;
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