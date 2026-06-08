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

    private FormulaSupport _formulaSupport;

    public override string NodePrefabPath => "PUMP/Prefab/Node/FORMULA";

    protected override string NodeDisplayName => null;

    protected override float InEnumeratorXPos => 2f;

    protected override float OutEnumeratorXPos => 257f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(35f, 50f);

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
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void OnAfterInit()
    {
        _formulaSupport = Support.GetComponent<FormulaSupport>();
        _formula.OnError += ex => _formulaSupport.ShowError(ex.Message);

        _formulaSupport.Initialize(_formulaString, text =>
        {
            if (text == _formulaString)
            {
                return;
            }

            _formulaString = text;
            TransitionType? beforeType = _formula.OutputType;

            if (_formula.Inspect(_formulaString))
            {
                _formulaString = _formula.Expression;
                _formulaSupport.Text = _formulaString;

                if (beforeType != _formula.OutputType)
                {
                    OutputToken.SetTypeAll((TransitionType)_formula.OutputType!);
                }
                
                string[] portNames = InputToken.GetNames();

                if (_formula.Variables.SequenceEqual(portNames))
                {
                    if (OutputToken.Count == 0)  // 입력이 아예 없는 경우에서 터졌을 경우 출력을 만들어줌
                    {
                        FuseIOCounts(_formula.VariablesCount, 1);
                    }

                    Calculate();
                }
                else
                {
                    FuseIOCounts(_formula.VariablesCount, 1);
                    Calculate();
                }
            }
            else
            {
                FuseIOCounts(0, 0);
            }

            ReportChanges();
        });

        if (_formula.Inspect(_formulaString))
        {
            FuseIOCounts(_formula.VariablesCount, 1);
        }
        else
        {
            FuseIOCounts(0, 0);
        }
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (!args.IsStateChange)
        {
            return;
        }

        Calculate();
    }

    private void Calculate()
    {
        if (OutputToken.Count == 0)  // 출력이 없다는건 올바르지 않은 식을 가지고 있음을 의미
        {
            return;
        }

        if (InputToken.HasAnyNull)  // 수식의 변수 중 하나라도 null이 있다면 결과는 null
        {
            OutputToken.PushAllAsNull();
            return;
        }

        if (InputToken.TryGetNameStateDictionary(out Dictionary<string, Transition> inputMap))  // 현재 입력포트 포트명, 상태 가져옴
        {
            Dictionary<string, float> inputFloatMap = inputMap.ToDictionary(keySelector: pair => pair.Key, elementSelector: pair => (float)pair.Value);

            if (_formula.Evaluate(inputFloatMap) is { } result && result.Type == OutputToken.FirstType)  // 계산 결과가 null이 아니고, 출력 포트의 타입과 일치할 때
            {
                OutputToken.PushFirst(result);
                return;
            }
        }

        OutputToken.PushAllAsNull();
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

        // Logical / pattern
        "and", "or", "not", "like",
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
        if (variableValues == null)
        {
            return null;
        }

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

            if (c == '\'' || c == '"')
            {
                char quote = c;
                result.Append(c);
                i++;
                while (i < _expression.Length && _expression[i] != quote)
                {
                    result.Append(_expression[i]);
                    i++;
                }
                if (i < _expression.Length)
                {
                    result.Append(_expression[i]);
                    i++;
                }
                continue;
            }

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

    public event Action<Exception> OnError;

    public bool Inspect(string expression)
    {
        Clear();

        if (string.IsNullOrEmpty(expression))
        {
            return false;
        }

        StringBuilder lowered = new StringBuilder(expression.Length);
        int j = 0;
        while (j < expression.Length)
        {
            char c = expression[j];

            if (c == '\'' || c == '"')
            {
                char quote = c;
                lowered.Append(c);
                j++;
                while (j < expression.Length && expression[j] != quote)
                {
                    lowered.Append(expression[j]);
                    j++;
                }
                if (j < expression.Length)
                {
                    lowered.Append(expression[j]);
                    j++;
                }
            }
            else
            {
                lowered.Append(char.ToLowerInvariant(c));
                j++;
            }
        }
        _expression = lowered.ToString();

        int i = 0;
        while (i < _expression.Length)
        {
            char c = _expression[i];

            if (c == '\'' || c == '"')
            {
                char quote = c;
                i++;
                while (i < _expression.Length && _expression[i] != quote)
                {
                    i++;
                }
                if (i < _expression.Length)
                {
                    i++;
                }
                continue;
            }

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
            Expression exp = new Expression(sampleFormula, ExpressionOptions.IgnoreCaseAtBuiltInFunctions | ExpressionOptions.NoStringTypeCoercion);
            result = exp.Evaluate();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
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
            Expression exp = new Expression(formula, ExpressionOptions.IgnoreCaseAtBuiltInFunctions | ExpressionOptions.NoStringTypeCoercion);
            object result = exp.Evaluate();
            return AsTransition(result);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
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