using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class PuzzleStagePrefab : MonoBehaviour
{
    [SerializeField]
    private Image puzzleImage;
    [SerializeField]
    private TextMeshProUGUI puzzleStageNameText;
    [SerializeField]
    private TextMeshProUGUI clearTiemText;
    [SerializeField]
    private GameObject clearInfo;
    [SerializeField]
    private bool hasReward = false;
    [SerializeField]
    private TextMeshProUGUI rewardNodeTextObj;
    [SerializeField]
    private GameObject rewardObj;
    [SerializeField]
    private bool hasNeedNode = false;
    [SerializeField] 
    private string needNode;
    [SerializeField]
    private TextMeshProUGUI needNodeTextObj;
    [SerializeField]
    private GameObject lockPanel;
    [SerializeField]
    private Button puzzleButton;

    private StageData stageData;

    [SerializeField]
    private PuzzleInteraction puzzleInteraction;
    //클리어하면 Puzzle이름을 플레이어 노드 인벤토리에 String으로 넘겨서 퍼즐추가시키기

    private void Start()
    {
        SetInfo();

        puzzleInteraction.OnPuzzleValidation += PuzzleSolved;
        PlayerNodeInventory.OnNodeUnlocked += OnNodeUnlocked;
    }
    public void SetInfo()
    {
        // 이미지는 어떻게 불러와야할까.. 제작씬에서 저장한 그 이미지를 가져와야하나? 이미지가 필요한가? 퍼즐 이름만 있는게 나을까?
        puzzleStageNameText.text = puzzleInteraction.puzzleName;

        bool isClear;
        stageData = GameSaveManager.Instance.FindPuzzleDataState(puzzleInteraction.puzzleName);

        if (stageData == null)
        {
            isClear = false;
        }
        else
        {
            isClear = stageData.Clear;
            clearTiemText.text = stageData.ClearTime.ToString("F2") + " sec";
        }

        if (isClear)
        {
            clearInfo.SetActive(true);
            clearTiemText.gameObject.SetActive(true);
        }
        else
        {
            clearInfo.SetActive(false);
            clearTiemText.gameObject.SetActive(false);
        }
        if (hasReward)
        {
            rewardNodeTextObj.text = puzzleInteraction.puzzleName;
            rewardObj.SetActive(true);
        }
        else
        {
            rewardObj.SetActive(false);
        }
        if (hasNeedNode)
        {
            needNodeTextObj.text = needNode;

            Type needNodeType = Type.GetType(needNode);

            if (needNodeType != null && PlayerNodeInventory.IsNodeAvailable(needNodeType))
            {
                // 필요한 노드가 이미 언락되어 있으면 잠금 해제
                lockPanel.SetActive(false);
            }
            else
            {
                // 필요한 노드가 없으면 잠금 유지
                lockPanel.SetActive(true);
            }
        }
        else
        {
            lockPanel.SetActive(false);
        }

        puzzleButton.onClick.AddListener(() =>
        {
                puzzleInteraction.Interact();
        });
    }

    private void PuzzleSolved(bool isSolved)
    {
        if (isSolved)
        {
            SetInfo();
            Debug.Log("Puzzle Solved: " + puzzleInteraction.puzzleName);
        }
    }
    private void OnNodeUnlocked(Type unlockedNodeType)
    {
        if (hasNeedNode)
        {
            Type needNodeType = Type.GetType(needNode);

            if (needNodeType == unlockedNodeType)
            {
                SetInfo();
            }
        }
    }
    
    private void OnDestroy()
    {
        puzzleInteraction.OnPuzzleValidation -= PuzzleSolved;
        PlayerNodeInventory.OnNodeUnlocked -= OnNodeUnlocked;
    }
}
