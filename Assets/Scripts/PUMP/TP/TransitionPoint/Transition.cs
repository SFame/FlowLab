using OdinSerializer;
using System;
using System.Linq;
using System.Numerics;
using UnityEngine;

public enum TransitionType
{
    None,
    Bool,
    Int,
    Float,
    String,
}

[Serializable]
public struct Transition : IEquatable<Transition>
{
    #region Static Interface
    public static Transition Null(TransitionType type)
    {
        return new Transition(type, new TransitionValue(), true);
    }

    // ---- Bool constants ----
    public static Transition True => true;
    public static Transition False => false;

    // ---- Int constants ----
    public static Transition Zero => 0;
    public static Transition One => 1;
    public static Transition MinInt => int.MinValue;
    public static Transition MaxInt => int.MaxValue;
    public static Transition NegativeOne => -1;
    public static Transition Ten => 10;
    public static Transition Hundred => 100;

    // ---- Float constants ----
    public static Transition Pi => (float)Math.PI;
    public static Transition E => (float)Math.E;
    public static Transition NaN => float.NaN;
    public static Transition PositiveInfinity => float.PositiveInfinity;
    public static Transition NegativeInfinity => float.NegativeInfinity;
    public static Transition Half => 0.5f;
    public static Transition DegToRad => (float)(Math.PI / 180.0);
    public static Transition RadToDeg => (float)(180.0 / Math.PI);

    // ---- String constants ----
    public static Transition Empty => string.Empty;
    public static Transition Space => " ";
    public static Transition NewLine => Environment.NewLine;
    public static Transition Tab => "\t";
    public static Transition Comma => ",";
    public static Transition Dot => ".";
    #endregion

    #region Interface
    /// <summary>
    /// Transition Type
    /// </summary>
    public TransitionType Type => _type;

    /// <summary>
    /// Transition Value
    /// </summary>
    public TransitionValue Value => _value;

    /// <summary>
    /// IsNull == true: When the connection is disconnected
    /// </summary>
    public bool IsNull => _isNull;

    public dynamic GetValueAsDynamic()
    {
        dynamic value = this switch
        {
            { Type: TransitionType.None } => throw new TransitionNoneTypeException(),
            { Type: TransitionType.Bool, IsNull: false } => (bool)this,
            { Type: TransitionType.Int, IsNull: false } => (int)this,
            { Type: TransitionType.Float, IsNull: false } => (float)this,
            { Type: TransitionType.String, IsNull: false } => (string)this,
            { Type: TransitionType.Bool, IsNull: true } => false,
            { Type: TransitionType.Int, IsNull: true } => 0,
            { Type: TransitionType.Float, IsNull: true } => 0f,
            { Type: TransitionType.String, IsNull: true } => string.Empty,
            _ => null
        };

        return value;
    }

    public string GetValueString()
    {
        if (IsNull)
        {
            return "Null";
        }

        return Type switch
        {
            TransitionType.None => "None",
            TransitionType.Bool => IsNull ? "Null" : Value.BoolValue.ToString(),
            TransitionType.Int => IsNull ? "Null" : Value.IntValue.ToString(),
            TransitionType.Float => IsNull ? "Null" : Value.FloatValue.ToString(),
            TransitionType.String => IsNull ? "Null" : Value.StringValue ?? string.Empty,
            _ => "Unknown Type"
        };
    }
    #endregion

    #region Overrided
    public override bool Equals(object obj) => obj is Transition other && Equals(other);

    public bool Equals(Transition other)
    {
        if (Type != other.Type)
            return false;

        if (Type == TransitionType.None)
            return true;

        if (IsNull && other.IsNull)
            return true;

        if (IsNull != other.IsNull)
            return false;

        return Type switch
        {
            TransitionType.Bool => Value.BoolValue == other.Value.BoolValue,
            TransitionType.Int => Value.IntValue == other.Value.IntValue,
            TransitionType.Float => Value.FloatValue.Equals(other.Value.FloatValue),
            TransitionType.String => string.Equals(Value.StringValue, other.Value.StringValue, StringComparison.Ordinal),
            _ => false
        };
    }

    public override int GetHashCode()
    {
        if (Type == TransitionType.None)
            return Type.GetHashCode();

        if (IsNull)
            return HashCode.Combine(Type, "Null");

        return Type switch
        {
            TransitionType.Bool => HashCode.Combine(Type, Value.BoolValue),
            TransitionType.Int => HashCode.Combine(Type, Value.IntValue),
            TransitionType.Float => HashCode.Combine(Type, Value.FloatValue),
            TransitionType.String => HashCode.Combine(Type, Value.StringValue ?? string.Empty),
            _ => 0
        };
    }

    public override string ToString()
    {
        if (IsNull)
        {
            return $"Type: {Type}\nValue: Null";
        }

        string value = Type switch
        {
            TransitionType.None => "None",
            TransitionType.Bool => IsNull ? "Null" : Value.BoolValue.ToString(),
            TransitionType.Int => IsNull ? "Null" : Value.IntValue.ToString(),
            TransitionType.Float => IsNull ? "Null" : Value.FloatValue.ToString(),
            TransitionType.String => IsNull ? "Null" : Value.StringValue ?? string.Empty,
            _ => "Unknown Type"
        };

        return $"Type: {Type}\nValue: {value}";
    }
    #endregion

    #region Backing fields
    [OdinSerialize] public TransitionType _type;
    [OdinSerialize] public TransitionValue _value;
    [OdinSerialize] public bool _isNull;
    #endregion

    #region Non Interface
    public Transition(TransitionType type, TransitionValue value, bool isNull = false)
    {
        if (type == TransitionType.None)
        {
            throw new TransitionNoneTypeException();
        }

        _type = type;
        _value = value;
        _isNull = isNull;
    }

    public Transition(bool value, bool isNull = false)
    {
        _type = TransitionType.Bool;
        _value = new TransitionValue(boolValue: value);
        _isNull = isNull;
    }

    public Transition(int value, bool isNull = false)
    {
        _type = TransitionType.Int;
        _value = new TransitionValue(intValue: value);
        _isNull = isNull;
    }

    public Transition(float value, bool isNull = false)
    {
        _type = TransitionType.Float;
        _value = new TransitionValue(floatValue: value);
        _isNull = isNull;
    }

    public Transition(string value, bool isNull = false)
    {
        _type = TransitionType.String;
        _value = new TransitionValue(stringValue: value);
        _isNull = isNull;
    }

    public Transition(dynamic value, bool isNull = false)
    {
        if (value is bool b)
        {
            _type = TransitionType.Bool;
            _value = new TransitionValue(boolValue: b);
            _isNull = isNull;
        }
        else if (value is int i)
        {
            _type = TransitionType.Int;
            _value = new TransitionValue(intValue: i);
            _isNull = isNull;
        }
        else if (value is BigInteger bi)
        {
            int clamped;
            if (bi < int.MinValue)
                clamped = int.MinValue;
            else if (bi > int.MaxValue)
                clamped = int.MaxValue;
            else
                clamped = (int)bi;

            _type = TransitionType.Int;
            _value = new TransitionValue(intValue: clamped);
            _isNull = isNull;
        }
        else if (value is float f)
        {
            _type = TransitionType.Float;
            _value = new TransitionValue(floatValue: f);
            _isNull = isNull;
        }
        else if (value is double d)
        {
            _type = TransitionType.Float;
            _value = new TransitionValue(floatValue: (float)d);
            _isNull = isNull;
        }
        else if (value is string s)
        {
            _type = TransitionType.String;
            _value = new TransitionValue(stringValue: s);
            _isNull = isNull;
        }
        else if (value == null)
        {
            throw new TransitionArgumentNullException("Dynamic constructor: Argument cannot be Null");
        }
        else
        {
            throw new TransitionException("Dynamic constructor: Casting Error");
        }
    }
    #endregion

    #region Casting
    // ---------- Bool ----------
    public static implicit operator Transition(bool b)
    {
        return new Transition(TransitionType.Bool, new TransitionValue(boolValue: b));
    }

    public static implicit operator bool(Transition t)
    {
        if (t.Type == TransitionType.None)
        {
            throw new TransitionNoneTypeException();
        }

        if (t.Type != TransitionType.Bool)
        {
            throw new TransitionTypeCastException(t.Type, typeof(bool));
        }

        return t.Value.BoolValue;
    }

    // ---------- Int ----------
    public static implicit operator Transition(int i)
    {
        return new Transition(TransitionType.Int, new TransitionValue(intValue: i));
    }

    public static implicit operator int(Transition t)
    {
        if (t.Type == TransitionType.None)
        {
            throw new TransitionNoneTypeException();
        }

        if (t.Type != TransitionType.Int)
        {
            throw new TransitionTypeCastException(t.Type, typeof(int));
        }

        return t.Value.IntValue;
    }

    // ---------- Float ----------
    public static implicit operator Transition(float f)
    {
        return new Transition(TransitionType.Float, new TransitionValue(floatValue: f));
    }

    public static implicit operator float(Transition t)
    {
        if (t.Type == TransitionType.None)
        {
            throw new TransitionNoneTypeException();
        }

        if (t.Type != TransitionType.Float)
        {
            throw new TransitionTypeCastException(t.Type, typeof(float));
        }

        return t.Value.FloatValue;
    }

    // ---------- String ----------
    public static implicit operator Transition(string s)
    {
        return new Transition(TransitionType.String, new TransitionValue(stringValue: s ?? string.Empty));
    }

    public static implicit operator string(Transition t)
    {
        if (t.Type == TransitionType.None)
        {
            throw new TransitionNoneTypeException();
        }

        if (t.Type != TransitionType.String)
        {
            throw new TransitionTypeCastException(t.Type, typeof(string));
        }

        return t.Value.StringValue ?? string.Empty;
    }
    #endregion

    #region Arithmetic Operator
    public static Transition operator +(Transition t) => t.Type switch
    {
        TransitionType.Int => new Transition(t.Type, new TransitionValue(intValue: +t.Value.IntValue)),
        TransitionType.Float => new Transition(t.Type, new TransitionValue(floatValue: +t.Value.FloatValue)),
        TransitionType.None => throw new TransitionNoneTypeException(),
        _ => throw new TransitionInvalidOperationException("+", t.Type)
    };

    public static Transition operator -(Transition t) => t.Type switch
    {
        TransitionType.Int => new Transition(t.Type, new TransitionValue(intValue: -t.Value.IntValue)),
        TransitionType.Float => new Transition(t.Type, new TransitionValue(floatValue: -t.Value.FloatValue)),
        TransitionType.None => throw new TransitionNoneTypeException(),
        _ => throw new TransitionInvalidOperationException("-", t.Type)
    };

    public static Transition operator +(Transition t1, Transition t2) => Arithmetic(t1, t2, "+");
    public static Transition operator -(Transition t1, Transition t2) => Arithmetic(t1, t2, "-");
    public static Transition operator *(Transition t1, Transition t2) => Arithmetic(t1, t2, "*");
    public static Transition operator /(Transition t1, Transition t2) => Arithmetic(t1, t2, "/");
    public static Transition operator %(Transition t1, Transition t2) => Arithmetic(t1, t2, "%");

    private static Transition Arithmetic(Transition t1, Transition t2, string op)
    {
        if (t1.Type == TransitionType.None || t2.Type == TransitionType.None)
            throw new TransitionNoneTypeException();

        if (t1.Type != t2.Type)
            throw new TransitionTypeMismatchException(t1.Type, t2.Type);

        if (t1.Type != TransitionType.Int && t1.Type != TransitionType.Float && t1.Type != TransitionType.String)
            throw new TransitionInvalidOperationException(op, t1.Type, t2.Type);

        return t1.Type switch
        {
            TransitionType.Int => new Transition(t1.Type, new TransitionValue(intValue: ApplyIntOp(t1.Value.IntValue, t2.Value.IntValue, op))),
            TransitionType.Float => new Transition(t1.Type, new TransitionValue(floatValue: ApplyFloatOp(t1.Value.FloatValue, t2.Value.FloatValue, op))),
            TransitionType.String => new Transition(t1.Type, new TransitionValue(stringValue: ApplyStringOp(t1.Value.StringValue, t2.Value.StringValue, op))),
            _ => throw new TransitionInvalidOperationException(op, t1.Type, t2.Type)
        };
    }

    private static int ApplyIntOp(int a, int b, string op) => op switch
    {
        "+" => a + b,
        "-" => a - b,
        "*" => a * b,
        "/" => b != 0 ? a / b : throw new DivideByZeroException(),
        "%" => b != 0 ? a % b : throw new DivideByZeroException(),
        _ => throw new TransitionInvalidOperationException(op, TransitionType.Int, TransitionType.Int)
    };

    private static float ApplyFloatOp(float a, float b, string op) => op switch
    {
        "+" => a + b,
        "-" => a - b,
        "*" => a * b,
        "/" => b != 0f ? a / b : throw new DivideByZeroException(),
        "%" => b != 0f ? a % b : throw new DivideByZeroException(),
        _ => throw new TransitionInvalidOperationException(op, TransitionType.Float, TransitionType.Float)
    };

    private static string ApplyStringOp(string a, string b, string op) => op switch
    {
        "+" => a + b,
        _ => throw new TransitionInvalidOperationException(op, TransitionType.String, TransitionType.String),
    };
    #endregion

    #region Comparison Operators
    public static bool operator ==(Transition t1, Transition t2)
    {
        if (t1.Type == TransitionType.None || t2.Type == TransitionType.None)
        {
            throw new TransitionNoneTypeException();
        }

        return t1.Equals(t2);
    }

    public static bool operator !=(Transition t1, Transition t2)
    {
        if (t1.Type == TransitionType.None || t2.Type == TransitionType.None)
        {
            throw new TransitionNoneTypeException();
        }

        return !t1.Equals(t2);
    }

    public static bool operator <(Transition t1, Transition t2) => CompareNumeric(t1, t2, "<");
    public static bool operator <=(Transition t1, Transition t2) => CompareNumeric(t1, t2, "<=");
    public static bool operator >(Transition t1, Transition t2) => CompareNumeric(t1, t2, ">");
    public static bool operator >=(Transition t1, Transition t2) => CompareNumeric(t1, t2, ">=");

    private static bool CompareNumeric(Transition t1, Transition t2, string op)
    {
        if (t1.Type == TransitionType.None || t2.Type == TransitionType.None)
        {
            throw new TransitionNoneTypeException();
        }

        if (t1.Type != t2.Type)
            throw new TransitionTypeMismatchException(t1.Type, t2.Type);

        if (t1.Type != TransitionType.Int && t1.Type != TransitionType.Float)
            throw new TransitionInvalidOperationException(op, t1.Type, t2.Type);

        return t1.Type switch
        {
            TransitionType.Int => ApplyIntComparison(t1.Value.IntValue, t2.Value.IntValue, op),
            TransitionType.Float => ApplyFloatComparison(t1.Value.FloatValue, t2.Value.FloatValue, op),
            _ => throw new TransitionInvalidOperationException(op, t1.Type, t2.Type)
        };
    }

    private static bool ApplyIntComparison(int a, int b, string op) => op switch
    {
        "<" => a < b,
        "<=" => a <= b,
        ">" => a > b,
        ">=" => a >= b,
        _ => throw new TransitionInvalidOperationException(op, TransitionType.Int, TransitionType.Int)
    };

    private static bool ApplyFloatComparison(float a, float b, string op) => op switch
    {
        "<" => a < b,
        "<=" => a <= b,
        ">" => a > b,
        ">=" => a >= b,
        _ => throw new TransitionInvalidOperationException(op, TransitionType.Float, TransitionType.Float)
    };
    #endregion
}

public struct TransitionValue
{
    public TransitionValue(bool boolValue = false, int intValue = 0, float floatValue = 0f, string stringValue = "")
    {
        _boolValue = boolValue;
        _intValue = intValue;
        _floatValue = floatValue;
        _stringValue = stringValue;
    }

    public bool BoolValue => _boolValue;
    public int IntValue => _intValue;
    public float FloatValue => _floatValue;
    public string StringValue => _stringValue;

    #region Backing fields
    [OdinSerialize] private bool _boolValue;
    [OdinSerialize] private int _intValue;
    [OdinSerialize] private float _floatValue;
    [OdinSerialize] private string _stringValue;
    #endregion
}

public static class TransitionUtil
{
    public static Type AsType(this TransitionType transitionType) => transitionType switch
    {
        TransitionType.Bool => typeof(bool),
        TransitionType.Int => typeof(int),
        TransitionType.Float => typeof(float),
        TransitionType.String => typeof(string),
        TransitionType.None => throw new TransitionNoneTypeException(),
        _ => throw new ArgumentOutOfRangeException(nameof(transitionType), $"Unsupported transition type: {transitionType}")
    };

    public static TransitionType AsTransitionType(this Type type) => type switch
    {
        null => TransitionType.None,
        Type t when t == typeof(bool) => TransitionType.Bool,
        Type t when t == typeof(int) => TransitionType.Int,
        Type t when t == typeof(float) => TransitionType.Float,
        Type t when t == typeof(string) => TransitionType.String,
        _ => throw new ArgumentException($"Unsupported type: {type?.FullName ?? "null"}")
    };

    public static Transition Null(this TransitionType transitionType)
    {
        return Transition.Null(transitionType);
    }

    public static void ThrowIfTypeMismatch(this Transition transition, TransitionType type)
    {
        if (type == TransitionType.None || transition.Type == TransitionType.None)
        {
            throw new TransitionNoneTypeException();
        }

        if (transition.Type != type)
        {
            throw new TransitionTypeMismatchException(transition.Type, type);
        }
    }

    public static void ThrowIfTypeMismatch(this TransitionType type1, TransitionType type2)
    {
        if (type1 == TransitionType.None || type2 == TransitionType.None)
        {
            throw new TransitionNoneTypeException();
        }

        if (type1 != type2)
        {
            throw new TransitionTypeMismatchException(type1, type2);
        }
    }

    public static Color GetColor(this TransitionType transitionType) => transitionType switch
    {
        TransitionType.Bool => Color.black,
        TransitionType.Int => new Color(0f, 0.94f, 0.47f),
        TransitionType.Float => new Color(0.94f, 0.69f, 0f),
        TransitionType.String => new Color(0.94f, 0.34f, 1f),
        TransitionType.None => throw new TransitionNoneTypeException(),
        _ => Color.black,
    };

    public static string GetColorHexCodeString(this TransitionType transitionType, bool containSharp = false)
    {
        Color color = GetColor(transitionType);
        string colorString = ColorUtility.ToHtmlStringRGB(color);
        return containSharp ? "#" + colorString : colorString;
    }
}

#region Exceptions
public class TransitionException : Exception
{
    public TransitionException(string message) : base(message) { }
}

public class TransitionTypeCastException : TransitionException
{
    public TransitionType From { get; }
    public Type[] To { get; }

    public TransitionTypeCastException(TransitionType from, params Type[] to)
        : base($"Cannot convert from Transition Type '{from.ToString()}' to Type '{string.Join(", ", to.Select(t => t.Name))}'.")
    {
        From = from;
        To = to;
    }
}

public class TransitionTypeMismatchException : TransitionException
{
    public TransitionType T1 { get; }
    public TransitionType T2 { get; }

    public TransitionTypeMismatchException(TransitionType t1, TransitionType t2)
        : base($"Cannot perform operation between incompatible transition types: {t1} and {t2}.")
    {
        T1 = t1;
        T2 = t2;
    }
}

public class TransitionNoneTypeException : TransitionException
{
    public TransitionNoneTypeException() : base("TransitionType.None cannot be used as a value.") { }
}

public class TransitionInvalidOperationException : TransitionException
{
    public string Operator { get; }
    public TransitionType[] Types { get; }

    public TransitionInvalidOperationException(string @operator, params TransitionType[] types)
    : base($"The '{@operator}' operator cannot be applied to Transition of type '{string.Join(", ", types)}'.")
    {
        Operator = @operator;
        Types = types;
    }
}

public class TransitionArgumentNullException : TransitionException
{
    public TransitionArgumentNullException(string message) : base(message) { }
}
#endregion