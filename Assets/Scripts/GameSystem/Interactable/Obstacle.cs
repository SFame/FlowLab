using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] private PuzzleInteraction linkedPuzzle; // 직접 연결

    private void Start()
    {
        
        // 방법 2: 직접 퍼즐 오브젝트 연결
        if (linkedPuzzle != null)
        {
            linkedPuzzle.OnPuzzleSolved += HandleDirectPuzzleSolved;
        }
    }

    private void HandleDirectPuzzleSolved(bool success)
    {
        if (success)
        {
            PuzzleSolved();
        }
    }

    private void PuzzleSolved()
    {
        // 애니메이션 재생, 콜라이더 비활성화 등의 작업
        this.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (linkedPuzzle != null)
        {
            linkedPuzzle.OnPuzzleSolved -= HandleDirectPuzzleSolved;
        }
    }
}
