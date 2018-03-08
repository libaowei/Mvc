// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    internal class RazorCompiledItemFeatureProvider : IApplicationFeatureProvider<ViewsFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewsFeature feature)
        {
            foreach (var provider in parts.OfType<IRazorCompiledItemProvider>())
            {
                // Ensure parts do not specify views with differing cases. This is not supported
                // at runtime and we should flag at as such for precompiled views.
                var duplicates = provider.CompiledItems
                    .GroupBy(i => i.Identifier, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(g => g.Count() > 1);

                if (duplicates != null)
                {
                    var first = duplicates.ElementAt(0);
                    var second = duplicates.ElementAt(1);

                    var message = string.Join(
                        Environment.NewLine,
                        Resources.RazorViewCompiler_ViewPathsDifferOnlyInCase,
                        first.Identifier,
                        second.Identifier);
                    throw new InvalidOperationException(message);
                }

                foreach (var item in provider.CompiledItems)
                {
                    var descriptor = new CompiledViewDescriptor(item, attribute: null);
                    feature.ViewDescriptors.Add(descriptor);
                }
            }
        }
    }
}
