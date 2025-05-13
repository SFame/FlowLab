using OdinSerializer;
using System;
using System.Linq;

public enum TransitionType
{
    Null = 1,
    Bool = 2,
    Int = 4,
    Float = 8,
}

[Serializable]
public struct Transition : IEquatable<Transition>
{
    public Transition(TransitionType type, TransitionValue value)
    {
        Type = type;
        Value = value;
    }

    [OdinSerialize] public TransitionType Type { get; }
    [OdinSerialize] public TransitionValue Value { get; }

    #region Overriding
    public override bool Equals(object obj) => obj is Transition other && Equals(other);

    public bool Equals(Transition other)
    {
        if (Type != other.Type)
            return false;

        return Type switch
        {
            TransitionType.Null => true,
            TransitionType.Bool => Value.BoolValue == other.Value.BoolValue,
            TransitionType.Int => Value.IntValue == other.Value.IntValue,
            TransitionType.Float => Value.FloatValue.Equals(other.Value.FloatValue),
            _ => false
        };
    }

    public override int GetHashCode()
    {
        return Type switch
        {
            TransitionType.Null => Type.GetHashCode(),
            TransitionType.Bool => HashCode.Combine(Type, Value.BoolValue),
            TransitionType.Int => HashCode.Combine(Type, Value.IntValue),
            TransitionType.Float => HashCode.Combine(Type, Value.FloatValue),
            _ => 0
        };
    }

    public override string ToString()
    {
        string value = Type switch
        {
            TransitionType.Null => "Null",
            TransitionType.Bool => Value.BoolValue.ToString(),
            TransitionType.Int => Value.IntValue.ToString(),
            TransitionType.Float => Value.FloatValue.ToString(),
            _ => "Unknown Type"
        };

        return $"Type: {Type}\nValue: {value}";
    }

    #endregion

    #region Casting
    // ---------- Bool ----------
    public static implicit operator Transition(bool b)
    {
        return new Transition(TransitionType.Bool, new TransitionValue(boolValue: b));
    }

    public static explicit operator bool(Transition t)
    {
        if (t.Type != TransitionType.Bool)
        {
            throw new TransitionTypeCastException(TransitionType.Bool, typeof(bool));
        }

        return t.Value.BoolValue;
    }

    // ---------- Int ----------
    public static implicit operator Transition(int i)
    {
        return new Transition(TransitionType.Int, new TransitionValue(intValue: i));
    }

    public static explicit operator int(Transition t)
    {
        if (t.Type != TransitionType.Int)
        {
            throw new TransitionTypeCastException(TransitionType.Int, typeof(int));
        }

        return t.Value.IntValue;
    }

    // ---------- Float ----------
    public static implicit operator Transition(float f)
    {
        return new Transition(TransitionType.Float, new TransitionValue(floatValue: f));
    }

    public static explicit operator float(Transition t)
    {
        if (t.Type != TransitionType.Float)
        {
            throw new TransitionTypeCastException(TransitionType.Float, typeof(float));
        }

        return t.Value.FloatValue;
    }
    #endregion

    #region Arithmetic Operator
    public static Transition operator +(Transition t) => t.Type switch
    {
        TransitionType.Int => new Transition(t.Type, new TransitionValue(intValue: +t.Value.IntValue)),
        TransitionType.Float => new Transition(t.Type, new TransitionValue(floatValue: +t.Value.FloatValue)),
        _ => throw new TransitionTypeCastException(t.Type, typeof(int), typeof(float))
    };

    public static Transition operator -(Transition t) => t.Type switch
    {
        TransitionType.Int => new Transition(t.Type, new TransitionValue(intValue: -t.Value.IntValue)),
        TransitionType.Float => new Transition(t.Type, new TransitionValue(floatValue: -t.Value.FloatValue)),
        _ => throw new TransitionTypeCastException(t.Type, typeof(int), typeof(float))
    };

    public static Transition operator +(Transition t1, Transition t2) => Arithmetic(t1, t2, "+");
    public static Transition operator -(Transition t1, Transition t2) => Arithmetic(t1, t2, "-");
    public static Transition operator *(Transition t1, Transition t2) => Arithmetic(t1, t2, "*");
    public static Transition operator /(Transition t1, Transition t2) => Arithmetic(t1, t2, "/");
    public static Transition operator %(Transition t1, Transition t2) => Arithmetic(t1, t2, "%");

    private static Transition Arithmetic(Transition t1, Transition t2, string op)
    {
        if (t1.Type != t2.Type)
            throw new TransitionTypeMismatchException(t1.Type, t2.Type);

        if (t1.Type != TransitionType.Int && t1.Type != TransitionType.Float)
            throw new TransitionTypeCastException(t1.Type, typeof(int), typeof(float));

        return t1.Type switch
        {
            TransitionType.Int => new Transition(t1.Type, new TransitionValue(intValue: ApplyIntOp(t1.Value.IntValue, t2.Value.IntValue, op))),
            TransitionType.Float => new Transition(t1.Type, new TransitionValue(floatValue: ApplyFloatOp(t1.Value.FloatValue, t2.Value.FloatValue, op))),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static int ApplyIntOp(int a, int b, string op) => op switch
    {
        "+" => a + b,
        "-" => a - b,
        "*" => a * b,
        "/" => b != 0 ? a / b : throw new DivideByZeroException(),
        "%" => b != 0 ? a % b : throw new DivideByZeroException(),
        _ => throw new InvalidOperationException($"Unsupported int operator: {op}")
    };

    private static float ApplyFloatOp(float a, float b, string op) => op switch
    {
        "+" => a + b,
        "-" => a - b,
        "*" => a * b,
        "/" => b != 0f ? a / b : throw new DivideByZeroException(),
        "%" => b != 0f ? a % b : throw new DivideByZeroException(),
        _ => throw new InvalidOperationException($"Unsupported float operator: {op}")
    };
    #endregion

    #region Comparison Operators
    public static bool operator ==(Transition t1, Transition t2) => t1.Equals(t2);
    public static bool operator !=(Transition t1, Transition t2) => !t1.Equals(t2);
    public static bool operator <(Transition t1, Transition t2) => CompareNumeric(t1, t2, "<");
    public static bool operator <=(Transition t1, Transition t2) => CompareNumeric(t1, t2, "<=");
    public static bool operator >(Transition t1, Transition t2) => CompareNumeric(t1, t2, ">");
    public static bool operator >=(Transition t1, Transition t2) => CompareNumeric(t1, t2, ">=");

    private static bool CompareNumeric(Transition t1, Transition t2, string op)
    {
        if (t1.Type != t2.Type)
            throw new TransitionTypeMismatchException(t1.Type, t2.Type);

        if (t1.Type != TransitionType.Int && t1.Type != TransitionType.Float)
            throw new TransitionTypeCastException(t1.Type, typeof(int), typeof(float));

        return t1.Type switch
        {
            TransitionType.Int => ApplyIntComparison(t1.Value.IntValue, t2.Value.IntValue, op),
            TransitionType.Float => ApplyFloatComparison(t1.Value.FloatValue, t2.Value.FloatValue, op),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static bool ApplyIntComparison(int a, int b, string op) => op switch
    {
        "<" => a < b,
        "<=" => a <= b,
        ">" => a > b,
        ">=" => a >= b,
        _ => throw new InvalidOperationException($"Unsupported int comparison: {op}")
    };

    private static bool ApplyFloatComparison(float a, float b, string op) => op switch
    {
        "<" => a < b,
        "<=" => a <= b,
        ">" => a > b,
        ">=" => a >= b,
        _ => throw new InvalidOperationException($"Unsupported float comparison: {op}")
    };
    #endregion
}

public struct TransitionValue
{
    public TransitionValue(bool boolValue = false, int intValue = 0, float floatValue = 0f)
    {
        BoolValue = boolValue;
        IntValue = intValue;
        FloatValue = floatValue;
    }

    [OdinSerialize] public bool BoolValue { get; }
    [OdinSerialize] public int IntValue { get; }
    [OdinSerialize] public float FloatValue { get; }
}

public static class TransitionUtil
{
    public static Type AsType(this TransitionType transitionType) => transitionType switch
    {
        TransitionType.Null => null,
        TransitionType.Bool => typeof(bool),
        TransitionType.Int => typeof(int),
        TransitionType.Float => typeof(float),
        _ => throw new ArgumentOutOfRangeException()
    };
}

public class TransitionTypeCastException : InvalidCastException
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

public class TransitionTypeMismatchException : Exception
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