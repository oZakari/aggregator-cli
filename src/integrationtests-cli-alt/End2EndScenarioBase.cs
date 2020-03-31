using NUnit.Framework;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace integrationtests.cli
{
    public abstract class End2EndScenarioBase
    {
        protected TestLogonData TestLogonData;

        [OneTimeSetUp]
        protected void OneTimeSetUpBase()
        {
            TestLogonData = new TestLogonData(
                // CI scenario
                Environment.GetEnvironmentVariable("DOWNLOADSECUREFILE_SECUREFILEPATH")
                // Visual Studio
                ?? "logon-data.json");

            // sanity checks
            if (!Guid.TryParse(TestLogonData.SubscriptionId, out Guid dummy))
            {
                throw new ApplicationException("logon-data.json not found or invalid");
            }
        }

        protected (int rc, string output) RunAggregatorCommand(string commandLine)
        {
            // see https://stackoverflow.com/a/14655145/100864
            var args = Regex.Matches(commandLine, @"[\""](?<a>.+?)[\""]|(?<a>[^ ]+)")
                .Cast<Match>()
                .Select(m => m.Groups["a"].Value)
                .ToArray();

            var saveOut = Console.Out;
            var saveErr = Console.Error;
            var buffered = new StringWriter();
            Console.SetOut(buffered);
            Console.SetError(buffered);

            var rc = aggregator.cli.Program.Main(args);

            Console.SetOut(saveOut);
            Console.SetError(saveErr);

            var output = buffered.ToString();

            TestContext.WriteLine(output);

            return (rc, output);
        }
    }
}