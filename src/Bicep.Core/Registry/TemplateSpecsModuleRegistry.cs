// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Bicep.Core.Diagnostics;
using Bicep.Core.Modules;

namespace Bicep.Core.Registry
{
    public class TemplateSpecsModuleRegistry : IModuleRegistry
    {
        public string Scheme => ModuleReferenceSchemes.TemplateSpecs;

        // TODO: restore a template spec.
        public bool IsModuleRestoreRequired(ModuleReference reference) => false;

        // TODO: restore a template spec.
        public IDictionary<ModuleReference, DiagnosticBuilder.ErrorBuilderDelegate> RestoreModules(IEnumerable<ModuleReference> references) =>
            ImmutableDictionary<ModuleReference, DiagnosticBuilder.ErrorBuilderDelegate>.Empty;

        public Uri? TryGetLocalModuleEntryPointPath(Uri parentModuleUri, ModuleReference reference, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            throw new NotImplementedException();
        }

        public ModuleReference? TryParseModuleReference(string reference, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            throw new NotImplementedException();
        }
    }
}
