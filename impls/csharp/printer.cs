namespace Mal;

internal class Printer
{
    public static string? pr_str(IMalType? type, bool print_readably) =>
        print_readably ? type?.ToReadableString() : type?.ToString();
}
