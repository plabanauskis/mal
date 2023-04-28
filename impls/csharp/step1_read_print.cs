namespace Mal;

class step1_read_print
{
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

    static IMalType? EVAL(IMalType? arg) => arg;

    static string? PRINT(IMalType? arg) => Printer.pr_str(arg, true);

    static string? rep(string arg)
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