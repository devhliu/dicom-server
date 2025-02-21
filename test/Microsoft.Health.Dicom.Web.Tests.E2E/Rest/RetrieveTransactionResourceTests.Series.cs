﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Comparers;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Dicom.Tests.Common.TranscoderTests;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public partial class RetrieveTransactionResourceTests
    {
        [Theory]
        [InlineData(RequestOriginalContentTestFolder, "*")]
        [InlineData(FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder, null)]
        [InlineData(FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder, "1.2.840.10008.1.2.1")]
        [InlineData(FromExplicitVRLittleEndianToJPEG2000LosslessTestFolder, "1.2.840.10008.1.2.4.90")]
        public async Task GivenSupportedAcceptHeaders_WhenRetrieveSeries_ThenServerShouldReturnExpectedContent(string testDataFolder, string transferSyntax)
        {
            TranscoderTestData transcoderTestData = TranscoderTestDataHelper.GetTestData(testDataFolder);
            DicomFile inputDicomFile = DicomFile.Open(transcoderTestData.InputDicomFile);
            var instanceId = RandomizeInstanceIdentifier(inputDicomFile.Dataset);
            await _instancesManager.StoreAsync(new[] { inputDicomFile });

            using DicomWebAsyncEnumerableResponse<DicomFile> response = await _client.RetrieveSeriesAsync(instanceId.StudyInstanceUid, instanceId.SeriesInstanceUid, transferSyntax);
            Assert.Equal(DicomWebConstants.MultipartRelatedMediaType, response.ContentHeaders.ContentType.MediaType);

            await foreach (DicomFile actual in response)
            {
                // TODO: verify media type once https://microsofthealth.visualstudio.com/Health/_workitems/edit/75185 is done
                var expected = DicomFile.Open(transcoderTestData.ExpectedOutputDicomFile);
                Assert.Equal(expected, actual, new DicomFileEqualityComparer(
                    ignoredTags: new[]
                    {
                        DicomTag.ImplementationVersionName,  // Version name is updated as we update fo-dicom
                        DicomTag.StudyInstanceUID,
                        DicomTag.SeriesInstanceUID,
                        DicomTag.SOPInstanceUID
                    }));
            }
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedAcceptHeadersForStudiesAndSeries))]
        public async Task GivenUnsupportedAcceptHeaders_WhenRetrieveSeries_ThenServerShouldReturnNotAcceptable(bool singlePart, string mediaType, string transferSyntax)
        {
            var requestUri = new Uri(DicomApiVersions.Latest + string.Format(DicomWebConstants.BaseSeriesUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate()), UriKind.Relative);

            using HttpRequestMessage request = new HttpRequestMessageBuilder().Build(requestUri, singlePart: singlePart, mediaType, transferSyntax);
            using HttpResponseMessage response = await _client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
        }

        [Fact]
        public async Task GivenMultipleInstances_WhenRetrieveSeries_ThenServerShouldReturnExpectedInstances()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            DicomFile dicomFile2 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            DicomFile dicomFile3 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                TestUidGenerator.Generate(),
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            await _instancesManager.StoreAsync(new[] { dicomFile1, dicomFile2, dicomFile3 });

            using DicomWebAsyncEnumerableResponse<DicomFile> response = await _client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid);

            DicomFile[] instancesInStudy = await response.ToArrayAsync();

            Assert.Equal(2, instancesInStudy.Length);

            byte[][] actual = instancesInStudy.Select(item => item.ToByteArray()).ToArray();

            Assert.Contains(dicomFile1.ToByteArray(), actual);
            Assert.Contains(dicomFile2.ToByteArray(), actual);
        }

        [Fact]
        public async Task GivenMultipleInstancesWithMixTransferSyntax_WhenRetrieveSeries_ThenServerShouldReturnNotAcceptable()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            DicomFile dicomFile2 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.MPEG2.UID.UID,
                encode: false);

            await _instancesManager.StoreAsync(new[] { dicomFile1, dicomFile2 });

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid, dicomTransferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID));
            Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
        }

        [Fact]
        public async Task GivenMultipleInstancesWithMixTransferSyntax_WhenRetrieveSeriesWithOriginalTransferSyntax_ThenServerShouldReturnOrignialContents()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            DicomFile dicomFile2 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.MPEG2.UID.UID,
                encode: false);

            await _instancesManager.StoreAsync(new[] { dicomFile1, dicomFile2 });

            using DicomWebAsyncEnumerableResponse<DicomFile> response = await _client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid, dicomTransferSyntax: "*");

            // check response multi-part part content-type header
            await using Stream stream = await response.Content.ReadAsStreamAsync(CancellationToken.None)
                .ConfigureAwait(false);
            stream.Seek(0, SeekOrigin.Begin);

            MultipartSection part;
            var media = MediaTypeHeaderValue.Parse(response.Content.Headers.ContentType.ToString());
            var multipartReader = new MultipartReader(HeaderUtilities.RemoveQuotes(media.Boundary).Value, stream, 100);

            List<string> partContentTypeHeader = new List<string>();
            while ((part = await multipartReader.ReadNextSectionAsync(CancellationToken.None).ConfigureAwait(false)) != null)
            {
                partContentTypeHeader.Add(part.ContentType);
            }
            Assert.Equal($"application/dicom; transfer-syntax={DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID}", partContentTypeHeader[0]);
            Assert.Equal($"application/dicom; transfer-syntax={DicomTransferSyntax.MPEG2.UID.UID}", partContentTypeHeader[1]);

            // check content body
            DicomFile[] instancesInStudy = await response.ToArrayAsync();

            Assert.Equal(2, instancesInStudy.Length);

            byte[][] actual = instancesInStudy.Select(item => item.ToByteArray()).ToArray();

            Assert.Contains(dicomFile1.ToByteArray(), actual);
            Assert.Contains(dicomFile2.ToByteArray(), actual);
        }

        [Fact]
        public async Task GivenNonExistingSeries_WhenRetrieveSeries_ThenServerShouldReturnNotFound()
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveSeriesAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate()));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenInstanceWithoutPixelData_WhenRetrieveSeries_ThenServerShouldReturnExpectedContent()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid);
            await _instancesManager.StoreAsync(new[] { dicomFile1 });

            using DicomWebAsyncEnumerableResponse<DicomFile> response = await _client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid, dicomTransferSyntax: "*");

            DicomFile[] studyRetrieve = await response.ToArrayAsync();

            Assert.Equal(
                new[] { dicomFile1.ToByteArray() },
                studyRetrieve.Select(item => item.ToByteArray()));
        }
    }
}
