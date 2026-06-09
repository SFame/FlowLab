using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NCalc;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using Expression = NCalc.Expression;
#if !UNITY_EDITOR
using UnityEngine;
#endif

public class ConsoleDefaultCommandInjector
{
    private static bool _isInjected = false;
    private const string BAR_STRING = "========================";
    private static readonly List<ConsoleCommand> _defaultCommands = new List<ConsoleCommand>()
    {
        new ConsoleCommand
        (
            command: "/help",
            doc: "Help about help. Very meta.",
            isSystem: true,
            queryProcess: async context =>
            {
                ConsoleCommand[] commands = ConsoleWindow.GetCommands();
                string commandsNames = string.Join('\n', commands.Select(command => command.Command).ToArray());
                QueryResult? result = await context.Query($"<Select Command>\n{BAR_STRING}\n{commandsNames}\n{BAR_STRING}");

                if (result == null)
                {
                    return null;
                }

                string resultText = result.Value.Text.StartsWith("/") ? result.Value.Text : $"/{result.Value.Text}";
                if (commands.FirstOrDefault(command => command.Command == resultText) is { } commandResult)
                {
                    string argsString = commandResult.Args == null || commandResult.Args.Length == 0
                        ? string.Empty
                        : $" {string.Join(' ', commandResult.Args.Select(arg => $"<{arg}>").ToArray())}";
                    return $"{BAR_STRING}\n<{commandResult.Command}>\nDoc: {commandResult.Doc}\nFormat: \"{commandResult.Command}{argsString}\"\n{BAR_STRING}";
                }
                
                return $"Command not found: {resultText}";
            }
        ),
        new ConsoleCommand
        (
            command: "/clear",
            doc: "Clear console output.",
            isSystem: true,
            queryProcess: async context =>
            {
                ConsoleWindow.Clear(context.InitSource == ConsoleInputSource.InputField);
                return null;
            }
        ),
        new ConsoleCommand
        (
            command: "/echo",
            doc: "Echo back the given text.",
            args: new[] { "text" },
            isSystem: true,
            queryProcess: async context => context.GetArg("text")),
        new ConsoleCommand
        (
            command: "/open",
            doc: "Open console window.",
            isSystem: true,
            queryProcess: async _ =>
            {
                ConsoleWindow.IsOpen = true;
                return null;
            }
        ),
        new ConsoleCommand
        (
            command: "/close",
            doc: "Close console window.",
            isSystem: true,
            queryProcess: async _ =>
            {
                ConsoleWindow.IsOpen = false;
                return null;
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
            doc: @"
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
**: Exponentiation",
            isSystem: true,
            args: new []{ "expression" },
            queryProcess: async context =>
            {
                try
                {
                    Expression exp = new Expression(context.GetArg("expression"), ExpressionOptions.IgnoreCaseAtBuiltInFunctions);
                    object result = exp.Evaluate();
                    return result.ToString();
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
        ),
        new ConsoleCommand
        (
            command: "/badapple",
            doc: "Play Bad Apple!! in ASCII. Press Q to stop.",
            isSystem: true,
            queryProcess: async context =>
            {
                const int FRAME_HEIGHT = 27;
                const float FPS = 15f;
                const char FRAME_SEP = '\x1e';
                const string RESOURCE_PATH = "badapple"; // Resources 기준 경로, 확장자 제외

                TextAsset asset = Resources.Load<TextAsset>(RESOURCE_PATH);
                if (asset == null)
                {
                    return "ERROR: badapple resource not found.";
                }

                // 프레임 미리 분할 (한 번만)
                string[] frames = asset.text.Split(FRAME_SEP);
                if (frames.Length == 0)
                {
                    return "ERROR: no frames.";
                }

                // 45줄 토큰 발급
                TextUpdateToken[] rows = new TextUpdateToken[FRAME_HEIGHT];
                for (int y = 0; y < FRAME_HEIGHT; y++)
                {
                    rows[y] = context.GetUpdateToken(string.Empty);
                }

                int frameDelayMs = (int)(1000f / FPS);
                string[] lineBuffer = new string[FRAME_HEIGHT];

                // 시간 기반 재생 (처리 지연 보정해서 드리프트 방지)
                float startTime = Time.realtimeSinceStartup;

                for (int f = 0; f < frames.Length; f++)
                {
                    // 현재 시각에 맞는 프레임으로 스킵 (느린 환경에서 프레임 드랍)
                    float elapsed = Time.realtimeSinceStartup - startTime;
                    int targetFrame = (int)(elapsed * FPS);
                    if (targetFrame > f && targetFrame < frames.Length)
                    {
                        f = targetFrame; // 밀린 만큼 건너뜀
                    }

                    // 프레임을 줄 단위로 분할
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

                    // 각 줄 토큰 갱신 (하나라도 만료=취소면 중단)
                    for (int y = 0; y < FRAME_HEIGHT; y++)
                    {
                        if (!rows[y].TryUpdate(lineBuffer[y]))
                        {
                            return null;
                        }
                    }

                    // 다음 프레임 시각까지 대기
                    float nextFrameTime = startTime + (f + 1) / FPS;
                    float wait = nextFrameTime - Time.realtimeSinceStartup;
                    if (wait > 0)
                    {
                        await UniTask.Delay((int)(wait * 1000));
                    }
                }

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