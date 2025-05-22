using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class DebugGUI : MonoBehaviour
{
    /// <summary>
    /// 디버그창 위치 설정용 Enum
    /// </summary>
    public enum Position
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    //디버그창 크기
    private Rect windowRect = new Rect(0f, 0f, 600f, 300f);
    //디버그창 색상
    private Color windowColor = new Color(0f, 0f, 0f, 0.8f);
    //디버그창 마진
    private float margin = 10f;
    //버튼간 간격
    private float interval = 32f;
    //폰트 크기
    private int fontSize = 22;
    //폰트 색상
    private Color fontColor = Color.white;
    
    private List<string> textLines = new List<string>();
    private List<StackTrace> stackTraceList = new List<StackTrace>();
    private Vector2 scrollPosition = Vector2.zero;
    private bool shouldScrollToBottom = false;

    private void OnGUI()
    {
        GUI.color = windowColor;
        GUI.Box(windowRect, "");
        GUI.color = fontColor;

        float contentHeight = textLines.Count * interval;
        scrollPosition = GUI.BeginScrollView(windowRect, scrollPosition, new Rect(0, 0, windowRect.width - 20, contentHeight));

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.alignment = TextAnchor.MiddleLeft;
        boxStyle.padding = new RectOffset(10, 0, 0, 0);
        boxStyle.fontSize = fontSize;

        for (int i = 0; i < textLines.Count; i++)
        {
            GUI.Box(new Rect(0, i * interval, windowRect.width - 20, interval), textLines[i], boxStyle);
#if UNITY_EDITOR
            if (GUI.Button(new Rect(0, i * interval, windowRect.width - 20, interval), "", GUIStyle.none))
            {
                OpenScriptAtLine(stackTraceList[i].ToString());
            }
#endif
        }

        GUI.EndScrollView();

        if (shouldScrollToBottom)
        {
            scrollPosition.y = Mathf.Infinity;
            shouldScrollToBottom = false;
        }
    }

#if UNITY_EDITOR
    //클릭해서 디버그를 호출한 스크립트의 줄로 링크
    private void OpenScriptAtLine(string stackTrace)
    {
        string pattern = @"at\s+(.*?)\s+\[.*?\]\s+in\s+(.*?):(\d+)";
        MatchCollection matches = Regex.Matches(stackTrace, pattern);

        if (matches.Count >= 2) //두 번째 "at" 구문이 존재하는지 확인
        {
            Match match = matches[1]; //두 번째 "at" 구문

            string filePath = match.Groups[2].Value;
            int lineNumber = int.Parse(match.Groups[3].Value);

            Object script = AssetDatabase.LoadAssetAtPath<Object>(AbsoluteToRelativePath(filePath));
            AssetDatabase.OpenAsset(script, lineNumber);
        }
        else
        {
            UnityEngine.Debug.LogError("The second 'at' statement was not found in the stack trace.");
        }
    }

    //절대경로 => 상대경로
    private string AbsoluteToRelativePath(string absolutePath)
    {
        // 프로젝트의 Assets 폴더 경로
        string assetsPath = Application.dataPath;
        absolutePath = absolutePath.Replace('\\', '/');
        if (absolutePath.StartsWith(assetsPath))
        {
            return "Assets" + absolutePath.Substring(assetsPath.Length);
        }

        UnityEngine.Debug.LogError("경로가 유효하지 않음: " + absolutePath);
        return null;
    }
#endif

    //창 위치 설정
    private void SetPosition(Position position)
    {
        switch (position)
        {
            case Position.TopLeft:
                windowRect.x = margin;
                windowRect.y = margin;
                break;
            case Position.TopRight:
                windowRect.x = Screen.width - windowRect.width - margin;
                windowRect.y = margin;
                break;
            case Position.BottomLeft:
                windowRect.x = margin;
                windowRect.y = Screen.height - windowRect.height - margin;
                break;
            case Position.BottomRight:
                windowRect.x = Screen.width - windowRect.width - margin;
                windowRect.y = Screen.height - windowRect.height - margin;
                break;
        }
    }

    /// <summary>
    /// 디버그 텍스트 추가
    /// </summary>
    /// <param name="debugTarget"></param>
    /// <param name="stackTrace"></param>
    public void AddDebug(object debugTarget, StackTrace stackTrace, Position position)
    {
        SetPosition(position);
        string targetType = debugTarget.GetType().Name;
        textLines.Add($"[{Time.time:F3}]  " + $"<{targetType}> {debugTarget}");
        stackTraceList.Add(stackTrace);
        shouldScrollToBottom = true;
    }
}
