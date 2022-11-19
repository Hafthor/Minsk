namespace Minsk;

public class Program
{
    public static int Main(string[] args)
    {
        for(; ;)
        {
            Console.Write("> ");
            string? line = Console.ReadLine();
            if (line == null || line == "") break;
            var d = Parser.LexParse(line).Eval();
            Console.WriteLine(d.String);
        }
        return 0;
    }
}

