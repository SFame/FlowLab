using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using OdinSerializer;
using UnityEngine;
using UnityEngine.UI;
using static SFB.StandaloneFileBrowser;
using Utils;
using Serializer = Utils.Serializer;

public class ClassedImporter : MonoBehaviour
{
    [SerializeField] private Button m_ImportButton;
    [SerializeField] private Button m_ExportButton;
    [SerializeField] private Component m_ClassedImportTarget;

    [Space(10)]

    [SerializeField] private List<GameObject> m_exportingHighlightObject;

    private IClassedImportTarget _importTarget;
    private bool _targetFindMode;
    private IClassedDataTargetUi _currentDataTarget;
    private SafetyCancellationTokenSource _cts;

    private const string FILE_EXTENSION = "lcm";

    private bool TargetFindMode
    {
        get => _targetFindMode;
        set
        {
            _targetFindMode = value;
            if (_targetFindMode)
            {
                SetHighlights(true);
                _cts?.CancelAndDispose();
                _cts = new SafetyCancellationTokenSource(false);
                FindDataTarget(_cts.Token).Forget();
                return;
            }

            SetHighlights(false);
            _cts?.CancelAndDispose();
        }
    }

    private IClassedImportTarget ImportTarget
    {
        get
        {

            if (_importTarget == null && !m_ClassedImportTarget.TryGetComponent(out _importTarget))
            {
                throw new MissingComponentException("ClassedImportTarget does not implement IClassedImportTarget");
            }
            
            return _importTarget;
        }
    }

    private void Awake()
    {
        m_ExportButton.onClick.AddListener(() => TargetFindMode = !TargetFindMode);
        m_ImportButton.onClick.AddListener(ImportData);
        SetHighlights(false);
    }

    private void OnDisable()
    {
        Exit();
    }

    private async UniTaskVoid FindDataTarget(CancellationToken token)
    {
        try
        {
            List<IClassedDataTargetUi> finds = new();
            while (!token.IsCancellationRequested)
            {
                await UniTask.Yield(token);
                RaycasterUtil.FindUnderPoint(finds, Input.mousePosition);
                IClassedDataTargetUi foundTarget = finds.FirstOrDefault();

                if (foundTarget != null)
                {
                    if (_currentDataTarget == foundTarget)
                    {
                        if (Input.GetKeyDown(KeyCode.Mouse0))
                        {
                            ExportData(_currentDataTarget.Data);
                            return;
                        }

                        continue;
                    }

                    _currentDataTarget?.IsPointEnter(false);
                    _currentDataTarget = foundTarget;
                    _currentDataTarget.IsPointEnter(true);
                    continue;
                }

                _currentDataTarget?.IsPointEnter(false);
                _currentDataTarget = null;

                if (Input.GetKeyDown(KeyCode.Mouse0))
                    return;
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _currentDataTarget?.IsPointEnter(false);
            _currentDataTarget = null;
            TargetFindMode = false;
        }
    }

    private void ImportData()
    {
        string[] paths = OpenFilePanel("Import new module", "", FILE_EXTENSION, false);

        if (paths == null || paths.Length <= 0)
            return;

        string path = paths[0];

        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            string directoryPath = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            if (string.IsNullOrEmpty(directoryPath))
                return;

            if (string.IsNullOrEmpty(fileName))
                return;

            PUMPSaveDataStructure newStructure =
                Serializer.LoadData<PUMPSaveDataStructure>(fileName, directoryPath, DataFormat.Binary);

            if (newStructure == null)
                return;

            ImportTarget.Import(newStructure);
        }
        catch { }
    }

    private void ExportData(PUMPSaveDataStructure structure)
    {
        if (structure == null)
            return;

        string path = SaveFilePanel("Export new module", "", structure.Name, FILE_EXTENSION);

        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            string directoryPath = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            if (string.IsNullOrEmpty(directoryPath))
                return;
            
            if (string.IsNullOrEmpty(fileName))
                return;


            _ = Serializer.SaveDataAsync(fileName, structure, directoryPath, DataFormat.Binary);
        }
        catch { }
    }

    private void SetHighlights(bool highlight)
    {
        if (m_exportingHighlightObject == null)
            return;

        foreach (GameObject go in m_exportingHighlightObject)
        {
            if (go != null && go)
            {
                go.SetActive(highlight);
            }
        }
    }

    private void Exit()
    {
        TargetFindMode = false;
    }
}

public interface IClassedDataTargetUi
{
    PUMPSaveDataStructure Data { get; }
    void IsPointEnter(bool isEnter);
}

public interface IClassedImportTarget
{
    Task Import(PUMPSaveDataStructure structure);
}