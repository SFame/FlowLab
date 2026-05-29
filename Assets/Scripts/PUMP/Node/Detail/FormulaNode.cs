using NCalc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

public class FormulaNode : DynamicIONode, INodeAdditionalArgs<string>
{
    private readonly Formula _formula = new();
    private string _formulaString = "a+b";

    public override string NodePrefabPath => "PUMP/Prefab/Node/FORMULA";

    protected override string NodeDisplayName => "Formula";

    protected override float InEnumeratorXPos => -90f;

    protected override float OutEnumeratorXPos => 90f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(50f, 50f);

    protected override float NameTextSize => 20f;

    protected override int DefaultInputCount => 0;

    protected override int DefaultOutputCount => 0;

    protected override string DefineInputName(int tpIndex)
    {
        string[] variables = _formula.Variables;
        
        if (tpIndex >= 0 && tpIndex < variables.Length)
        {
            return variables[tpIndex];
        }

        return "null";
    }

    protected override string DefineOutputName(int tpIndex)
    {
        return "out";
    }

    protected override TransitionType DefineInputType(int tpIndex)
    {
        return TransitionType.Float;
    }

    protected override TransitionType DefineOutputType(int tpIndex)
    {
        return _formula.OutputType ?? TransitionType.Float;
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetDefaultArray(outputTypes);
    }

    protected override void OnAfterInit()
    {
        if (_formula.Inspect(_formulaString))
        {
            FuseIOCounts(_formula.VariablesCount, 1);
        }
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        
    }

    public string AdditionalArgs
    {
        get => _formulaString;
        set => _formulaString = value;
    }
}

public class Formula
{
    #region Privates
    private readonly List<string> _variables = new List<string>();
    private TransitionType? _outputType = null;
    private string _expression;

    private float BUILD_FORMULA_SAMPLE_VALUE = 1.0f;

    private static readonly HashSet<string> Keyword = new HashSet<string>()
    {
        // Mathematical
        "abs", "acos", "asin", "atan", "atan2",
        "ceiling", "cos", "exp", "floor", "ieeeremainder",
        "ln", "log", "log10", "max", "min",
        "pow", "round", "sign", "sin", "sqrt",
        "tan", "truncate",

        // Utility
        "if", "ifs", "in",
    };

    private void AddVariable(string variable)
    {
        if (_variables.Contains(variable))
        {
            return;
        }

        _variables.Add(variable);
    }

    private string BuildFormula(Dictionary<string, float> variableValues)
    {
        if (variableValues.Count != _variables.Count)
        {
            return null;
        }

        if (string.IsNullOrEmpty(_expression))
        {
            return null;
        }

        foreach (string variable in _variables)
        {
            if (!variableValues.ContainsKey(variable))
            {
                return null;
            }
        }

        StringBuilder result = new StringBuilder(_expression.Length);
        int i = 0;

        while (i < _expression.Length)
        {
            char c = _expression[i];

            if (c == '_' || char.IsLetter(c))
            {
                int start = i;
                i++;
                while (i < _expression.Length && (_expression[i] == '_' || char.IsLetterOrDigit(_expression[i])))
                {
                    i++;
                }

                string token = _expression.Substring(start, i - start);

                if (_variables.Contains(token))
                {
                    if (!variableValues.TryGetValue(token, out float value))
                    {
                        return null;
                    }

                    result.Append(value.ToString("0.0##############", CultureInfo.InvariantCulture));
                }
                else
                {
                    result.Append(token);
                }
            }
            else
            {
                result.Append(c);
                i++;
            }
        }

        return result.ToString();
    }

    private TransitionType? FindTransitionType(object target)
    {
        return target switch
        {
            byte => TransitionType.Int,
            sbyte => TransitionType.Int,
            short => TransitionType.Int,
            ushort => TransitionType.Int,
            int => TransitionType.Int,
            uint => TransitionType.Int,
            long => TransitionType.Int,
            ulong => TransitionType.Int,
            float => TransitionType.Float,
            double => TransitionType.Float,
            decimal => TransitionType.Float,
            string => TransitionType.String,
            char => TransitionType.String,
            bool => TransitionType.Bool,
            DateTime => TransitionType.String,
            TimeSpan => TransitionType.String,
            Guid => TransitionType.String,
            _ => null,
        };
    }

    private Transition? AsTransition(object value) => value switch
    {
        byte b => (int)b,
        sbyte sb => (int)sb,
        short s => (int)s,
        ushort us => (int)us,
        int i => i,
        uint ui => ui > (uint)int.MaxValue ? int.MaxValue : (int)ui,
        long l => l < (long)int.MinValue ? int.MinValue : (l > (long)int.MaxValue ? int.MaxValue : (int)l),
        ulong ul => ul > (ulong)int.MaxValue ? int.MaxValue : (int)ul,
        float f => f,
        double d => (float)d,
        decimal m => (float)m,
        string str => str,
        char c => new string(c, 1),
        bool b => b,
        DateTime dt => dt.ToString(),
        TimeSpan ts => ts.ToString(),
        Guid g => g.ToString(),
        _ => null
    };
    #endregion

    #region Public
    public string Expression => _expression;

    public string[] Variables => _variables.ToArray();

    public int VariablesCount => _variables.Count;

    public TransitionType? OutputType => _outputType;

    public bool Inspect(string expression)
    {
        Clear();

        if (string.IsNullOrEmpty(expression))
        {
            return false;
        }

        _expression = expression.ToLowerInvariant();

        int i = 0;
        while (i < _expression.Length)
        {
            char c = _expression[i];

            if (c == '_' || char.IsLetter(c))
            {
                int start = i;
                i++;
                while (i < _expression.Length && (_expression[i] == '_' || char.IsLetterOrDigit(_expression[i])))
                {
                    i++;
                }

                string token = _expression.Substring(start, i - start);

                if (i < _expression.Length && _expression[i] == '(')
                {
                    continue;
                }

                if (Keyword.Contains(token))
                {
                    continue;
                }

                AddVariable(token);
            }
            else
            {
                i++;
            }
        }

        string sampleFormula = BuildFormula(_variables.ToDictionary(keySelector: v => v, elementSelector: _ => BUILD_FORMULA_SAMPLE_VALUE));

        object result;

        try
        {
            Expression exp = new Expression(sampleFormula, EvaluateOptions.IgnoreCase);
            result = exp.Evaluate();
        }
        catch
        {
            Clear();
            return false;
        }

        _outputType = FindTransitionType(result);

        if (_outputType == null)
        {
            Clear();
            return false;
        }

        return true;
    }

    public Transition? Evaluate(Dictionary<string, float> variableValues)
    {
        if (_outputType == null || BuildFormula(variableValues) is not { } formula)
        {
            return null;
        }
        
        try
        {
            Expression exp = new Expression(formula, EvaluateOptions.IgnoreCase);
            object result = exp.Evaluate();
            return AsTransition(result);
        }
        catch
        {
            return null;
        }

    }

    public void Clear()
    {
        _variables.Clear();
        _expression = string.Empty;
        _outputType = null;
    }
    #endregion
}