using ChillSharp.Client;

namespace ChillSharp.Test;

[TestClass]
public class ChillClientAttachmentUploadItemTests
{
    [TestMethod]
    public void FromBytes_TrimsMetadataAndUsesDefaultContentType()
    {
        var item = ChillClientAttachmentUploadItem.FromBytes([1, 2, 3], " report.csv ");

        Assert.AreEqual("report.csv", item.FileName);
        Assert.AreEqual("application/octet-stream", item.ContentType);
    }

    [TestMethod]
    public void FromBytes_PreservesExplicitContentTypeAfterTrimming()
    {
        var item = ChillClientAttachmentUploadItem.FromBytes([], "document.txt", " text/plain ");

        Assert.AreEqual("text/plain", item.ContentType);
    }

    [TestMethod]
    public void FromBytes_RejectsNullContent()
    {
        Assert.Throws<ArgumentNullException>(() => ChillClientAttachmentUploadItem.FromBytes(null!, "document.txt"));
    }

    [TestMethod]
    public void FromBytes_RejectsBlankFileName()
    {
        Assert.Throws<ArgumentException>(() => ChillClientAttachmentUploadItem.FromBytes([], "  "));
    }

    [TestMethod]
    public void FromFile_UsesFileNameRatherThanFullPath()
    {
        var item = ChillClientAttachmentUploadItem.FromFile("  folder/subfolder/report.pdf  ", "application/pdf");

        Assert.AreEqual("report.pdf", item.FileName);
        Assert.AreEqual("application/pdf", item.ContentType);
    }

    [TestMethod]
    public void FromFile_RejectsBlankPath()
    {
        Assert.Throws<ArgumentException>(() => ChillClientAttachmentUploadItem.FromFile(" "));
    }

    [TestMethod]
    public void FromStream_RejectsUnreadableStream()
    {
        using var stream = new WriteOnlyStream();

        Assert.Throws<ArgumentException>(() => ChillClientAttachmentUploadItem.FromStream(stream, "output.bin"));
    }

    [TestMethod]
    public async Task FromStream_ReturnsNonDisposingWrapperAndForwardsStreamOperations()
    {
        using var source = new MemoryStream();
        source.Write([1, 2, 3, 4]);
        source.Position = 0;
        var item = ChillClientAttachmentUploadItem.FromStream(source, " data.bin ");

        var stream = await item.OpenReadStreamAsync(CancellationToken.None);

        Assert.AreNotSame(source, stream);
        Assert.IsTrue(stream.CanRead);
        Assert.IsTrue(stream.CanSeek);
        Assert.IsTrue(stream.CanWrite);
        Assert.AreEqual(4, stream.Length);
        Assert.AreEqual(0, stream.Position);
        stream.Position = 1;
        Assert.AreEqual(2, stream.ReadByte());
        Assert.AreEqual(0, stream.Seek(0, SeekOrigin.Begin));
        Assert.AreEqual(1, stream.ReadByte());
        stream.Flush();

        stream.Position = 0;
        var buffer = new byte[1];
        Assert.AreEqual(1, await stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None));
        Assert.AreEqual(1, buffer[0]);
        stream.Position = 0;
        Assert.AreEqual(1, stream.Read(new Span<byte>(new byte[1])));
        stream.Position = 0;
        Assert.AreEqual(1, await stream.ReadAsync(new Memory<byte>(new byte[1])));

        stream.Position = stream.Length;
        stream.Write(new byte[] { 5 }, 0, 1);
        stream.Write(new ReadOnlySpan<byte>(new byte[] { 6 }));
        await stream.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 7 }));
        stream.SetLength(6);

        await stream.DisposeAsync();
        stream.Dispose();
        Assert.IsTrue(source.CanRead);
    }

    [TestMethod]
    public async Task FromStream_CanTransferOwnershipAndRejectsNullContent()
    {
        Assert.Throws<ArgumentNullException>(() => ChillClientAttachmentUploadItem.FromStream(null!, "file.bin"));

        var source = new MemoryStream();
        var item = ChillClientAttachmentUploadItem.FromStream(source, "file.bin", leaveOpen: false);

        Assert.AreSame(source, await item.OpenReadStreamAsync(CancellationToken.None));
        source.Dispose();
        Assert.IsFalse(source.CanRead);
    }

    [TestMethod]
    public async Task FromBytes_OpensIndependentReadOnlyStreams()
    {
        var item = ChillClientAttachmentUploadItem.FromBytes([1, 2], "file.bin");

        await using var first = await item.OpenReadStreamAsync(CancellationToken.None);
        await using var second = await item.OpenReadStreamAsync(CancellationToken.None);

        Assert.AreNotSame(first, second);
        Assert.IsFalse(first.CanWrite);
        Assert.AreEqual(2, first.Length);
    }

    [TestMethod]
    public async Task FromFile_OpensFileContents()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bin");
        await File.WriteAllBytesAsync(path, [9, 8]);

        try
        {
            var item = ChillClientAttachmentUploadItem.FromFile(path);
            await using var stream = await item.OpenReadStreamAsync(CancellationToken.None);

            Assert.AreEqual(Path.GetFileName(path), item.FileName);
            Assert.AreEqual(9, stream.ReadByte());
            Assert.AreEqual("application/octet-stream", item.ContentType);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class WriteOnlyStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) { }
    }
}
