using UnityEngine;

public class TPEnumOut : TPEnumerator
{
    protected override string PrefebPath => "PUMP/Prefab/TP/TPOut";

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