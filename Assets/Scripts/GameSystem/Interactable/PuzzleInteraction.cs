using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class PuzzleInteraction : MonoBehaviour, IInteractable
{
    [Header("Puzzle Configuration")]
    [SerializeField] private string puzzleName;
    [SerializeField] private GameObject highlightIndicator;
    [SerializeField] private GameObject puzzlePumpCanvasPrefab;
    [SerializeField] private string fileName = "puzzle_data.bin";

    // 퍼즐 완료 이벤트
    public event Action<bool> OnPuzzleSolved;

    private bool _onSelected = false;
    private GameObject _instantiatedCanvas;

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

        if (puzzlePumpCanvasPrefab == null)
        {
            return;
        }

        try
        {
            // 이미 캔버스가 존재하면 제거
            if (_instantiatedCanvas != null)
            {
                Destroy(_instantiatedCanvas);
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

            // 캔버스 인스턴스화
            _instantiatedCanvas = Instantiate(puzzlePumpCanvasPrefab);

            // PuzzleBackground 컴포넌트 찾기
            var puzzleBackground = _instantiatedCanvas.GetComponentInChildren<PuzzleBackground>();
            if (puzzleBackground == null)
            {
                Destroy(_instantiatedCanvas);
                return;
            }

            // 퍼즐 캔버스 설정
            puzzleBackground.SetPuzzleCanvas(_instantiatedCanvas);
            // 데이터 설정
            puzzleBackground.currentData = puzzleData;

            // 퍼즐 검증 결과 이벤트 구독
            puzzleBackground.OnValidationComplete += HandlePuzzleValidationComplete;

            // PUMPBackground에 노드 정보 설정
            var pumpBackground = puzzleBackground.pumpBackground;
            if (pumpBackground != null)
            {
                pumpBackground.SetSerializeNodeInfos(puzzleData.NodeInfos);
                //((IChangeObserver)background).ReportChanges();
            }
            // PuzzleData 설정 (테스트 케이스)
            if (puzzleData.Tag is PuzzleData taggedPuzzleData)
            {
                // PuzzleTestCasePanel에 테스트 케이스 데이터 설정
                var puzzleTestCasePanel = _instantiatedCanvas.GetComponentInChildren<PuzzleTestCasePanel>();
                if (puzzleTestCasePanel != null)
                {
                    // 테스트 케이스 패널에 직접 데이터 설정
                    puzzleTestCasePanel.SetupTestCases(taggedPuzzleData);
                }

                // 문제 검증을 위해 PuzzleBackground에도 퍼즐 데이터 설정
                if (puzzleBackground != null)
                {
                    puzzleBackground.SetPuzzleData(taggedPuzzleData);
                }
            }
            else
            {
                Debug.LogError("PUMPBackground component not found");
            }

            Debug.Log($"Loaded puzzle: {puzzleName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading puzzle: {e.Message}");
            if (_instantiatedCanvas != null)
            {
                Destroy(_instantiatedCanvas);
                _instantiatedCanvas = null;
            }
        }
    }

    private void HandlePuzzleValidationComplete(bool success)
    {
        // 로컬 이벤트 발생
        OnPuzzleSolved?.Invoke(success);

        // 전역 이벤트 발생하고 (반응할 오브젝트가 전역 이벤트 구독하거나 PuzzleInteraction을 참조시켜서 연결해야함)
        //PuzzleEvents.TriggerPuzzleCompleted(puzzleName, success);

        // if (success)로 성공이면
        // 퍼즐UI도 닫고
        // else로 실패면 프리펩 속 Panel이 틀린거 보여주니깐 그냥 풀게냅두기
    }

    // 캔버스를 닫는 메서드, 퍼즐캔버스 속 Exit버튼이 있지만 퍼즐완료했을때를 위해 추가
    public void ClosePuzzle()
    {
        if (_instantiatedCanvas != null)
        {
            Destroy(_instantiatedCanvas);
            _instantiatedCanvas = null;
        }
    }
}