using System;
using System.Collections.Generic;
using UnityEngine;

public class PrismView : MonoBehaviour
{
    [SerializeField] private GameObject m_PrismGridTemplate;
    [SerializeField] private GameObject m_PrismTriggerTemplate;

    [Space(10)]

    [SerializeField] private RectTransform m_GridParent;
    [SerializeField] private RectTransform m_TriggerParent;

    private readonly List<PrismPair> _pairs = new();

    /// <summary>
    /// Key: 카테고리
    /// Value: 내부 요소
    /// </summary>
    public void Initialize(Dictionary<string, List<RectTransform>> prism, int startActive = 0)
    {
        Clear();

        foreach (KeyValuePair<string, List<RectTransform>> kvp in prism)
        {
            var tuple = GetNewTuple();
            PrismPair newPair = new PrismPair(tuple.grid, tuple.trigger, kvp.Key, kvp.Value, currentPair =>
            {
                foreach (PrismPair pair in _pairs)
                {
                    if (pair == currentPair)
                        continue;

                    pair.Deactivate();
                }
            });

            _pairs.Add(newPair);
        }

        if (_pairs.Count == 0)
            return;

        if (_pairs.Count <= startActive)
        {
            _pairs[0].Activate();
            return;
        }

        _pairs[startActive].Activate();
    }

    public (PrismGrid grid, PrismTrigger trigger) GetNewTuple()
    {
        GameObject gridObj = Instantiate(m_PrismGridTemplate, m_GridParent);
        GameObject triggerObj = Instantiate(m_PrismTriggerTemplate, m_TriggerParent);
        gridObj.SetActive(true);
        triggerObj.SetActive(true);

        return (gridObj.GetComponent<PrismGrid>(), triggerObj.GetComponent<PrismTrigger>());
    }

    public void Clear()
    {
        foreach (PrismPair pair in _pairs)
        {
            pair.Destroy();
        }

        _pairs.Clear();
    }
}

public class PrismPair
{
    private PrismGrid _grid;
    private PrismTrigger _trigger;
    private Action<PrismPair> _onActivate;

    public PrismPair(PrismGrid grid, PrismTrigger trigger, string name, List<RectTransform> gridElem, Action<PrismPair> onActivate)
    {
        _grid = grid;
        _trigger = trigger;
        _onActivate = onActivate;

        _grid.Initialize(gridElem);
        _trigger.Initialize(name, Activate);

        Deactivate();
    }

    public void Activate()
    {
        _trigger.IsActive = true;
        _grid.IsActive = true;
        _onActivate?.Invoke(this);
    }

    public void Deactivate()
    {
        _trigger.IsActive = false;
        _grid.IsActive = false;
    }

    public void Destroy()
    {
        _onActivate = null;
        _grid.Destroy();
        _trigger.Destroy();
    }
}