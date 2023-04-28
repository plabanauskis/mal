namespace Mal;

internal class readline
{
    internal class AutoComplete : System.IAutoCompleteHandler
    {
        public char[] Separators { get; set; } = new char[] { ' ', '.', '/' };

        public string[] GetSuggestions(string text, int index)
        {
            var history = System.ReadLine.GetHistory();

            return history.Where(e => e.StartsWith(text)).ToArray();
        }
    }

    static readline() => System.ReadLine.AutoCompletionHandler = new AutoComplete();

    public static string ReadLine(string prompt)
    {
        var line = System.ReadLine.Read(prompt);
        System.ReadLine.AddHistory(line);
        return line;
    }
}