using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class InputBlockingObject : MonoBehaviour
{
    [SerializeField] private InputBlockingMode m_Mode;
    [Space(10)]
    [SerializeField] private ActionKeyCode m_TargetActionKey;
    [SerializeField] private ModifierKeyCode[] m_TargetModifiers;
    [FormerlySerializedAs("m_IgnoreName")]
    [Space(10)]
    [SerializeField] private string[] m_AllowingNames;

    private InputBlockingMode _mode;
    private object _inputBlocker;
    private InputExclusionTarget _exclusionTarget;
    private string[] _allowingNames;

    private void Awake()
    {
        _mode = m_Mode;

        switch (_mode)
        {
            case InputBlockingMode.All:
                _inputBlocker = new object();
                break;
            case InputBlockingMode.ExclusionKeys:
                if (m_TargetActionKey == ActionKeyCode.None)
                {
                    _exclusionTarget = new InputExclusionTarget(m_TargetModifiers);
                    return;
                }

                _exclusionTarget = new InputExclusionTarget(m_TargetActionKey, m_TargetModifiers);
                break;
            case InputBlockingMode.AllowingName:
                _allowingNames = m_AllowingNames?.ToArray() ?? Array.Empty<string>();
                break;
        }
    }

    private void OnEnable()
    {
        switch (_mode)
        {
            case InputBlockingMode.All:
                InputManager.AddBlocker(_inputBlocker);
                return;
            case InputBlockingMode.ExclusionKeys:
                InputManager.AddInputExclusion(_exclusionTarget);
                return;
            case InputBlockingMode.AllowingName:
                foreach (string name in _allowingNames)
                {
                    InputManager.AddAllowingName(name);
                }
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnDisable()
    {
        switch (_mode)
        {
            case InputBlockingMode.All:
                InputManager.RemoveBlocker(_inputBlocker);
                return;
            case InputBlockingMode.ExclusionKeys:
                InputManager.RemoveInputExclusion(_exclusionTarget);
                return;
            case InputBlockingMode.AllowingName:
                foreach (string name in _allowingNames)
                {
                    InputManager.RemoveAllowingName(name);
                }
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnDestroy()
    {
        switch (_mode)
        {
            case InputBlockingMode.All:
                InputManager.RemoveBlocker(_inputBlocker);
                return;
            case InputBlockingMode.ExclusionKeys:
                InputManager.RemoveInputExclusion(_exclusionTarget);
                return;
            case InputBlockingMode.AllowingName:
                foreach (string name in _allowingNames)
                {
                    InputManager.RemoveAllowingName(name);
                }
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

public enum InputBlockingMode
{
    All,
    AllowingName,
    ExclusionKeys
}