namespace Minsk;

public class Parser
{
    private Parser() { }

    public static Token LexParse(string text) => Parse(Lex(text));

    private static List<Token> Lex(string text)
    {
        var tokens = new List<Token>();
        for (int i = 0; i < text.Length;)
            tokens.Add(
                WhiteSpaceToken.Lex(text, ref i) ??
                CommentToken.Lex(text, ref i) ??
                StringToken.Lex(text, ref i) ??
                IdentifierToken.Lex(text, ref i) ??
                NumberToken.Lex(text, ref i) ??
                SymbolToken.Lex(text, ref i) ??
                new BadToken(text, i++, 1));
        return tokens;
    }

    private static readonly List<List<string>> unaryOperators = new()
    {
        new() { "+", "-" },
        new() { "!" },
    };
    private static readonly List<List<string>> binaryOperators = new()
    {
        new() { "^" },
        new() { "*", "/", "%" },
        new() { "+", "-" },
        new() { "=", "!=", ">", "<", ">=", "<=" },
        new() { ":" },
    };
    private static Token Parse(List<Token> tokens)
    {
        tokens = tokens.Where(_ => _ is not WhiteSpaceToken && _ is not CommentToken).ToList();
        for (; ; ) // parse parenthesis loop
        {
            var rightParen = tokens.FindIndex(t => t is SymbolToken st && st.TextEquals(")"));
            if (rightParen < 0) break;
            var leftParen = tokens.FindLastIndex(rightParen - 1, t => t is SymbolToken st && st.TextEquals("("));
            if (leftParen < 0) throw new Exception("Unmatch parenthesis");
            var innerTokens = tokens.GetRange(leftParen + 1, rightParen - leftParen - 1);
            tokens.RemoveRange(leftParen, rightParen - leftParen + 1);
            tokens.Insert(leftParen, ParseTokens(innerTokens));
        }
        return ParseTokens(tokens);
    }

    private static Token ParseTokens(List<Token> tokens)
    {
        foreach (var ops in unaryOperators) tokens = ParseUnary(tokens, ops);
        foreach (var ops in binaryOperators) tokens = ParseBinary(tokens, ops);
        if (tokens.Count != 1) throw new Exception($"Expected one node but ended up with {tokens.Count}");
        return tokens[0];
    }

    private static List<Token> ParseUnary(List<Token> tokens, List<string> operators)
    {
        var newTokens = new List<Token>();
        for (int i = 0; i < tokens.Count; i++)
            if (tokens[i] is SymbolToken st && operators.Contains(st.TextString.Value) &&
                (i==0 || tokens[i-1] is SymbolToken))
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