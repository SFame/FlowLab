using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using UnityEngine;

public class PuzzleInteraction : MonoBehaviour, IInteractable
{
    [Header("Puzzle Configuration")]
    [SerializeField] private string puzzleName;
    [SerializeField] private GameObject highlightIndicator;
    [SerializeField] private GameObject puzzleUIPrefab;
    [SerializeField] private string fileName = "puzzle_data.bin";
    [SerializeField] private PUMPSeparator PUMPSeparator;
    private PUMPBackground _pumpBackground;
    private PlayerController playerController;

    // 퍼즐 완료 이벤트
    public event Action<bool> OnPuzzleSolved;

    private bool _onSelected = false;
    private GameObject _pumpUI;

    [SerializeField] private float successDelayBeforeClose = 3.0f; // 퍼즐 성공 후 닫기까지의 지연 시간
    public bool OnSelected
    {
        get => _onSelected;
        set
        {
            if (_onSelected != value)
            {
                _onSelected = value;
                if (highlightIndicator != null)
                {
                    highlightIndicator.SetActive(value);
                }
            }
        }
    }

    private void Awake()
    {
        // PUMPSeparator가 설정되지 않았다면 자동으로 찾기
        if (PUMPSeparator == null)
        {
            PUMPSeparator = FindFirstObjectByType<PUMPSeparator>();
        }
        // 참조가 설정되지 않았다면 자동으로 찾기
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void Interact()
    {
        LoadPuzzleAsync().Forget();
    }

    private async UniTaskVoid LoadPuzzleAsync()
    {
        if (string.IsNullOrEmpty(puzzleName))
        {
            Debug.LogError("Puzzle Name Not Set");
            return;
        }

        if (puzzleUIPrefab == null)
        {
            return;
        }

        // 데이터 로드
        var puzzleData = (await PUMPSerializeManager.GetDatas(fileName))
            .Where(structure => structure.Name == puzzleName)
            .FirstOrDefault();

        if (puzzleData == null)
        {
            Debug.LogError($"Could not find puzzle with name: {puzzleName}");
            return;
        }

        PUMPSeparator.SetVisible(true);

        if (playerController != null)
        {
            playerController.SetPuzzleActive(true);
        }

        _pumpBackground = PUMPSeparator.GetBackground();
        _pumpBackground.Open();

       
        if (_pumpBackground.GetComponentInChildZone<PuzzleBackground>() == null)
        {
            _pumpUI = Instantiate(puzzleUIPrefab);
            RectTransform rect = _pumpUI.GetComponent<RectTransform>();
            _pumpBackground.SetChildZoneAsFull(rect);
        }

        PuzzleBackground puzzleBackground = _pumpBackground.GetComponentInChildZone<PuzzleBackground>();
        puzzleBackground.pumpBackground = _pumpBackground;
        puzzleBackground.testCasePanel = _pumpBackground.GetComponentInChildZone<PuzzleTestCasePanel>();
        puzzleBackground.exitButton.onClick.AddListener(() => ClosePuzzle());

        // 데이터 설정
        puzzleBackground.currentData = puzzleData;

        // 퍼즐 검증 결과 이벤트 구독
        puzzleBackground.OnValidationComplete += HandlePuzzleValidationComplete;
        if (_pumpBackground != null)
        {
            _pumpBackground.SetSerializeNodeInfos(puzzleData.NodeInfos);
            //((IChangeObserver)background).ReportChanges();
        }
        else
        {
            Debug.LogError("PUMPBackground component not found");
        }
        // PuzzleData 설정 (테스트 케이스)
        if (puzzleData.Tag is PuzzleData taggedPuzzleData)
        {
            // PuzzleTestCasePanel에 테스트 케이스 데이터 설정
            //var puzzleTestCasePanel = _pumpUI.GetComponentInChildren<PuzzleTestCasePanel>();
            if (puzzleBackground.testCasePanel != null)
            {
                // 테스트 케이스 패널에 직접 데이터 설정
                //puzzleTestCasePanel.SetupTestCases(taggedPuzzleData);
                puzzleBackground.testCasePanel.SetupTestCases(taggedPuzzleData);
            }

            // 문제 검증을 위해 PuzzleBackground에도 퍼즐 데이터 설정
            if (puzzleBackground != null)
            {
                puzzleBackground.SetPuzzleData(taggedPuzzleData);
            }
        }
        else
        {
            Debug.LogError("PuzzleData not found in the loaded data.");
        }

        Debug.Log($"Loaded puzzle: {puzzleName}");
    }

    private void HandlePuzzleValidationComplete(bool success)
    {
        // 로컬 이벤트 발생
        OnPuzzleSolved?.Invoke(success);

        if (success)
        {
            DelayedClosePuzzle().Forget();
        }
    }
    private async UniTaskVoid DelayedClosePuzzle()
    {
        // 지정된 시간만큼 대기 후 닫기
        await UniTask.Delay(TimeSpan.FromSeconds(successDelayBeforeClose));
        ClosePuzzle();
    }

    // 캔버스를 닫는 메서드, 퍼즐캔버스 속 Exit버튼이 있지만 퍼즐완료했을때를 위해 추가
    public void ClosePuzzle()
    {
        
        PUMPSeparator.SetVisible(false);

        if (playerController != null)
        {
            playerController.SetPuzzleActive(false);
        }

        _pumpBackground.Close();

    }
}