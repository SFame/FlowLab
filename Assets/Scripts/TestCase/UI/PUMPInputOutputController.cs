using TMPro;
using UnityEngine;

/// <summary>
/// 입력 및 출력 게이트웨이 노드 수를 관리하는 클래스
/// PUMPCanvas 프리팹에 추가하여 사용
/// </summary>
public class PUMPInputOutputController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField inputCountField;
    [SerializeField] private TMP_InputField outputCountField;

    [Header("Configuration")]
    [SerializeField] private int minNodeCount = 1;
    [SerializeField] private int maxNodeCount = 8;

    [SerializeField] private PuzzleDataPanel puzzleDataPanel;

    private PUMPBackground _pumpBackground;
    private int _currentInputCount = 2;
    private int _currentOutputCount = 2;
    private bool _isInitializing = true;

    private void Start()
    {
        // PUMPBackground 참조 찾기
        _pumpBackground = Object.FindAnyObjectByType<PUMPBackground>();

        if (_pumpBackground == null)
        {
            return;
        }

        // 현재 게이트웨이 노드 상태 확인 및 초기화
        if (_pumpBackground.ExternalInput != null)
        {
            _currentInputCount = _pumpBackground.ExternalInput.GateCount;
            inputCountField.text = _currentInputCount.ToString();
        }

        if (_pumpBackground.ExternalOutput != null)
        {
            _currentOutputCount = _pumpBackground.ExternalOutput.GateCount;
            outputCountField.text = _currentOutputCount.ToString();
        }

        // InputField 이벤트 연결
        inputCountField.onValueChanged.AddListener(OnInputCountChanged);
        outputCountField.onValueChanged.AddListener(OnOutputCountChanged);

        _isInitializing = false;
    }


    private void OnInputCountChanged(string value)
    {
        // 초기화 중에는 처리하지 않음
        if (_isInitializing)
            return;
            
        if (!int.TryParse(value, out int result))
        {
            // 값을 입력했다가 지운 경우 등 아무것도 하지 않음
            return;
        }

        // 값을 허용 범위 내로 조정
        int clampedValue = Mathf.Clamp(result, minNodeCount, maxNodeCount);

        // 값이 변경된 경우에만 적용
        if (clampedValue != _currentInputCount)
        {
            _currentInputCount = clampedValue;

            // UI에 표시되는 값이 다른 경우 동기화
            if (int.TryParse(inputCountField.text, out int currentDisplayedValue) && 
                currentDisplayedValue != clampedValue)
            {
                // 커서 위치 저장
                int caretPosition = inputCountField.caretPosition;
                inputCountField.text = clampedValue.ToString();
                // 가능하면 커서 위치 복원
                if (caretPosition <= inputCountField.text.Length)
                    inputCountField.caretPosition = caretPosition;
            }

            ApplyChanges();

            // Input 개수가 변경되었으므로 테스트 케이스 초기화
            ClearTestCases();
        }
    }
    
    private void OnOutputCountChanged(string value)
    {
        // 초기화 중에는 처리하지 않음
        if (_isInitializing)
            return;
            
        if (!int.TryParse(value, out int result))
        {
            // 값을 입력했다가 지운 경우 등 아무것도 하지 않음
            return;
        }

        // 값을 허용 범위 내로 조정
        int clampedValue = Mathf.Clamp(result, minNodeCount, maxNodeCount);

        // 값이 변경된 경우에만 적용
        if (clampedValue != _currentOutputCount)
        {
            _currentOutputCount = clampedValue;

            // UI에 표시되는 값이 다른 경우 동기화
            if (int.TryParse(outputCountField.text, out int currentDisplayedValue) && 
                currentDisplayedValue != clampedValue)
            {
                // 커서 위치 저장
                int caretPosition = outputCountField.caretPosition;
                outputCountField.text = clampedValue.ToString();
                // 가능하면 커서 위치 복원
                if (caretPosition <= outputCountField.text.Length)
                    outputCountField.caretPosition = caretPosition;
            }
            ApplyChanges();

            ClearTestCases();
        }
    }

    private void ApplyChanges()
    {
        // 초기화 플래그 설정으로 이벤트 핸들러가 중복 호출되는 것 방지
        _isInitializing = true;


        
        //_pumpBackground.Initialize(_currentInputCount, _currentOutputCount);
        _pumpBackground.ExternalOutput.GateCount = _currentOutputCount;
        _pumpBackground.ExternalInput.GateCount = _currentInputCount;
        
        // 변경 후 실제 적용된 값 확인 및 동기화
        if (_pumpBackground.ExternalInput != null)
        {
            _currentInputCount = _pumpBackground.ExternalInput.GateCount;
            inputCountField.text = _currentInputCount.ToString();
        }

        if (_pumpBackground.ExternalOutput != null)
        {
            _currentOutputCount = _pumpBackground.ExternalOutput.GateCount;
            outputCountField.text = _currentOutputCount.ToString();
        }
        //_pumpBackground.ResetBackground();


        _isInitializing = false;
    }

    private void ClearTestCases()
    {
        if (puzzleDataPanel != null)
        {
            // 기존 테스트 케이스 UI 제거
            puzzleDataPanel.ClearTestCases();

            // 데이터도 초기화
            puzzleDataPanel.currentPuzzleData = new PuzzleData();

            // 새로운 빈 테스트 케이스 추가 (자동으로 현재 입출력 개수에 맞게 조정)
            puzzleDataPanel.AddNewTestCase();
        }
    }
}