using System.Collections.Generic;
using UnityEngine;
using static Utils.RectTransformUtils;

public class SaveDisplayer : MonoBehaviour
{
    [SerializeField] private RectTransform m_Boundary;
    [SerializeField] private GameObject m_NodeCell;

    private List<RectTransform> _currentCells = new();
    private Pool<RectTransform> _nodeCellPool;

    private Pool<RectTransform> NodeCellPool
    {
        get
        {
            if (_nodeCellPool == null)
            {
                _nodeCellPool = new
                (
                    createFunc: () =>
                    {
                        GameObject newCell = Instantiate(m_NodeCell, m_Boundary);
                        newCell.SetActive(false);
                        return newCell.GetComponent<RectTransform>();
                    },
                    actionOnGet: cell => cell.gameObject.SetActive(true),
                    actionOnRelease: cell => cell.gameObject.SetActive(false),
                    initSize: 20,
                    maxSize: 500,
                    actionOnDestroy: cell => Destroy(cell.gameObject)
                );
            }

            return _nodeCellPool;
        }
    }

    public void SetNodeCell(List<Vector2> normalizedPosition)
    {

    }
}
