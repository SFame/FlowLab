using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using Utils;
using static ConsoleCommand;
using Debug = UnityEngine.Debug;

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
        ConsoleDefaultCommandInjector.Inject();
        InternalInput(text, ConsoleInputSource.System);
    }

    public static void Clear(bool setFocus = false)
    {
        CancelQuery();
        _onCommand = false;
        HeaderActive = true;
        _text.Clear();
        Instance.PushText(GetCurrentText(), setFocus);
    }

    public static void SetFocus(bool activateFocus)
    {
        if (activateFocus)
        {
            Instance.m_InputField.ActivateInputField();
            return;
        }

        Instance.m_InputField.DeactivateInputField();
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
    private const int MAX_LINE_COUNT = 200;
    private const string HEADER_TEXT = "FlowLab> ";
    private static Text _text = new Text(MAX_LINE_COUNT);
    private static bool _headerActive = true;
    private static readonly HashSet<ConsoleCommand> _commands = new HashSet<ConsoleCommand>();
    private static bool _onCommand = false;
    private static bool _onQuery = false;
    private static ConsoleInputSource _lastQuerySource = ConsoleInputSource.InputField;
    private static bool _isOpen = false;
    private static QueryResult? _queryCache = null;
    private static SafetyCancellationTokenSource _queryCts;
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

    private static TextLine InternalInput(string text, ConsoleInputSource inputSource)
    {
        bool setFocus = inputSource == ConsoleInputSource.InputField;
        text ??= string.Empty;

        // 커맨드 도중인데 쿼리가 아닐 때는 입력하는 타이밍이 아님
        if (_onCommand && !_onQuery)
        {
            if (inputSource == ConsoleInputSource.InputField)
            {
                Instance.ClearInputField(setFocus);
            }
            return null;
        }

        // 텍스트라인에 더함
        TextLine line = AddCurrentText(HeaderActive ? $"{HEADER_TEXT}{text}" : text);
        Instance.PushText(GetCurrentText(), setFocus);

        // 쿼리 도중에는 캐쉬 설정 후 그대로 리턴
        if (_onCommand && _onQuery)
        {
            _queryCache = new(text, inputSource);
            _lastQuerySource = inputSource;
            return line;
        }

        // 슬래쉬로 시작하지 않으면 이전해 Add한 문자열 그대로 찍음
        if (!text.StartsWith("/"))
        {
            return line;
        }

        // 슬래쉬로 시작했다면 명령어 판독
        ProgressCommand(text, inputSource).Forget();
        return line;
    }

    private static TextLine InternalInputRaw(string text)
    {
        TextLine line = AddCurrentText(text);
        Instance.PushTextNotChangeFocus(GetCurrentText());
        return line;
    }

    private static TextLine AddCurrentText(string line)
    {
        TextLine textLine = _text.Append(line);
        textLine.OnTextUpdate += _ => Instance.PushTextNotChangeFocus(GetCurrentText());
        return textLine;
    }

    private static string GetCurrentText()
    {
        return _text.ToString();
    }

    private static async UniTaskVoid ProgressCommand(string input, ConsoleInputSource inputSource)
    {
        int firstSpaceIdx = input.IndexOf(' ');

        string currentCommand = firstSpaceIdx >= 0 ? input.Substring(0, firstSpaceIdx) : input;
        string argString = firstSpaceIdx >= 0 ? input.Substring(firstSpaceIdx + 1) : "";

        List<string> inputArgs = new();

        if (argString.Length > 0)
        {
            int quotesIdx = argString[0] == '\"' ? 0 : -1;
            bool removeSpaceFlag = true;
            StringBuilder sb = new();

            for (int currentIdx = quotesIdx + 1; currentIdx < argString.Length; currentIdx++)
            {
                char current = argString[currentIdx];

                if (quotesIdx >= 0 && currentIdx == argString.Length -1 && current != '\"')
                {
                    currentIdx = quotesIdx;
                    quotesIdx = -1;
                    sb.Clear();
                    sb.Append('\"');
                    continue;
                }

                if (quotesIdx < 0)
                {
                    if (current == ' ')
                    {
                        if (removeSpaceFlag)
                        {
                            continue;
                        }

                        removeSpaceFlag = true;
                        inputArgs.Add(sb.ToString());
                        sb.Clear();
                        continue;
                    }

                    removeSpaceFlag = false;

                    if (current == '\"')
                    {
                        if (currentIdx == argString.Length - 1)
                        {
                            sb.Append('\"');
                        }
                        else
                        {
                            quotesIdx = currentIdx;
                        }

                        if (sb.Length > 0)
                        {
                            inputArgs.Add(sb.ToString());
                            sb.Clear();
                        }

                        continue;
                    }

                    sb.Append(current);
                    continue;
                }

                if (current == '\"')
                {
                    quotesIdx = -1;
                    inputArgs.Add(sb.ToString());
                    sb.Clear();
                    removeSpaceFlag = true;
                    continue;
                }

                sb.Append(current);
            }

            if (quotesIdx >= 0)
            {
                sb.Append('\"');
            }

            if (sb.Length > 0)
            {
                inputArgs.Add(sb.ToString());
            }
        }

        if (_commands.FirstOrDefault(command => command.Command == currentCommand) is { } resultCommand)
        {
            if ((resultCommand.Args == null || resultCommand.Args.Length <= 0))
            {
                if (inputArgs.Count > 0)
                {
                    InternalInput("ERROR: Argument not match.", inputSource);
                    return;
                }
            }
            else if (inputArgs.Count > resultCommand.Args.Length)
            {
                InternalInput("ERROR: Argument not match.", inputSource);
                return;
            }

            Dictionary<string, string> argsDict = new();

            if (resultCommand.Args != null)
            {
                for (int i = 0; i < resultCommand.Args.Length; i++)
                {
                    string arg = inputArgs.Count > i ? inputArgs[i] : null;
                    argsDict.Add(resultCommand.Args[i], arg);
                }
            }

            CancelQuery();
            ChargeCancelQueryCts();
            _lastQuerySource = inputSource;
            HeaderActive = false;
            _onCommand = true;
            CommandContextDisposer contextDisposer = new CommandContextDisposer();

            try
            {
                UniTask<string> startQueryTask = ((IStartQuery)resultCommand).StartQuery(new CommandContext(Query, text => InternalInputRaw(text), InternalInputRaw, argsDict, inputSource, contextDisposer));
                UniTask<string> cancelTask = WaitUntilCancel(_queryCts.Token);

                (int winIndex, string result1, string result2) = await UniTask.WhenAny(startQueryTask, cancelTask);

                _onCommand = false;
                string result = winIndex == 0 ? result1 : result2;

                bool cancelled = winIndex == 1 || _queryCts.IsCancellationRequested;
                if (result != null && !cancelled)
                {
                    InternalInput(result, _lastQuerySource);
                }
            }
            catch (Exception e)
            {
                _onCommand = false;
                InternalInput($"Error during command: {e.Message}", _lastQuerySource);
            }
            finally
            {
                contextDisposer.Dispose();
            }

            CancelQuery();
            HeaderActive = true;
            return;
        }

        InternalInput("ERROR: Unknown command", _lastQuerySource);
    }

    private static async UniTask<string> WaitUntilCancel(CancellationToken token)
    {
        try
        {
            await UniTask.WaitUntil(() => token.IsCancellationRequested, cancellationToken: token, cancelImmediately: true);
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private static async UniTask<QueryResult?> Query(string query)
    {
        if (_onQuery)
        {
            Debug.LogError("이전 쿼리 미완료. 커맨드 설계 재검토 필요");
            return null;
        }

        _onQuery = true;
        InternalInput(query ?? string.Empty, _lastQuerySource);
        _queryCache = null;

        try
        {
            await UniTask.WaitUntil(() => _queryCache != null, cancellationToken: _queryCts.Token);
            return _queryCache;
        }
        catch (OperationCanceledException)
        {
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
        _queryCts?.CancelAndDispose();
    }

    private static void ChargeCancelQueryCts()
    {
        _queryCts = new(false);
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
        Dispatcher.Post(() =>
        {
            m_InputField.onSubmit.AddListener(text => InternalInput(text, ConsoleInputSource.InputField));
            m_InputField.onSelect.AddListener(_ => InputManager.AddBlocker(_inputBlocker));
            m_InputField.onDeselect.AddListener(_ => InputManager.RemoveBlocker(_inputBlocker));
            m_InputField.onFocusSelectAll = false;
            SetHeaderActive(true);
            m_InputField.text = initText;
        });
    }

    private void PushText(string text, bool setFocus)
    {
        if (text.EndsWith("\n"))
        {
            text += "\r";
        }

        Dispatcher.Post(() =>
        {
            m_MainTextField.text = text;
            ClearInputField(setFocus);
        });
    }

    private void PushTextNotChangeFocus(string text)
    {
        if (text.EndsWith("\n"))
        {
            text += "\r";
        }

        Dispatcher.Post(() =>
        {
            m_MainTextField.text = text;
        });
    }

    private void ClearInputField(bool setFocus)
    {
        m_InputField.text = string.Empty;

        if (setFocus)
        {
            Dispatcher.Post(() =>
            {
                m_InputField.ActivateInputField();
            });
            return;
        }

        Dispatcher.Post(() =>
        {
            m_InputField.DeactivateInputField();
        });
    }

    private void SetHeaderActive(bool active)
    {
        float widthResult = active ? m_SpaceWidth : 0;
        Vector2 sizeDelta = new Vector2(widthResult, m_HeaderSpaceRect.sizeDelta.y);
        string text = active ? HEADER_TEXT : string.Empty;

        Dispatcher.Post(() =>
        {
            m_HeaderSpaceRect.sizeDelta = sizeDelta;
            m_InputHeader.text = text;
        });
    }

    private void Show()
    {
        Dispatcher.Post(() =>
        {
            m_CanvasGroup.DOKill();

            m_CanvasGroup.interactable = true;
            m_CanvasGroup.blocksRaycasts = true;

            m_CanvasGroup.DOFade(1f, m_FadeDuration);
        });
    }

    private void Hide()
    {
        Dispatcher.Post(() =>
        {
            m_CanvasGroup.DOKill();

            m_CanvasGroup.interactable = false;
            m_CanvasGroup.blocksRaycasts = false;

            m_CanvasGroup.DOFade(0f, m_FadeDuration);
        });
    }
    #endregion

    public interface ITextLine
    {
        public string Text { get; set; }
        public int LineIndex { get; }
        public event Action<string> OnTextUpdate;
        public event Action OnHide;
    }

    private class TextLine : ITextLine
    {
        public TextLine(Func<TextLine, int> lineIndexGetter)
        {
            _lineIndexGetter = lineIndexGetter;
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnTextUpdate?.Invoke(_text);
            }
        }

        public int LineIndex => _lineIndexGetter?.Invoke(this) ?? -1;

        public event Action<string> OnTextUpdate;
        public event Action OnHide;

        private string _text = null;
        private bool _initialized = false;
        private Func<TextLine, int> _lineIndexGetter;

        public void Initialize(string text)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _text = text;
        }

        public void Terminate()
        {
            if (!_initialized)
            {
                return;
            }

            _initialized = false;
            Text = null;
            OnTextUpdate = null;
            OnHide = null;
        }

        public void InvokeHide()
        {
            if (!_initialized)
            {
                return;
            }

            OnHide?.Invoke();
        }

        public override string ToString()
        {
            return Text;
        }
    }

    private class Text
    {
        private readonly int _maxLineCount;
        private readonly Pool<TextLine> _pool;
        private readonly Queue<TextLine> _textQueue;

        public Text(int maxLineCount)
        {
            _maxLineCount = maxLineCount;
            _textQueue = new Queue<TextLine>();
            _pool = new Pool<TextLine>(
                createFunc: () => new TextLine(GetLineIndex),
                initSize: 1,
                maxSize: maxLineCount + 1,
                actionOnRelease: line =>
                {
                    line.InvokeHide();
                    line.Terminate();
                },
                actionOnDestroy: line =>
                {
                    line.InvokeHide();
                    line.Terminate();
                });
        }

        public TextLine Append(string text)
        {
            TextLine newTextLine = _pool.Get();
            newTextLine.Initialize(text);
            _textQueue.Enqueue(newTextLine);

            if (_textQueue.Count > _maxLineCount)
            {
                _pool.Release(_textQueue.Dequeue());
            }

            return newTextLine;
        }

        public void Clear()
        {
            while (_textQueue.TryDequeue(out TextLine line))
            {
                _pool.Release(line);
            }
        }

        public override string ToString()
        {
            return string.Join('\n', _textQueue);
        }

        private int GetLineIndex(TextLine line)
        {
            return _textQueue.ToList().IndexOf(line);
        }
    }
}

/// <summary>
/// 콘솔 커맨드 클래스
/// </summary>
public class ConsoleCommand: IStartQuery
{
    public readonly struct CommandContext
    {
        #region Privates
        public CommandContext(Func<string, UniTask<QueryResult?>> queryFunc, Action<string> printAction, Func<string, ConsoleWindow.ITextLine> textLineGetter, Dictionary<string, string> args, ConsoleInputSource initSource, CommandContextDisposer disposer)
        {
            if (queryFunc == null || printAction == null || textLineGetter == null || args == null || disposer == null)
            {
                throw new ArgumentNullException($"{nameof(CommandContext)}: Param is Null");
            }
            _queryFunc = queryFunc;
            _printAction = printAction;
            _textLineGetter = textLineGetter;
            _args = args;
            InitSource = initSource;
            _textUpdateTokens = new List<TextUpdateToken>();
            disposer.OnDispose += Dispose;
        }

        private readonly Func<string, UniTask<QueryResult?>> _queryFunc;
        private readonly Action<string> _printAction;
        private readonly Func<string, ConsoleWindow.ITextLine> _textLineGetter;
        private readonly Dictionary<string, string> _args;
        private readonly List<TextUpdateToken> _textUpdateTokens;

        private void Dispose()
        {
            foreach (TextUpdateToken token in _textUpdateTokens)
            {
                token.Dispose();
            }

            _textUpdateTokens.Clear();
        }
        #endregion

        #region Interface
        /// <summary>
        /// 사용자의 최초 커맨드 입력 소스
        /// </summary>
        public ConsoleInputSource InitSource { get; }

        /// <summary>
        /// 사용자가 입력한 커맨드 뒷쪽 Arguments Get
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>해당하는 Argument</returns>
        public string GetArg(string key)
        {
            return _args.GetValueOrDefault(key);
        }

        /// <summary>
        /// 사용자에게 쿼리
        /// </summary>
        /// <param name="ask">쿼리 문장</param>
        /// <returns>쿼리 결과를 가져오는 UniTask. Result가 null 반환 시 쿼리가 강제 종료된 상황이므로 즉시 return 필요</returns>
        public UniTask<QueryResult?> Query(string ask)
        {
            return _queryFunc(ask);
        }

        /// <summary>
        /// 콘솔에 Print
        /// </summary>
        /// <param name="text">Print할 문자열</param>
        public void Print(string text)
        {
            _printAction(text);
        }

        /// <summary>
        /// 동일 라인을 지속적으로 업데이트할 수 있는 토큰을 발급
        /// </summary>
        /// <param name="initText">최초로 보여질 문자열</param>
        /// <returns>문자열 업데이트 토큰</returns>
        public TextUpdateToken GetUpdateToken(string initText)
        {
            TextUpdateToken token = new TextUpdateToken(_textLineGetter(initText));
            _textUpdateTokens.Add(token);
            return token;
        }
        #endregion
    }

    #region Privates
    UniTask<string> IStartQuery.StartQuery(CommandContext context) => QueryProcess(context);
    #endregion

    #region Interface
    /// <summary>
    /// 커맨드 생성자
    /// </summary>
    /// <param name="command">쿼리 호출을 위한 커멘드</param>
    /// <param name="queryProcess">커맨드 실행 로직을 정의하는 함수</param>
    /// <param name="doc">해당 커멘드의 Document</param>
    /// <param name="args">커멘드 뒷쪽에 올 수 있는 Arguments</param>
    /// <param name="isSystem">시스템 소속: 삭제 불가</param>
    /// <exception cref="ArgumentNullException">queryProcess가 null일 때 발생</exception>
    /// <exception cref="ArgumentException">args에 중복이 존재할 때 발생</exception>
    public ConsoleCommand(
        string command, 
        Func<CommandContext, UniTask<string>> queryProcess,
        string doc,
        string[] args = null,
        bool isSystem = false)
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

    /// <summary>
    /// 커멘드 뒷쪽에 올 수 있는 Arguments
    /// </summary>
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
    #endregion
}

public struct QueryResult
{
    #region Privates
    public QueryResult(string text, ConsoleInputSource inputSource)
    {
        Text = text ?? string.Empty;
        InputSource = inputSource;
    }
    #endregion

    #region Interface
    /// <summary>
    /// 쿼리 결과: 사용자가 입력한 텍스트
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// 해당 쿼리에 사용된 입력 소스
    /// </summary>
    public ConsoleInputSource InputSource { get; }
    #endregion
}

public enum ConsoleInputSource
{
    /// <summary>
    /// 사용자가 입력 필드에 직접 입력
    /// </summary>
    InputField,

    /// <summary>
    /// ConsoleWindow.Input()을 통한 시스템 입력
    /// </summary>
    System
}

public class TextUpdateToken : IDisposable
{
    #region Privates
    public TextUpdateToken(ConsoleWindow.ITextLine textLine)
    {
        _textLine = textLine;
        _textLine.OnHide += OnHideHandler;
    }

    private void OnHideHandler()
    {
        if (_invokeOnHideHandler)
        {
            return;
        }
        _invokeOnHideHandler = true;

        _textLine.OnHide -= OnHideHandler;
        IsExpired = true;
        _textLine = null;
        OnExpired?.Invoke();
    }

    private ConsoleWindow.ITextLine _textLine;

    private bool _invokeOnHideHandler = false;
    #endregion

    #region Interface
    /// <summary>
    /// 현재 라인 텍스트
    /// </summary>
    public string Text => IsExpired ? null : _textLine.Text;

    /// <summary>
    /// 현재 라인 인덱스
    /// </summary>
    public int LineIndex => IsExpired ? -1 : _textLine.LineIndex;

    /// <summary>
    /// 라인 만료 이벤트
    /// </summary>
    public event Action OnExpired;

    /// <summary>
    /// 라인 만료 여부
    /// </summary>
    public bool IsExpired { get; private set; }

    /// <summary>
    /// 라인 텍스트 업데이트
    /// </summary>
    /// <param name="text">Text value</param>
    /// <returns>라인 민료 시 false</returns>
    public bool TryUpdate(string text)
    {
        if (IsExpired)
        {
            return false;
        }

        _textLine.Text = text;
        return true;
    }

    /// <summary>
    /// 명시적 Dispose
    /// 토큰 만료로 처리
    /// </summary>
    public void Dispose()
    {
        OnHideHandler();
    }
    #endregion

}

public interface IStartQuery
{
    UniTask<string> StartQuery(CommandContext context);
}

public class CommandContextDisposer : IDisposable
{
    public event Action OnDispose;

    public void Dispose()
    {
        OnDispose?.Invoke();
        OnDispose = null;
    }
}