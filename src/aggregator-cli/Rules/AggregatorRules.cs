﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace aggregator.cli
{
    class AggregatorRules
    {
        private IAzure azure;

        public AggregatorRules(IAzure azure)
        {
            this.azure = azure;
        }

        public class KuduFunctionBinding
        {
            public string name { get; set; }
            public string type { get; set; }
            public string direction { get; set; }
            public string webHookType { get; set; }
        }

        public class KuduFunctionConfig
        {
            public KuduFunctionBinding[] bindings { get; set; }
            public bool disabled { get; set; }
        }

        public class KuduFunction
        {
            public string name { get; set; }
            public KuduFunctionConfig config { get; set; }
        }

        internal async Task<IEnumerable<KuduFunction>> List(string instance)
        {
            var instances = new AggregatorInstances(azure);
            (string username, string password) = instances.GetPublishCredentials(instance);
            string apiUrl = $"https://{instance}.scm.azurewebsites.net/api/functions";
            CancellationToken cancellationToken;

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, apiUrl))
            {
                request.Headers.UserAgent.Add(new ProductInfoHeaderValue("aggregator", "3.0"));
                string base64AuthInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes(($"{username}:{password}")));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64AuthInfo);

                using (var response = await client.SendAsync(request, cancellationToken))
                {
                    var stream = await response.Content.ReadAsStreamAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        using (var sr = new StreamReader(stream))
                        using (var jtr = new JsonTextReader(sr))
                        {
                            var js = new JsonSerializer();
                            var functionList = js.Deserialize<KuduFunction[]>(jtr);
                            return functionList;
                        }
                    }
                    else
                        return new KuduFunction[0];
                }
            }
        }

        internal async Task AddAsync(string instance, string name, string filePath)
        {
            byte[] zipContent = CreateTemporaryZipForRule(name, filePath);

            var instances = new AggregatorInstances(azure);
            (string username, string password) = instances.GetPublishCredentials(instance);

            await UploadZipWithRule(instance, zipContent, username, password);
        }

        private static byte[] CreateTemporaryZipForRule(string name, string filePath)
        {
            // see https://docs.microsoft.com/en-us/azure/azure-functions/deployment-zip-push

            // working directory
            var rand = new Random((int)DateTime.UtcNow.Ticks);
            string baseDirPath = Path.Combine(
                Path.GetTempPath(),
                $"aggregator-{rand.Next().ToString()}");
            string tempDirPath = Path.Combine(
                baseDirPath,
                name);
            Directory.CreateDirectory(tempDirPath);

            // copy content
            File.Copy(filePath, Path.Combine(tempDirPath, Path.GetFileName(filePath)));
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream reader = assembly.GetManifestResourceStream("aggregator.cli.Rules.function.json"))
            using (var writer = File.Create(Path.Combine(tempDirPath, "function.json")))
            {
                reader.CopyTo(writer);
            }
            using (Stream reader = assembly.GetManifestResourceStream("aggregator.cli.Rules.run.csx"))
            using (var writer = File.Create(Path.Combine(tempDirPath, "run.csx")))
            {
                reader.CopyTo(writer);
            }

            // zip
            string tempZipPath = Path.GetTempFileName();
            File.Delete(tempZipPath);
            ZipFile.CreateFromDirectory(baseDirPath, tempZipPath);
            var zipContent = File.ReadAllBytes(tempZipPath);

            // clean-up: everything is in memory
            Directory.Delete(tempDirPath, true);
            File.Delete(tempZipPath);
            return zipContent;
        }

        private static async Task UploadZipWithRule(string instance, byte[] zipContent, string username, string password)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("aggregator", "3.0"));
            // POST /api/zipdeploy?isAsync=true
            // Deploy from zip asynchronously. The Location header of the response will contain a link to a pollable deployment status.
            string apiUrl = $"https://{instance}.scm.azurewebsites.net/api/zipdeploy";
            string base64AuthInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes(($"{username}:{password}")));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64AuthInfo);
            var body = new ByteArrayContent(zipContent);
            await client.PostAsync(apiUrl, body);
        }
    }
}