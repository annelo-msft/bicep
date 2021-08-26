// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Bicep.Core.Modules;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bicep.Core.UnitTests.Modules
{
    [TestClass]
    public class TemplateSpecModuleReferenceTests
    {
        [DataTestMethod]
        [DynamicData(nameof(GetEqualData), DynamicDataSourceType.Method)]
        public void Equals_SameReferences_ReturnsTrue(TemplateSpecModuleReference first, TemplateSpecModuleReference second) =>
            first.Equals(second).Should().BeTrue();

        [DataTestMethod]
        [DynamicData(nameof(GetNotEqualData), DynamicDataSourceType.Method)]
        public void Equals_DifferentReferences_ReturnsFalse(TemplateSpecModuleReference first, TemplateSpecModuleReference second) =>
            first.Equals(second).Should().BeFalse();

        [DataTestMethod]
        [DynamicData(nameof(GetEqualData), DynamicDataSourceType.Method)]
        public void GetHashCode_SameReferences_ReturnsEqualHashCode(TemplateSpecModuleReference first, TemplateSpecModuleReference second) =>
            first.GetHashCode().Should().Be(second.GetHashCode());

        [DataTestMethod]
        [DynamicData(nameof(GetNotEqualData), DynamicDataSourceType.Method)]
        public void GetHashCode_DifferentReferences_ReturnsEqualHashCode(TemplateSpecModuleReference first, TemplateSpecModuleReference second) =>
            first.GetHashCode().Should().NotBe(second.GetHashCode());

        [DataRow("D9EEC7DB-8454-4EC1-8CD3-BB79D4CFEBEE/myRG/myTemplateSpec1:v123")]
        [DataRow("Test-RG/test-ts:mainVersion")]
        [DataTestMethod]
        public void TryParse_ValidReference_ReturnsParsedReference(string rawValue)
        {
            var reference = Parse(rawValue);

            reference.UnqualifiedReference.Should().Be(rawValue);
        }

        public static IEnumerable<object[]> GetEqualData()
        {
            yield return new object[]
            {
                Parse("D9EEC7DB-8454-4EC1-8CD3-BB79D4CFEBEE/myRG/myTemplateSpec1:v123"),
                Parse("d9eec7db-8454-4ec1-8cd3-bb79d4cfebee/myrg/mytemplatespec1:v123"),
            };

            yield return new object[]
            {
                Parse("test-rg/test-ts:mainVersion"),
                Parse("Test-RG/test-ts:mainVersion"),
            };
        }

        public static IEnumerable<object[]> GetNotEqualData()
        {
            yield return new object[]
            {
                Parse("D9EEC7DB-8454-4EC1-8CD3-BB79D4CFEBEE/myRG1/myTemplateSpec1:v123"),
                Parse("myrg/mytemplatespec:v2")
            };

            yield return new object[]
            {
                Parse("test-rg/test-ts:mainVersion"),
                Parse("prod-RG/test-ts2:mainVersion")
            };
        }

        private static TemplateSpecModuleReference Parse(string rawValue)
        {
            var parsed = TemplateSpecModuleReference.TryParse(rawValue, out var failureBuilder);

            parsed.Should().NotBeNull();
            failureBuilder.Should().BeNull();

            return parsed!;
        }
    }
}
