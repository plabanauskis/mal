using System.Collections.Immutable;

namespace Mal;

class step3_env
{
    internal class EvaluationException : Exception
    {
        public EvaluationException(string message) : base(message) { }
    }

    private static Env repl_env = new Env(null);

    static step3_env()
    {
        repl_env.Set(new MalSymbol("+"), new MalFunction(functions.Add));
        repl_env.Set(new MalSymbol("-"), new MalFunction(functions.Subtract));
        repl_env.Set(new MalSymbol("*"), new MalFunction(functions.Multiply));
        repl_env.Set(new MalSymbol("/"), new MalFunction(functions.Divide));
    }

    static IMalType? READ(string arg) => Reader.read_str(arg);

    private static IMalType eval_ast(IMalType ast, Env env) =>
        ast switch
        {
            _ when ast is MalSymbol symbol => env.Get(symbol),
            _ when ast is MalList list => new MalList(ImmutableList.CreateRange(list.Items.Select(i => EVAL(i, env)))),
            _ when ast is MalVector vector => new MalVector(ImmutableList.CreateRange(vector.Items.Select(i => EVAL(i, env)))),
            _ when ast is MalHashMap hashMap => new MalHashMap(ImmutableDictionary.CreateRange(hashMap.Items.Select(i => new KeyValuePair<IMalType, IMalType>(i.Key, EVAL(i.Value, env))))),
            _ => ast
        };

    static IMalType? EVAL(IMalType? ast, Env env)
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

        var symbol = (MalSymbol)list.Items[0];

        if (symbol.Name == "def!")
        {
            var result = EVAL(list.Items[2], env);
            env.Set((MalSymbol)list.Items[1], result);

            return result;
        }
        else if (symbol.Name == "let*")
        {
            var newEnv = new Env(env);

            var bindings = (IMalSequence)list.Items[1];

            for (var i = 0; i < bindings.Items.Count; i += 2)
            {
                var key = (MalSymbol)bindings.Items[i];
                var value = EVAL(bindings.Items[i + 1], newEnv);

                newEnv.Set(key, value);
            }

            return EVAL(list.Items[2], newEnv);
        }
        else
        {
            var evaluatedList = (MalList)eval_ast(list, env);

            if (evaluatedList.Items[0] is not MalFunction function)
            {
                throw new EvaluationException("First list item must be a function");
            }

            return function.Function(evaluatedList.Items.Skip(1).ToArray());
        }
    }

    static string? PRINT(IMalType? arg) => Printer.pr_str(arg, true);

    static string? rep(string arg)
    {
        try
        {
            var read = READ(arg);
            var eval = EVAL(read, repl_env);
            return PRINT(eval);
        }
        catch (Exception e) when (e is EvaluationException ||
                                  e is Env.EnvironmentException ||
                                  e is Reader.ReaderException)
        {
            Console.WriteLine(e.Message);
            return null;
        }
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