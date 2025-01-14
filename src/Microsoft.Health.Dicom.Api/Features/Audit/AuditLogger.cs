﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Core.Configs;

namespace Microsoft.Health.Dicom.Api.Features.Audit
{
    /// <summary>
    /// Provides mechanism to log the audit event using default logger.
    /// </summary>
    public class AuditLogger : IAuditLogger
    {
        private const string AuditEventType = "AuditEvent";

        private static readonly string AuditMessageFormat =
            "ActionType: {ActionType}" + Environment.NewLine +
            "EventType: {EventType}" + Environment.NewLine +
            "Audience: {Audience}" + Environment.NewLine +
            "Authority: {Authority}" + Environment.NewLine +
            "RequestUri: {ResourceUri}" + Environment.NewLine +
            "Action: {Action}" + Environment.NewLine +
            "StatusCode: {StatusCode}" + Environment.NewLine +
            "CorrelationId: {CorrelationId}" + Environment.NewLine +
            "Claims: {Claims}";

        private readonly SecurityConfiguration _securityConfiguration;
        private readonly ILogger<IAuditLogger> _logger;

        public AuditLogger(
            IOptions<SecurityConfiguration> securityConfiguration,
            ILogger<IAuditLogger> logger)
        {
            EnsureArg.IsNotNull(securityConfiguration?.Value, nameof(securityConfiguration));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _securityConfiguration = securityConfiguration.Value;
            _logger = logger;
        }

        /// <inheritdoc />
        public void LogAudit(
            AuditAction auditAction,
            string operation,
            Uri requestUri,
            HttpStatusCode? statusCode,
            string correlationId,
            string callerIpAddress,
            IReadOnlyCollection<KeyValuePair<string, string>> callerClaims,
            IReadOnlyDictionary<string, string> customHeaders = null)
        {
            string claimsInString = null;
            string customerHeadersInString = null;

            if (callerClaims != null)
            {
                claimsInString = string.Join(";", callerClaims.Select(claim => $"{claim.Key}={claim.Value}"));
            }

            if (customHeaders != null)
            {
                customerHeadersInString = string.Join(";", customHeaders.Select(header => $"{header.Key}={header.Value}"));
            }

#pragma warning disable CA2254
            // AuditMessageFormat is not const and erroneously flags CA2254.
            // While the template does indeed change per OS, it does not change the variables in use.
            _logger.LogInformation(
                AuditMessageFormat,
                auditAction,
                AuditEventType,
                _securityConfiguration.Authentication?.Audience,
                _securityConfiguration.Authentication?.Authority,
                requestUri,
                operation,
                statusCode,
                correlationId,
                claimsInString,
                customerHeadersInString);
#pragma warning restore CA2254
        }
    }
}
