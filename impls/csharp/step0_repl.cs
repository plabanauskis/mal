namespace Mal;

class step0_repl
{
    static string? READ(string? arg) => arg;

    static string? EVAL(string? arg) => arg;

    static string? PRINT(string? arg) => arg;

    static string? rep(string? arg)
    {
        var read = READ(arg);
        var eval = EVAL(read);
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