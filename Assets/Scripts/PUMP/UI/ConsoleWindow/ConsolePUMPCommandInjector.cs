using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            queryProcess: _ =>
            {
                BackgroundActionMapper.GetAction(BackgroundActionType.Undo)();
                return UniTask.FromResult<string>(null);
            },
            doc: "Revert the most recent change.",
            isSystem: false
        ),
        new ConsoleCommand
        (
            command: "/redo",
            queryProcess: _ =>
            {
                BackgroundActionMapper.GetAction(BackgroundActionType.Redo)();
                return UniTask.FromResult<string>(null);
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
            command: "/load",
            queryProcess: async context =>
            {
                PUMPBackground currentBackground = PUMPBackground.Current;
                string savePath = currentBackground.ComponentGetter.PumpSaveLoadPanel.SavePath;
                DataDirectory targetDir = PUMPBackground.Current.ComponentGetter.PumpSaveLoadPanel.TargetDirectory;

                Task<List<PUMPSaveDataStructure>> saveDataTask = SerializeManagerCatalog.GetDatas<PUMPSaveDataStructure>(targetDir, savePath);

                string[] spinner = { "|", "/", "-", "\\" };
                TextUpdateToken token = context.GetUpdateToken(spinner[0]);

                int frame = 0;
                while (!saveDataTask.IsCompleted)
                {
                    if (!token.TryUpdate(spinner[frame % spinner.Length]))
                    {
                        return null;
                    }

                    frame++;
                    await UniTask.Delay(100);
                }

                if (!token.TryUpdate("Done"))
                {
                    return null;
                }

                List<PUMPSaveDataStructure> saveDatas = await saveDataTask;

                if (saveDatas.Count <= 0)
                {
                    return "No saved data.";
                }

                saveDatas.Reverse();

                if (currentBackground != PUMPBackground.Current)
                {
                    return "Workspace has changed. Please try again.";
                }

                UniTaskCompletionSource<QueryResult?> tcs = new UniTaskCompletionSource<QueryResult?>();

                Action<PUMPSaveDataStructure> updateHandler = _ =>
                {
                    tcs.TrySetResult(null);
                };

                foreach (PUMPSaveDataStructure data in saveDatas)
                {
                    data.SubscribeUpdateNotification(updateHandler);
                    data.SubscribeDeleteRequest(updateHandler);
                }

                CancellationTokenSource observerCts = new();
                
                async UniTask<QueryResult?> ObserveBackground(CancellationToken t)
                {
                    try
                    {
                        await UniTask.WaitWhile(() => currentBackground == PUMPBackground.Current, cancelImmediately: true, cancellationToken: t);
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    return null;
                }

                try
                {
                    string[] saveTitles = saveDatas.Select((structure, index) =>
                    {
                        string indexPart = $"{index}.".PadRight(4);
                        string namePart = structure.Name.PadRight(16);
                        string countPart = $"Node Count: {structure.NodeInfos.Count}".PadRight(18);
                        string datePart = structure.LastUpdate.ToString();
                        return $"{indexPart}{namePart}{countPart}{datePart}";
                    }).ToArray();

                    async UniTask<string> SelectionLoop()
                    {
                        while (true)
                        {
                            QueryResult? result = await context.Query($"{string.Join('\n', saveTitles)}\n\nSelect (q to quit): ");

                            if (result == null) return null;
                            if (result.Value.Text.Trim().ToLower() == "q") return null;

                            if (!int.TryParse(result.Value.Text.Trim(), out int idx))
                            {
                                context.Print("Invalid input. Please enter a number.");
                                continue;
                            }
                            if (idx < 0 || idx >= saveDatas.Count)
                            {
                                context.Print("Out of range. Please enter a valid number.");
                                continue;
                            }

                            PUMPSaveDataStructure selectedStructure = saveDatas[idx];
                            currentBackground.ComponentGetter.PumpSaveLoadPanel.LoadSave(selectedStructure);
                            return $"Loaded: {selectedStructure.Name}";
                        }
                    }

                    (int winIndex, QueryResult? result1, QueryResult? result2, string result3) tuple = await UniTask.WhenAny(tcs.Task, ObserveBackground(observerCts.Token), SelectionLoop());

                    if (tuple.winIndex == 0)
                    {
                        return "Save data has changed. Please try again.";
                    }

                    if (tuple.winIndex == 1)
                    {
                        return "Workspace has changed. Please try again.";
                    }

                    return tuple.result3;
                }
                finally
                {
                    observerCts.Cancel();
                    observerCts.Dispose();

                    foreach (PUMPSaveDataStructure data in saveDatas)
                    {
                        data.UnsubscribeUpdateNotification(updateHandler);
                        data.UnsubscribeDeleteRequest(updateHandler);
                    }
                }
            },
            doc: "Load a saved workspace.",
            isSystem: false
        ),
                new ConsoleCommand
        (
            command: "/delete",
            queryProcess: async context =>
            {
                PUMPBackground currentBackground = PUMPBackground.Current;
                string savePath = currentBackground.ComponentGetter.PumpSaveLoadPanel.SavePath;
                DataDirectory targetDir = PUMPBackground.Current.ComponentGetter.PumpSaveLoadPanel.TargetDirectory;

                Task<List<PUMPSaveDataStructure>> saveDataTask = SerializeManagerCatalog.GetDatas<PUMPSaveDataStructure>(targetDir, savePath);

                string[] spinner = { "|", "/", "-", "\\" };
                TextUpdateToken token = context.GetUpdateToken(spinner[0]);

                int frame = 0;
                while (!saveDataTask.IsCompleted)
                {
                    if (!token.TryUpdate(spinner[frame % spinner.Length]))
                    {
                        return null;
                    }

                    frame++;
                    await UniTask.Delay(100);
                }

                if (!token.TryUpdate("Done"))
                {
                    return null;
                }

                List<PUMPSaveDataStructure> saveDatas = await saveDataTask;

                if (saveDatas.Count <= 0)
                {
                    return "No saved data.";
                }

                saveDatas.Reverse();

                if (currentBackground != PUMPBackground.Current)
                {
                    return "Workspace has changed. Please try again.";
                }

                UniTaskCompletionSource<QueryResult?> tcs = new UniTaskCompletionSource<QueryResult?>();

                Action<PUMPSaveDataStructure> updateHandler = structure =>
                {
                    tcs.TrySetResult(null);
                };

                foreach (PUMPSaveDataStructure data in saveDatas)
                {
                    data.SubscribeUpdateNotification(updateHandler);
                    data.SubscribeDeleteRequest(updateHandler);
                }

                CancellationTokenSource observerCts = new();

                async UniTask<QueryResult?> ObserveBackground(CancellationToken t)
                {
                    try
                    {
                        await UniTask.WaitWhile(() => currentBackground == PUMPBackground.Current, cancelImmediately: true, cancellationToken: t);
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    return null;
                }

                try
                {
                    string[] saveTitles = saveDatas.Select((structure, index) =>
                    {
                        string indexPart = $"{index}.".PadRight(4);
                        string namePart = structure.Name.PadRight(16);
                        string countPart = $"Node Count: {structure.NodeInfos.Count}".PadRight(18);
                        string datePart = structure.LastUpdate.ToString();
                        return $"{indexPart}{namePart}{countPart}{datePart}";
                    }).ToArray();

                    async UniTask<int> SelectionLoop()
                    {
                        while (true)
                        {
                            QueryResult? result = await context.Query($"{string.Join('\n', saveTitles)}\n\nSelect (q to quit): ");

                            if (result == null)
                            {
                                return -1;
                            }

                            if (result.Value.Text.Trim().ToLower() == "q")
                            {
                                return -1;
                            }

                            if (!int.TryParse(result.Value.Text.Trim(), out int idx))
                            {
                                context.Print("Invalid input. Please enter a number.");
                                continue;
                            }
                            if (idx < 0 || idx >= saveDatas.Count)
                            {
                                context.Print("Out of range. Please enter a valid number.");
                                continue;
                            }

                            return idx;
                        }
                    }

                    (int winIndex, QueryResult? result1, QueryResult? result2, int result3) tuple = await UniTask.WhenAny(tcs.Task, ObserveBackground(observerCts.Token), SelectionLoop());

                    if (tuple.winIndex == 0)
                    {
                        return "Save data has changed. Please try again.";
                    }

                    if (tuple.winIndex == 1)
                    {
                        return "Workspace has changed. Please try again.";
                    }

                    int idx = tuple.result3;

                    if (idx < 0)
                    {
                        return null;
                    }

                    PUMPSaveDataStructure targetStructure = saveDatas[idx];

                    QueryResult? confirm = await context.Query($"Delete \"{targetStructure.Name}\"? (y = yes, anything else = no)");

                    if (confirm == null)
                    {
                        return null;
                    }

                    if (confirm.Value.Text.Trim().ToLower() != "y")
                    {
                        return "Deletion canceled.";
                    }

                    targetStructure.Delete();

                    return $"Deleted: {targetStructure.Name}";
                }
                finally
                {
                    observerCts.Cancel();
                    observerCts.Dispose();

                    foreach (PUMPSaveDataStructure data in saveDatas)
                    {
                        data.UnsubscribeUpdateNotification(updateHandler);
                        data.UnsubscribeDeleteRequest(updateHandler);
                    }
                }
            },
            doc: "Delete a saved workspace.",
            isSystem: false
        ),
        new ConsoleCommand
        (
            command: "/nodes",
            queryProcess: _ =>
            {
                Node[] nodes = PUMPBackground.Current?.AllNodes;

                if (nodes == null)
                {
                    return UniTask.FromResult<string>(null);
                }

                List<IGrouping<Type, Node>> groups = nodes.GroupBy(node => node.GetType()).ToList();
                StringBuilder sb = new();
                sb.AppendLine($"===== Nodes ({nodes.Length}) =====");

                foreach (IGrouping<Type, Node> group in groups)
                {
                    sb.AppendLine($"{group.Key.Name}: {group.Count()}");
                }

                sb.Append("===========================");
                return UniTask.FromResult(sb.ToString());
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