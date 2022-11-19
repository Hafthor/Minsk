namespace Minsk;

public class Token
{
    internal readonly string text;
    internal readonly int start, length;

    public Token(string text, int start, int length)
    {
        this.text = text;
        this.start = start;
        this.length = length;
        TextString = new Lazy<string>(() => Text.ToString());
    }
    internal Lazy<string> TextString { get; }
    public ReadOnlySpan<char> Text => text.AsSpan().Slice(start, length);
    public bool TextEquals(ReadOnlySpan<char> str) => Text.CompareTo(str, StringComparison.Ordinal) == 0;
    public bool TextEquals(string str) => Text.CompareTo(str, StringComparison.Ordinal) == 0;
    public virtual IValue Eval(Func<string, IValue>? func = null, Action<string, IValue>? assignFunc = null) => throw new Exception($"unhandled {GetType().Name}");
}

public class BadToken : Token
{
    public BadToken(string text, int start, int length) : base(text, start, length) { }
}

public class WhiteSpaceToken : Token
{
    public WhiteSpaceToken(string text, int start, int length) : base(text, start, length) { }
    public static Token? Lex(string text, ref int i)
    {
        if (!char.IsWhiteSpace(text[i])) return null;
        var start = i++;
        while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
        return new WhiteSpaceToken(text, start, i - start);
    }
}

public class CommentToken : Token
{
    public CommentToken(string text, int start, int length) : base(text, start, length) { }
    public static Token? Lex(string text, ref int i)
    {
        if (i + 1 >= text.Length || text[i] != '/' || (text[i + 1] != '/' && text[i + 1] != '*')) return null;
        var start = i++;
        if (text[i++] == '/')
            while (i < text.Length && text[i] != '\n' && text[i] != '\r') i++;
        else // handle /* style */ comments
            do
                while (i < text.Length && text[i++] != '*') ;
            while (i < text.Length && text[i++] != '/');
        return new CommentToken(text, start, i - start);
    }
}

public class NumberToken : Token
{
    public NumberToken(string text, int start, int length) : base(text, start, length) { }
    public static Token? Lex(string text, ref int i)
    {
        if (!char.IsDigit(text[i])) return null;
        var start = i++;
        while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.' && text.IndexOf('.', start) == i)) i++;
        return new NumberToken(text, start, i - start);
    }
    public override IValue Eval(Func<string, IValue>? func = null, Action<string, IValue>? assignFunc = null) => new DoubleValue(double.Parse(Text));
}

public class StringToken : Token
{
    public StringToken(string text, int start, int length) : base(text, start, length) => String = UnescapeString(text, start, length);
    public static Token? Lex(string text, ref int i)
    {
        if (text[i] != '"') return null;
        // token has Text = undecoded raw inner Text (not including quotes)
        var start = ++i;
        while (i < text.Length && text[i] != '"')
            if (text[i++] == '\\') i++; // skip char after escape
        return new StringToken(text, start, i++ - start);
    }
    public string String;
    private static string UnescapeString(string text, int start, int length)
    {
        int i = start, e = start + length;
        var sb = new System.Text.StringBuilder();
        for(; ;)
        {
            var j = text.IndexOf('\\', i);
            if (j < 0 || j >= e) return sb.Append(text, i, e - i).ToString();
            sb.Append(text, i, j - i).Append(UnescapeChar(text[j + 1]));
            i = j + 2;
        }
    }
    private static char UnescapeChar(char c) => c switch
    {
        '0' => '\0',
        'r' => '\r',
        'n' => '\n',
        'b' => '\b',
        'f' => '\f',
        'a' => '\a',
        't' => '\t',
        'v' => '\v',
        _ => c
    };
    public override IValue Eval(Func<string, IValue>? func = null, Action<string, IValue>? assignFunc = null) => new StringValue(String);
}

public class IdentifierToken : Token
{
    public IdentifierToken(string text, int start, int length) : base(text, start, length) { }
    public static Token? Lex(string text, ref int i)
    {
        if (!char.IsLetter(text[i]) || text[i] == '_') return null;
        var start = i;
        while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_')) i++;
        return new IdentifierToken(text, start, i - start);
    }
    public override IValue Eval(Func<string, IValue>? func = null, Action<string, IValue>? assignFunc = null) => func!(TextString.Value);
}

public class SymbolToken : Token
{
    public SymbolToken(string text, int start, int length) : base(text, start, length) { }
    private static readonly List<string> twoCharSymbols = new() { "!=", ">=", "<=" };
    private static readonly string oneCharSymbols = "!%*()-+=:<>/^{}[]";
    public static Token? Lex(string text, ref int i)
    {
        if (i+2<=text.Length && twoCharSymbols.Contains(text.Substring(i,2)))
        {
            var symbol = new SymbolToken(text, i, 2);
            i += 2;
            return symbol;
        }
        return oneCharSymbols.Contains(text[i]) ? new SymbolToken(text, i++, 1) : null;
    }
}

public class UnaryToken : Token
{
    private readonly SymbolToken root;
    private readonly Token right;
    public UnaryToken(SymbolToken root, Token right) : base(root.text, root.start, right.start + right.length - root.start)
    {
        this.root = root;
        this.right = right;
    }
    public override IValue Eval(Func<string, IValue>? func = null, Action<string, IValue>? assignFunc = null)
    {
        static DoubleValue EvalNot(IValue v) => new DoubleValue((v is StringValue ? string.IsNullOrEmpty(v.String) : v.Double == 0.0) ? 1.0 : 0.0);
        return root.Text switch
        {
            "+" => new DoubleValue(right.Eval(func).Double),
            "-" => new DoubleValue(-right.Eval(func).Double),
            "!" => EvalNot(right.Eval(func)),
            _ => throw new Exception($"unknown unary symbol {root.Text} in expression {Text}"),
        };
    }
}

public class BinaryToken : Token
{
    private readonly SymbolToken root;
    private readonly Token left, right;
    public BinaryToken(SymbolToken root, Token left, Token right) : base(left.text, left.start, right.start + right.length - left.start)
    {
        this.root = root;
        this.left = left;
        this.right = right;
    }
    public override IValue Eval(Func<string, IValue>? func = null, Action<string, IValue>? assignFunc = null)
    {
        var rv = right.Eval(func);
        if (root.Text.SequenceEqual(":")) // root.Text==":" does not work because root.Text is a ReadOnlySpan<char> :(
        {
            if (left is not IdentifierToken it) throw new Exception("Cannot assign to non-identifier");
            if (assignFunc is not null) assignFunc(it.TextString.Value, rv);
            return rv;
        }

        var lv = left.Eval(func);
        return root.Text switch
        {
            "+" => lv is StringValue ? new StringValue(lv.String + rv.String) : new DoubleValue(lv.Double + rv.Double),
            "-" => new DoubleValue(lv.Double - rv.Double),
            "*" => new DoubleValue(lv.Double * rv.Double),
            "/" => new DoubleValue(lv.Double / rv.Double),
            "%" => new DoubleValue(lv.Double % rv.Double),
            "^" => new DoubleValue(Math.Pow(lv.Double, rv.Double)),
            "=" => new DoubleValue((lv is StringValue ? lv.String == rv.String : lv.Double == rv.Double) ? 1.0 : 0.0),
            "!=" => new DoubleValue((lv is StringValue ? lv.String != rv.String : lv.Double != rv.Double) ? 1.0 : 0.0),
            ">" => new DoubleValue((lv is StringValue ? lv.String.CompareTo(rv.String) > 0 : lv.Double > rv.Double) ? 1.0 : 0.0),
            "<" => new DoubleValue((lv is StringValue ? lv.String.CompareTo(rv.String) < 0 : lv.Double < rv.Double) ? 1.0 : 0.0),
            ">=" => new DoubleValue((lv is StringValue ? lv.String.CompareTo(rv.String) >= 0 : lv.Double >= rv.Double) ? 1.0 : 0.0),
            "<=" => new DoubleValue((lv is StringValue ? lv.String.CompareTo(rv.String) <= 0 : lv.Double <= rv.Double) ? 1.0 : 0.0),
            _ => throw new Exception($"unknown binary symbol {root.Text} in expression {Text}"),
        };
    }
}

public class DerefToken : Token
{
    private readonly IdentifierToken root;
    private readonly Token right;
    public DerefToken(IdentifierToken root, Token right) : base(root.text, root.start, right.start + right.length - root.start)
    {
        this.root = root;
        this.right = right;
    }
    public override IValue Eval(Func<string, IValue>? func = null, Action<string, IValue>? assignFunc = null)
    {
        var rv = right.Eval(func);
        var lv = root.Eval(func);
        if (lv is DictionaryValue dv && rv is StringValue)
            return dv.ObjectByKey(rv.String);
        if (lv is ArrayValue av && rv is DoubleValue)
            return av.ObjectByIndex(rv.Double);
        throw new Exception($"unable to dereference {lv.GetType().Name} by {rv.GetType().Name}");
    }
}

public class MethodInvokeToken : Token
{
    private readonly IdentifierToken root;
    private readonly Token right;
    public MethodInvokeToken(IdentifierToken root, Token right) : base(root.text, root.start, right.start + right.length - root.start)
    {
        this.root = root;
        this.right = right;
    }
    public override IValue Eval(Func<string, IValue>? func = null, Action<string, IValue>? assignFunc = null) {
        var rv = right.Eval(func);
        var lv = root.Eval(func);
        if (lv is FunctionValue fv)
            return fv.InvokeWith(rv);
        throw new Exception($"unable to invoke {lv.GetType().Name}");
    }
}