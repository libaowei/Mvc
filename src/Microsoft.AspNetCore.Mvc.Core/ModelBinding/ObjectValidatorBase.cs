// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public abstract class ObjectValidatorBase : IObjectModelValidator
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly ValidatorCache _validatorCache;
        private readonly CompositeModelValidatorProvider _validatorProvider;

        public ObjectValidatorBase(
            IModelMetadataProvider modelMetadataProvider,
            IList<IModelValidatorProvider> validatorProviders)
        {
            if (modelMetadataProvider == null)
            {
                throw new ArgumentNullException(nameof(modelMetadataProvider));
            }

            if (validatorProviders == null)
            {
                throw new ArgumentNullException(nameof(validatorProviders));
            }

            _modelMetadataProvider = modelMetadataProvider;
            _validatorCache = new ValidatorCache();

            _validatorProvider = new CompositeModelValidatorProvider(validatorProviders);
        }

        /// <inheritdoc />
        public virtual void Validate(
            ActionContext actionContext,
            ValidationStateDictionary validationState,
            string prefix,
            object model)
        {
            var visitor = GetValidationVisitor(
                actionContext,
                _validatorProvider,
                _validatorCache,
                _modelMetadataProvider,
                validationState);

            var metadata = model == null ? null : _modelMetadataProvider.GetMetadataForType(model.GetType());
            visitor.Validate(metadata, prefix, model, alwaysValidateAtTopLevel: false);
        }

        public virtual void Validate(
            ActionContext actionContext,
            ValidationStateDictionary validationState,
            ModelMetadata metadata,
            string prefix,
            object model)
        {
            var visitor = GetValidationVisitor(
                actionContext,
                _validatorProvider,
                _validatorCache,
                _modelMetadataProvider,
                validationState);

            visitor.Validate(metadata, prefix, model, metadata.IsRequired);
        }

        public abstract ValidationVisitor GetValidationVisitor(
            ActionContext actionContext,
            IModelValidatorProvider validatorProvider,
            ValidatorCache validatorCache,
            IModelMetadataProvider metadataProvider,
            ValidationStateDictionary validationState);
    }
}
