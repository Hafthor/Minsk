namespace Minsk;

public class Program
{
    public static int Main(string[] args)
    {
        bool prettyPrint = false;
        for (; ; )
        {
            Console.Write("> ");
            string? line = Console.ReadLine();
            if (line == null || line == "") break;
            if (line == "#pretty") { prettyPrint = !prettyPrint; continue; }
            var root = Parser.LexParse(line);
            if (prettyPrint) root.PrettyPrint("");
            var d = root.Eval();
            Console.WriteLine(d.String);
        }
        return 0;
    }
}