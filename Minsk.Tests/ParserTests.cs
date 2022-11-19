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
}
