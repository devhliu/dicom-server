﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Api.Features.Routing
{
    public sealed class UrlResolver : IUrlResolver
    {
        private readonly IUrlHelperFactory _urlHelperFactory;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IActionContextAccessor _actionContextAccessor;

        public UrlResolver(
            IUrlHelperFactory urlHelperFactory,
            IHttpContextAccessor httpContextAccessor,
            IActionContextAccessor actionContextAccessor)
        {
            EnsureArg.IsNotNull(urlHelperFactory, nameof(urlHelperFactory));
            EnsureArg.IsNotNull(httpContextAccessor, nameof(httpContextAccessor));
            EnsureArg.IsNotNull(actionContextAccessor, nameof(actionContextAccessor));

            _urlHelperFactory = urlHelperFactory;
            _httpContextAccessor = httpContextAccessor;
            _actionContextAccessor = actionContextAccessor;
        }

        private IUrlHelper UrlHelper => _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

        /// <inheritdoc />
        public Uri ResolveOperationStatusUri(Guid operationId)
        {
            var hasVersion = _httpContextAccessor.HttpContext.Request.RouteValues.ContainsKey("version");

            return RouteUri(
                hasVersion ? KnownRouteNames.VersionedOperationStatus : KnownRouteNames.OperationStatus,
                new RouteValueDictionary
                {
                    { KnownActionParameterNames.OperationId, OperationId.ToString(operationId) },
                });
        }

        /// <inheritdoc />
        public Uri ResolveQueryTagUri(string tagPath)
        {
            var hasVersion = _httpContextAccessor.HttpContext.Request.RouteValues.ContainsKey("version");

            return RouteUri(
                hasVersion ? KnownRouteNames.VersionedGetExtendedQueryTag : KnownRouteNames.GetExtendedQueryTag,
                new RouteValueDictionary
                {
                    { KnownActionParameterNames.TagPath, tagPath },
                });
        }

        /// <inheritdoc />
        public Uri ResolveQueryTagErrorsUri(string tagPath)
        {
            var hasVersion = _httpContextAccessor.HttpContext.Request.RouteValues.ContainsKey("version");

            return RouteUri(
                hasVersion ? KnownRouteNames.VersionedGetExtendedQueryTagErrors : KnownRouteNames.GetExtendedQueryTagErrors,
                new RouteValueDictionary
                {
                    { KnownActionParameterNames.TagPath, tagPath },
                });
        }

        /// <inheritdoc />
        public Uri ResolveRetrieveStudyUri(string studyInstanceUid)
        {
            EnsureArg.IsNotNull(studyInstanceUid, nameof(studyInstanceUid));
            var routeValues = new RouteValueDictionary
            {
                { KnownActionParameterNames.StudyInstanceUid, studyInstanceUid },
            };

            AddRouteValues(routeValues, out bool hasVersion, out bool hasPartition);

            var routeName = hasPartition
                ? (hasVersion ? KnownRouteNames.VersionedPartitionRetrieveStudy : KnownRouteNames.PartitionRetrieveStudy)
                : hasVersion ? KnownRouteNames.VersionedRetrieveStudy : KnownRouteNames.RetrieveStudy;

            return RouteUri(routeName, routeValues);
        }

        /// <inheritdoc />
        public Uri ResolveRetrieveWorkitemUri(string workitemInstanceUid)
        {
            EnsureArg.IsNotNull(workitemInstanceUid, nameof(workitemInstanceUid));
            var routeValues = new RouteValueDictionary
            {
                { KnownActionParameterNames.WorkItemInstanceUid, workitemInstanceUid },
            };

            AddRouteValues(routeValues, out bool hasVersion, out bool hasPartition);

            var routeName = hasPartition
                ? (hasVersion ? KnownRouteNames.VersionedPartitionRetrieveWorkitemInstance : KnownRouteNames.PartitionedRetrieveWorkitemInstance)
                : hasVersion ? KnownRouteNames.VersionedRetrieveWorkitemInstance : KnownRouteNames.RetrieveWorkitemInstance;

            return RouteUri(routeName, routeValues);
        }

        /// <inheritdoc />
        public Uri ResolveRetrieveInstanceUri(InstanceIdentifier instanceIdentifier)
        {
            EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));

            var routeValues = new RouteValueDictionary
            {
                { KnownActionParameterNames.StudyInstanceUid, instanceIdentifier.StudyInstanceUid },
                { KnownActionParameterNames.SeriesInstanceUid, instanceIdentifier.SeriesInstanceUid },
                { KnownActionParameterNames.SopInstanceUid, instanceIdentifier.SopInstanceUid },
            };

            AddRouteValues(routeValues, out bool hasVersion, out bool hasPartition);

            var routeName = hasPartition
                ? (hasVersion ? KnownRouteNames.VersionedPartitionRetrieveInstance : KnownRouteNames.PartitionRetrieveInstance)
                : hasVersion ? KnownRouteNames.VersionedRetrieveInstance : KnownRouteNames.RetrieveInstance;

            return RouteUri(routeName, routeValues);
        }

        private void AddRouteValues(RouteValueDictionary routeValues, out bool hasVersion, out bool hasPartition)
        {
            hasVersion = _httpContextAccessor.HttpContext.Request.RouteValues.ContainsKey(KnownActionParameterNames.Version);
            hasPartition = _httpContextAccessor.HttpContext.Request.RouteValues.TryGetValue(KnownActionParameterNames.PartitionName, out var partitionName);

            if (hasPartition)
            {
                routeValues.Add(KnownActionParameterNames.PartitionName, partitionName);
            }
        }

        private Uri RouteUri(string routeName, RouteValueDictionary routeValues)
        {
            HttpRequest request = _httpContextAccessor.HttpContext.Request;

            return new Uri(
                UrlHelper.RouteUrl(
                    routeName,
                    routeValues,
                    request.Scheme,
                    request.Host.Value));
        }
    }
}
