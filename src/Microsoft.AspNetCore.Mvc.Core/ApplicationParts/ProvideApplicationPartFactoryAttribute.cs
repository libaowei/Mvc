// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Provides a <see cref="ApplicationPartFactory"/> type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class ProvideApplicationPartFactoryAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of <see cref="ProvideApplicationPartFactoryAttribute"/> with the specified type.
        /// </summary>
        /// <param name="factoryType">The factory type.</param>
        public ProvideApplicationPartFactoryAttribute(Type factoryType)
        {
            ApplicationPartFactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
        }

        /// <summary>
        /// Creates a new instance of <see cref="ProvideApplicationPartFactoryAttribute"/> with the specified type name.
        /// </summary>
        /// <param name="factoryTypeName">The assembly qualified type name.</param>
        public ProvideApplicationPartFactoryAttribute(string factoryTypeName)
        {
            if (string.IsNullOrEmpty(factoryTypeName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(factoryTypeName));
            }

            ApplicationPartFactoryTypeName = factoryTypeName;
        }

        /// <summary>
        /// The factory type.
        /// </summary>
        public Type ApplicationPartFactoryType { get; }

        /// <summary>
        /// THe factory type name.
        /// </summary>
        public string ApplicationPartFactoryTypeName { get; }
    }
}
