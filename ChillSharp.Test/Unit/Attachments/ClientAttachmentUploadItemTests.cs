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
