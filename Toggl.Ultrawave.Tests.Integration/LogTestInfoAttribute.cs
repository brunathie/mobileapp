using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit.Sdk;

namespace Toggl.Ultrawave.Tests.Integration
{
    public sealed class LogTestInfoAttribute : BeforeAfterTestAttribute
    {
        private string formattedTestName;
        private readonly Stopwatch stopwatch = new Stopwatch();

        public override void Before(MethodInfo methodUnderTest)
        {
            var methodName = toSentenceCase(methodUnderTest.Name);
            formattedTestName = getFormattedName(methodUnderTest.ReflectedType, methodName);
            Console.WriteLine(formattedTestName.Replace("`1", ""));

            // This is to make integration tests run slightly slower to prevent SecureChannelFailure
            // errors caused by too many HTTP calls in quick succession in most cases
            // (combined with limiting the XUnit max parallel threads)
            // Empirically, a delay of 0.5s is too short, while 2s sees no further improvements
            Thread.Sleep(TimeSpan.FromSeconds(1));

            stopwatch.Start();
        }

        public override void After(MethodInfo methodUnderTest)
        {
            stopwatch.Stop();
            Console.WriteLine($"{formattedTestName} ended after: {stopwatch.Elapsed}");
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
}
