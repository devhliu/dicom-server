﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// The error store saving extended query tag errors.
    /// </summary>
    public interface IExtendedQueryTagErrorStore
    {
        /// <summary>
        /// Get extended query tags errors by tag path.
        /// </summary>
        /// <param name="tagPath">The tag path.</param>
        /// <param name="limit">The maximum number of results to retrieve.</param>
        /// <param name="offset">The offset from which to retrieve paginated results.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of Extended Query Tag Errors.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="limit"/> is less than <c>1</c></para>
        /// <para>-or-</para>
        /// <para><paramref name="offset"/> is less than <c>0</c>.</para>
        /// </exception>
        Task<IReadOnlyList<ExtendedQueryTagError>> GetExtendedQueryTagErrorsAsync(string tagPath, int limit, int offset = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously adds an error for a specified Extended Query Tag.
        /// </summary>
        /// <param name="tagKey">TagKey of the extended query tag to which an error will be added.</param>
        /// <param name="errorCode">Validation error code.</param>
        /// <param name="watermark">Watermark.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        Task AddExtendedQueryTagErrorAsync(
            int tagKey,
            ValidationErrorCode errorCode,
            long watermark,
            CancellationToken cancellationToken = default);
    }
}
