// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core;
using Bicep.Core.FileSystem;
using Bicep.Core.Modules;
using Bicep.Core.Registry;
using Bicep.Core.Semantics;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.UnitTests;
using Bicep.Core.UnitTests.Registry;
using Bicep.Core.UnitTests.Utils;
using Bicep.Core.Workspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace Bicep.Core.Samples
{
    public static class DataSetsExtensions
    {
        public static IEnumerable<object[]> ToDynamicTestData(this IEnumerable<DataSet> source) => source.Select(ToDynamicTestData);

        public static object[] ToDynamicTestData(this DataSet ds) => new object[] {ds};

        public static bool HasCrLfNewlines(this DataSet dataSet)
            => dataSet.Name.EndsWith("_CRLF",  StringComparison.Ordinal);
            
        public static string SaveFilesToTestDirectory(this DataSet dataSet, TestContext testContext)
            => FileHelper.SaveEmbeddedResourcesWithPathPrefix(testContext, typeof(DataSet).Assembly, dataSet.GetStreamPrefix());

        public static async Task<(Compilation compilation, string outputDirectory, Uri fileUri)> SetupPrerequisitesAndCreateCompilation(this DataSet dataSet, TestContext testContext)
        {
            var outputDirectory = dataSet.SaveFilesToTestDirectory(testContext);
            var clientFactory = dataSet.CreateMockRegistryClients(testContext);
            await dataSet.PublishModulesToRegistryAsync(clientFactory, testContext);
            var fileUri = PathHelper.FilePathToFileUrl(Path.Combine(outputDirectory, DataSet.TestFileMain));
            var dispatcher = new ModuleDispatcher(new DefaultModuleRegistryProvider(BicepTestConstants.FileResolver, clientFactory, BicepTestConstants.CreateFeaturesProvider(testContext, registryEnabled: dataSet.HasModulesToPublish)));
            var workspace = new Workspace();
            var sourceFileGrouping = SourceFileGroupingBuilder.Build(BicepTestConstants.FileResolver, dispatcher, workspace, fileUri);
            if (await dispatcher.RestoreModules(dispatcher.GetValidModuleReferences(sourceFileGrouping.ModulesToRestore)))
            {
                sourceFileGrouping = SourceFileGroupingBuilder.Rebuild(dispatcher, workspace, sourceFileGrouping);
            }

            return (new Compilation(AzResourceTypeProvider.CreateWithAzTypes(), sourceFileGrouping), outputDirectory, fileUri);
        }

        public static IContainerRegistryClientFactory CreateMockRegistryClients(this DataSet dataSet, TestContext testContext, params (Uri registryUri, string repository)[] additionalClients)
        {
            var clientsBuilder = ImmutableDictionary.CreateBuilder<(Uri registryUri, string repository), MockRegistryBlobClient>();

            var dispatcher = new ModuleDispatcher(new DefaultModuleRegistryProvider(BicepTestConstants.FileResolver, BicepTestConstants.ClientFactory, BicepTestConstants.CreateFeaturesProvider(testContext, registryEnabled: dataSet.HasModulesToPublish)));

            foreach (var (moduleName, publishInfo) in dataSet.ModulesToPublish)
            {
                if(dispatcher.TryGetModuleReference(publishInfo.Metadata.Target, out _) is not OciArtifactModuleReference targetReference)
                {
                    throw new InvalidOperationException($"Module '{moduleName}' has an invalid target reference '{publishInfo.Metadata.Target}'. Specify a reference to an OCI artifact.");
                }

                Uri registryUri = new Uri($"https://{targetReference.Registry}");
                clientsBuilder.TryAdd((registryUri, targetReference.Repository), new MockRegistryBlobClient());
            }

            foreach (var additionalClient in additionalClients)
            {
                clientsBuilder.TryAdd((additionalClient.registryUri, additionalClient.repository), new MockRegistryBlobClient());
            }

            var repoToClient = clientsBuilder.ToImmutable();

            var clientFactory = new Mock<IContainerRegistryClientFactory>(MockBehavior.Strict);
            clientFactory
                .Setup(m => m.CreateBlobClient(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<TokenCredential>()))
                .Returns<Uri, string, TokenCredential>((registryUri, repository, _) =>
                {
                    if (repoToClient.TryGetValue((registryUri, repository), out var client))
                    {
                        return client;
                    }

                    throw new InvalidOperationException($"No mock client was registered for Uri '{registryUri}' and repository '{repository}'.");
                });

            return clientFactory.Object;
        }

        public static async Task PublishModulesToRegistryAsync(this DataSet dataSet, IContainerRegistryClientFactory clientFactory, TestContext testContext)
        {
            var dispatcher = new ModuleDispatcher(new DefaultModuleRegistryProvider(BicepTestConstants.FileResolver, clientFactory, BicepTestConstants.CreateFeaturesProvider(testContext, registryEnabled: dataSet.HasModulesToPublish)));

            foreach (var (moduleName, publishInfo) in dataSet.ModulesToPublish)
            {
                var targetReference = dispatcher.TryGetModuleReference(publishInfo.Metadata.Target, out _) ?? throw new InvalidOperationException($"Module '{moduleName}' has an invalid target reference '{publishInfo.Metadata.Target}'. Specify a reference to an OCI artifact.");

                var result = CompilationHelper.Compile(publishInfo.ModuleSource);
                if (result.Template is null)
                {
                    throw new InvalidOperationException($"Module {moduleName} failed to procuce a template.");
                }

                var stream = new MemoryStream();
                using (var streamWriter = new StreamWriter(stream, leaveOpen: true))
                using (var writer = new JsonTextWriter(streamWriter))
                {
                    await result.Template.WriteToAsync(writer);
                }

                stream.Position = 0;
                await dispatcher.PublishModule(targetReference, stream);
            }
        }
    }
}

