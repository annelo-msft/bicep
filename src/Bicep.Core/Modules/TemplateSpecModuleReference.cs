// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;
using Azure.Deployments.Core.Uri;
using Bicep.Core.Diagnostics;

namespace Bicep.Core.Modules
{
    public class TemplateSpecModuleReference : ModuleReference
    {
        private static readonly UriTemplate FullyQualifiedTemplateSpecUriTemplate = new("/{subscriptionId}/{resourceGroupName}/{templateSpecName}");

        private static readonly UriTemplate UnqualifiedTemplateSpecUriTemplate = new("/{resourceGroupName}/{templateSpecName}");

        private static readonly Regex ResourceGroupNameRegex = new(@"^[-\w\._\(\)]{1,89}[-\w_\(\)]$"); // A resource group name cannot end with period.

        private static readonly Regex TemplateSpecNameRegex = new(@"^[-\w\._\(\)]{1,90}$");

        private static readonly Regex TemplateSpecVersionRegex = new(@"^[-\w\._\(\)]{1,90}$");

        public TemplateSpecModuleReference(string? subscriptionId, string resourceGroupName, string templateSpecName, string version)
            : base(ModuleReferenceSchemes.TemplateSpecs)
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroupName = resourceGroupName;
            this.TemplateSpecName = templateSpecName;
            this.Version = version;
        }

        public override string UnqualifiedReference => this.SubscriptionId is not null
            ? $"{this.SubscriptionId}/{this.ResourceGroupName}/{this.TemplateSpecName}:{this.Version}"
            : $"{this.ResourceGroupName}/{this.TemplateSpecName}:{this.Version}";

        public string? SubscriptionId { get; }

        public string ResourceGroupName { get; }

        public string TemplateSpecName { get; }

        public string Version { get; }

        public override bool Equals(object? obj) =>
            obj is TemplateSpecModuleReference other &&
            StringComparer.OrdinalIgnoreCase.Equals(this.SubscriptionId, other.SubscriptionId) &&
            StringComparer.OrdinalIgnoreCase.Equals(this.ResourceGroupName, other.ResourceGroupName) &&
            StringComparer.OrdinalIgnoreCase.Equals(this.TemplateSpecName, other.TemplateSpecName) &&
            StringComparer.OrdinalIgnoreCase.Equals(this.Version, other.Version);

        public override int GetHashCode()
        {
            var hash = new HashCode();

            hash.Add(this.SubscriptionId, StringComparer.OrdinalIgnoreCase);
            hash.Add(this.ResourceGroupName, StringComparer.OrdinalIgnoreCase);
            hash.Add(this.TemplateSpecName, StringComparer.OrdinalIgnoreCase);
            hash.Add(this.Version, StringComparer.OrdinalIgnoreCase);

            return hash.ToHashCode();
        }

        public static TemplateSpecModuleReference? TryParse(string rawValue, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            static DiagnosticBuilder.ErrorBuilderDelegate CreateErrorFunc(string rawValue) => x => x.InvalidTemplateSpecReference($"oci:{rawValue}");

            if (rawValue.Split(':') is not { Length: 2 } parts)
            {
                failureBuilder = CreateErrorFunc(rawValue);
                return null;
            }

            var templateSpecPath = parts[0];
            var templateSpecVersion = parts[1];

            var fullyQualifiedMatch = FullyQualifiedTemplateSpecUriTemplate.GetTemplateMatch(templateSpecPath);
            var unqualifiedMatch = UnqualifiedTemplateSpecUriTemplate.GetTemplateMatch(templateSpecPath);

            string? subscriptionId = null;
            string resourceGroupName = "";
            string templateSpecName = "";

            switch (fullyQualifiedMatch, unqualifiedMatch)
            {
                case (not null, null):
                    subscriptionId = fullyQualifiedMatch.BoundVariables["subscriptionId"];
                    resourceGroupName = fullyQualifiedMatch.BoundVariables["resourceGroupName"];
                    templateSpecName = fullyQualifiedMatch.BoundVariables["templateSpecName"];
                    break;

                case (null, not null):
                    resourceGroupName = unqualifiedMatch.BoundVariables["resourceGroupName"];
                    templateSpecName = unqualifiedMatch.BoundVariables["templateSpecName"];
                    break;

                default:
                    failureBuilder = CreateErrorFunc(rawValue);
                    return null;
            }

            if ((subscriptionId is not null && !Guid.TryParse(subscriptionId, out _)) ||
                !ResourceGroupNameRegex.IsMatch(resourceGroupName) ||
                !TemplateSpecNameRegex.IsMatch(templateSpecName) ||
                !TemplateSpecVersionRegex.IsMatch(templateSpecVersion))
            {
                failureBuilder = CreateErrorFunc(rawValue);
                return null;
            }

            failureBuilder = null;
            return new(subscriptionId, resourceGroupName, templateSpecName, templateSpecVersion);
        }
    }
}
