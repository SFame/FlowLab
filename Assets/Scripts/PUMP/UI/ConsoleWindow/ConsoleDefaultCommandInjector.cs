using System;
using System.Collections.Generic;
using System.Linq;
using NCalc;
using UnityEngine;

public class ConsoleDefaultCommandInjector
{
    private static bool _isInjected = false;
    private const string BAR_STRING = "========================";
    private static List<ConsoleCommand> _deafultCommands = new List<ConsoleCommand>()
    {
        new ConsoleCommand
        (
            command: "/help",
            doc: "Help about help. Very meta.",
            isSystem: true,
            queryProcess: async context =>
            {
                ConsoleCommand[] commands = ConsoleWindow.GetCommands();
                string commandsNames = string.Join("\n", commands.Select(command => command.Command).ToArray());
                InputElement? result = await context.Query($"<Select Command>\n{BAR_STRING}\n{commandsNames}\n{BAR_STRING}");

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
                InputElement? result = await context.Query("Confirm exit? (y/n)");

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
            doc: @"Evaluates the entered expression.

=== Mathematical Functions ===
Abs(n): Returns the absolute value
Acos(n): Returns the arc cosine (in radians)
Asin(n): Returns the arc sine (in radians)
Atan(n): Returns the arc tangent (in radians)
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
ifs(cond1, val1, cond2, val2, ..., default): Evaluates multiple conditions",
            isSystem: true,
            args: new []{ "expression" },
            queryProcess: async context =>
            {
                try
                {
                    Expression exp = new Expression(context.GetArg("expression"));
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
    };

    public static void Inject()
    {
        if (_isInjected)
        {
            return;
        }
        _isInjected = true;

        foreach (ConsoleCommand defaultCommand in _deafultCommands)
        {
            ConsoleWindow.AddCommand(defaultCommand);
        }
    }
}