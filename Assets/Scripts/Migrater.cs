using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using OdinSerializer;
using Serializer = Utils.Serializer;
using static SFB.StandaloneFileBrowser;
using static Utils.RectTransformUtils;
using SFB;
using Utils;

public class Migrater : MonoBehaviour
{
    [SerializeField] private RectTransform m_PumpBackground;
    [SerializeField] private Button m_FindFolderButton;
    [SerializeField] private Button m_MigrateButton;
    [SerializeField] private Button m_SaveButton;
    [SerializeField] private TextMeshProUGUI m_Indicator;

    private string _fileName;
    private List<PUMPSaveDataStructure> _legacyData;
    private List<PUMPSaveDataStructure> _newData;

    private void Awake()
    {
        m_Indicator.text = "Open legacy data";
        m_FindFolderButton.onClick.AddListener(() =>
        {
            m_Indicator.text = "Open legacy data";
            _legacyData = null;
            _newData = null;
            _fileName = string.Empty;
            m_MigrateButton.gameObject.SetActive(false);
            m_SaveButton.gameObject.SetActive(false);
            FindLegacyData();
        });
        m_MigrateButton.onClick.AddListener(() =>
        {
            m_Indicator.text = $"Find data! Target Legacy Saves: {_legacyData?.Count ?? 0}";
            _newData = null;
            m_SaveButton.gameObject.SetActive(false);
            Migrate();
        });
        m_SaveButton.onClick.AddListener(Save);
    }

    private void FindLegacyData()
    {
        string[] paths = OpenFilePanel
        (
            "Legacy Data", 
            string.Empty,
            new[] { new ExtensionFilter("", "bytes"), new ExtensionFilter("", "bin") },
            false
        );
        
        if (paths is not { Length: > 0 })
            return;

        string path = paths[0];
        string fileName = Path.GetFileName(path);
        string directoryName = Path.GetDirectoryName(path);

        if (string.IsNullOrEmpty(fileName))
            return;

        if (!Path.HasExtension(path))
            return;

        _legacyData = Serializer.LoadData<List<PUMPSaveDataStructure>>(fileName, directoryName, DataFormat.Binary);

        if (_legacyData == null)
        {
            m_Indicator.text = "Error";
            return;
        }

        _fileName = fileName;
        m_Indicator.text = $"Find data! Target Legacy Saves: {_legacyData.Count}";
        m_MigrateButton.gameObject.SetActive(true);
    }

    private void Migrate()
    {
        Vector2 parentSize = m_PumpBackground.rect.size;
        parentSize.Log();
        _newData = _legacyData.Select(structure =>
        {
            structure.NodeInfos = structure.NodeInfos.Select(info =>
            {
                info.NodePosition = GetNormalizeFromLocalPosition(parentSize, info.NodePosition);

                foreach (var conn in info.InConnectionTargets)
                {
                    if (conn is { Vertices: not null })
                        conn.Vertices = conn.Vertices.Select(v => GetNormalizeFromLocalPosition(parentSize, v)).ToList();
                }

                foreach (var conn in info.OutConnectionTargets)
                {
                    if (conn is { Vertices: not null })
                        conn.Vertices = conn.Vertices.Select(v => GetNormalizeFromLocalPosition(parentSize, v)).ToList();
                }

                return info;
            }).ToList();

            return structure;
        }).ToList();

        m_Indicator.text = "<color=green>Migrate success</color>";
        m_SaveButton.gameObject.SetActive(true);
    }

    private void Save()
    {
        string path = SaveFilePanel
        (
            "Save New", 
            "", 
            _fileName,
            new[] { new ExtensionFilter("", "bytes"), new ExtensionFilter("", "bin") }
        );

        if (string.IsNullOrEmpty(path))
            return;

        string fileName = Path.GetFileName(path);
        string directoryName = Path.GetDirectoryName(path);

        if (string.IsNullOrEmpty(fileName))
            return;

        if (!Directory.Exists(directoryName))
            return;

        Serializer.SaveData(fileName, _newData, directoryName, DataFormat.Binary);

        m_Indicator.text = $"Save In\n\"{path}\"";
    }
}
