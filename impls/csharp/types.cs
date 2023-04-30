using System.Collections.Immutable;

namespace Mal;

interface IMalType
{
    string ToString();
    string ToReadableString();
}

interface IAdditiveType : IMalType
{
    IMalType Add(IMalType other);
}

struct MalFunction : IMalType
{
    public MalFunction(Func<IMalType[], IMalType> function) => Function = function;

    public readonly Func<IMalType[], IMalType> Function { get; }

    public override string ToString() => $"<function>@{GetHashCode()}";
    public string ToReadableString() => ToString();
}

interface IMalSequence : IMalType
{
    IReadOnlyList<IMalType> Items { get; }
}

struct MalList : IMalSequence
{
    private const char PREFIX = '(';
    private const char SUFFIX = ')';

    public MalList(IImmutableList<IMalType> items) => Items = items;

    public IReadOnlyList<IMalType> Items { get; }

    public override string ToString() => $"{PREFIX}{string.Join(" ", Items)}{SUFFIX}";
    public string ToReadableString() => $"{PREFIX}{string.Join(" ", Items.Select(e => e.ToReadableString()))}{SUFFIX}";
}

struct MalVector : IMalSequence
{
    private const char PREFIX = '[';
    private const char SUFFIX = ']';

    public MalVector(IImmutableList<IMalType> items) => Items = items;

    public IReadOnlyList<IMalType> Items { get; }

    public override string ToString() => $"{PREFIX}{string.Join(" ", Items)}{SUFFIX}";
    public string ToReadableString() => $"{PREFIX}{string.Join(" ", Items.Select(e => e.ToReadableString()))}{SUFFIX}";
}

class MalHashMap : IMalType
{
    public MalHashMap(IImmutableDictionary<IMalType, IMalType> items) => Items = items;

    public IReadOnlyDictionary<IMalType, IMalType> Items { get; }

    public override string ToString() => $"{{{string.Join(" ", Items.Select(e => $"{e.Key} {e.Value}"))}}}";
    public string ToReadableString() => $"{{{string.Join(" ", Items.Select(e => $"{e.Key.ToReadableString()} {e.Value.ToReadableString()}"))}}}";
}

interface IMalScalarType<T> : IMalType
{
    T Value { get; }
}

struct MalInteger : IMalScalarType<int>
{
    public MalInteger(int value) => Value = value;

    public int Value { get;}

    override public string ToString() => Value.ToString()!.ToLower();
    public string ToReadableString() => ToString();
}

struct MalBoolean : IMalScalarType<bool>
{
    public MalBoolean(bool value) => Value = value;

    public bool Value { get; }

    override public string ToString() => Value.ToString()!.ToLower();
    public string ToReadableString() => ToString();
}

struct MalNil : IMalType
{
    override public string ToString() => "nil";
    public string ToReadableString() => ToString();
}

class MalString : IMalScalarType<string>
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

class MalKeywordType : IMalType
{
    public MalKeywordType(string name) => Name = name;

    public string Name { get; }

    override public string ToString() => Name;
    public string ToReadableString() => ToString();
}

record MalSymbol : IMalType, IEquatable<MalSymbol>
{
    public MalSymbol(string name) => Name = name;

    public string Name { get; init;}

    override public string ToString() => Name;
    public string ToReadableString() => ToString();
}
