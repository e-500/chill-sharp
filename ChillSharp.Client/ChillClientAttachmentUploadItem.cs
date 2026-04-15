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

namespace ChillSharp.Client
{
    /// <summary>
    /// Describes a file payload uploaded through the ChillSharp attachment client helpers.
    /// </summary>
    public sealed class ChillClientAttachmentUploadItem
    {
        private readonly Func<CancellationToken, Task<Stream>> _openReadStreamAsync;

        private ChillClientAttachmentUploadItem(string fileName, string contentType, Func<CancellationToken, Task<Stream>> openReadStreamAsync)
        {
            FileName = string.IsNullOrWhiteSpace(fileName) ? throw new ArgumentException("fileName is required.", nameof(fileName)) : fileName.Trim();
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
            _openReadStreamAsync = openReadStreamAsync ?? throw new ArgumentNullException(nameof(openReadStreamAsync));
        }

        public string FileName { get; }

        public string ContentType { get; }

        internal Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken)
        {
            return _openReadStreamAsync(cancellationToken);
        }

        public static ChillClientAttachmentUploadItem FromFile(string filePath, string? contentType = null)
        {
            var normalizedPath = filePath?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedPath))
                throw new ArgumentException("filePath is required.", nameof(filePath));

            var fileName = Path.GetFileName(normalizedPath);
            return new ChillClientAttachmentUploadItem(
                fileName,
                contentType ?? "application/octet-stream",
                _ => Task.FromResult<Stream>(File.OpenRead(normalizedPath)));
        }

        public static ChillClientAttachmentUploadItem FromBytes(byte[] content, string fileName, string? contentType = null)
        {
            ArgumentNullException.ThrowIfNull(content);

            return new ChillClientAttachmentUploadItem(
                fileName,
                contentType ?? "application/octet-stream",
                _ => Task.FromResult<Stream>(new MemoryStream(content, writable: false)));
        }

        public static ChillClientAttachmentUploadItem FromStream(Stream content, string fileName, string? contentType = null, bool leaveOpen = true)
        {
            ArgumentNullException.ThrowIfNull(content);

            if (!content.CanRead)
                throw new ArgumentException("content stream must be readable.", nameof(content));

            return new ChillClientAttachmentUploadItem(
                fileName,
                contentType ?? "application/octet-stream",
                _ => Task.FromResult<Stream>(leaveOpen ? new NonDisposingStream(content) : content));
        }

        private sealed class NonDisposingStream(Stream innerStream) : Stream
        {
            private readonly Stream _innerStream = innerStream;

            public override bool CanRead => _innerStream.CanRead;
            public override bool CanSeek => _innerStream.CanSeek;
            public override bool CanWrite => _innerStream.CanWrite;
            public override long Length => _innerStream.Length;

            public override long Position
            {
                get => _innerStream.Position;
                set => _innerStream.Position = value;
            }

            public override void Flush()
            {
                _innerStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _innerStream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _innerStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _innerStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _innerStream.Write(buffer, offset, count);
            }

            public override ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }

            protected override void Dispose(bool disposing)
            {
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            public override int Read(Span<byte> buffer)
            {
                return _innerStream.Read(buffer);
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return _innerStream.ReadAsync(buffer, cancellationToken);
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                _innerStream.Write(buffer);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return _innerStream.WriteAsync(buffer, cancellationToken);
            }
        }
    }
}
