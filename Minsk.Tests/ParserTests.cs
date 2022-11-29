namespace Minsk.Tests;

[TestClass]
public class ParserTests
{
    private readonly Dictionary<string, double> doubleTests = new Dictionary<string, double>() {
        {"123",123.0},
        {"1+2",3.0},
        {"2*3",6.0},
        {"3/2",1.5},
        {"1+2*3",7.0},
        {"3*2+1",7.0},
        {" 1 + 2 * 3 ",7.0},
        {"-1",-1.0},
        {"2*-3",-6.0},
        {"2.5",2.5},
        {"3*-(1-4)",9.0},
        {"1+(2*3)-(1+(2*3))",0.0},
        {"3 * -( // comment\r1-4) // more comment",9.0},
        {"3 * -(/* comment */1-4)/* more comment */",9.0},
        {"6=2*3",1.0},
        {"2*3>5",1.0},
        {"!(2*3>5)",0.0},
        {"!\"abc\"",0.0},
        {"-\"123\"",-123.0},
        {"123+\"456\"",579.0},
        {"\"456\"-\"123\"",333.0},
        {"\"abc\">\"def\"",0.0},
        {"\"abc\">=\"def\"",0.0},
        {"\"abc\"!=\"def\"",1.0},
        {"1+2*3^4",163.0},
        {"50.5%7",1.5},
    };

    private readonly Dictionary<string, string> stringTests = new Dictionary<string, string>()
    {
        {"\"abc\"","abc"},
        {"\" \\r \\\" \\\\ \""," \r \" \\ "},
        {"\"abc\"+\"def\"","abcdef"},
        {"\"abc\"+123","abc123"},
    };

    private Context ctx = new Context(new Variables());
    
    [TestMethod]
    public void ParseDoubles()
    {
        foreach (var e in doubleTests)
        {
            var actual = Parser.LexParse(e.Key).Eval(ctx);
            Assert.IsTrue(actual is DoubleValue);
            Assert.AreEqual(e.Value, actual.Double);
        }
    }

    [TestMethod]
    public void ParseStrings()
    {
        foreach (var e in stringTests)
        {
            var actual = Parser.LexParse(e.Key).Eval(ctx);
            Assert.IsTrue(actual is StringValue);
            Assert.AreEqual(e.Value, actual.String);
        }
    }

    [TestMethod]
    public void ParseIdentifier()
    {
        ctx.Set("abc", new DoubleValue(123));
        var actual = Parser.LexParse("abc").Eval(ctx);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(123.0, actual.Double);
    }

    [TestMethod]
    public void ParseAssignment()
    {
        var actual = Parser.LexParse("abc:123").Eval(ctx);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(123.0, actual.Double);
        Assert.IsTrue(ctx.Get("abc") is DoubleValue);
        Assert.AreEqual(123.0, ctx.Get("abc").Double);
    }
    
    [TestMethod]
    public void ParseArrayDeref()
    {
        var arr = new ArrayValue(new List<IValue>() { new DoubleValue(3.14), new DoubleValue(2.718) });
        ctx.Set("arr", arr);
        var actual = Parser.LexParse("arr[1]").Eval(ctx);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(2.718, actual.Double);
    }

    [TestMethod]
    public void ParseArraySet()
    {
        var arr = new ArrayValue(new List<IValue>() { new DoubleValue(2.718) });
        ctx.Set("arr", arr);
        var actual = Parser.LexParse("arr[0]:3.14").Eval(ctx);
        var obj0 = arr.ObjectByIndex(0);
        Assert.IsTrue(obj0 is DoubleValue);
        Assert.AreEqual(3.14, obj0.Double);
    }

    [TestMethod]
    public void ParseDictDeref()
    {
        var dict = new DictionaryValue(new Dictionary<string, IValue>() { { "pi", new DoubleValue(3.14) }, { "e", new DoubleValue(2.718) } });
        ctx.Set("dict", dict);
        var actual = Parser.LexParse("dict[\"e\"]").Eval(ctx);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(2.718, actual.Double);
    }

    [TestMethod]
    public void ParseDictSet()
    {
        var dict = new DictionaryValue(new Dictionary<string, IValue>() { { "pi", new DoubleValue(3.14) } });
        ctx.Set("dict", dict);
        var actual = Parser.LexParse("dict[\"e\"]:2.718").Eval(ctx);
        var objE = dict.ObjectByKey("e");
        Assert.IsTrue(objE is DoubleValue);
        Assert.AreEqual(2.718, objE.Double);
    }

    [TestMethod]
    public void ParseDotDictDeref()
    {
        var dict = new DictionaryValue(new Dictionary<string, IValue>() { { "pi", new DoubleValue(3.14) }, { "e", new DoubleValue(2.718) } });
        ctx.Set("dict", dict);
        var actual = Parser.LexParse("dict.pi").Eval(ctx);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(3.14, actual.Double);
    }

    [TestMethod]
    public void ParseDotDictSet()
    {
        var dict = new DictionaryValue(new Dictionary<string, IValue>() { { "pi", new DoubleValue(3.14) } });
        ctx.Set("dict", dict);
        var actual = Parser.LexParse("dict.e:2.718").Eval(ctx);
        var objE = dict.ObjectByKey("e");
        Assert.IsTrue(objE is DoubleValue);
        Assert.AreEqual(2.718, objE.Double);
    }

    [TestMethod]
    public void ParseMethodInvoke()
    {
        var upper = new FunctionValue("v", (v) =>
        {
            if (v is StringValue sv) return new StringValue(sv.String.ToUpper());
            throw new Exception("unexpected value type");
        });
        ctx.Set("upper", upper);
        var actual = Parser.LexParse("upper(\"pi\")").Eval(ctx);
        Assert.IsTrue(actual is StringValue);
        Assert.AreEqual("PI", actual.String);
    }

    [TestMethod]
    public void ParseStatements()
    {
        var actual = Parser.LexParse("a:1;b:2;c:3;d:a+b*c").Eval(ctx);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(7.0, actual.Double);
        Assert.IsTrue(ctx.Get("a") is DoubleValue);
        Assert.AreEqual(1.0, ctx.Get("a").Double);
        Assert.IsTrue(ctx.Get("b") is DoubleValue);
        Assert.AreEqual(2.0, ctx.Get("b").Double);
        Assert.IsTrue(ctx.Get("c") is DoubleValue);
        Assert.AreEqual(3.0, ctx.Get("c").Double);
        Assert.IsTrue(ctx.Get("d") is DoubleValue);
        Assert.AreEqual(7.0, ctx.Get("d").Double);
    }

    [TestMethod]
    public void ParseNewObjects()
    {
        var actual = Parser.LexParse("a:[];b:{};a[0]:3.14;b[\"e\"]:2.718").Eval(ctx);
        Assert.IsTrue(ctx.Get("a") is ArrayValue);
        Assert.AreEqual(3.14, ctx.Get("a") is ArrayValue av ? av.ObjectByIndex(0).Double : 0);
        Assert.IsTrue(ctx.Get("b") is DictionaryValue);
        Assert.AreEqual(2.718, ctx.Get("b") is DictionaryValue dv ? dv.ObjectByKey("e").Double : 0);
    }

    [TestMethod]
    public void ParseIfFalse()
    {
        var actual = Parser.LexParse("a:3.14;a=3.2??a:2.718").Eval(ctx);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(0.0, actual.Double);
        Assert.IsTrue(ctx.Get("a") is DoubleValue);
        Assert.AreEqual(3.14, ctx.Get("a").Double);
    }

    [TestMethod]
    public void ParseIfTrue()
    {
        var actual = Parser.LexParse("a:3.14;a=3.14??a:2.718").Eval(ctx);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(2.718, actual.Double);
        Assert.IsTrue(ctx.Get("a") is DoubleValue);
        Assert.AreEqual(2.718, ctx.Get("a").Double);
    }

    [TestMethod]
    public void ParseWhile()
    {
        var actual = Parser.LexParse("a:0;a<10?a:a+3").Eval(ctx);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(0.0, actual.Double);
        Assert.IsTrue(ctx.Get("a") is DoubleValue);
        Assert.AreEqual(12.0, ctx.Get("a").Double);
    }

    [TestMethod]
    public void ParseFact()
    {
        var actual = Parser.LexParse("n:1; fact:1; n<=10 ? { fact:fact*n; n:n+1 }; fact").Eval(ctx);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(3628800.0, actual.Double);
    }

    [TestMethod]
    public void ParseMethodDefine()
    {
        var actualMethodDefine = Parser.LexParse("add3(n):n+3").Eval(ctx);
        Assert.IsTrue(actualMethodDefine is FunctionValue);
        var actualMethodInvoke = Parser.LexParse("add3(2)").Eval(ctx);
        Assert.IsTrue(actualMethodInvoke is DoubleValue);
        Assert.AreEqual(5.0, actualMethodInvoke.Double);
    }

    [TestMethod]
    public void ParseMethodScope()
    {
        var actual = Parser.LexParse("n:2;add3(n):n+3;add3(n)").Eval(ctx);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(5.0, actual.Double);
        Assert.IsTrue(ctx.Get("n") is DoubleValue);
        Assert.AreEqual(2.0, ctx.Get("n") is DoubleValue dv ? dv.Double : 0.0);
    }

    [TestMethod]
    public void ParseMethodScope2()
    {
        var actual = Parser.LexParse("n:2;add3(i):i+3;add3(n)").Eval(ctx);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(5.0, actual.Double);
        Assert.IsTrue(ctx.Get("n") is DoubleValue);
        Assert.AreEqual(2.0, ctx.Get("n") is DoubleValue dv ? dv.Double : 0.0);
    }

    [TestMethod]
    public void ParseNotIf()
    {
        var actualNotIfTrue = Parser.LexParse("1 !? 2.718").Eval(ctx);
        Assert.IsTrue(actualNotIfTrue is DoubleValue);
        Assert.AreEqual(1.0, actualNotIfTrue.Double);

        var actualNotIfFalse = Parser.LexParse("0 !? 2.718").Eval(ctx);
        Assert.IsTrue(actualNotIfFalse is DoubleValue);
        Assert.AreEqual(2.718, actualNotIfFalse.Double);
    }

    [TestMethod]
    public void ParseElse()
    {
        var actualIfFalse = Parser.LexParse("0 ?? 3.14 :: 2.718").Eval(ctx);
        Assert.IsTrue(actualIfFalse is DoubleValue);
        Assert.AreEqual(2.718, actualIfFalse.Double);

        var actualIfTrue = Parser.LexParse("1 ?? 3.14 :: 2.718").Eval(ctx);
        Assert.IsTrue(actualIfTrue is DoubleValue);
        Assert.AreEqual(3.14, actualIfTrue.Double);

        var actualNotIfFalse = Parser.LexParse("0 !? 3.14 :: 2.718").Eval(ctx);
        Assert.IsTrue(actualNotIfFalse is DoubleValue);
        Assert.AreEqual(3.14, actualNotIfFalse.Double);

        var actualNotIfTrue = Parser.LexParse("1 !? 3.14 :: 2.718").Eval(ctx);
        Assert.IsTrue(actualNotIfTrue is DoubleValue);
        Assert.AreEqual(2.718, actualNotIfTrue.Double);
    }

    [TestMethod]
    public void ParseBreak()
    {
        var actual = Parser.LexParse("i:0; i<10 ? {i:i+1; i>5 ?? ~ }; i").Eval(ctx);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(6, actual.Double);
    }

    [TestMethod]
    public void ParseReturn()
    {
        var actual = Parser.LexParse("f(i):{ i>5 ?? ~~ 3.14; 2.718 }; f(7)").Eval(ctx);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(3.14, actual.Double);
    }
}