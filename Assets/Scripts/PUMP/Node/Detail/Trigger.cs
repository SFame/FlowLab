using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Trigger : Node
{
    private TriggerSupport _triggerSupport;
    private bool _isDragged = false;

    private TriggerSupport TriggerSupport
    {
        get
        {
            if (_triggerSupport == null)
            {
                _triggerSupport = Support.GetComponent<TriggerSupport>();
            }

            return _triggerSupport;
        }
    }

    protected override string NodeDisplayName => "";

    protected override float NameTextSize => 25f;

    protected override string InputEnumeratorPrefabPath => "PUMP/Prefab/TP/Logic/LogicTPEnumIn";

    protected override string OutputEnumeratorOutPrefabPath => "PUMP/Prefab/TP/Logic/LogicTPEnumOut";

    protected override List<string> InputNames => new List<string>();

    protected override List<string> OutputNames => new List<string>() { "" };

    protected override List<TransitionType> InputTypes => new List<TransitionType>();

    protected override List<TransitionType> OutputTypes => new List<TransitionType>() { TransitionType.Pulse };

    protected override float InEnumeratorXPos => 0f;

    protected override float OutEnumeratorXPos => 65f;

    protected override float EnumeratorSpacing => 3f;

    protected override Vector2 DefaultNodeSize => new Vector2(90f, 90f);

    public override string NodePrefabPath => "PUMP/Prefab/Node/TRIGGER";

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetDefaultArray(outputTypes);
    }

    protected override void OnAfterInit()
    {
        Support.OnDragStart += eventData =>
        {
            TriggerSupport.SetDown(false);
            _isDragged = true;
        };

        Support.OnMouseDown += eventData =>
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _isDragged = false;
                TriggerSupport.SetDown(true);
                TriggerSupport.PlaySound(true);
            }
        };

        Support.OnMouseUp += eventData =>
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (_isDragged)
                {
                    _isDragged = false;
                    return;
                }

                OutputToken.PushFirst(Transition.Pulse());
                TriggerSupport.PlaySound(false);
                TriggerSupport.SetDown(false);
                ReportChanges();
            }
        };
    }

    protected override void StateUpdate(TransitionEventArgs args) { }
}