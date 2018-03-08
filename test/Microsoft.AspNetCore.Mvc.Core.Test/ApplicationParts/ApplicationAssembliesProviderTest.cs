// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    public class ApplicationAssembliesProviderTest
    {
        private static readonly Assembly TestAssembly = typeof(ApplicationAssembliesProviderTest).Assembly;

        [Fact]
        public void ResolveAssemblies_ReturnsCurrentAssembly_IfNoDepsFileIsPresent()
        {
            // Arrange
            var provider = new TestApplicationAssembliesProvider();

            // Act
            var result = provider.ResolveAssemblies(TestAssembly);

            // Assert
            Assert.Equal(new[] { TestAssembly }, result);
        }

        [Fact]
        public void ResolveAssemblies_ReturnsRelatedPartsOrderedByName()
        {
            // Arrange
            var assembly1 = typeof(ApplicationAssembliesProvider).Assembly;
            var assembly2 = typeof(IActionResult).Assembly;
            var assembly3 = typeof(FactAttribute).Assembly;

            var item = new ApplicationAssembliesProvider.AssemblyItem(TestAssembly, new[]
            {
                assembly1,
                assembly2,
                assembly3,
            });
            var provider = new TestApplicationAssembliesProvider
            {
                GetAssemblyItemDelegate = (assembly) => item,
            };

            // Act
            var result = provider.ResolveAssemblies(TestAssembly);

            // Assert
            Assert.Equal(new[] { TestAssembly, assembly2, assembly1, assembly3 }, result);
        }

        [Fact]
        public void ResolveAssemblies_ReturnsLibrariesFromTheDepsFileThatReferenceMvc()
        {
            // Arrange
            var mvcAssembly = typeof(IActionResult).Assembly;
            var classLibrary = typeof(FactAttribute).Assembly;

            var dependencyContext = GetDependencyContext(new[]
            {
                GetLibrary(TestAssembly.GetName().Name, new[] { mvcAssembly.GetName().Name, classLibrary.GetName().Name }),
                GetLibrary(mvcAssembly.GetName().Name),
                GetLibrary(classLibrary.GetName().Name, new[] { mvcAssembly.GetName().Name }),
            });

            var provider = new TestApplicationAssembliesProvider
            {
                DependencyContext = dependencyContext,
            };

            // Act
            var result = provider.ResolveAssemblies(TestAssembly);

            // Assert
            Assert.Equal(new[] { TestAssembly, classLibrary, }, result);
        }

        [Fact]
        public void ResolveAssemblies_ReturnsRelatedPartsForLibrariesFromDepsFile()
        {
            // Arrange
            var mvcAssembly = typeof(IActionResult).Assembly;
            var classLibrary = typeof(object).Assembly;
            var relatedPart = typeof(FactAttribute).Assembly;

            var dependencyContext = GetDependencyContext(new[]
            {
                GetLibrary(TestAssembly.GetName().Name, new[] { relatedPart.GetName().Name, classLibrary.GetName().Name }),
                GetLibrary(classLibrary.GetName().Name, new[] { mvcAssembly.GetName().Name }),
                GetLibrary(relatedPart.GetName().Name, new[] { mvcAssembly.GetName().Name }),
                GetLibrary(mvcAssembly.GetName().Name),
            });

            var provider = new TestApplicationAssembliesProvider
            {
                DependencyContext = dependencyContext,
                GetAssemblyItemDelegate = (assembly) =>
                {
                    if (assembly == classLibrary)
                    {
                        return new ApplicationAssembliesProvider.AssemblyItem(classLibrary, new[] { relatedPart });
                    }

                    return new ApplicationAssembliesProvider.AssemblyItem(assembly, Array.Empty<Assembly>());
                },
            };

            // Act
            var result = provider.ResolveAssemblies(TestAssembly);

            // Assert
            Assert.Equal(new[] { TestAssembly, classLibrary, relatedPart, }, result);
        }

        [Fact]
        public void CandidateResolver_ThrowsIfDependencyContextContainsDuplicateRuntimeLibraryNames()
        {
            // Arrange
            var upperCaseLibrary = "Microsoft.AspNetCore.Mvc";
            var mixedCaseLibrary = "microsoft.aspNetCore.mvc";

            var dependencyContext = GetDependencyContext(new[]
            {
                GetLibrary(mixedCaseLibrary),
                GetLibrary(upperCaseLibrary),
            });

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => ApplicationAssembliesProvider.GetCandidateLibraries(dependencyContext));

            // Assert
            Assert.Equal($"A duplicate entry for library reference {upperCaseLibrary} was found. Please check that all package references in all projects use the same casing for the same package references.", exception.Message);
        }

        [Fact]
        public void GetCandidateLibraries_IgnoresMvcAssemblies()
        {
            // Arrange
            var expected = GetLibrary("SomeRandomAssembly", "Microsoft.AspNetCore.Mvc.Abstractions");
            var dependencyContext = GetDependencyContext(new[]
            {
                GetLibrary("Microsoft.AspNetCore.Mvc.Core"),
                GetLibrary("Microsoft.AspNetCore.Mvc"),
                GetLibrary("Microsoft.AspNetCore.Mvc.Abstractions"),
                expected,
            });

            // Act
            var candidates = ApplicationAssembliesProvider.GetCandidateLibraries(dependencyContext);

            // Assert
            Assert.Equal(new[] { expected }, candidates);
        }

        [Fact]
        public void GetCandidateLibraries_DoesNotThrow_IfLibraryDoesNotHaveRuntimeComponent()
        {
            // Arrange
            var expected = GetLibrary("MyApplication", "Microsoft.AspNetCore.Server.Kestrel", "Microsoft.AspNetCore.Mvc");
            var dependencyContext = GetDependencyContext(new[]
            {
                expected,
                GetLibrary("Microsoft.AspNetCore.Server.Kestrel", "Libuv"),
                GetLibrary("Microsoft.AspNetCore.Mvc"),
            });

            // Act
            var candidates = ApplicationAssembliesProvider.GetCandidateLibraries(dependencyContext).ToList();

            // Assert
            Assert.Equal(new[] { expected }, candidates);
        }

        [Fact]
        public void GetCandidateLibraries_ReturnsLibrariesReferencingAnyMvcAssembly()
        {
            // Arrange
            var dependencyContext = GetDependencyContext(new[]
            {
                GetLibrary("Foo", "Microsoft.AspNetCore.Mvc.Core"),
                GetLibrary("Bar", "Microsoft.AspNetCore.Mvc"),
                GetLibrary("Qux", "Not.Mvc.Assembly", "Unofficial.Microsoft.AspNetCore.Mvc"),
                GetLibrary("Baz", "Microsoft.AspNetCore.Mvc.Abstractions"),
                GetLibrary("Microsoft.AspNetCore.Mvc.Core"),
                GetLibrary("Microsoft.AspNetCore.Mvc"),
                GetLibrary("Not.Mvc.Assembly"),
                GetLibrary("Unofficial.Microsoft.AspNetCore.Mvc"),
                GetLibrary("Microsoft.AspNetCore.Mvc.Abstractions"),
            });

            // Act
            var candidates = ApplicationAssembliesProvider.GetCandidateLibraries(dependencyContext);

            // Assert
            Assert.Equal(new[] { "Foo", "Bar", "Baz" }, candidates.Select(a => a.Name));
        }

        [Fact]
        public void GetCandidateLibraries_LibraryNameComparisonsAreCaseInsensitive()
        {
            // Arrange
            var dependencyContext = GetDependencyContext(new[]
            {
                GetLibrary("Foo", "MICROSOFT.ASPNETCORE.MVC.CORE"),
                GetLibrary("Bar", "microsoft.aspnetcore.mvc"),
                GetLibrary("Qux", "Not.Mvc.Assembly", "Unofficial.Microsoft.AspNetCore.Mvc"),
                GetLibrary("Baz", "mIcRoSoFt.AsPnEtCoRe.MvC.aBsTrAcTiOnS"),
                GetLibrary("Microsoft.AspNetCore.Mvc.Core"),
                GetLibrary("LibraryA", "LIBRARYB"),
                GetLibrary("LibraryB", "microsoft.aspnetcore.mvc"),
                GetLibrary("Microsoft.AspNetCore.Mvc"),
                GetLibrary("Not.Mvc.Assembly"),
                GetLibrary("Unofficial.Microsoft.AspNetCore.Mvc"),
                GetLibrary("Microsoft.AspNetCore.Mvc.Abstractions"),
            });

            // Act
            var candidates = ApplicationAssembliesProvider.GetCandidateLibraries(dependencyContext);

            // Assert
            Assert.Equal(new[] { "Foo", "Bar", "Baz", "LibraryA", "LibraryB" }, candidates.Select(a => a.Name));
        }

        [Fact]
        public void GetCandidateLibraries_ReturnsLibrariesWithTransitiveReferencesToAnyMvcAssembly()
        {
            // Arrange
            var expectedLibraries = new[] { "Foo", "Bar", "Baz", "LibraryA", "LibraryB", "LibraryC", "LibraryE", "LibraryG", "LibraryH" };

            var dependencyContext = GetDependencyContext(new[]
            {
                GetLibrary("Foo", "Bar"),
                GetLibrary("Bar", "Microsoft.AspNetCore.Mvc"),
                GetLibrary("Qux", "Not.Mvc.Assembly", "Unofficial.Microsoft.AspNetCore.Mvc"),
                GetLibrary("Baz", "Microsoft.AspNetCore.Mvc.Abstractions"),
                GetLibrary("Microsoft.AspNetCore.Mvc"),
                GetLibrary("Not.Mvc.Assembly"),
                GetLibrary("Microsoft.AspNetCore.Mvc.Abstractions"),
                GetLibrary("Unofficial.Microsoft.AspNetCore.Mvc"),
                GetLibrary("LibraryA", "LibraryB"),
                GetLibrary("LibraryB","LibraryC"),
                GetLibrary("LibraryC", "LibraryD", "Microsoft.AspNetCore.Mvc.Abstractions"),
                GetLibrary("LibraryD"),
                GetLibrary("LibraryE","LibraryF","LibraryG"),
                GetLibrary("LibraryF"),
                GetLibrary("LibraryG", "LibraryH"),
                GetLibrary("LibraryH", "LibraryI", "Microsoft.AspNetCore.Mvc"),
                GetLibrary("LibraryI"),
            });

            // Act
            var candidates = ApplicationAssembliesProvider.GetCandidateLibraries(dependencyContext);

            // Assert
            Assert.Equal(expectedLibraries, candidates.Select(a => a.Name));
        }

        [Fact]
        public void GetCandidateLibraries_SkipsMvcAssemblies()
        {
            // Arrange
            var dependencyContext = GetDependencyContext(new[]
            {
                GetLibrary("MvcSandbox", "Microsoft.AspNetCore.Mvc.Core", "Microsoft.AspNetCore.Mvc"),
                GetLibrary("Microsoft.AspNetCore.Mvc.Core", "Microsoft.AspNetCore.HttpAbstractions"),
                GetLibrary("Microsoft.AspNetCore.HttpAbstractions"),
                GetLibrary("Microsoft.AspNetCore.Mvc", "Microsoft.AspNetCore.Mvc.Abstractions", "Microsoft.AspNetCore.Mvc.Core"),
                GetLibrary("Microsoft.AspNetCore.Mvc.Abstractions"),
                GetLibrary("Microsoft.AspNetCore.Mvc.TagHelpers", "Microsoft.AspNetCore.Mvc.Razor"),
                GetLibrary("Microsoft.AspNetCore.Mvc.Razor"),
                GetLibrary("ControllersAssembly", "Microsoft.AspNetCore.Mvc"),
            });

            // Act
            var candidates = ApplicationAssembliesProvider.GetCandidateLibraries(dependencyContext);

            // Assert
            Assert.Equal(new[] { "MvcSandbox", "ControllersAssembly" }, candidates.Select(a => a.Name));
        }

        // This test verifies DefaultAssemblyPartDiscoveryProvider.ReferenceAssemblies reflects the actual loadable assemblies
        // of the libraries that Microsoft.AspNetCore.Mvc depends on.
        // If we add or remove dependencies, this test should be changed together.
        [Fact]
        public void ReferenceAssemblies_ReturnsLoadableReferenceAssemblies()
        {
            // Arrange
            var excludeAssemblies = new string[]
            {
                "Microsoft.AspNetCore.Mvc.Core.Test",
                "Microsoft.AspNetCore.Mvc.TestCommon",
                "Microsoft.AspNetCore.Mvc.TestDiagnosticListener",
                "Microsoft.AspNetCore.Mvc.WebApiCompatShim",
            };

            var additionalAssemblies = new[]
            {
                // The following assemblies are not reachable from Microsoft.AspNetCore.Mvc
                "Microsoft.AspNetCore.Mvc.Formatters.Xml",
            };

            var dependencyContextLibraries = DependencyContext.Load(TestAssembly)
                .CompileLibraries
                .Where(r => r.Name.StartsWith("Microsoft.AspNetCore.Mvc", StringComparison.OrdinalIgnoreCase) &&
                    !excludeAssemblies.Contains(r.Name, StringComparer.OrdinalIgnoreCase))
                .Select(r => r.Name);

            var expected = dependencyContextLibraries
                .Concat(additionalAssemblies)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

            // Act
            var referenceAssemblies = ApplicationAssembliesProvider
                .ReferenceAssemblies
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

            // Assert
            Assert.Equal(expected, referenceAssemblies, StringComparer.OrdinalIgnoreCase);
        }

        private class TestApplicationAssembliesProvider : ApplicationAssembliesProvider
        {
            public DependencyContext DependencyContext { get; set; }

            public Func<Assembly, AssemblyItem> GetAssemblyItemDelegate { get; set; } = (assembly) => new AssemblyItem(assembly, Array.Empty<Assembly>());

            protected override DependencyContext LoadDependencyContext(Assembly assembly) => DependencyContext;

            protected override AssemblyItem GetAssemblyItem(Assembly assembly) => GetAssemblyItemDelegate(assembly);

            protected override IEnumerable<Assembly> GetLibraryAssemblies(DependencyContext dependencyContext, RuntimeLibrary runtimeLibrary)
            {
                var assemblyName = new AssemblyName(runtimeLibrary.Name);
                yield return Assembly.Load(assemblyName);
            }
        }

        private static DependencyContext GetDependencyContext(RuntimeLibrary[] libraries)
        {
            var dependencyContext = new DependencyContext(
                new TargetInfo("framework", "runtime", "signature", isPortable: true),
                CompilationOptions.Default,
                new CompilationLibrary[0],
                libraries,
                Enumerable.Empty<RuntimeFallbacks>());
            return dependencyContext;
        }

        private static RuntimeLibrary GetLibrary(string name, params string[] dependencyNames)
        {
            var dependencies = dependencyNames?.Select(d => new Dependency(d, "42.0.0")) ?? new Dependency[0];

            return new RuntimeLibrary(
                "package",
                name,
                "23.0.0",
                "hash",
                new RuntimeAssetGroup[0],
                new RuntimeAssetGroup[0],
                new ResourceAssembly[0],
                dependencies: dependencies.ToArray(),
                serviceable: true);
        }
    }
}
