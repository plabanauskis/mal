using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace Mal;

internal class Reader
{
    internal record Token(string Value);

    internal class ReaderException: Exception
    {
        public ReaderException(string message) : base(message) { }
    }

    private int _position = 0;
    private IReadOnlyList<Token> _tokens;

    public Reader(IEnumerable<Token> tokens) => _tokens = tokens.ToList();

    public Token? next() => _position < _tokens.Count ? _tokens[_position++] : null;

    public Token? peek() => _position < _tokens.Count ? _tokens[_position] : null;

    private static IEnumerable<Token> tokenize(string str)
    {
        var tokenRegExp = new Regex(@"[\s,]*(~@|[\[\]{}()'`~^@]|""(?:\\.|[^\\""])*""?|;.*|[^\s\[\]{}('""`,;)]*)");

        var tokens = tokenRegExp.Matches(str).Cast<Match>().Select(m => m.Groups[1].Value).Select(s => new Token(s));

        return tokens;
    }

    private static IMalType? read_form(Reader reader) =>
        reader.peek()?.Value switch
        {
            "(" => read_sequence(reader),
            "[" => read_sequence(reader),
            "{" => read_hash_map(reader),
            "'" => read_quote(reader, "quote"),
            "`" => read_quote(reader, "quasiquote"),
            "~" => read_quote(reader, "unquote"),
            "~@" => read_quote(reader, "splice-unquote"),
            "@" => read_deref(reader),
            "^" => read_meta(reader),
            var form when form!.StartsWith(";") => null,
            _ => read_atom(reader)
        };

    private static IMalSequence? read_quote(Reader reader, string type)
    {
        reader.next(); // consume "'"

        var form = read_form(reader);

        if (form == null)
        {
            throw new ReaderException("unbalanced");
        }

        var list = ImmutableList.Create(new MalSymbol(type), form);
        return new MalList(list);
    }

    private static IMalSequence? read_deref(Reader reader)
    {
        reader.next(); // consume "@"

        var form = read_form(reader);

        if (form == null)
        {
            throw new ReaderException("unbalanced");
        }

        var list = ImmutableList.Create(new MalSymbol("deref"), form);
        return new MalList(list);
    }

    private static IMalSequence? read_meta(Reader reader)
    {
        reader.next(); // consume "^"

        var metadataForm = read_form(reader);
        var form = read_form(reader);

        if (metadataForm == null || form == null)
        {
            throw new ReaderException("unbalanced");
        }

        var list = ImmutableList.Create(new MalSymbol("with-meta"), form, metadataForm);
        return new MalList(list);
    }

    private enum MalSequenceType
    {
        List,
        Vector
    }

    private static IMalSequence? read_sequence(Reader reader)
    {
        var list = ImmutableList.Create<IMalType>();
        
        var type = reader.next()!.Value switch // consume '(' or '['
        {
            "(" => MalSequenceType.List,
            "[" => MalSequenceType.Vector,
            _ => throw new NotImplementedException()
        };

        var suffix = type switch
        {
            MalSequenceType.List => ")",
            MalSequenceType.Vector => "]",
            _ => throw new NotImplementedException()
        };

        while (reader.peek() != null && reader.peek()?.Value != suffix)
        {
            var form = read_form(reader);
            
            if (form == null)
            {
                return null;
            }

            list = list.Add(form);
        }

        if (reader.peek()?.Value != suffix)
        {
            throw new ReaderException("unbalanced");
        }

        reader.next(); // consume ')' or ']'

        return type switch
        {
            MalSequenceType.List => new MalList(list),
            MalSequenceType.Vector => new MalVector(list),
            _ => throw new NotImplementedException()
        };
    }

    private static MalHashMap read_hash_map(Reader reader)
    {
        var dict = ImmutableDictionary.Create<IMalType, IMalType>();
        reader.next(); // consume '{'

        while (reader.peek() != null && reader.peek()?.Value != "}")
        {
            var key = read_form(reader);

            if (key == null)
            {
                throw new ReaderException("unbalanced");
            }

            if (reader.peek() == null || reader.peek()!.Value == "}")
            {
                throw new ReaderException("unbalanced");
            }

            var value = read_form(reader);

            if (value == null)
            {
                throw new ReaderException("unbalanced");
            }

            dict = dict.Add(key, value);
        }

        reader.next(); // consume '}'

        return new MalHashMap(dict);
    }

    private static MalString ReadString(string s)
    {
        string ParseInput(string s)
        {
            var sb = new StringBuilder();

            for (var i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\')
                {
                    if (++i >= s.Length)
                    {
                        throw new ReaderException("unbalanced");
                    }

                    switch (s[i])
                    {
                        case '"':
                            sb.Append('"');
                            break;
                        case '\\':
                            sb.Append('\\');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        default:
                            throw new ReaderException("unbalanced");
                    }
                }
                else
                {
                    sb.Append(s[i]);
                }
            }

            return sb.ToString();
        }

        if (s[0] == '"' && s[^1] == '"' && s.Length > 1)
        {
            s = s[1..^1];

            if (s.Length > 0 && s[^1] == '\\' && (s.Length < 2 || s[^2] != '\\'))
            {
                throw new ReaderException("unbalanced");
            }

            var value = ParseInput(s);
            var readableValue = $"\"{s}\"";

            return new MalString(value, readableValue);
        }

        throw new ReaderException("unbalanced");
    }

    private static IMalType? read_atom(Reader reader)
    {
        var token = reader.next();
        if (int.TryParse(token!.Value, out var intValue))
        {
            return new MalInteger(intValue);
        }

        return token.Value switch
        {
            "true" => new MalBoolean(true),
            "false" => new MalBoolean(false),
            "nil" => new MalNil(),
            (var s) when s.StartsWith('"') => ReadString(s),
            (var s) when s.StartsWith(':') => new MalKeywordType(s),
            _ => new MalSymbol(token.Value)
        };
    }

    public static IMalType? read_str(string str)
    {
        var tokens = tokenize(str);
        var reader = new Reader(tokens);
        return read_form(reader);
    }
}