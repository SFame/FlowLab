using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Utils;

public class UiMouseListener : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    #region  On Inspactor
    [SerializeField] private UnityEvent<PointerEventData> m_OnClick;
    [SerializeField] private UnityEvent<PointerEventData> m_OnDoubleClick;
    [SerializeField] private UnityEvent<PointerEventData> m_OnPointerEnter;
    [SerializeField] private UnityEvent<PointerEventData> m_OnPointerExit;
    
    [Space(10)]
    
    [SerializeField] private float m_DoubleClickInterval = 0.3f;
    #endregion

    #region Privates
    private const float DOUBLE_CLICK_ACCEPT_RANGE = 10f;
    private const float DOUBLE_CLICK_COOLDOWN = 0.5f;
    private CancellationTokenSource _cts = null;
    private Vector2? _lastClickPosition = null;
    private UniTask _lastClickTask = UniTask.CompletedTask;
    private UniTask _clickCooldownTask = UniTask.CompletedTask;
    #endregion
    
    #region Interface
    public event UnityAction<PointerEventData> OnClick
    {
        add
        {
            m_OnClick ??= new UnityEvent<PointerEventData>();
            m_OnClick.AddListener(value); 
        }
        remove => m_OnClick?.RemoveListener(value);
    }

    public event UnityAction<PointerEventData> OnDoubleClick
    {
        add 
        { 
            m_OnDoubleClick ??= new UnityEvent<PointerEventData>();
            m_OnDoubleClick.AddListener(value); 
        }
        remove => m_OnDoubleClick?.RemoveListener(value);
    }

    public event UnityAction<PointerEventData> OnEnter
    {
        add 
        { 
            m_OnPointerEnter ??= new UnityEvent<PointerEventData>();
            m_OnPointerEnter.AddListener(value); 
        }
        remove => m_OnPointerEnter?.RemoveListener(value);
    }

    public event UnityAction<PointerEventData> OnExit
    {
        add 
        { 
            m_OnPointerExit ??= new UnityEvent<PointerEventData>();
            m_OnPointerExit.AddListener(value); 
        }
        remove => m_OnPointerExit?.RemoveListener(value);
    }
    #endregion
    
    [Obsolete("직접 호출 금지", true)]
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || _clickCooldownTask.Status == UniTaskStatus.Pending)
            return;
        
        if (_lastClickTask.Status == UniTaskStatus.Pending)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            bool isDoubleClick = CheckDoubleClickDistance(eventData);

            if (isDoubleClick)
            {
                m_OnDoubleClick?.Invoke(eventData);
                _clickCooldownTask = DoubleClickCoolDownTask();
            }
            else
            {
                m_OnClick?.Invoke(eventData);
            }

            return;
        }

        _lastClickPosition = eventData.position;
        _cts = new CancellationTokenSource();
        _lastClickTask = CheckDoubleClickTime(eventData, _cts.Token);
    }

    [Obsolete("직접 호출 금지", true)]
    public void OnPointerEnter(PointerEventData eventData)
    {
        m_OnPointerEnter?.Invoke(eventData);
    }

    [Obsolete("직접 호출 금지", true)]
    public void OnPointerExit(PointerEventData eventData)
    {
        m_OnPointerExit?.Invoke(eventData);
    }

    private async UniTask CheckDoubleClickTime(PointerEventData eventData, CancellationToken token)
    {
        await UniTask.WaitForSeconds(m_DoubleClickInterval, true, PlayerLoopTiming.Update, token);
        m_OnClick?.Invoke(eventData);
    }

    private UniTask DoubleClickCoolDownTask()
    {
        return UniTask.WaitForSeconds(DOUBLE_CLICK_COOLDOWN, true);
    }

    private bool CheckDoubleClickDistance(PointerEventData eventData)
    {
        if (_lastClickPosition == null)
            return false;
        
        return Vector2.Distance(_lastClickPosition.Value, eventData.position) <= DOUBLE_CLICK_ACCEPT_RANGE;
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
