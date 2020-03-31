using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using System.Linq;
using NUnit.Framework;
using System.Collections;

namespace integrationtests.cli
{
    public class Scenario3_DataClass
    {
        public static IEnumerable Instances
        {
            get
            {
                yield return new TestCaseData("my45");
                yield return new TestCaseData("my54");
            }
        }

        public static IEnumerable InstancesAndTests
        {
            get
            {
                yield return new TestCaseData("my45", "test4");
                yield return new TestCaseData("my54", "test5");
            }
        }
    }

    [NonParallelizable]
    [TestFixture]
    public class Scenario3_MultiInstance : End2EndScenarioBase
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
        [TestCaseSource(typeof(Scenario3_DataClass), "Instances")]
        public void InstallInstances(string instancePrefix)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"install.instance --name {instance} --resourceGroup {TestLogonData.ResourceGroup} --location {TestLogonData.Location}"
                + (string.IsNullOrWhiteSpace(TestLogonData.RuntimeSourceUrl)
                ? string.Empty
                : $" --sourceUrl {TestLogonData.RuntimeSourceUrl}"));

            Assert.AreEqual(0, rc);
            Assert.That(!output.Contains("] Failed!"));
        }

        [Test, Order(3)]
        public void ListInstances()
        {
            (int rc, string output) = RunAggregatorCommand($"list.instances --resourceGroup {TestLogonData.ResourceGroup}");

            Assert.AreEqual(0, rc);
            Assert.That(output.Contains("Instance my45"));
            Assert.That(output.Contains("Instance my54"));
        }

        [Test, Order(4)]
        [TestCaseSource(typeof(Scenario3_DataClass), "InstancesAndTests")]
        public void AddRules(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"add.rule --instance {instance} --resourceGroup {TestLogonData.ResourceGroup} --name {rule} --file {rule}.rule");

            Assert.AreEqual(0, rc);
            Assert.That(!output.Contains("] Failed!"));
        }

        [Test, Order(5)]
        [TestCaseSource(typeof(Scenario3_DataClass), "InstancesAndTests")]
        public void ListRules(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"list.rules --instance {instance} --resourceGroup {TestLogonData.ResourceGroup}");

            Assert.AreEqual(0, rc);
            Assert.That(output.Contains($"Rule {instance}/{rule}"));
            Assert.That(!output.Contains("] Failed!"));
        }

        [Test, Order(6)]
        [TestCaseSource(typeof(Scenario3_DataClass), "InstancesAndTests")]
        public void MapRules(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"map.rule --project \"{TestLogonData.ProjectName}\" --event workitem.created --instance {instance} --resourceGroup {TestLogonData.ResourceGroup} --rule {rule}");

            Assert.AreEqual(0, rc);
            Assert.That(!output.Contains("] Failed!"));
        }

        [Test, Order(7)]
        [TestCaseSource(typeof(Scenario3_DataClass), "InstancesAndTests")]
        public void ListMappings(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"list.mappings --instance {instance} --resourceGroup {TestLogonData.ResourceGroup}");

            Assert.AreEqual(0, rc);
            Assert.That(output.Contains($"invokes rule {instance}/{rule}"));
            Assert.That(!output.Contains("] Failed!"));
        }

        [Test, Order(101)]
        public void CreateWorkItemAndCheckTrigger()
        {
            string instancePrefix= "my45",  rule = "test4";

            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"test.create --verbose --resourceGroup {TestLogonData.ResourceGroup} --instance {instance} --project \"{TestLogonData.ProjectName}\" ");
            Assert.AreEqual(0, rc);
            // Sample output from rule:
            //  Returning 'Hello Task #118 from Rule 5!' from 'test5'
            Assert.That(output.Contains($"Returning 'Hello Task #"));
            Assert.That(output.Contains($"!' from '{rule}'"));
            Assert.That(!output.Contains("] Failed!"));
        }

        [Test, Order(901)]
        public void UninstallInstances()
        {
            string instancePrefix = "my45";

            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"uninstall.instance --name {instance} --resourceGroup {TestLogonData.ResourceGroup} --location {TestLogonData.Location}");

            Assert.AreEqual(0, rc);
            Assert.That(!output.Contains("] Failed!"));
        }

        [Test, Order(902)]
        public void ListInstancesAfterUninstall()
        {
            (int rc, string output) = RunAggregatorCommand($"list.instances --resourceGroup {TestLogonData.ResourceGroup}");

            Assert.AreEqual(0, rc);
            Assert.That(!output.Contains("Instance my45"));
            Assert.That(output.Contains("Instance my54"));
        }

        [Test, Order(903)]
        public void UnmapRules()
        {
            string instancePrefix = "my54", rule = "test5";

            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"unmap.rule --project \"{TestLogonData.ProjectName}\" --event workitem.created --instance {instance} --resourceGroup {TestLogonData.ResourceGroup} --rule {rule}");

            Assert.AreEqual(0, rc);
            Assert.That(!output.Contains("] Failed!"));
        }

        [Test, Order(999)]
        public void FinalCleanUp()
        {
            var credentials = SdkContext.AzureCredentialsFactory
                .FromServicePrincipal(
                    TestLogonData.ClientId,
                    TestLogonData.ClientSecret,
                    TestLogonData.TenantId,
                    AzureEnvironment.AzureGlobalCloud);
            var azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.None)
                .Authenticate(credentials)
                .WithSubscription(TestLogonData.SubscriptionId);

            // tip from https://www.wintellect.com/how-to-remove-all-resources-in-a-resource-group-without-removing-the-group-on-azure/
            string armTemplateString = @"
{
  ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
  ""contentVersion"": ""1.0.0.0"",
  ""parameters"": {},
  ""variables"": {},
  ""resources"": [],
  ""outputs"": {}
}
";
            string deploymentName = SdkContext.RandomResourceName("aggregator", 24);
            azure.Deployments.Define(deploymentName)
                    .WithExistingResourceGroup(TestLogonData.ResourceGroup)
                    .WithTemplate(armTemplateString)
                    .WithParameters("{}")
                    .WithMode(DeploymentMode.Complete)
                    .Create();

            Assert.True(true);
        }
    }
}
