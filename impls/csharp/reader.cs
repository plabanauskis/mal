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
            "(" => read_sequence(reader, MalSequence.MalSequenceType.List),
            "[" => read_sequence(reader, MalSequence.MalSequenceType.Vector),
            "{" => read_hash_map(reader),
            "'" => read_quote(reader, "quote"),
            "`" => read_quote(reader, "quasiquote"),
            "~" => read_quote(reader, "unquote"),
            "~@" => read_quote(reader, "splice-unquote"),
            "@" => read_deref(reader),
            "^" => read_meta(reader),
            var form when form.StartsWith(";") => null,
            _ => read_atom(reader)
        };

    private static MalSequence? read_quote(Reader reader, string type)
    {
        reader.next(); // consume "'"

        if (reader.peek() == null)
        {
            throw new ReaderException("unbalanced");
        }

        var form = read_form(reader);

        var list = new List<IMalType> { new MalSymbol(type), form};
        return new MalSequence(list, MalSequence.MalSequenceType.List);
    }

    private static MalSequence? read_deref(Reader reader)
    {
        reader.next(); // consume "@"

        if (reader.peek() == null)
        {
            throw new ReaderException("unbalanced");
        }

        var form = read_form(reader);

        var list = new List<IMalType> { new MalSymbol("deref"), form};
        return new MalSequence(list, MalSequence.MalSequenceType.List);
    }

    private static MalSequence? read_meta(Reader reader)
    {
        reader.next(); // consume "^"

        if (reader.peek() == null)
        {
            throw new ReaderException("unbalanced");
        }

        var metadataForm = read_form(reader);
        var form = read_form(reader);

        var list = new List<IMalType> { new MalSymbol("with-meta"), form, metadataForm };
        return new MalSequence(list, MalSequence.MalSequenceType.List);
    }

    private static MalSequence? read_sequence(Reader reader, MalSequence.MalSequenceType type)
    {
        var list = new List<IMalType>();
        reader.next(); // consume '(' or '['
        while (reader.peek() != null && reader.peek()?.Value !=
            type switch
            {
                MalSequence.MalSequenceType.List => ")",
                MalSequence.MalSequenceType.Vector => "]",
                _ => throw new NotImplementedException()
            })
        {
            var form = read_form(reader);
            
            if (form == null)
            {
                return null;
            }

            list.Add(form);
        }

        if (reader.peek()?.Value !=
            type switch
            {
                MalSequence.MalSequenceType.List => ")",
                MalSequence.MalSequenceType.Vector => "]",
                _ => throw new NotImplementedException()
            })
        {
            throw new ReaderException("unbalanced");
        }

        reader.next(); // consume ')' or ']'

        return new MalSequence(list, type);
    }

    private static MalHashMap read_hash_map(Reader reader)
    {
        var dict = new Dictionary<IMalType, IMalType>();
        reader.next(); // consume '{'

        while (reader.peek() != null && reader.peek()?.Value != "}")
        {
            var key = read_form(reader);

            if (key == null)
            {
                throw new ReaderException("unbalanced");
            }

            if (reader.peek() == null || reader.peek().Value == "}")
            {
                throw new ReaderException("unbalanced");
            }

            var value = read_form(reader);

            dict.Add(key, value);
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

    private static IMalScalarType? read_atom(Reader reader)
    {
        var token = reader.next();
        if (int.TryParse(token!.Value, out var intValue))
        {
            return new MalScalarType<int>(intValue);
        }

        return token.Value switch
        {
            "true" => new MalScalarType<bool>(true),
            "false" => new MalScalarType<bool>(false),
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