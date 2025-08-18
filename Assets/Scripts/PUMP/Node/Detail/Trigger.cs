using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Trigger : Node
{
    private TriggerSupport _triggerSupport;

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

    protected override string NodeDisplayName => "Trig";

    protected override float NameTextSize => 25f;

    protected override List<string> InputNames => new List<string>();

    protected override List<string> OutputNames => new List<string>() { "out" };

    protected override List<TransitionType> InputTypes => new List<TransitionType>();

    protected override List<TransitionType> OutputTypes => new List<TransitionType>() { TransitionType.Pulse };

    protected override float InEnumeratorXPos => 0f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorSpacing => 3f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 80f);

    public override string NodePrefabPath => "PUMP/Prefab/Node/TRIGGER";

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetDefaultArray(outputTypes);
    }

    protected override void OnAfterInit()
    {
        Support.OnClick += eventData =>
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OutputToken.PushFirst(Transition.Pulse());
                TriggerSupport.PlayEffect();
                ReportChanges();
            }
        };
    }

    protected override void StateUpdate(TransitionEventArgs args) { }
}