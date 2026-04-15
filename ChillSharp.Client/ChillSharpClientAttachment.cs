/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using ChillSharp.Client.Dto;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ChillSharp.Client
{
    /// <summary>
    /// Adds attachment upload, listing, and download helpers.
    /// </summary>
    public partial class ChillSharpClient
    {
        private const string AttachmentEntityChillType = "ChillSharp.Attachment.Model.Attachment";
        private const string AttachmentQueryChillType = "ChillSharp.Attachment.Query.AttachmentQuery";

        public List<ChillDtoEntity> UploadAttachment(ChillDtoEntity entity, string filePath, string? title = null, string? description = null, bool isPublic = false)
        {
            return UploadAttachmentAsync(entity, filePath, title, description, isPublic).GetAwaiter().GetResult();
        }

        public Task<List<ChillDtoEntity>> UploadAttachmentAsync(ChillDtoEntity entity, string filePath, string? title = null, string? description = null, bool isPublic = false, CancellationToken cancellationToken = default)
        {
            return UploadAttachmentsAsync(entity, [ChillClientAttachmentUploadItem.FromFile(filePath)], title, description, isPublic, cancellationToken);
        }

        public List<ChillDtoEntity> UploadAttachment(ChillDtoEntity entity, byte[] content, string fileName, string? contentType = null, string? title = null, string? description = null, bool isPublic = false)
        {
            return UploadAttachmentAsync(entity, content, fileName, contentType, title, description, isPublic).GetAwaiter().GetResult();
        }

        public Task<List<ChillDtoEntity>> UploadAttachmentAsync(ChillDtoEntity entity, byte[] content, string fileName, string? contentType = null, string? title = null, string? description = null, bool isPublic = false, CancellationToken cancellationToken = default)
        {
            return UploadAttachmentsAsync(entity, [ChillClientAttachmentUploadItem.FromBytes(content, fileName, contentType)], title, description, isPublic, cancellationToken);
        }

        public List<ChillDtoEntity> UploadAttachment(ChillDtoEntity entity, Stream content, string fileName, string? contentType = null, string? title = null, string? description = null, bool isPublic = false, bool leaveOpen = true)
        {
            return UploadAttachmentAsync(entity, content, fileName, contentType, title, description, isPublic, leaveOpen).GetAwaiter().GetResult();
        }

        public Task<List<ChillDtoEntity>> UploadAttachmentAsync(ChillDtoEntity entity, Stream content, string fileName, string? contentType = null, string? title = null, string? description = null, bool isPublic = false, bool leaveOpen = true, CancellationToken cancellationToken = default)
        {
            return UploadAttachmentsAsync(entity, [ChillClientAttachmentUploadItem.FromStream(content, fileName, contentType, leaveOpen)], title, description, isPublic, cancellationToken);
        }

        public List<ChillDtoEntity> UploadAttachments(ChillDtoEntity entity, IEnumerable<ChillClientAttachmentUploadItem> files, string? title = null, string? description = null, bool isPublic = false)
        {
            return UploadAttachmentsAsync(entity, files, title, description, isPublic).GetAwaiter().GetResult();
        }

        public async Task<List<ChillDtoEntity>> UploadAttachmentsAsync(ChillDtoEntity entity, IEnumerable<ChillClientAttachmentUploadItem> files, string? title = null, string? description = null, bool isPublic = false, CancellationToken cancellationToken = default)
        {
            ValidateAttachmentTarget(entity);

            ArgumentNullException.ThrowIfNull(files);

            var fileItems = files.ToList();
            if (fileItems.Count == 0)
                throw new ArgumentException("At least one file is required.", nameof(files));

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(entity.ChillType), "attachToChillType");
            form.Add(new StringContent(entity.Guid.ToString()), "attachToGuid");

            if (!string.IsNullOrWhiteSpace(title))
            {
                form.Add(new StringContent(title.Trim()), "title");
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                form.Add(new StringContent(description.Trim()), "description");
            }

            form.Add(new StringContent(isPublic ? "true" : "false"), "public");

            var openedStreams = new List<Stream>();
            try
            {
                foreach (var file in fileItems)
                {
                    var stream = await file.OpenReadStreamAsync(cancellationToken);
                    openedStreams.Add(stream);

                    var content = new StreamContent(stream);
                    content.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
                    form.Add(content, "file", file.FileName);
                }

                return await SendAttachmentJsonAsync<List<ChillDtoEntity>>(HttpMethod.Post, "attachment/upload", form, allowRetry: false, cancellationToken: cancellationToken)
                    ?? [];
            }
            finally
            {
                foreach (var stream in openedStreams)
                {
                    await stream.DisposeAsync();
                }
            }
        }

        public List<ChillDtoEntity> GetAttachments(ChillDtoEntity entity)
        {
            return GetAttachmentsAsync(entity).GetAwaiter().GetResult();
        }

        public Task<List<ChillDtoEntity>> GetAttachmentsAsync(ChillDtoEntity entity, CancellationToken cancellationToken = default)
        {
            ValidateAttachmentTarget(entity);

            cancellationToken.ThrowIfCancellationRequested();

            var query = new ChillDtoQuery
            {
                ChillType = AttachmentQueryChillType
            };
            query.Properties["AttachToChillType"] = entity.ChillType;
            query.Properties["AttachToGuid"] = entity.Guid;

            return Task.FromResult(Query(query).Results ?? []);
        }

        public byte[] DownloadAttachment(Guid attachmentGuid)
        {
            return DownloadAttachmentAsync(attachmentGuid).GetAwaiter().GetResult();
        }

        public byte[] DownloadAttachment(ChillDtoEntity attachmentEntity)
        {
            return DownloadAttachmentAsync(attachmentEntity).GetAwaiter().GetResult();
        }

        public Task<byte[]> DownloadAttachmentAsync(ChillDtoEntity attachmentEntity, CancellationToken cancellationToken = default)
        {
            if (attachmentEntity == null)
                throw new ArgumentNullException(nameof(attachmentEntity));

            return DownloadAttachmentAsync(attachmentEntity.Guid, cancellationToken);
        }

        public async Task<byte[]> DownloadAttachmentAsync(Guid attachmentGuid, CancellationToken cancellationToken = default)
        {
            if (attachmentGuid == Guid.Empty)
                throw new ArgumentException("attachmentGuid is required.", nameof(attachmentGuid));

            return await SendAttachmentBytesAsync(
                HttpMethod.Get,
                $"attachment/download?guid={Uri.EscapeDataString(attachmentGuid.ToString())}",
                allowAnonymous: !CanUseAuthentication(),
                cancellationToken: cancellationToken);
        }

        public string DownloadAttachmentToFile(Guid attachmentGuid, string destinationFilePath)
        {
            return DownloadAttachmentToFileAsync(attachmentGuid, destinationFilePath).GetAwaiter().GetResult();
        }

        public async Task<string> DownloadAttachmentToFileAsync(Guid attachmentGuid, string destinationFilePath, CancellationToken cancellationToken = default)
        {
            var normalizedPath = NormalizeRequiredValue(destinationFilePath, nameof(destinationFilePath));
            var content = await DownloadAttachmentAsync(attachmentGuid, cancellationToken);

            var directory = Path.GetDirectoryName(normalizedPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(normalizedPath, content, cancellationToken);
            return normalizedPath;
        }

        private async Task<T?> SendAttachmentJsonAsync<T>(HttpMethod method, string relativeUrl, HttpContent? content = null, bool expectResponseBody = true, bool allowAnonymous = false, bool allowRetry = true, CancellationToken cancellationToken = default)
        {
            var start = DateTime.Now;

            try
            {
                if (!allowAnonymous && CanUseAuthentication())
                {
                    GetAuthTokenWithPasswordIfNecessary();
                }

                using var client = _httpClientFactory?.Invoke() ?? new HttpClient();
                using var request = new HttpRequestMessage(method, BuildAttachmentUrl(relativeUrl));
                if (!allowAnonymous && !string.IsNullOrWhiteSpace(_AccessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _AccessToken);
                }

                request.Content = content;
                var response = await client.SendAsync(request, cancellationToken);

                if ((response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden) &&
                    !allowAnonymous &&
                    allowRetry &&
                    TryRefreshAuthentication())
                {
                    response.Dispose();
                    return await SendAttachmentJsonAsync<T>(method, relativeUrl, content, expectResponseBody, allowAnonymous, allowRetry: false, cancellationToken);
                }

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"\n\nExecution time {Math.Round((DateTime.Now - start).TotalMilliseconds / 1000, 2)} s");
                    if (!expectResponseBody)
                    {
                        response.Dispose();
                        return default;
                    }

                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    response.Dispose();
                    if (string.IsNullOrWhiteSpace(responseBody))
                    {
                        return default;
                    }

                    return JsonSerializer.Deserialize<T>(responseBody, _jsonOptions);
                }

                var errorDetails = await response.Content.ReadAsStringAsync(cancellationToken);
                response.Dispose();
                throw new ChillClientException($"Error: {response.StatusCode} {errorDetails}");
            }
            catch (ChillClientException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ChillClientException($"Unexpected error executing {method} {BuildAttachmentUrl(relativeUrl)}, see inner exception for details", ex);
            }
        }

        private async Task<byte[]> SendAttachmentBytesAsync(HttpMethod method, string relativeUrl, bool allowAnonymous = false, bool allowRetry = true, CancellationToken cancellationToken = default)
        {
            var start = DateTime.Now;

            try
            {
                if (!allowAnonymous && CanUseAuthentication())
                {
                    GetAuthTokenWithPasswordIfNecessary();
                }

                using var client = _httpClientFactory?.Invoke() ?? new HttpClient();
                using var request = new HttpRequestMessage(method, BuildAttachmentUrl(relativeUrl));
                if (!allowAnonymous && !string.IsNullOrWhiteSpace(_AccessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _AccessToken);
                }

                var response = await client.SendAsync(request, cancellationToken);
                if ((response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden) &&
                    !allowAnonymous &&
                    allowRetry &&
                    TryRefreshAuthentication())
                {
                    response.Dispose();
                    return await SendAttachmentBytesAsync(method, relativeUrl, allowAnonymous, allowRetry: false, cancellationToken);
                }

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"\n\nExecution time {Math.Round((DateTime.Now - start).TotalMilliseconds / 1000, 2)} s");
                    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    response.Dispose();
                    return bytes;
                }

                var errorDetails = await response.Content.ReadAsStringAsync(cancellationToken);
                response.Dispose();
                throw new ChillClientException($"Error: {response.StatusCode} {errorDetails}");
            }
            catch (ChillClientException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ChillClientException($"Unexpected error executing {method} {BuildAttachmentUrl(relativeUrl)}, see inner exception for details", ex);
            }
        }

        private string BuildAttachmentUrl(string relativeUrl)
        {
            return $"{GetAttachmentBaseUrl().TrimEnd('/')}/{relativeUrl.TrimStart('/')}";
        }

        internal string GetAttachmentBaseUrl()
        {
            const string chillSuffix = "/chill";
            if (_BaseUrl.EndsWith(chillSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return _BaseUrl.Substring(0, _BaseUrl.Length - chillSuffix.Length) + "/chill-attachment";
            }

            return _BaseUrl.TrimEnd('/') + "-attachment";
        }

        private static void ValidateAttachmentTarget(ChillDtoEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity.Guid == Guid.Empty)
                throw new ArgumentException("entity.Guid is required.", nameof(entity));

            if (string.IsNullOrWhiteSpace(entity.ChillType))
                throw new ArgumentException("entity.ChillType is required.", nameof(entity));
        }
    }
}
