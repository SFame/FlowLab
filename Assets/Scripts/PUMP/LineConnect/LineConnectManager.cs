using System;
using UnityEngine;

public class LineConnectManager : MonoBehaviour
{
    #region Privates
    private PUMPBackground Background
    {
        get
        {
            if (_background is null)
                _background = GetComponentInParent<PUMPBackground>();
            
            return _background;
        }
    }
    private PUMPBackground _background;

    private Action _lineRefreshAction;
    #endregion
    
    #region Interface
    public LineConnector AddLineConnector()
    {
        GameObject lineGo = new GameObject("LineConnector");
        lineGo.transform.SetParent(transform);
        
        LineConnector lc = lineGo.AddComponent<LineConnector>();
        lc.OnDragEnd += ((IChangeObserver)Background).ReportChanges;
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
