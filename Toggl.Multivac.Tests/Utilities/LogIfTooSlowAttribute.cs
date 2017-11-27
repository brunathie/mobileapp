using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit.Sdk;

public sealed class LogIfTooSlowAttribute : BeforeAfterTestAttribute
{
    private readonly Stopwatch stopwatch = new Stopwatch();

    private static readonly TimeSpan fast = TimeSpan.FromSeconds(0.5f);
    private static readonly TimeSpan moderate = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan slow = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan critical = TimeSpan.FromSeconds(5);

    public override void Before(MethodInfo methodUnderTest)
    {
        stopwatch.Start();
    }

    public override void After(MethodInfo methodUnderTest)
    {
        stopwatch.Stop();

        if (stopwatch.Elapsed <= fast) return;

        var formattedTestName = 
            getFormattedName(methodUnderTest.ReflectedType, toSentenceCase(methodUnderTest.Name)).Replace("`1", "");

        Console.ForegroundColor = getColorForTime(stopwatch.Elapsed);
        Console.WriteLine($"{stopwatch.Elapsed} - {formattedTestName}");
    }

    private ConsoleColor getColorForTime(TimeSpan elapsed)
    {
        if (elapsed <= moderate) return ConsoleColor.DarkMagenta;
        if (elapsed <= slow) return ConsoleColor.DarkYellow;
        if (elapsed <= critical) return ConsoleColor.Yellow;
        return ConsoleColor.Red;
    }

    private string getFormattedName(Type typeInfo, string accumulator)
    {
        if (typeInfo.DeclaringType == null)
            return $"{typeInfo.Name}: {accumulator[0]}{accumulator.Substring(1).ToLower()}";

        return getFormattedName(typeInfo.DeclaringType, $"{toSentenceCase(typeInfo.Name)} {accumulator}");
    }

    public static string toSentenceCase(string value)
        => Regex.Replace(value, "[a-z][A-Z]", x => $"{x.Value[0]} {char.ToLower(x.Value[1])}");
}
