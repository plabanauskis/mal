using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

interface IMalType
{
    string ToString();
    string ToReadableString();
}

class MalSequence : IMalType
{
    internal enum MalSequenceType
    {
        List,
        Vector
    }

    public MalSequence(IEnumerable<IMalType> items, MalSequenceType type)
    {
        Items = items.ToList();
        Type = type;
    }

    public List<IMalType> Items { get; }
    private MalSequenceType Type { get; }

    private char prefix => Type switch
    {
        MalSequenceType.List => '(',
        MalSequenceType.Vector => '[',
        _ => throw new System.NotImplementedException()
    };

    private char suffix => Type switch
    {
        MalSequenceType.List => ')',
        MalSequenceType.Vector => ']',
        _ => throw new System.NotImplementedException()
    };

    public override string ToString() => $"{prefix}{string.Join(" ", Items)}{suffix}";
    public string ToReadableString() => $"{prefix}{string.Join(" ", Items.Select(e => e.ToReadableString()))}{suffix}";
}

class MalHashMap : IMalType
{
    public MalHashMap(IDictionary<IMalType, IMalType> items) => Items = items;

    public IDictionary<IMalType, IMalType> Items { get; }

    public override string ToString() => $"{{{string.Join(" ", Items.Select(e => $"{e.Key} {e.Value}"))}}}";
    public string ToReadableString() => $"{{{string.Join(" ", Items.Select(e => $"{e.Key.ToReadableString()} {e.Value.ToReadableString()}"))}}}";
}

interface IMalScalarType : IMalType { }

struct MalScalarType<T> : IMalScalarType
    where T : struct
{
    public MalScalarType(T value) => Value = value;

    public T Value { get; }

    override public string ToString() => Value.ToString()!.ToLower();
    public string ToReadableString() => ToString();
}

struct MalNil : IMalScalarType
{
    override public string ToString() => "nil";
    public string ToReadableString() => ToString();
}

class MalString : IMalScalarType
{
    public MalString(string value, string readableValue)
    {
        Value = value;
        ReadableValue = readableValue;
    }

    public string Value { get; private set; }
    public string ReadableValue { get; private set; }

    override public string ToString() => Value;
    public string ToReadableString() => ReadableValue;
}

class MalKeywordType : IMalScalarType
{
    public MalKeywordType(string name) => Name = name;

    public string Name { get; }

    override public string ToString() => Name;
    public string ToReadableString() => ToString();
}

class MalSymbol : IMalScalarType
{
    public MalSymbol(string name) => Name = name;

    public string Name { get; }

    override public string ToString() => Name;
    public string ToReadableString() => ToString();
}
