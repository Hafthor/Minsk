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
    public virtual IValue Eval(Variables vars) => throw new Exception($"unhandled {GetType().Name}");
    public virtual void PrettyPrint(string indent) => Console.WriteLine($"{indent} {this.GetType().Name} - text={Text}");
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
    public override IValue Eval(Variables vars) => new DoubleValue(double.Parse(Text));
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
        for (; ; )
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
    public override IValue Eval(Variables vars) => new StringValue(String);
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
    public override IValue Eval(Variables vars) => vars.Get(TextString.Value);
}

public class SymbolToken : Token
{
    public SymbolToken(string text, int start, int length) : base(text, start, length) { }
    private static readonly List<string> twoCharSymbols = new() { "!=", ">=", "<=", "[]", "{}", "??", "!?", "::" };
    private static readonly string oneCharSymbols = "!%*()-+=:<>/^{}[].;?";
    public static Token? Lex(string text, ref int i)
    {
        if (i + 2 <= text.Length && twoCharSymbols.Contains(text.Substring(i, 2)))
        {
            var symbol = new SymbolToken(text, i, 2);
            i += 2;
            return symbol;
        }
        return oneCharSymbols.Contains(text[i]) ? new SymbolToken(text, i++, 1) : null;
    }
}

public class EmptyObjectToken : Token
{
    public EmptyObjectToken(SymbolToken token) : base(token.text, token.start, token.length) { }
    public override IValue Eval(Variables vars) => Text switch
    {
        "[]" => new ArrayValue(),
        "{}" => new DictionaryValue(),
        _ => throw new Exception("Unexpected EmptyObjectToken")
    };
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
    public override IValue Eval(Variables vars)
    {
        static DoubleValue EvalNot(IValue v) => (v is StringValue ? string.IsNullOrEmpty(v.String) : v.Double == 0.0) ? DoubleValue.One : DoubleValue.Zero;
        return root.Text switch
        {
            "+" => new DoubleValue(right.Eval(vars).Double),
            "-" => new DoubleValue(-right.Eval(vars).Double),
            "!" => EvalNot(right.Eval(vars)),
            _ => throw new Exception($"unknown unary symbol {root.Text} in expression {Text}"),
        };
    }
    public override void PrettyPrint(string indent)
    {
        Console.WriteLine($"{indent} {this.GetType().Name} - text={Text}");
        root.PrettyPrint(indent + "  ");
        right.PrettyPrint(indent + "  ");
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
    public override IValue Eval(Variables vars)
    {
        if (root.Text.SequenceEqual(";"))
        {
            left.Eval(vars);
            return right.Eval(vars);
        }
        if (root.Text.SequenceEqual("?"))
        {
            for (; ; )
            {
                var lv2 = left.Eval(vars);
                if (lv2.Double == 0.0) return lv2;
                right.Eval(vars);
            }
        }
        if (root.Text.SequenceEqual("??"))
        {
            var lv2 = left.Eval(vars);
            return lv2.Double == 0.0 ? lv2 : right.Eval(vars);
        }
        if (root.Text.SequenceEqual("!?"))
        {
            var lv2 = left.Eval(vars);
            return lv2.Double != 0.0 ? lv2 : right.Eval(vars);
        }
        if (root.Text.SequenceEqual("::"))
        {
            if (left is BinaryToken bt && bt.root is SymbolToken st &&  (st.Text.SequenceEqual("??") || st.Text.SequenceEqual("!?")))
            {
                var lv2 = bt.left.Eval(vars).Double;
                if (st.Text.SequenceEqual("!?")) lv2 = lv2 == 0.0 ? 1.0 : 0.0;
                return lv2 != 0.0 ? bt.right.Eval(vars) : right.Eval(vars);
            }
            throw new Exception("Unbalanced else(::) found");
        }
        if (root.Text.SequenceEqual("."))
        {
            var lv2 = left.Eval(vars);
            if (lv2 is DictionaryValue dv && right is IdentifierToken iv)
                return dv.ObjectByKey(iv.TextString.Value);
            throw new Exception($"cannot dereference {lv2.GetType().Name} by {right.GetType().Name}");
        }
        if (root.Text.SequenceEqual(":") && left is MethodInvokeToken mit)
            return (IValue)mit.Set(vars, right);
        var rv = right.Eval(vars);
        if (root.Text.SequenceEqual(":")) // root.Text==":" does not work because root.Text is a ReadOnlySpan<char> :(
        {
            if (left is BinaryToken bt && bt.root.Text.SequenceEqual(".")) { bt.SetDot(rv, vars); return rv; }
            if (left is DerefToken dt) { dt.Set(rv, vars); return rv; }
            if (left is not IdentifierToken it) throw new Exception("Cannot assign to non-identifier");
            vars.Set(it.TextString.Value, rv);
            return rv;
        }

        var lv = left.Eval(vars);
        if (lv is DoubleValue ldv)
            return EvalDoubleOperator(ldv, rv);
        if (lv is StringValue lsv)
            return EvalStringOperator(lsv, rv);
        if (lv is ArrayValue lav)
            return EvalArrayOperator(lav, rv);
        if (lv is DictionaryValue lov)
            return EvalDictionaryOperator(lov, rv);
        if (lv is FunctionValue lfv)
            return EvalFunctionOperator(lfv, rv);
        throw new Exception($"Unknown lvalue type {lv.GetType().Name}");
    }

    private IValue EvalDoubleOperator(IValue lv, IValue rv) => root.Text switch
    {
        "+" => new DoubleValue(lv.Double + rv.Double),
        "-" => new DoubleValue(lv.Double - rv.Double),
        "*" => new DoubleValue(lv.Double * rv.Double),
        "/" => new DoubleValue(lv.Double / rv.Double),
        "%" => new DoubleValue(lv.Double % rv.Double),
        "^" => new DoubleValue(Math.Pow(lv.Double, rv.Double)),
        "=" => lv.Double == rv.Double ? DoubleValue.One : DoubleValue.Zero,
        "!=" => lv.Double != rv.Double ? DoubleValue.One : DoubleValue.Zero,
        ">" => lv.Double > rv.Double ? DoubleValue.One : DoubleValue.Zero,
        "<" => lv.Double < rv.Double ? DoubleValue.One : DoubleValue.Zero,
        ">=" => lv.Double >= rv.Double ? DoubleValue.One : DoubleValue.Zero,
        "<=" => lv.Double <= rv.Double ? DoubleValue.One : DoubleValue.Zero,
        _ => throw new Exception($"unknown binary symbol {root.Text} in expression {Text}"),
    };

    private IValue EvalStringOperator(StringValue lv, IValue rv) => root.Text switch
    {
        "+" => new StringValue(lv.String + rv.String),
        "-" => new DoubleValue(lv.Double - rv.Double),
        "*" => new DoubleValue(lv.Double * rv.Double),
        "/" => new DoubleValue(lv.Double / rv.Double),
        "%" => new DoubleValue(lv.Double % rv.Double),
        "^" => new DoubleValue(Math.Pow(lv.Double, rv.Double)),
        "=" => lv.String == rv.String ? DoubleValue.One : DoubleValue.Zero,
        "!=" => lv.String != rv.String ? DoubleValue.One : DoubleValue.Zero,
        ">" => lv.String.CompareTo(rv.String) > 0 ? DoubleValue.One : DoubleValue.Zero,
        "<" => lv.String.CompareTo(rv.String) < 0 ? DoubleValue.One : DoubleValue.Zero,
        ">=" => lv.String.CompareTo(rv.String) >= 0 ? DoubleValue.One : DoubleValue.Zero,
        "<=" => lv.String.CompareTo(rv.String) <= 0 ? DoubleValue.One : DoubleValue.Zero,
        _ => throw new Exception($"unknown binary symbol {root.Text} in expression {Text}"),
    };

    private IValue EvalArrayOperator(ArrayValue av, IValue rv) => root.Text switch
    {
        _ => throw new Exception($"unknown binary symbol {root.Text} in expression {Text}"),
    };

    private IValue EvalDictionaryOperator(DictionaryValue ov, IValue rv) => root.Text switch
    {
        _ => throw new Exception($"unknown binary symbol {root.Text} in expression {Text}"),
    };

    private IValue EvalFunctionOperator(FunctionValue ov, IValue rv) => root.Text switch
    {
        _ => throw new Exception($"unknown binary symbol {root.Text} in expression {Text}"),
    };

    public void SetDot(IValue value, Variables vars)
    {
        var lv2 = left.Eval(vars);
        if (lv2 is DictionaryValue dv && right is IdentifierToken iv)
            dv.SetObjectByKey(iv.TextString.Value, value);
        else throw new Exception($"Cannot assign to {lv2.GetType().Name} . {right.GetType().Name}");
    }
    public override void PrettyPrint(string indent)
    {
        Console.WriteLine($"{indent} {this.GetType().Name} - text={Text}");
        root.PrettyPrint(indent + "  ");
        left.PrettyPrint(indent + "  ");
        right.PrettyPrint(indent + "  ");
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
    public override IValue Eval(Variables vars)
    {
        IValue rv = right.Eval(vars), lv = root.Eval(vars);
        if (lv is DictionaryValue dv && rv is StringValue)
            return dv.ObjectByKey(rv.String);
        if (lv is ArrayValue av && rv is DoubleValue)
            return av.ObjectByIndex(rv.Double);
        throw new Exception($"unable to dereference {lv.GetType().Name} by {rv.GetType().Name}");
    }
    public void Set(IValue value, Variables vars)
    {
        var indexOrKey = right.Eval(vars);
        IValue rv = right.Eval(vars), lv = root.Eval(vars);
        if (lv is DictionaryValue dv && rv is StringValue)
            dv.SetObjectByKey(rv.String, value);
        else if (lv is ArrayValue av && rv is DoubleValue)
            av.SetObjectByIndex(rv.Double, value);
        else throw new Exception($"cannot assign to {lv.GetType().Name} by {rv.GetType().Name}");
    }
    public override void PrettyPrint(string indent)
    {
        Console.WriteLine($"{indent} {this.GetType().Name} - text={Text}");
        root.PrettyPrint(indent + "  ");
        right.PrettyPrint(indent + "  ");
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
    public override IValue Eval(Variables vars)
    {
        IValue rv = right.Eval(vars), lv = root.Eval(vars);
        if (lv is FunctionValue fv)
            return fv.InvokeWith(rv, vars);
        throw new Exception($"unable to invoke {lv.GetType().Name}");
    }
    public override void PrettyPrint(string indent)
    {
        Console.WriteLine($"{indent} {this.GetType().Name} - text={Text}");
        root.PrettyPrint(indent + "  ");
        right.PrettyPrint(indent + "  ");
    }
    public FunctionValue Set(Variables vars, Token definition)
    {
        var methodName = this.root.TextString.Value;
        var parameterName = (this.right as IdentifierToken)!.TextString.Value;

        var fv  = new FunctionValue(parameterName, (n) => definition.Eval(vars));
        vars.Set(methodName, fv);
        return fv;
    }
}