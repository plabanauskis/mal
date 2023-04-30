using System.Linq;

namespace Mal;

static class functions
{
    public static IMalType Add(params IMalType[] args) =>
        new MalInteger(args.Cast<MalInteger>().Aggregate(0, (a, b) => a + b.Value));

    public static IMalType Subtract(params IMalType[] args)
    {
        int? first = null;
        if (args.Length >= 1)
        {
            first = ((MalInteger)args[0]).Value;
        }
        
        return new MalInteger(args.Skip(1).Cast<MalInteger>().Aggregate(
            first.GetValueOrDefault(), (a, b) => a - b.Value));
    }

    public static IMalType Multiply(params IMalType[] args) =>
        new MalInteger(args.Cast<MalInteger>().Aggregate(1, (a, b) => a * b.Value));

    public static IMalType Divide(params IMalType[] args)
    {
        int? first = null;
        if (args.Length >= 1)
        {
            first = ((MalInteger)args[0]).Value;
        }

        return new MalInteger(args.Skip(1).Cast<MalInteger>().Aggregate(
            first.GetValueOrDefault(), (a, b) => a / b.Value));
    }
}
