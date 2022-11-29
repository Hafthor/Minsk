namespace Minsk;

public class Parser
{
    private static readonly string openParenOperators = "{[(", closeParenOperators = "}])";
    private static readonly List<string> emptyObjectOperators = new List<string>() { "[]", "{}", "~" }; // array,dict,break
    private static readonly List<List<string>> unaryOperators = new()
    {
        new() { "+", "-" }, // unary plus, minus
        new() { "!" }, // not
        new() { "~~" }, // return
    }, binaryOperators = new()
    {
        new() { "." }, // deref
        new() { "^" }, // exp
        new() { "*", "/", "%" }, // mul, div, mod
        new() { "+", "-" }, // add, sub
        new() { "=", "!=", ">", "<", ">=", "<=" }, // equality
        new() { ":" }, // assign
        new() { "?", "??", "!?", "::" }, // while, if/notif, else
        new() { ";" }, // seperator
    };

    internal static readonly List<string> AllSymbols =
        (openParenOperators + closeParenOperators).ToCharArray().ToList().Select(c => "" + c)
        .Union(emptyObjectOperators)
        .Union(unaryOperators.SelectMany(s => s))
        .Union(binaryOperators.SelectMany(s => s))
        .ToList();

    private Parser() { }

    public static Token LexParse(string text) => Parse(Lex(text));

    private static List<Token> Lex(string text)
    {
        var tokens = new List<Token>();
        for (int i = 0; i < text.Length;)
            tokens.Add(Token.Lex(text, ref i));
        return tokens;
    }

    private static Token Parse(List<Token> tokens) {
        return ParseTokens(ParseParenthesis(tokens
            .Where(t => t is not WhiteSpaceToken && t is not CommentToken)
            .Select(t => t is SymbolToken st && emptyObjectOperators.Contains(st.Text) ? new RootToken(st) : t)
            .ToList()));
    }

    private static List<Token> ParseParenthesis(List<Token> tokens) {
        for (; ; )
        {
            var rightParen = tokens.FindIndex(t => t is SymbolToken st && closeParenOperators.Contains(st.Text));
            if (rightParen < 0) break;
            var leftChar = openParenOperators[closeParenOperators.IndexOf(tokens[rightParen].Text)];
            var leftParen = tokens.FindLastIndex(rightParen - 1, t => t is SymbolToken st && st.Text == "" + leftChar);
            if (leftParen < 0) throw new Exception("Unmatched parenthesis/brackets/braces");
            var prefix = leftParen == 0 ? null : tokens[leftParen - 1] as IdentifierToken;
            var innerTokens = tokens.GetRange(leftParen + 1, rightParen - leftParen - 1);
            tokens.RemoveRange(leftParen, rightParen - leftParen + 1);
            var token = ParseTokens(innerTokens);
            if (prefix != null) {
                switch (leftChar) {
                    case '{': break;
                    case '[': tokens.RemoveAt(--leftParen); token = new DerefToken(prefix, token); break;
                    case '(': tokens.RemoveAt(--leftParen); token = new MethodInvokeToken(prefix, token); break;
                    default: throw new Exception("unknown opening parentheis operator " + leftChar);
                }
            }
            tokens.Insert(leftParen, token);
        }
        return tokens;
    }

    private static Token ParseTokens(List<Token> tokens)
    {
        foreach (var ops in unaryOperators)
            tokens = ParseUnary(tokens, ops);
        foreach (var ops in binaryOperators)
            tokens = ParseBinary(tokens, ops);
        if (tokens.Count != 1) throw new Exception($"Expected one node but ended up with {tokens.Count}");
        return tokens[0];
    }

    private static List<Token> ParseUnary(List<Token> tokens, List<string> operators)
    {
        var newTokens = new List<Token>();
        for (int i = 0; i < tokens.Count; i++)
            if (tokens[i] is SymbolToken st && operators.Contains(st.Text) &&
                (i == 0 || tokens[i - 1] is SymbolToken))
                newTokens.Add(new UnaryToken(st, tokens[++i]));
            else
                newTokens.Add(tokens[i]);
        return newTokens;
    }

    private static List<Token> ParseBinary(List<Token> tokens, List<string> operators)
    {
        var newTokens = new List<Token>();
        for (int i = 0; i < tokens.Count; i++)
            if (i > 0 && tokens[i] is SymbolToken st && operators.Contains(st.Text))
                newTokens[^1] = new BinaryToken(st, newTokens[^1], tokens[++i]);
            else
                newTokens.Add(tokens[i]);
        return newTokens;
    }
}