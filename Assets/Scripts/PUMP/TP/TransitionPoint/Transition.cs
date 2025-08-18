using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using ColorUtility = UnityEngine.ColorUtility;

public enum TransitionType
{
    None,
    Bool,
    Int,
    Float,
    String,
    Pulse,
}

public readonly struct Pulse
{
    public Pulse(bool isNull = false)
    {
        IsNull = isNull;
    }

    public bool IsNull { get; }
}

[Serializable]
public struct Transition : IComparable<Transition>, IEquatable<Transition>
{
    #region Static Interface
    public static Transition Null(TransitionType transitionType)
    {
        if (transitionType == TransitionType.None)
        {
            throw new TransitionNoneTypeException();
        }

        return new Transition(transitionType, TransitionValue.Default(), true);
    }

    public static Transition Default(TransitionType transitionType) => transitionType switch
    {
        TransitionType.None => throw new TransitionNoneTypeException(),
        TransitionType.Bool => Transition.False,
        TransitionType.Int => Transition.Zero,
        TransitionType.Float => Transition.FloatZero,
        TransitionType.String => Transition.Empty,
        TransitionType.Pulse => Pulse(),
        _ => throw new ArgumentOutOfRangeException(nameof(transitionType), $"Unsupported transition type: {transitionType}")
    };

    public static Transition Pulse() => new Transition(TransitionType.Pulse, TransitionValue.Default(), false);

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
    public static Transition FloatZero => 0.0f;
    public static Transition FloatOne => 1.0f;
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
            { Type: TransitionType.Pulse, IsNull: false} => (Pulse)this,
            { Type: TransitionType.Bool, IsNull: true } => null,
            { Type: TransitionType.Int, IsNull: true } => null,
            { Type: TransitionType.Float, IsNull: true } => null,
            { Type: TransitionType.String, IsNull: true } => null,
            { Type: TransitionType.Pulse, IsNull: true } => null,
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
            TransitionType.Float => IsNull ? "Null" : Value.FloatValue.ToString("0.0###"),
            TransitionType.String => IsNull ? "Null" : Value.StringValue ?? string.Empty,
            TransitionType.Pulse => IsNull ? "Null" : "Pulse",
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
            TransitionType.Pulse => true,
            _ => false
        };
    }

    public int CompareTo(Transition other)
    {
        if (Type == TransitionType.None || other.Type == TransitionType.None)
        {
            throw new TransitionNoneTypeException();
        }

        if (Type != other.Type)
        {
            throw new TransitionTypeMismatchException(Type, other.Type);
        }

        if (IsNull && other.IsNull)
        {
            return 0;
        }

        if (IsNull)
        {
            return -1;
        }

        if (other.IsNull)
        {
            return 1;
        }

        return Type switch
        {
            TransitionType.Bool => ((bool)this).CompareTo((bool)other),
            TransitionType.Int => ((int)this).CompareTo((int)other),
            TransitionType.Float => ((float)this).CompareTo((float)other),
            TransitionType.String => string.Compare(((string)this), (string)other, StringComparison.Ordinal),
            TransitionType.Pulse => 0,
            _ => throw new TransitionTypeArgumentOutOfRangeException(Type)
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
            TransitionType.Pulse => HashCode.Combine(Type, true),
            _ => throw new TransitionTypeArgumentOutOfRangeException(Type)
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
            TransitionType.Pulse => IsNull ? "Null" : "Pulse",
            _ => "Unknown Type"
        };

        return $"Type: {Type}\nValue: {value}";
    }
    #endregion

    #region Backing fields
    [OdinSerialize] private TransitionType _type;
    [OdinSerialize] private TransitionValue _value;
    [OdinSerialize] private bool _isNull;
    #endregion

    #region Non Interface
    public Transition(TransitionType type, TransitionValue value, bool isNull = false)
    {
        if (type == TransitionType.None)
        {
            throw new TransitionNoneTypeException();
        }

        if (value.StringValue == null)
        {
            throw new TransitionNullStringException("TransitionType.String Transition's Value does not allow null. Please use an empty string instead");
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

    public Transition(Pulse pulse, bool isNull = false)
    {
        _type = TransitionType.Pulse;
        _value = TransitionValue.Default();
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
        else if (value is Pulse p)
        {
            _type = TransitionType.Pulse;
            _value = TransitionValue.Default();
            _isNull = isNull;
        }
        else if (value == null)
        {
            throw new TransitionArgumentNullException("Dynamic constructor: Argument cannot be Null");
        }
        else
        {
            throw new TransitionException($"Dynamic constructor: Casting Error: {value.GetType().Name}");
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
        
        return t.IsNull ? false : t.Value.BoolValue;
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

        return t.IsNull ? 0 : t.Value.IntValue;
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

        return t.IsNull ? 0.0f : t.Value.FloatValue;
    }

    // ---------- String ----------
    public static implicit operator Transition(string s)
    {
        if (s == null)
        {
            throw new TransitionNullStringException("TransitionType.String Transition's Value does not allow null. Please use an empty string instead");
        }

        return new Transition(TransitionType.String, new TransitionValue(stringValue: s));
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

        return t.IsNull ? string.Empty : t.Value.StringValue ?? throw new TransitionNullStringException("TransitionType.String Transition's Value does not allow null. Please use an empty string instead"); ;
    }

    // ---------- Pulse ----------
    public static implicit operator Transition(Pulse p)
    {
        return new Transition(TransitionType.Pulse, TransitionValue.Default());
    }

    public static implicit operator Pulse(Transition t)
    {
        if (t.Type == TransitionType.None)
        {
            throw new TransitionNoneTypeException();
        }

        if (t.Type != TransitionType.Pulse)
        {
            throw new TransitionTypeCastException(t.Type, typeof(Pulse));
        }

        return t.IsNull ? new Pulse(true) : new Pulse();
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
    public static TransitionValue Default()
    {
        return new TransitionValue(false, 0, 0f, string.Empty);
    }

    public TransitionValue(bool boolValue = false, int intValue = 0, float floatValue = 0f, string stringValue = "")
    {
        _boolValue = boolValue;
        _intValue = intValue;
        _floatValue = floatValue;
        _stringValue = stringValue ?? throw new TransitionNullStringException("TransitionType.String Transition's Value does not allow null. Please use an empty string instead");
    }

    public bool BoolValue => _boolValue;
    public int IntValue => _intValue;
    public float FloatValue => _floatValue;
    public string StringValue
    {
        get
        {
            _stringValue ??= string.Empty;
            return _stringValue;
        }
    }

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
        TransitionType.Pulse => typeof(Pulse),
        TransitionType.None => throw new TransitionNoneTypeException(),
        _ => throw new TransitionTypeArgumentOutOfRangeException(transitionType)
    };

    public static TransitionType AsTransitionType(this Type type) => type switch
    {
        null => TransitionType.None,
        Type t when t == typeof(bool) => TransitionType.Bool,
        Type t when t == typeof(int) => TransitionType.Int,
        Type t when t == typeof(float) => TransitionType.Float,
        Type t when t == typeof(string) => TransitionType.String,
        Type t when t == typeof(Pulse) => TransitionType.Pulse,
        _ => throw new TransitionTypeArgumentOutOfRangeException(type)
    };

    public static Transition Null(this TransitionType transitionType)
    {
        return Transition.Null(transitionType);
    }

    public static Transition Default(this TransitionType transitionType)
    {
        return Transition.Default(transitionType);
    }

    public static Transition Convert(this Transition fromTransition, TransitionType toType)
    {
        if (fromTransition.Type == TransitionType.None || toType == TransitionType.None)
        {
            throw new TransitionNoneTypeException();
        }

        Transition RefiningNull(Transition targetTransition)
        {
            if (targetTransition.IsNull)
            {
                return targetTransition.Type.Null();
            }

            return targetTransition;
        }

        if (fromTransition.Type == toType)
        {
            return RefiningNull(fromTransition);
        }

        switch (toType)
        {
            case TransitionType.Bool:
                switch (fromTransition.Type)
                {
                    case (TransitionType.Int):
                        bool boolValue = (int)fromTransition != 0;
                        return RefiningNull(new Transition(boolValue, fromTransition.IsNull));
                    case (TransitionType.Float):
                        throw new TransitionIncompatibleTypeException(fromTransition.Type, toType);
                    case (TransitionType.String):
                        string fromString = fromTransition;
                        if (fromString is "True" or "true")
                        {
                            return RefiningNull(new Transition(true, fromTransition.IsNull));
                        }

                        if (fromString is "False" or "false")
                        {
                            return RefiningNull(new Transition(false, fromTransition.IsNull));
                        }
                        throw new TransitionStringConversionException(fromTransition.Type, toType, fromTransition);
                    case (TransitionType.Pulse):
                        throw new TransitionIncompatibleTypeException(fromTransition.Type, toType);
                    default:
                        throw new TransitionTypeArgumentOutOfRangeException(fromTransition.Type);
                }

            case TransitionType.Int:
                switch (fromTransition.Type)
                {
                    case (TransitionType.Bool):
                        int intValue1 = (bool)fromTransition ? 1 : 0;
                        return RefiningNull(new Transition(intValue1, fromTransition.IsNull));
                    case (TransitionType.Float):
                        int intValue2 = (int)((float)fromTransition);
                        return RefiningNull(new Transition(intValue2, fromTransition.IsNull));
                    case (TransitionType.String):
                        if (!int.TryParse(fromTransition, out int intValue3))
                            throw new TransitionStringConversionException(fromTransition.Type, toType, fromTransition);
                        return RefiningNull(new Transition(intValue3, fromTransition.IsNull));
                    case (TransitionType.Pulse):
                        throw new TransitionIncompatibleTypeException(fromTransition.Type, toType);
                    default:
                        throw new TransitionTypeArgumentOutOfRangeException(fromTransition.Type);
                }

            case TransitionType.Float:
                switch (fromTransition.Type)
                {
                    case (TransitionType.Bool):
                        throw new TransitionIncompatibleTypeException(fromTransition.Type, toType);
                    case (TransitionType.Int):
                        float floatValue1 = (int)fromTransition;
                        return RefiningNull(new Transition(floatValue1, fromTransition.IsNull));
                    case (TransitionType.String):
                        if (!float.TryParse(fromTransition, out float floatValue2))
                            throw new TransitionStringConversionException(fromTransition.Type, toType, fromTransition);
                        return RefiningNull(new Transition(floatValue2, fromTransition.IsNull));
                    case (TransitionType.Pulse):
                        throw new TransitionIncompatibleTypeException(fromTransition.Type, toType);
                    default:
                        throw new TransitionTypeArgumentOutOfRangeException(fromTransition.Type);
                }

            case TransitionType.String:
                return RefiningNull(new Transition(fromTransition.GetValueString(), fromTransition.IsNull));

            case TransitionType.Pulse:
                throw new TransitionIncompatibleTypeException(fromTransition.Type, toType);

            default:
                throw new TransitionTypeArgumentOutOfRangeException(toType);
        }
    }

    public static bool TryConvert(this Transition fromTransition, TransitionType toType, out Transition result, bool stringFailOk = false)
    {
        result = default;

        if (fromTransition.Type == TransitionType.None || toType == TransitionType.None)
        {
            return false;
        }

        Transition RefiningNull(Transition targetTransition)
        {
            if (targetTransition.IsNull)
            {
                return targetTransition.Type.Null();
            }

            return targetTransition;
        }

        if (fromTransition.Type == toType)
        {
            result = RefiningNull(fromTransition);
            return true;
        }

        switch (toType)
        {
            case TransitionType.Bool:
                switch (fromTransition.Type)
                {
                    case (TransitionType.Int):
                        bool boolValue = (int)fromTransition != 0;
                        result = RefiningNull(new Transition(boolValue, fromTransition.IsNull));
                        return true;
                    case (TransitionType.Float):
                        return false;
                    case (TransitionType.String):
                        string fromString = fromTransition;
                        if (fromString is "True" or "true")
                        {
                            result = RefiningNull(new Transition(true, fromTransition.IsNull));
                            return true;
                        }

                        if (fromString is "False" or "false")
                        {
                            result = RefiningNull(new Transition(false, fromTransition.IsNull));
                            return true;
                        }

                        if (stringFailOk)
                        {
                            result = TransitionType.Bool.Null();
                            return true;
                        }

                        return false;
                    case (TransitionType.Pulse):
                        return false;
                    default:
                        return false;
                }

            case TransitionType.Int:
                switch (fromTransition.Type)
                {
                    case (TransitionType.Bool):
                        int intValue1 = (bool)fromTransition ? 1 : 0;
                        result = RefiningNull(new Transition(intValue1, fromTransition.IsNull));
                        return true;
                    case (TransitionType.Float):
                        int intValue2 = (int)((float)fromTransition);
                        result = RefiningNull(new Transition(intValue2, fromTransition.IsNull));
                        return true;
                    case (TransitionType.String):
                        if (!int.TryParse(fromTransition, out int intValue3))
                        {
                            if (stringFailOk)
                            {
                                result = TransitionType.Int.Null();
                                return true;
                            }

                            return false;
                        }
                        result = RefiningNull(new Transition(intValue3, fromTransition.IsNull));
                        return true;
                    case (TransitionType.Pulse):
                        return false;
                    default:
                        return false;
                }

            case TransitionType.Float:
                switch (fromTransition.Type)
                {
                    case (TransitionType.Bool):
                        return false;
                    case (TransitionType.Int):
                        float floatValue1 = (int)fromTransition;
                        result = RefiningNull(new Transition(floatValue1, fromTransition.IsNull));
                        return true;
                    case (TransitionType.String):
                        if (!float.TryParse(fromTransition, out float floatValue2))
                        {
                            if (stringFailOk)
                            {
                                result = TransitionType.Float.Null();
                                return true;
                            }

                            return false;
                        }
                        result = RefiningNull(new Transition(floatValue2, fromTransition.IsNull));
                        return true;
                    case (TransitionType.Pulse):
                        return false;
                    default:
                        return false;
                }

            case TransitionType.String:
                switch (fromTransition.Type)
                {
                    case (TransitionType.Pulse):
                        return false;
                    default:
                        result = RefiningNull(new Transition(fromTransition.GetValueString(), fromTransition.IsNull));
                        return true;
                }

            case (TransitionType.Pulse):
                return false;

            default:
                return false;
        }
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

    public static Transition[] GetNullArray(IEnumerable<TransitionType> transitionTypes)
    {
        if (transitionTypes == null)
        {
            throw new ArgumentNullException(nameof(transitionTypes), $"{nameof(GetNullArray)}: Argument cannot be null");
        }

        return transitionTypes.Select(type => type.Null()).ToArray();
    }

    public static Transition[] GetDefaultArray(IEnumerable<TransitionType> transitionTypes)
    {
        if (transitionTypes == null)
        {
            throw new ArgumentNullException(nameof(transitionTypes), $"{nameof(GetDefaultArray)}: Argument cannot be null");
        }

        return transitionTypes.Select(type => type.Default()).ToArray();
    }

    public static Color GetColor(this TransitionType transitionType) => transitionType switch
    {
        TransitionType.Bool => new Color(0f, 0.8f, 1f),
        TransitionType.Int => new Color(0f, 1f, 0f),
        TransitionType.Float => new Color(0.94f, 0.69f, 0f),
        TransitionType.String => new Color(0.94f, 0.34f, 1f),
        TransitionType.Pulse => new Color(1.0f, 1.0f, 1.0f),
        TransitionType.None => throw new TransitionNoneTypeException(),
        _ => Color.black,
    };

    public static string GetColorHexCodeString(this TransitionType transitionType, bool containSharp = false)
    {
        Color color = GetColor(transitionType);
        string colorString = ColorUtility.ToHtmlStringRGB(color);
        return containSharp ? "#" + colorString : colorString;
    }

    public static List<Transition> PutList(this Transition transition)
    {
        return new List<Transition>() { transition };
    }

    public static Transition[] PutArray(this Transition transition)
    {
        return new[] { transition };
    }
}

#region Exceptions
public class TransitionException : Exception
{
    public TransitionException(string message) : base($"<color=#39E336><b>TRANSITION ERROR ▶</b></color> {message}") { }
}

public class TransitionTypeCastException : TransitionException
{
    public TransitionType From { get; }
    public Type[] To { get; }

    public TransitionTypeCastException(TransitionType from, params Type[] to)
        : base($"<color=#7DAAFF><b>[CASTING]</b></color> Cannot cast from Transition Type <color={from.GetColorHexCodeString(true)}><b>{from.ToString()}</b></color> to Type <color=#7DAAFF><b>{string.Join(", ", to.Select(t => t.Name))}</b></color>.")
    {
        From = from;
        To = to;
    }
}

public class TransitionTypeConvertException : TransitionException
{
    public TransitionType From { get; }
    public TransitionType To { get; }

    public TransitionTypeConvertException(TransitionType from, TransitionType to, string message) : base(message)
    {
        From = from;
        To = to;
    }
}

public class TransitionIncompatibleTypeException : TransitionTypeConvertException
{
    public TransitionIncompatibleTypeException(TransitionType from, TransitionType to)
        : base(from, to,
            $"<color=#7DAAFF><b>[CONVERT]</b></color> Cannot convert between incompatible types: From [<color={from.GetColorHexCodeString(true)}><b>{from.ToString()}</b></color>] To [<color={to.GetColorHexCodeString(true)}><b>{to.ToString()}</b></color>]")
    { }
}

public class TransitionStringConversionException : TransitionTypeConvertException
{
    public string FromString { get; }
    public TransitionStringConversionException(TransitionType from, TransitionType to, string fromString) 
        : base(from, to, $"<color=#7DAAFF><b>[CONVERT]</b></color> Invalid numeric format in string: From [{fromString}: <color={from.GetColorHexCodeString(true)}><b>{from.ToString()}</b></color>] To [<color={to.GetColorHexCodeString(true)}><b>{to.ToString()}</b></color>]")
    {
        FromString = fromString;
    }
}

public class TransitionTypeMismatchException : TransitionException
{
    public TransitionType T1 { get; }
    public TransitionType T2 { get; }

    public TransitionTypeMismatchException(TransitionType t1, TransitionType t2)
        : base($"<color=#7DAAFF><b>[MISMATCH]</b></color> Cannot perform operation between incompatible transition types: <color={t1.GetColorHexCodeString(true)}><b>{t1}</b></color> and <color={t2.GetColorHexCodeString(true)}><b>{t2}</b></color>.")
    {
        T1 = t1;
        T2 = t2;
    }
}

public class TransitionNoneTypeException : TransitionException
{
    public TransitionNoneTypeException() : base("<color=#FF7B00><b>[NONE TYPE]</b></color> TransitionType.None cannot be used as a value.") { }
}

public class TransitionInvalidOperationException : TransitionException
{
    public string Operator { get; }
    public TransitionType[] Types { get; }

    public TransitionInvalidOperationException(string @operator, params TransitionType[] types)
        : base($"<color=#7DAAFF><b>[INVALID OPERATION]</b></color> The '<color=#7DAAFF><b>{@operator}</b></color>' operator cannot be applied to Transition of type <color=#7DAAFF><b>{string.Join(", ", types.Select(type => $"<color={type.GetColorHexCodeString(true)}>{type.ToString()}</color>"))}</b></color>.")
    {
        Operator = @operator;
        Types = types;
    }
}

public class TransitionArgumentNullException : TransitionException
{
    public TransitionArgumentNullException(string message) : base($"<color=#7DAAFF><b>[NULL ARGUMENT]</b></color> {message}") { }
}

public class TransitionTypeArgumentOutOfRangeException : TransitionException
{
    public TransitionType? OutRangeTransitionType { get; }
    public Type OutRangeType { get; }
    public TransitionTypeArgumentOutOfRangeException(TransitionType outRangeTrType)
        : base($"<color=#7DAAFF><b>[OUT OF RANGE]</b></color> TransitionType <color={outRangeTrType.GetColorHexCodeString(true)}><b>{outRangeTrType}</b></color> is not supported or out of range.")
    {
        OutRangeTransitionType = outRangeTrType;
        OutRangeType = null;
    }
    public TransitionTypeArgumentOutOfRangeException(Type outRangeType)
        : base($"<color=#7DAAFF><b>[OUT OF RANGE]</b></color> Type '<color=#7DAAFF><b>{outRangeType?.Name ?? "null"}</b></color>' is not supported or out of range.")
    {
        OutRangeTransitionType = null;
        OutRangeType = outRangeType;
    }
}

public class TransitionNullStringException : TransitionException
{
    public TransitionNullStringException(string message) : base($"<color=#FF7B00><b>[NULL STRING]</b></color> {message}") { }
}
#endregion