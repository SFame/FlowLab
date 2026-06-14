using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NCalc;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.Profiling;
using Expression = NCalc.Expression;
#if !UNITY_EDITOR
using UnityEngine;
#endif

public class ConsoleDefaultCommandInjector
{
    private static bool _isInjected = false;
    private const string BAR_STRING = "========================";
    private const string CALC_DOC = @"
Evaluates the entered expression.

=== Mathematical Functions ===
Abs(n): Returns the absolute value
Acos(n): Returns the arc cosine (in radians)
Asin(n): Returns the arc sine (in radians)
Atan(n): Returns the arc tangent (in radians)
Atan2(y, x): Returns the angle from the ratio of two numbers (in radians)
Ceiling(n): Returns the smallest integer greater than or equal to n
Cos(n): Returns the cosine (in radians)
Exp(n): Returns e raised to the specified power
Floor(n): Returns the largest integer less than or equal to n
IEEERemainder(x, y): Returns the remainder of x divided by y
Ln(n): Returns the natural logarithm (base e)
Log(n, base): Returns the logarithm with specified base
Log10(n): Returns the base 10 logarithm
Max(a, b): Returns the larger of two values
Min(a, b): Returns the smaller of two values
Pow(x, y): Returns x raised to the power of y
Round(n, digits): Rounds to the specified number of decimal places
Sign(n): Returns the sign (-1, 0, or 1)
Sin(n): Returns the sine (in radians)
Sqrt(n): Returns the square root
Tan(n): Returns the tangent (in radians)
Truncate(n): Returns the integral part (truncates decimals)

=== General Functions ===
in(value, v1, v2, ...): Checks if value is in the list
if(condition, trueVal, falseVal): Returns value based on condition
ifs(cond1, val1, cond2, val2, ..., default): Evaluates multiple conditions

=== Logical Operators ===
and, &&: Logical AND
or, ||: Logical OR
not, !: Logical NOT

=== Comparison Operators ===
=, ==: Equal
!=, <>: Not equal
<, <=, >, >=: Less / greater than (or equal)

=== Pattern Matching ===
in: Checks if value is in a collection or substring of a string
like: SQL-style pattern matching (% for any chars, _ for one char)
not in, not like: Negated forms

=== Arithmetic ===
+, -, *, /, %: Basic arithmetic
**: Exponentiation";

    private static readonly List<ConsoleCommand> _defaultCommands = new List<ConsoleCommand>()
    {
        new ConsoleCommand
        (
            command: "/flowlab",
            doc: "About FlowLab.",
            isSystem: true,
            queryProcess: async context =>
            {
                // 박스 안에 들어갈 로고 본문 (이 줄들끼리 길이 자동 정렬)
                string[] body =
                {
                    "  ███████ ██      ██████  ██     ██ ██       █████  ██████ ",
                    "  ██      ██     ██    ██ ██     ██ ██      ██   ██ ██   ██",
                    "  █████   ██     ██    ██ ██  █  ██ ██      ███████ ██████ ",
                    "  ██      ██     ██    ██ ██ ███ ██ ██      ██   ██ ██   ██",
                    "  ██      ███████ ██████   ███ ███  ███████ ██   ██ ██████ ",
                };

                // 가장 긴 줄 기준으로 폭 통일
                int innerWidth = 0;
                foreach (string s in body)
                {
                    if (s.Length > innerWidth) innerWidth = s.Length;
                }
                for (int i = 0; i < body.Length; i++)
                {
                    body[i] = body[i].PadRight(innerWidth);
                }

                string horizontal = new string('─', innerWidth);
                int portRow1 = 1;                    // 위쪽 포트가 붙을 본문 줄
                int portRow2 = body.Length - 2;      // 아래쪽 포트가 붙을 본문 줄

                // 전체 로고 조립
                List<string> lines = new List<string>
                {
                    "",
                    $"    ╭{horizontal}╮"
                };
                for (int i = 0; i < body.Length; i++)
                {
                    if (i == portRow1 || i == portRow2)
                    {
                        lines.Add($" ●──┤{body[i]}├──●");
                    }
                    else
                    {
                        lines.Add($"    │{body[i]}│");
                    }
                }
                lines.Add($"    ╰{horizontal}╯");
                lines.Add("");
                lines.Add("                https://github.com/SFame/FlowLab");
                lines.Add("");

                string[] logo = lines.ToArray();

                const string GLITCH_CHARS = "▓▒░#@%&";
                System.Random rng = new System.Random();

                int height = logo.Length;
                TextUpdateToken[] tokens = new TextUpdateToken[height];
                for (int i = 0; i < height; i++)
                {
                    tokens[i] = context.GetUpdateToken(logo[i], false);
                }

                // 블록 문자(█)만 골라서 글리치
                string Glitch(string line)
                {
                    char[] buf = line.ToCharArray();

                    // 이 줄의 █ 위치들 수집
                    List<int> blockPositions = new List<int>();
                    for (int i = 0; i < buf.Length; i++)
                    {
                        if (buf[i] == '█') blockPositions.Add(i);
                    }

                    if (blockPositions.Count == 0) return line;

                    // █ 중 일부를 깨진 문자로
                    int glitchCount = rng.Next(1, 3);
                    for (int g = 0; g < glitchCount; g++)
                    {
                        int pos = blockPositions[rng.Next(blockPositions.Count)];
                        buf[pos] = GLITCH_CHARS[rng.Next(GLITCH_CHARS.Length)];
                    }

                    return new string(buf);
                }

                async UniTask GlitchLoop(CancellationToken token)
                {
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            for (int i = 0; i < height; i++)
                            {
                                // 본문(█ 있는 줄)만 50% 확률로 글리치
                                string display = (logo[i].Contains('█') && rng.Next(100) < 15)
                                    ? Glitch(logo[i])
                                    : logo[i];

                                if (!tokens[i].TryUpdate(display))
                                {
                                    return;
                                }
                            }

                            await UniTask.Delay(120, cancellationToken: token);
                        }
                    }
                    catch (OperationCanceledException) { }
                }

                using CancellationTokenSource cts = new();
                UniTask glitchTask = GlitchLoop(cts.Token);

                while (true)
                {
                    QueryResult? result = await context.Query("q to quit...");

                    if (result == null)
                    {
                        break;
                    }

                    if (result.Value.Text.Trim().ToLower() == "q")
                    {
                        break;
                    }
                }

                cts.Cancel();
                await glitchTask;

                for (int i = 0; i < height; i++)
                {
                    tokens[i].TryUpdate(logo[i]);
                }

                return null;
            }
        ),
        new ConsoleCommand
        (
            command: "/help",
            doc: "Help about help. Very meta.",
            possibleArgs: new string[][] { Array.Empty<string>(), new[] { "command" }, },
            isSystem: true,
            queryProcess: async context =>
            {
                ConsoleCommand[] commands = ConsoleWindow.GetCommands();

                if (context.ArgsIndex == 0)
                {
                    string commandsNames = string.Join('\n', commands.Select(command => command.Command).ToArray());
                    QueryResult? result = await context.Query($"<Select Command>\n{BAR_STRING}\n{commandsNames}\n{BAR_STRING}");

                    if (result == null)
                    {
                        return null;
                    }

                    return GetCommandDoc(result.Value.Text);
                }

                return GetCommandDoc(context.GetArg("command"));

                string GetCommandDoc(string command)
                {
                    string resultText = command.StartsWith("/") ? command : $"/{command}";
                    if (commands.FirstOrDefault(command => command.Command == resultText) is { } commandResult)
                    {
                        string formatString;

                        if (commandResult.PossibleArgs == null || commandResult.PossibleArgs.Length == 0)
                        {
                            formatString = $"\"{commandResult.Command}\"";
                        }
                        else
                        {
                            formatString = string.Join('\n', commandResult.PossibleArgs.Select(overload =>
                            {
                                string argsPart = overload.Length == 0
                                    ? string.Empty
                                    : $" {string.Join(' ', overload.Select(arg => $"<{arg}>"))}";
                                return $"\"{commandResult.Command}{argsPart}\"";
                            }));
                        }

                        return $"{BAR_STRING}\n<{commandResult.Command}>\nDoc: {commandResult.Doc}\nFormat:\n{formatString}\n{BAR_STRING}";
                    }

                    return $"Command not found: {resultText}";
                }
            }
        ),
        new ConsoleCommand
        (
            command: "/clear",
            doc: "Clear console output.",
            isSystem: true,
            queryProcess: context =>
            {
                ConsoleWindow.Clear(context.InitSource == ConsoleInputSource.InputField);
                return UniTask.FromResult<string>(null);
            }
        ),
        new ConsoleCommand
        (
            command: "/echo",
            doc: "Echo back the given text.",
            possibleArgs: new string[][] { new[] { "text" }, },
            isSystem: true,
            queryProcess: context => UniTask.FromResult(context.GetArg("text"))),
        new ConsoleCommand
        (
            command: "/open",
            doc: "Open console window.",
            isSystem: true,
            queryProcess: _ =>
            {
                ConsoleWindow.IsOpen = true;
                return UniTask.FromResult<string>(null);
            }
        ),
        new ConsoleCommand
        (
            command: "/close",
            doc: "Close console window.",
            isSystem: true,
            queryProcess: _ =>
            {
                ConsoleWindow.IsOpen = false;
                return UniTask.FromResult<string>(null);
            }
        ),
        new ConsoleCommand
        (
            command: "/exit",
            doc: "Quit the application.",
            isSystem: true,
            queryProcess: async context =>
            {
                QueryResult? result = await context.Query("Confirm exit? (y/n)");

                if (result == null)
                {
                    return null;
                }

                if (result.Value.Text.ToLower() == "y")
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.ExitPlaymode();
#else
                    Application.Quit();
#endif
                }

                return null;
            }
        ),
        new ConsoleCommand
        (
            command: "/calc",
            doc: "Evaluates a math expression. Run without args for interactive mode.",
            isSystem: true,
            possibleArgs: new string[][] { new[] { "expression" }, Array.Empty<string>() },
            queryProcess: async context =>
            {
                string Eval(string expr)
                {
                    try
                    {
                        Expression exp = new Expression(expr, ExpressionOptions.IgnoreCaseAtBuiltInFunctions);
                        return exp.Evaluate().ToString();
                    }
                    catch (EvaluationException ee)
                    {
                        return $"Invalid Expression: {ee.Message}";
                    }
                    catch
                    {
                        return "Invalid Expression";
                    }
                }

                if (context.GetArg("expression") != null)
                {
                    return Eval(context.GetArg("expression"));
                }

                context.Print(CALC_DOC);

                while (true)
                {
                    QueryResult? result = await context.Query("calc> (q to quit): ");

                    if (result == null)
                    {
                        return null;
                    }

                    string input = result.Value.Text.Trim();

                    if (input.ToLower() == "q")
                    {
                        return null;
                    }

                    if (input.Length == 0)
                    {
                        continue;
                    }

                    context.Print($"{Eval(input)}\n");
                }
            }
        ),
        new ConsoleCommand
        (
            command: "/mem",
            doc: "Display current memory usage.",
            isSystem: true,
            possibleArgs: new string[][] { new[] { "interval" }, Array.Empty<string>() },
            queryProcess: async context =>
            {
                void GetMemoryInformation(Action<string>[] returnFunc)
                {
                    long monoUsed = GC.GetTotalMemory(false);
                    long totalReserved = Profiler.GetTotalReservedMemoryLong();
                    long totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
                    long totalUnused = Profiler.GetTotalUnusedReservedMemoryLong();

                    string Format(long bytes) => $"{bytes / 1024f / 1024f:F2} MB";

                    returnFunc[0]("=== Memory Usage ===");
                    returnFunc[1]($"Mono Heap (GC):   {Format(monoUsed)}");
                    returnFunc[2]($"Total Allocated:  {Format(totalAllocated)}");
                    returnFunc[3]($"Total Reserved:   {Format(totalReserved)}");
                    returnFunc[4]($"Unused Reserved:  {Format(totalUnused)}");
                    returnFunc[5]($"System RAM:       {SystemInfo.systemMemorySize} MB");
                    returnFunc[6]($"GFX RAM:          {SystemInfo.graphicsMemorySize} MB");
                }

                async UniTask PerformanceUpdate(float interval, CancellationToken token)
                {
                    TextUpdateToken[] tokens = new TextUpdateToken[7];
                    bool endFlag = false;

                    for (int i = 0; i < tokens.Length; i++)
                    {
                        tokens[i] = context.GetUpdateToken(string.Empty, false);
                    }

                    Action<string>[] tokenReturnAction = tokens.Select(tk =>
                    {
                        void action(string s)
                        {
                            if (endFlag)
                            {
                                return;
                            }

                            if (!tk.TryUpdate(s))
                            {
                                endFlag = true;
                            }
                        }

                        return (Action<string>)action;
                    }).ToArray();

                    try
                    {
                        while (!token.IsCancellationRequested && !endFlag)
                        {
                            GetMemoryInformation(tokenReturnAction);
                            await UniTask.WaitForSeconds(interval, cancellationToken: token, cancelImmediately: true);
                        }
                    }
                    catch (OperationCanceledException) { }
                }

                if (context.ArgsIndex == 1)
                {
                    string[] array = new string[7];
                    Action<string>[] returnAction = array.Select((_, idx) => new Action<string>(str => array[idx] = str)).ToArray();
                    GetMemoryInformation(returnAction);
                    return string.Join('\n', array);
                }

                if (!float.TryParse(context.GetArg("interval"), out float interval))
                {
                    return "Invalid interval. Please enter a number (seconds).";
                }

                using CancellationTokenSource cts = new();

                UniTask updateTask = PerformanceUpdate(Math.Clamp(interval, 0.1f, 60f), cts.Token);

                while (true)
                {
                    QueryResult? result = await context.Query("q to quit...");

                    if (result == null || result.Value.Text.ToLower().Trim() == "q")
                    {
                        cts.Cancel();
                        await updateTask;
                        return null;
                    }
                }
            }
        ),
        new ConsoleCommand
        (
            command: "/fps",
            doc: "Display current FPS.",
            isSystem: true,
            possibleArgs: new string[][] { new[] { "interval" }, Array.Empty<string>() },
            queryProcess: async context =>
            {
                float smoothedFps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);

                string GetFpsInfo()
                {
                    float current = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
                    smoothedFps = Mathf.Lerp(smoothedFps, current, 0.1f);
                    float frameMs = Time.unscaledDeltaTime * 1000f;
                    return $"FPS: {smoothedFps:F1}  ({frameMs:F1} ms)";
                }

                if (context.ArgsIndex == 1)
                {
                    return GetFpsInfo();
                }

                if (!float.TryParse(context.GetArg("interval"), out float interval))
                {
                    return "Invalid interval. Please enter a number (seconds).";
                }

                async UniTask FpsUpdate(float updateInterval, CancellationToken token)
                {
                    TextUpdateToken fpsToken = context.GetUpdateToken(string.Empty, false);

                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            if (!fpsToken.TryUpdate(GetFpsInfo()))
                            {
                                return;
                            }

                            await UniTask.WaitForSeconds(updateInterval, cancellationToken: token, cancelImmediately: true);
                        }
                    }
                    catch (OperationCanceledException) { }
                }

                using CancellationTokenSource cts = new();

                UniTask updateTask = FpsUpdate(Math.Clamp(interval, 0.1f, 60f), cts.Token);

                while (true)
                {
                    QueryResult? result = await context.Query("q to quit...");

                    if (result == null || result.Value.Text.ToLower().Trim() == "q")
                    {
                        cts.Cancel();
                        await updateTask;
                        return null;
                    }
                }
            }
        ),
        new ConsoleCommand
        (
            command: "/log",
            doc: "Debug logger.",
            isSystem: true,
            possibleArgs: new string[][] { new[] { "type", "text" }, new[] { "text" } },
            queryProcess: context =>
            {
                string text = context.GetArg("text");

                if (context.ArgsIndex == 0)
                {
                    string type = context.GetArg("type").ToLower().Trim();

                    if (type == "e" || type == "error")
                    {
                        Debug.LogError(text);
                        return UniTask.FromResult<string>(null);
                    }

                    if (type == "w" || type == "warning")
                    {
                        Debug.LogWarning(text);
                        return UniTask.FromResult<string>(null);
                    }

                    return UniTask.FromResult($"Unknown args: {type}");
                }

                Debug.Log(text);
                return UniTask.FromResult<string>(null);
            }
        ),
        new ConsoleCommand
        (
            command: "/apple",
            doc: "Play Bad Apple!! in ASCII.",
            isSystem: true,
            queryProcess: async context =>
            {
                const int FRAME_HEIGHT = 27;
                const float FPS = 15f;
                const char FRAME_SEP = '\x1e';
                const string RESOURCE_PATH = "badapple";

                TextAsset asset = Resources.Load<TextAsset>(RESOURCE_PATH);
                if (asset == null)
                {
                    return "ERROR: badapple resource not found.";
                }

                string[] frames = asset.text.Split(FRAME_SEP);
                if (frames.Length == 0)
                {
                    return "ERROR: no frames.";
                }

                TextUpdateToken[] rows = new TextUpdateToken[FRAME_HEIGHT];
                for (int y = 0; y < FRAME_HEIGHT; y++)
                {
                    rows[y] = context.GetUpdateToken(string.Empty, false);
                }

                string[] lineBuffer = new string[FRAME_HEIGHT];

                using CancellationTokenSource cts = new();

                async UniTask PlayLoop(CancellationToken token)
                {
                    try
                    {
                        float startTime = Time.realtimeSinceStartup;

                        for (int f = 0; f < frames.Length; f++)
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }

                            float elapsed = Time.realtimeSinceStartup - startTime;
                            int targetFrame = (int)(elapsed * FPS);
                            if (targetFrame > f && targetFrame < frames.Length)
                            {
                                f = targetFrame;
                            }

                            string frame = frames[f];
                            int start = 0;
                            for (int y = 0; y < FRAME_HEIGHT; y++)
                            {
                                int nl = frame.IndexOf('\n', start);
                                if (nl < 0)
                                {
                                    lineBuffer[y] = frame.Substring(start);
                                    start = frame.Length;
                                }
                                else
                                {
                                    lineBuffer[y] = frame.Substring(start, nl - start);
                                    start = nl + 1;
                                }
                            }

                            for (int y = 0; y < FRAME_HEIGHT; y++)
                            {
                                if (!rows[y].TryUpdate(lineBuffer[y]))
                                {
                                    return;
                                }
                            }

                            float nextFrameTime = startTime + (f + 1) / FPS;
                            float wait = nextFrameTime - Time.realtimeSinceStartup;
                            if (wait > 0)
                            {
                                await UniTask.Delay((int)(wait * 1000), cancellationToken: token);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                // q 입력 감시: q 들어오면 재생 취소
                async UniTask WaitQuit(CancellationToken token)
                {
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            QueryResult? r = await context.Query("q to quit", token);

                            if (r == null)
                            {
                                return;
                            }

                            if (r.Value.Text.Trim().ToLower() == "q")
                            {
                                cts.Cancel();
                                return;
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                }

                UniTask playTask = PlayLoop(cts.Token);
                UniTask quitTask = WaitQuit(cts.Token);

                // 재생 완료를 기다림 (q로 취소되면 여기가 먼저 풀림)
                await playTask;

                // 재생이 끝났든 q로 끊겼든, 남은 쪽 정리
                cts.Cancel();
                await quitTask;

                return null;
            }
        ),
    };

    public static void Inject()
    {
        if (_isInjected)
        {
            return;
        }
        _isInjected = true;

        foreach (ConsoleCommand defaultCommand in _defaultCommands)
        {
            ConsoleWindow.AddCommand(defaultCommand);
        }
    }
}