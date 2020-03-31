using System;
using NUnit.Framework;

namespace integrationtests.cli
{
    [TestFixture]
    [Order(10)]
    public class AddRuleTests : End2EndScenarioBase
    {
        [Test, Order(1)]
        public void Logon()
        {
            (int rc, string output) = RunAggregatorCommand(
                $"logon.azure --subscription {TestLogonData.SubscriptionId} --client {TestLogonData.ClientId} --password {TestLogonData.ClientSecret} --tenant {TestLogonData.TenantId}");
            Assert.AreEqual(0, rc);
            Assert.That(!output.Contains("] Failed!"));
            (int rc2, string output2) = RunAggregatorCommand(
                $"logon.ado --url {TestLogonData.DevOpsUrl} --mode PAT --token {TestLogonData.PAT}");
            Assert.AreEqual(0, rc2);
            Assert.That(!output2.Contains("] Failed!"));
        }

        [Test, Order(2)]
        public void GivenAnInvalidRuleFile_WhenAddingThisRule_ThenTheProcessing_ShouldBeAborted()
        {
            //Given
            const string invalidRuleFileName = "invalid_rule1.rule";

            //When
            (int rc, string output) = RunAggregatorCommand(FormattableString.Invariant($"add.rule --verbose --instance foobar --resourceGroup foobar --name foobar --file {invalidRuleFileName}"));

            //Then
            Assert.AreEqual(1, rc);
            Assert.That(output.Contains(@"Errors in the rule file invalid_rule1.rule:"));
        }
    }
}