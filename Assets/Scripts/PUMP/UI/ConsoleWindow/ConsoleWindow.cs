using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Utils;
using static ConsoleCommand;

public class ConsoleWindow : MonoBehaviour
{
    #region Static Layer
    // -=-=-=-=-=-=-=-=-=- Interface -=-=-=-=-=-=-=-=-=-
    public static void Wake()
    {
        _ = Instance;
    }

    public static void Input(string text)
    {
        InternalInput(text, false);
    }

    public static void Clear()
    {
        if (_onCommand)
        {
            return;
        }

        HeaderActive = true;
        _currentTextLine = string.Empty;
        Instance.PushText(_currentTextLine, true);
    }

    public static bool AddCommand(ConsoleCommand newCommand)
    {
        ConsoleDefaultCommandInjector.Inject();
        ConsoleCommand existingCommand = _commands.FirstOrDefault(c => c.Command == newCommand.Command);

        if (existingCommand != null)
        {
            if (existingCommand.IsSystem)
            {
                return false;
            }

            _commands.Remove(existingCommand);
        }

        _commands.Add(newCommand);
        return true;
    }

    public static bool RemoveCommand(ConsoleCommand command)
    {
        return _commands.Remove(command);
    }

    public static bool IsOpen
    {
        get => _isOpen;
        set
        {
            _isOpen = value;
            if (_isOpen)
            {
                Instance.Show();
                return;
            }

            Instance.Hide();
        }
    }

    public static ConsoleCommand[] GetCommands() => _commands.ToArray();
    // -=-=-=-=-=-=-=-=-=--=-=-=-=-=-=-=-=-=--=-=-=-=-=-

    // -=-=-=-=-=-=-=-=-=- Privates -=-=-=-=-=-=-=-=-=-
    private const string PREFAB_PATH = "PUMP/Prefab/UI/ConsoleWindow";
    private const int MAX_LINE_COUNT = 50;
    private const string HEADER_TEXT = "FlowLab> ";
    private static bool _headerActive = true;
    private static readonly HashSet<ConsoleCommand> _commands = new HashSet<ConsoleCommand>();
    private static string _currentTextLine = string.Empty;
    private static bool _onCommand = false;
    private static bool _onQuery = false;
    private static bool _isOpen = false;
    private static string _queryCache = null;
    private static SafetyCancellationTokenSource _queryCts = new();
    private static object _inputBlocker = new();
    private static GameObject _prefab;
    private static ConsoleWindow _instance;

    private static GameObject Prefab => _prefab ??= Resources.Load<GameObject>(PREFAB_PATH);

    private static ConsoleWindow Instance
    {
        get
        {
            if (_instance == null)
            {
                ConsoleDefaultCommandInjector.Inject();
                GameObject newObject = Instantiate(Prefab);
                RectTransform newRect = newObject.GetComponent<RectTransform>();
                PUMPUiManager.Render
                (
                    ui: newRect, 
                    layerIndex: 0,
                    onRender: rect =>
                    {
                        rect.SetRectFull();
                        rect.gameObject.SetActive(true);
                    },
                    onReturn: rect =>
                    {
                        Destroy(rect.gameObject);
                        _instance = null;
                    }
                );
                ConsoleWindow newWindow = newObject.GetComponent<ConsoleWindow>();
                newWindow.Initialize(string.Empty);
                _instance = newObject.GetComponent<ConsoleWindow>();
            }
            return _instance;
        }
    }

    private static bool HeaderActive
    {
        get => _headerActive;
        set
        {
            _headerActive = value;
            Instance.SetHeaderActive(_headerActive);
        }
    }

    private static void InternalInput(string text, bool setFocus)
    {
        text ??= string.Empty;
        AddCurrentTextLine(HeaderActive ? $"{HEADER_TEXT}{text}" : text);

        // 쿼리 도중에는 캐쉬 설정 후 그대로 출력
        if (_onCommand)
        {
            _queryCache = text;
            Instance.PushText(GetCurrentTextLine(), setFocus);
            return;
        }

        // 슬래쉬로 시작하지 않으면 이전해 Add한 문자열 그대로 찍음
        if (!text.StartsWith("/"))
        {
            Instance.PushText(GetCurrentTextLine(), setFocus);
            return;
        }

        // 슬래쉬로 시작했다면 명령어 판독
        ProgressCommand(text).Forget();
    }

    private static void AddCurrentTextLine(string text)
    {
        if (!string.IsNullOrEmpty(_currentTextLine))
        {
            _currentTextLine += "\n";
        }
        _currentTextLine += text;
        string[] lines = _currentTextLine.Split('\n');
        if (lines.Length > MAX_LINE_COUNT)
        {
            int excessLines = lines.Length - MAX_LINE_COUNT;
            _currentTextLine = string.Join("\n", lines.Skip(excessLines));
        }
    }

    private static string GetCurrentTextLine()
    {
        return _currentTextLine;
    }

    private static async UniTaskVoid ProgressCommand(string input)
    {
        string[] split = input.Split(' ');
        string currentCommand = split[0];
        string[] inputArgs = split.Skip(1).ToArray();

        if (_commands.FirstOrDefault(command => command.Command == currentCommand) is { } resultCommand)
        {
            if ((resultCommand.Args == null || resultCommand.Args.Length <= 0))
            {
                if (inputArgs.Length > 0)
                {
                    InternalInput("ERROR: Argument not match.", true);
                    return;
                }
            }
            else if (inputArgs.Length != resultCommand.Args.Length)
            {
                InternalInput("ERROR: Argument not match.", true);
                return;
            }

            Dictionary<string, string> argsDict = new();

            if (resultCommand.Args != null)
            {
                for (int i = 0; i < resultCommand.Args.Length; i++)
                {
                    argsDict.Add(resultCommand.Args[i], inputArgs[i]);
                }
            }

            _onCommand = true;
            HeaderActive = false;
            string result = await ((IStartQuery)resultCommand).StartQuery(new CommandContext(Query, argsDict));

            if (result != null)
            {
                InternalInput(result, true);
            }

            CancelQuery();
            _onCommand = false;
            HeaderActive = true;
            return;
        }

        InternalInput("ERROR: Unknown command", true);
    }

    private static async UniTask<string> Query(string query)
    {
        if (_onQuery)
        {
            Debug.LogError("이전 쿼리 미완료. 커맨드 설계 재검토 필요");
            return null;
        }

        _onQuery = true;
        InternalInput(query ?? string.Empty, true);
        _queryCache = null;

        try
        {
            await UniTask.WaitUntil(() => _queryCache != null, cancellationToken: _queryCts.Token);
            return _queryCache;
        }
        catch (OperationCanceledException)
        {
            InternalInput("ERROR: Command Shutdown", true);
            _queryCache = null;
            return null;
        }
        finally
        {
            _onQuery = false;
        }
    }

    private static void CancelQuery()
    {
        _queryCts = _queryCts.CancelAndDisposeAndGetNew();
    }
    // -=-=-=-=-=-=-=-=-=--=-=-=-=-=-=-=-=-=--=-=-=-=-=
    #endregion

    #region Instance Layer
    [SerializeField] private CanvasGroup m_CanvasGroup;
    [SerializeField] private float m_FadeDuration = 0.2f;
    [SerializeField] private TextMeshProUGUI m_MainTextField;
    [SerializeField] private TMP_InputField m_InputField;
    [SerializeField] private TextMeshProUGUI m_InputHeader;
    [SerializeField] private RectTransform m_HeaderSpaceRect;
    [SerializeField] private float m_SpaceWidth = 9.6f;

    private void Initialize(string initText)
    {
        m_InputField.onSubmit.AddListener(text => InternalInput(text, true));
        m_InputField.onSelect.AddListener(_ => InputManager.AddBlocker(_inputBlocker));
        m_InputField.onDeselect.AddListener(_ => InputManager.RemoveBlocker(_inputBlocker));
        m_InputField.onFocusSelectAll = false;
        SetHeaderActive(true);
        m_InputField.text = initText;
    }

    private void PushText(string text, bool setFocus)
    {
        m_MainTextField.text = text;
        m_InputField.text = string.Empty;

        if (setFocus)
        {
            m_InputField.ActivateInputField();
        }
    }

    private void SetHeaderActive(bool active)
    {
        float widthResult = active ? m_SpaceWidth : 0;
        m_HeaderSpaceRect.sizeDelta = new Vector2(widthResult, m_HeaderSpaceRect.sizeDelta.y);
        m_InputHeader.text = active ? HEADER_TEXT : string.Empty;
    }

    private void Show()
    {
        m_CanvasGroup.DOKill();
        m_CanvasGroup.DOFade(1f, m_FadeDuration).onComplete = () =>
        {
            m_CanvasGroup.interactable = true;
            m_CanvasGroup.blocksRaycasts = true;
        };
    }

    private void Hide()
    {
        m_CanvasGroup.DOKill();
        m_CanvasGroup.DOFade(0f, m_FadeDuration).onComplete = () =>
        {
            m_CanvasGroup.interactable = false;
            m_CanvasGroup.blocksRaycasts = false;
        };
    }
    #endregion
}

/// <summary>
/// 콘솔 커맨드 클래스
/// </summary>
public class ConsoleCommand: IStartQuery
{
    public struct CommandContext
    {
        public CommandContext(Func<string, UniTask<string>> queryFunc, Dictionary<string, string> args)
        {
            if (queryFunc == null || args == null)
            {
                throw new ArgumentNullException($"{nameof(CommandContext)}: Param is Null");
            }
            _queryFunc = queryFunc;
            _args = args;
        }

        private Func<string, UniTask<string>> _queryFunc;
        private Dictionary<string, string> _args;

        public string GetArg(string key)
        {
            return _args.GetValueOrDefault(key);
        }

        public UniTask<string> Query(string ask)
        {
            return _queryFunc(ask);
        }
    }

    public ConsoleCommand(
        string command, 
        Func<CommandContext, UniTask<string>> queryProcess, 
        string doc, 
        string[] args = null, 
        bool isSystem = true)
    {
        QueryProcess = queryProcess ?? throw new ArgumentNullException($"{nameof(ConsoleCommand)}: QueryProcess cannot be Null");
        Command = command.StartsWith("/") ? command : $"/{command}";
        Doc = doc;

        if (args != null && args.Length != args.Distinct().Count())
        {
            throw new ArgumentException($"{nameof(ConsoleCommand)}: Args duplicate");
        }

        Args = args;
        IsSystem = isSystem;
    }

    /// <summary>
    /// 쿼리 호출을 위한 커멘드
    /// </summary>
    public string Command { get; }

    public string[] Args { get; }

    /// <summary>
    /// 커맨드 실행 로직을 정의하는 함수
    /// </summary>
    /// <remarks>
    /// 사용 예시:
    /// <code>
    /// async context => 
    /// {
    ///     // 인자 가져오기
    ///     string arg = context.GetArg("argName");
    ///     
    ///     // 사용자에게 질문하고 답변 대기 (양방향 await)
    ///     string answer = await context.Query("질문 내용?");
    ///     if (answer == null) return null; // 쿼리 취소됨
    ///     
    ///     // 결과 반환 (콘솔에 출력됨, null이면 출력 생략)
    ///     return "실행 완료";
    /// }
    /// </code>
    /// - context.GetArg(key): 커맨드 인자 조회
    /// - context.Query(ask): 사용자에게 질문하고 답변을 await로 대기
    /// - 반환값: 콘솔에 출력할 최종 메시지 (null 가능)
    /// - await 결과가 null: 쿼리가 강제 취소됨
    /// </remarks>
    public Func<CommandContext, UniTask<string>> QueryProcess { get; }

    /// <summary>
    /// 해당 커멘드의 Document
    /// </summary>
    public string Doc { get; }

    /// <summary>
    /// 시스템 소속: 삭제 불가
    /// </summary>
    public bool IsSystem { get; }

    UniTask<string> IStartQuery.StartQuery(CommandContext context) => QueryProcess(context);
}

public interface IStartQuery
{
    UniTask<string> StartQuery(CommandContext context);
}