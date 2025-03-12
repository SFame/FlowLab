using UnityEngine;

public class TPEnumIn : TPEnumerator
{
    protected override string PrefebPath => "PUMP/Prefab/TP/TPIn";

    public override TPEnumeratorToken GetToken()
    {
        if (!_hasSet)
        {
            Debug.LogError("Require TPEnum set first");
            return null;
        }

        return new TPEnumeratorToken(TPs, this);
    }
}