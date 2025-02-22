﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Features.Common;

namespace Microsoft.Health.Dicom.Core.Features.Store.Entries
{
    /// <summary>
    /// Represents a DICOM instance entry originated from stream.
    /// </summary>
    public sealed class StreamOriginatedDicomInstanceEntry : IDicomInstanceEntry
    {
        private readonly Stream _stream;
        private readonly AsyncCache<DicomFile> _dicomFileCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamOriginatedDicomInstanceEntry"/> class.
        /// </summary>
        /// <param name="seekableStream">The stream.</param>
        /// <remarks>The <paramref name="seekableStream"/> must be seekable.</remarks>
        internal StreamOriginatedDicomInstanceEntry(Stream seekableStream)
        {
            // The stream must be seekable.
            EnsureArg.IsNotNull(seekableStream, nameof(seekableStream));
            EnsureArg.IsTrue(seekableStream.CanSeek, nameof(seekableStream));

            _stream = seekableStream;
            _dicomFileCache = new AsyncCache<DicomFile>(_ => DicomFile.OpenAsync(_stream, FileReadOption.SkipLargeTags));
        }

        /// <inheritdoc />
        public async ValueTask<DicomDataset> GetDicomDatasetAsync(CancellationToken cancellationToken)
        {
            try
            {
                DicomFile file = await _dicomFileCache.GetAsync(cancellationToken: cancellationToken);
                return file.Dataset;
            }
            catch (DicomFileException)
            {
                throw new InvalidInstanceException(DicomCoreResource.InvalidDicomInstance);
            }
        }

        /// <inheritdoc />
        public ValueTask<Stream> GetStreamAsync(CancellationToken cancellationToken)
        {
            _stream.Seek(0, SeekOrigin.Begin);
            return new ValueTask<Stream>(_stream);
        }

        public async ValueTask DisposeAsync()
        {
            _dicomFileCache.Dispose();
            await _stream.DisposeAsync();
        }
    }
}
