using System.Collections.Immutable;

namespace Mal;

class step2_eval
{
    internal class EvaluationException : Exception
    {
        public EvaluationException(string message) : base(message) { }
    }

    private static IDictionary<MalSymbol, IMalType> repl_env =
        new Dictionary<MalSymbol, IMalType>
        {
            [new MalSymbol("+")] = new MalFunction(functions.Add),
            [new MalSymbol("-")] = new MalFunction(functions.Subtract),
            [new MalSymbol("*")] = new MalFunction(functions.Multiply),
            [new MalSymbol("/")] = new MalFunction(functions.Divide)
        };

    static IMalType? READ(string arg)
    {
        try
        {
            return Reader.read_str(arg);
        }
        catch (Reader.ReaderException e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    private static IMalType eval_ast(IMalType ast, IDictionary<MalSymbol, IMalType> env)
    {
        IMalType eval_ast_symbol(MalSymbol symbol, IDictionary<MalSymbol, IMalType> env)
        {
            if (env.TryGetValue(symbol, out var value))
            {
                return value;
            }

            throw new EvaluationException($"Symbol {symbol} not found");
        }

        return ast switch
        {
            _ when ast is MalSymbol symbol => eval_ast_symbol(symbol, env),
            _ when ast is MalList list => new MalList(ImmutableList.CreateRange(list.Items.Select(i => EVAL(i, env)))),
            _ when ast is MalVector vector => new MalVector(ImmutableList.CreateRange(vector.Items.Select(i => EVAL(i, env)))),
            _ when ast is MalHashMap hashMap => new MalHashMap(ImmutableDictionary.CreateRange(hashMap.Items.Select(i => new KeyValuePair<IMalType, IMalType>(i.Key, EVAL(i.Value, env))))),
            _ => ast
        };
    }

    static IMalType? EVAL(IMalType? ast, IDictionary<MalSymbol, IMalType> env)
    {
        try
        {
            if (ast == null)
            {
                throw new EvaluationException("Cannot evaluate null");
            }

            if (ast is not MalList list)
            {
                return eval_ast(ast, env);
            }

            if (list.Items.Count == 0)
            {
                return list;
            }

            var evaluatedList = (MalList)eval_ast(list, env);

            if (evaluatedList.Items[0] is not MalFunction function)
            {
                throw new EvaluationException("First list item must be a function");
            }

            return function.Function(evaluatedList.Items.Skip(1).ToArray());
        }
        catch (EvaluationException e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    static string? PRINT(IMalType? arg) => Printer.pr_str(arg, true);

    static string? rep(string arg)
    {
        var read = READ(arg);
        var eval = EVAL(read, repl_env);
        return PRINT(eval);
    }

    static void Main()
    {
        while (true)
        {
            var line = readline.ReadLine("user> ");
            Console.WriteLine(rep(line));
        }
    }
}