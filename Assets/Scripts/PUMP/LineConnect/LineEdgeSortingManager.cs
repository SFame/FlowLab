using System;
using System.Buffers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LineEdgeSortingManager : MonoBehaviour
{
    #region Static
    public static bool Activate
    {
        get => _activate;
        set
        {
            _activate = value;

            if (!_activate)
            {
                OnDeactive?.Invoke();
            }
        }
    }

    private static bool _activate;
    private static Action OnDeactive;
    #endregion

    [SerializeField] private InputKeyMap m_SortModeKeyMap;
    [SerializeField] private GameObject m_LineImagePrefab;
    [SerializeField] private RectTransform m_SortingLineParent;
    [SerializeField] private Color m_SortingLineColor;
    [SerializeField] private float m_LineThickness = 1f;

    private const string KEY_MAP_NAME = "EdgeSorting_Start";
    private readonly HashSet<ISortingPositionGettable> _gettables = new();
    private Pool<RectTransform> _linePool;
    private bool _lineInitialized = false;

    private void Awake() => OnDeactive += RemoveLine;

    private void OnDestroy() => OnDeactive -= RemoveLine;

    private void LineInitialize()
    {
        if (_lineInitialized)
        {
            return;
        }

        _lineInitialized = true;

        _linePool = new Pool<RectTransform>
        (
            initSize: 2,
            createFunc: () =>
            {
                GameObject newObject = Instantiate(m_LineImagePrefab, m_SortingLineParent);
                newObject.GetComponent<Image>().color = m_SortingLineColor;
                return newObject.GetComponent<RectTransform>();
            },
            actionOnGet: rect => rect.gameObject.SetActive(true),
            actionOnRelease: rect => rect.gameObject.SetActive(false),
            actionOnDestroy: rect => Destroy(rect.gameObject)
        );
    }

    private void DrawLine(params LineArgs[] args)
    {
        LineInitialize();
        RemoveLine();

        foreach (LineArgs arg in args)
        {
            if (arg.IsNull)
            {
                continue;
            }

            RectTransform lineRect = _linePool.Get();

            lineRect.pivot = new Vector2(0, 0.5f);

            Vector2 direction = arg.End - arg.Start;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            lineRect.position = arg.Start;
            lineRect.sizeDelta = new Vector2(distance, m_LineThickness);
            lineRect.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void RemoveLine()
    {
        if (!_lineInitialized)
        {
            return;
        }

        _linePool.ReleaseAll();
    }

    public float StickDistance { get; set; } = 15f;

    public void AddGettable(ISortingPositionGettable gettable)
    {
        _gettables.Add(gettable);
        gettable.OnGettableRemove += () =>
        {
            _gettables.Remove(gettable);
        };
    }

    public void AddSettable(ISortingPositionSettable settable)
    {
        settable.OnSettableDrag += SettableSortingAction;
        settable.OnSettableDragEnd += RemoveLine;
    }

    private void SettableSortingAction(ISortingPositionSettable settable, Vector2 position, out bool isStick)
    {
        isStick = false;

        if (!_activate)
        {
            RemoveLine();
            return;
        }

        ArrayPool<ISortingPositionGettable> pool = ArrayPool<ISortingPositionGettable>.Shared;
        ISortingPositionGettable[] candidates = pool.Rent(_gettables.Count);

        try
        {
            int count = 0;
            foreach (ISortingPositionGettable gettable in _gettables)
            {
                if (!ReferenceEquals(gettable, settable))
                {
                    candidates[count++] = gettable;
                }
            }

            Vector2 stickPosition = position;
            float xDistanceCache = float.MaxValue;
            float yDistanceCache = float.MaxValue;
            LineArgs xArgs = new LineArgs(Vector2.zero, Vector2.zero, true);
            LineArgs yArgs = new LineArgs(Vector2.zero, Vector2.zero, true);

            for (int i = 0; i < count; i++)
            {
                ISortingPositionGettable gettable = candidates[i];

                if (!gettable.IsActive)
                {
                    continue;
                }

                Vector2 gettablePosition = gettable.GetPosition();

                float xDistance = Mathf.Abs(position.x - gettablePosition.x);
                if (xDistance < StickDistance && xDistance < xDistanceCache)
                {
                    xDistanceCache = xDistance;
                    stickPosition.x = gettablePosition.x;
                    xArgs.IsNull = false;
                    xArgs.Start = gettablePosition;
                    isStick = true;
                }

                float yDistance = Mathf.Abs(position.y - gettablePosition.y);
                if (yDistance < StickDistance && yDistance < yDistanceCache)
                {
                    yDistanceCache = yDistance;
                    stickPosition.y = gettablePosition.y;
                    yArgs.IsNull = false;
                    yArgs.Start = gettablePosition;
                    isStick = true;
                }
            }

            if (isStick)
            {
                settable.SetPosition(stickPosition);
                xArgs.End = stickPosition;
                yArgs.End = stickPosition;
                DrawLine(xArgs, yArgs);
                return;
            }

            RemoveLine();
        }
        finally
        {
            pool.Return(candidates);
        }
    }

    private struct LineArgs
    {
        public LineArgs(Vector2 start, Vector2 end, bool isNull)
        {
            Start = start;
            End = end;
            IsNull = isNull;
        }

        public Vector2 Start;
        public Vector2 End;
        public bool IsNull;
    }
}

public interface ISortingPositionGettable
{
    event Action OnGettableRemove;
    bool IsActive { get; }
    Vector2 GetPosition();
}

public interface ISortingPositionSettable
{
    event SettableEventHandler  OnSettableDrag;
    event Action OnSettableDragEnd;
    void SetPosition(Vector2 position);
}

public delegate void SettableEventHandler(ISortingPositionSettable settable, Vector2 position, out bool isStick);