using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        puzzleInteraction.OnPuzzleValidation += PuzzleSolved; // 이게 첫 시도엔 먼저 호출되서 첫퍼즐 풀이에 갱신이 안되나?
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
            lockPanel.SetActive(true);
        }
        else
        {
            lockPanel.SetActive(false);
        }

        // 플레이어 노드 인벤토리에서 NeedNode를 가지고 있는지 확인후 lockPanel 조정
        //TODO

        puzzleButton.onClick.AddListener(() =>
        {
                puzzleInteraction.Interact();
        });
    }

    private void PuzzleSolved(bool isSolved) // 왜 처음 퍼즐생성하고 풀면 안되고 2번째부터 갱신되지?
    {
        if (isSolved)
        {
            SetInfo();
            Debug.Log("Puzzle Solved: " + puzzleInteraction.puzzleName);
        }
    }
}
