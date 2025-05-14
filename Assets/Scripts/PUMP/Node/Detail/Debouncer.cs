using System.Collections.Generic;
using UnityEngine;

public class Debouncer : Node
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";
    public override string NodePrefabPath => "PUMP/Prefab/Node/DEBOUNCER";

    protected override List<string> InputNames { get; } = new() { "A" };

    protected override List<string> OutputNames { get; } = new() { "out" };
    protected override List<TransitionType> InputTypes { get; } = new() { TransitionType.Bool };
    protected override List<TransitionType> OutputTypes { get; } = new() { TransitionType.Bool };

    protected override float InEnumeratorXPos => -47f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 TPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(140f, 90f);

    protected override string NodeDisplayName => "Debouncer";


    private DebouncerSuppport _debouncerSuppport;
    private DebouncerSuppport DebouncerSuppport
    {
        get
        {
            _debouncerSuppport ??= Support.GetComponent<DebouncerSuppport>();
            return _debouncerSuppport;
        }
    }

    protected override Transition[] SetInitializeState(int outputCount)
    {
        return new[] { (Transition)false };
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        // 파라미터 args를 사용해야됨
    }

    //생명주기는 OnBeforeRemove, OnAfterInit 메서드를 사용
    //Timer노드랑 제일 비슷해서 이거 참고해서 만들면 될듯
}
