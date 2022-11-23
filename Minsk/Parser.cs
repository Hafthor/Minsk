namespace Minsk;

public class Parser
{
    private static readonly List<string> emptyObjectOperators = new List<string>() { "[]", "{}" };

    private static readonly List<List<string>> unaryOperators = new()
    {
        new() { "+", "-" }, // unary plus, minus
        new() { "!" }, // not
    };

    private static readonly List<List<string>> binaryOperators = new()
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

    private Parser() { }

    public static Token LexParse(string text) => Parse(Lex(text));

    private static List<Token> Lex(string text)
    {
        var tokens = new List<Token>();
        for (int i = 0; i < text.Length;)
            tokens.Add(Token.Lex(text, ref i));
        return tokens;
    }

    private static Token Parse(List<Token> tokens)
    {
        tokens = tokens
            .Where(t => t is not WhiteSpaceToken && t is not CommentToken)
            .Select(t => t is SymbolToken st && emptyObjectOperators.Contains(st.TextString.Value) ? new EmptyObjectToken(st) : t)
            .ToList();
        for (; ; ) // parse parenthesis loop
        {
            var rightParen = tokens.FindIndex(t => t is SymbolToken st && "}])".Contains(st.TextString.Value));
            if (rightParen < 0) break;
            var leftChar = "{[("["}])".IndexOf(tokens[rightParen].TextString.Value)];
            var leftParen = tokens.FindLastIndex(rightParen - 1, t => t is SymbolToken st && st.TextString.Value == "" + leftChar);
            if (leftParen < 0) throw new Exception("Unmatched parenthesis/brackets/braces");
            var prefix = leftParen == 0 ? null : tokens[leftParen - 1] as IdentifierToken;
            var innerTokens = tokens.GetRange(leftParen + 1, rightParen - leftParen - 1);
            tokens.RemoveRange(leftParen, rightParen - leftParen + 1);
            var token = ParseTokens(innerTokens);
            if (prefix != null && leftChar != '{')
            {
                tokens.RemoveAt(--leftParen);
                token = leftChar == '[' ? new DerefToken(prefix, token) : new MethodInvokeToken(prefix, token);
            }
            tokens.Insert(leftParen, token);
        }
        return ParseTokens(tokens);
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
            if (tokens[i] is SymbolToken st && operators.Contains(st.TextString.Value) &&
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
            if (i > 0 && tokens[i] is SymbolToken st && operators.Contains(st.TextString.Value))
                newTokens[^1] = new BinaryToken(st, newTokens[^1], tokens[++i]);
            else
                newTokens.Add(tokens[i]);
        return newTokens;
    }
}