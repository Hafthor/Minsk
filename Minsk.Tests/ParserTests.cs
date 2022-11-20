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

    [TestMethod]
    public void ParseDoubles()
    {
        foreach(var e in doubleTests)
        {
            var actual = Parser.LexParse(e.Key).Eval();
            Assert.IsTrue(actual is DoubleValue);
            Assert.AreEqual(e.Value, actual.Double);
        }
    }

    [TestMethod]
    public void ParseStrings()
    {
        foreach (var e in stringTests)
        {
            var actual = Parser.LexParse(e.Key).Eval();
            Assert.IsTrue(actual is StringValue);
            Assert.AreEqual(e.Value, actual.String);
        }
    }

    [TestMethod]
    public void ParseIdentifier()
    {
        var dict = new Dictionary<string, IValue>() { { "abc", new DoubleValue(123) } };
        var actual = Parser.LexParse("abc").Eval((k) => dict[k]);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(123.0, actual.Double);
    }

    [TestMethod]
    public void ParseAssignment()
    {
        var dict = new Dictionary<string, IValue>();
        var actual = Parser.LexParse("abc:123").Eval(null, (k, v) => dict[k] = v);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(123.0, actual.Double);
        Assert.IsTrue(dict["abc"] is DoubleValue);
        Assert.AreEqual(123.0, dict["abc"].Double);
    }

    [TestMethod]
    public void ParseArrayDeref()
    {
        var arr = new ArrayValue(new List<IValue>() { new DoubleValue(3.14), new DoubleValue(2.71) });
        var dict = new Dictionary<string, IValue>() { { "arr", arr } };
        var actual = Parser.LexParse("arr[1]").Eval((k) => dict[k]);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(2.71, actual.Double);
    }

    [TestMethod]
    public void ParseArraySet()
    {
        var arr = new ArrayValue(new List<IValue>() { new DoubleValue(2.71) });
        var dict = new Dictionary<string, IValue>() { { "arr", arr } };
        var actual = Parser.LexParse("arr[0]:3.14").Eval((k) => dict[k]);
        var obj0 = arr.ObjectByIndex(0);
        Assert.IsTrue(obj0 is DoubleValue);
        Assert.AreEqual(3.14, obj0.Double);
    }

    [TestMethod]
    public void ParseDictDeref()
    {
        var d = new DictionaryValue(new Dictionary<string, IValue>() { { "pi", new DoubleValue(3.14) }, { "e", new DoubleValue(2.71) } });
        var dict = new Dictionary<string, IValue>() { { "dict", d } };
        var actual = Parser.LexParse("dict[\"e\"]").Eval((k) => dict[k]);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(2.71, actual.Double);
    }

    [TestMethod]
    public void ParseDictSet()
    {
        var d = new DictionaryValue(new Dictionary<string, IValue>() { { "pi", new DoubleValue(3.14) } });
        var dict = new Dictionary<string, IValue>() { { "dict", d } };
        var actual = Parser.LexParse("dict[\"e\"]:2.71").Eval((k) => dict[k]);
        var objE = d.ObjectByKey("e");
        Assert.IsTrue(objE is DoubleValue);
        Assert.AreEqual(2.71, objE.Double);
    }

    [TestMethod]
    public void ParseDotDictDeref()
    {
        var d = new DictionaryValue(new Dictionary<string, IValue>() { { "pi", new DoubleValue(3.14) }, { "e", new DoubleValue(2.71) } });
        var dict = new Dictionary<string, IValue>() { { "dict", d } };
        var actual = Parser.LexParse("dict.pi").Eval((k) => dict[k]);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(3.14, actual.Double);
    }

    [TestMethod]
    public void ParseDotDictSet()
    {
        var d = new DictionaryValue(new Dictionary<string, IValue>() { { "pi", new DoubleValue(3.14) } });
        var dict = new Dictionary<string, IValue>() { { "dict", d } };
        var actual = Parser.LexParse("dict.e:2.71").Eval((k) => dict[k]);
        var objE = d.ObjectByKey("e");
        Assert.IsTrue(objE is DoubleValue);
        Assert.AreEqual(2.71, objE.Double);
    }

    [TestMethod]
    public void ParseMethodInvoke()
    {
        var upper = new FunctionValue((v) =>
        {
            if (v is StringValue sv) return new StringValue(sv.String.ToUpper());
            throw new Exception("unexpected value type");
        });
        var dict = new Dictionary<string, IValue>() { { "upper", upper } };
        var actual = Parser.LexParse("upper(\"pi\")").Eval((k) => dict[k]);
        Assert.IsTrue(actual is StringValue);
        Assert.AreEqual("PI", actual.String);
    }

    [TestMethod]
    public void ParseStatements()
    {
        var dict = new Dictionary<string, IValue>();
        var actual = Parser.LexParse("a:1;b:2;c:3;d:a+b*c").Eval((k) => dict[k], (k, v) => dict[k] = v);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(7.0, actual.Double);
        Assert.IsTrue(dict["a"] is DoubleValue);
        Assert.AreEqual(1.0, dict["a"].Double);
        Assert.IsTrue(dict["b"] is DoubleValue);
        Assert.AreEqual(2.0, dict["b"].Double);
        Assert.IsTrue(dict["c"] is DoubleValue);
        Assert.AreEqual(3.0, dict["c"].Double);
        Assert.IsTrue(dict["d"] is DoubleValue);
        Assert.AreEqual(7.0, dict["d"].Double);
    }
}