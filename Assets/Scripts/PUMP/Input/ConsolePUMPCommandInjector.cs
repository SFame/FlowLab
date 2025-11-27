using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ConsolePUMPCommandInjector : MonoBehaviour
{
    private bool _isInjected;
    private List<ConsoleCommand> _commands = new()
    {
        new ConsoleCommand
        (
            command: "/undo",
            queryProcess: async _ =>
            {
                BackgroundActionMapper.GetAction(BackgroundActionType.Undo)();
                return null;
            },
            doc: "Revert the most recent change.",
            isSystem: false
        ),
        new ConsoleCommand
        (
            command: "/redo",
            queryProcess: async _ =>
            {
                BackgroundActionMapper.GetAction(BackgroundActionType.Redo)();
                return null;
            },
            doc: "Reapply the most recently undone change.",
            isSystem: false
        ),
        new ConsoleCommand
        (
            command: "/save",
            queryProcess: async context =>
            {
                QueryResult? result = await context.Query("Enter save name: ");
                if (result == null)
                {
                    return null;
                }

                PUMPBackground current = PUMPBackground.Current;
                if (current == null)
                {
                    return "Save failed: No active panel exists.";
                }

                try
                {
                    context.Print("Saving...");
                    string saveName = result.Value.Text;
                    await current.ComponentGetter.PumpSaveLoadPanel.AddNewSave(saveName);

                    return $"Successfully saved as '{saveName}'.";
                }
                catch (Exception e)
                {
                    return $"Save failed: {e.Message}";
                }
            },
            doc: "Save the current panel with a name.",
            isSystem: false
        ),
        new ConsoleCommand
        (
            command: "/nodes",
            queryProcess: async _ =>
            {
                Node[] nodes = PUMPBackground.Current?.AllNodes;

                if (nodes == null)
                {
                    return null;
                }

                List<IGrouping<Type, Node>> groups = nodes.GroupBy(node => node.GetType()).ToList();
                StringBuilder sb = new();
                sb.AppendLine($"===== Nodes ({nodes.Length}) =====");

                foreach (IGrouping<Type, Node> group in groups)
                {
                    sb.AppendLine($"{group.Key.Name}: {group.Count()}");
                }

                sb.Append("===========================");
                return sb.ToString();
            },
            doc: "Display node count.",
            isSystem: false
        ),
        new ConsoleCommand
        (
            command: "/reset",
            queryProcess: async context =>
            {
                QueryResult? result = await context.Query("Confirm reset? (y/n)");
                if (result == null)
                {
                    return null;
                }

                string resultText = result.Value.Text.ToLower();

                if (resultText == "y")
                {
                    PUMPBackground.Current?.ResetBackground();
                    return "Panel reset";
                }

                return "Canceled";
            },
            doc: "Reset the panel. Removes all nodes and clears history.",
            isSystem: false
        ),
    };

    private void Inject()
    {
        if (_isInjected)
        {
            return;
        }
        _isInjected = true;

        foreach (ConsoleCommand defaultCommand in _commands)
        {
            ConsoleWindow.AddCommand(defaultCommand);
        }
    }

    private void Start() => Inject();
}
