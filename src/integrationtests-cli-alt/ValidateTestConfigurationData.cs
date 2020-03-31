using System;
using NUnit.Framework;

namespace integrationtests.cli
{
    [TestFixture]
    [Order(1)]
    public class ConfigurationDataTests
    {
        [Test]
        public void ValidateTestConfigurationData()
        {
            var data = new TestLogonData(
                Environment.GetEnvironmentVariable("DOWNLOADSECUREFILE_SECUREFILEPATH")
                ?? "logon-data.json");

            Assert.AreNotEqual("guid", data.SubscriptionId);
            Assert.AreNotEqual("guid", data.ClientId);
            Assert.AreNotEqual("password", data.ClientSecret);
            Assert.AreNotEqual("guid", data.TenantId);
            Assert.AreNotEqual("https://dev.azure.com/organization", data.DevOpsUrl);
            Assert.AreNotEqual("PAT", data.PAT);
            Assert.AreNotEqual("string", data.Location);
            Assert.AreNotEqual("string", data.ResourceGroup);
            Assert.AreNotEqual("string", data.ProjectName);
        }
    }
}
