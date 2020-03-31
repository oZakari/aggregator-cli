using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Extensions.Ordering;

namespace integrationtests.cli
{
    [Order(1)]
    public class ConfigurationDataTests
    {
        [Fact]
        public void ValidateTestConfigurationData()
        {
            var data = new TestLogonData(
                Environment.GetEnvironmentVariable("DOWNLOADSECUREFILE_SECUREFILEPATH")
                ?? "logon-data.json");

            Assert.NotEqual("guid", data.SubscriptionId);
            Assert.NotEqual("guid", data.ClientId);
            Assert.NotEqual("password", data.ClientSecret);
            Assert.NotEqual("guid", data.TenantId);
            Assert.NotEqual("https://dev.azure.com/organization", data.DevOpsUrl);
            Assert.NotEqual("PAT", data.PAT);
            Assert.NotEqual("string", data.Location);
            Assert.NotEqual("string", data.ResourceGroup);
            Assert.NotEqual("string", data.ProjectName);
        }
    }
}
