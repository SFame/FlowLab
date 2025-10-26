using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

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
                string result = await context.Query($"<Select Command>\n{BAR_STRING}\n{commandsNames}\n{BAR_STRING}");
                result = result.StartsWith("/") ? result : $"/{result}";
                if (commands.FirstOrDefault(command => command.Command == result) is { } commandResult)
                {
                    string argsString = commandResult.Args == null || commandResult.Args.Length == 0
                        ? string.Empty
                        : $" {string.Join(' ', commandResult.Args.Select(arg => $"<{arg}>").ToArray())}";
                    return $"{BAR_STRING}\n<{commandResult.Command}>\nDoc: {commandResult.Doc}\nFormat: \"{commandResult.Command}{argsString}\"\n{BAR_STRING}";
                }
                
                return $"Command not found: {result}";
            }
        ),
        new ConsoleCommand
        (
            command: "/clear",
            doc: "Clear console output.",
            isSystem: true,
            queryProcess: async _ =>
            {
                async UniTaskVoid clearAsync()
                {
                    await UniTask.Yield();
                    ConsoleWindow.Clear();
                }

                clearAsync().Forget();
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
                string result = await context.Query("Confirm exit? (y/n)");
                if (result.ToLower() == "y")
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
            command: "/test",
            doc: "Close console window.",
            isSystem: true,
            args: new [] { "A", "B" },
            queryProcess: async context =>
            {
                return $"{context.GetArg("A")}, {context.GetArg("B")}";
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