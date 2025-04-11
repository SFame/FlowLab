using UnityEngine;

public class CutsceneTest : MonoBehaviour
{
    private void StartOpening()
    {
        CutsceneDisplay.ShowCutsceneSequence("opening_sequence");

    }

    private void Awake()
    {
        // Subscribe to cutscene events
        CutsceneDisplay.OnCutsceneStarted += HandleCutsceneStarted;
        CutsceneDisplay.OnCutsceneEnded += HandleCutsceneEnded;
    }

    private void Update()
    {
        // Check for input to start the cutscene
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartOpening();
        }
    }
    
    private void HandleCutsceneStarted(string cutsceneId)
    {
        Debug.Log($"Cutscene started: {cutsceneId}");
        // Maybe pause gameplay, disable player controls, etc.
    }

    private void HandleCutsceneEnded(string cutsceneId)
    {
        Debug.Log($"Cutscene ended: {cutsceneId}");
        // Resume gameplay, enable player controls, etc.
    }
}
