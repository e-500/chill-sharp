# Attachment Module

`ChillSharp.Attachment` adds a built-in attachment entity plus upload and download endpoints backed by a filesystem archive.

## What It Adds

- `Attachment` Chill entity persisted in the `attachment` table
- generic Chill CRUD support for attachment metadata
- `GET /api/chill-attachment/attachment/download?guid=...`
- `POST /api/chill-attachment/attachment/upload`

Because `Attachment` is a real `ChillEntity`, it is also exposed through schema discovery and can be managed through the standard Chill CRUD endpoints once the host `DbContext` implements `IChillAttachmentDbContext`.

The module also exposes an `AttachmentQuery` Chill query type, which client helpers can use to list attachments linked to a target entity.

## Register The Module

Add the module model to your context and expose the `DbSet`:

```csharp
using ChillSharp.Attachment;
using ChillSharp.Attachment.Model;

public class AppDbContext : DbContext, IChillContext, IChillAttachmentDbContext
{
    public DbSet<Attachment> Attachments => Set<Attachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddChillAttachmentModel();
    }

    public string GetChillTypePrefix() => "MyApp.Data";
}
```

When the context implements `IChillAttachmentDbContext`, `services.AddChillApi<AppDbContext>()` automatically registers the attachment endpoints.

## Archive Root Configuration

The module reads the archive root from:

- startup options via `services.Configure<ChillAttachmentOptions>(...)`
- environment variable `CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT`

Example:

```csharp
builder.Services.Configure<ChillAttachmentOptions>(options =>
{
    options.ArchiveRoot = "/srv/chill/attachments";
});
```

Or through environment:

```env
CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT=/srv/chill/attachments
```

## Archive Layout

Files are stored under the configured archive root using:

```csharp
public static string BuildAttachmentPath(
    string archiveRoot,
    string attachToChillType,
    Guid id,
    string extension,
    DateTime createdAtUtc)
```

The resulting layout is:

```text
{archiveRoot}/{attachToChillType}/{year}/{guid[0..2]}/{guid[2..4]}/{guid}{extension}
```

Example:

```text
/srv/chill/attachments/Post/2026/ab/cd/abcd1234....pdf
```

## Upload Endpoint

`POST /api/chill-attachment/attachment/upload`

Multipart form fields:

- `attachToChillType`
- `attachToGuid`
- `title`
- `description`
- `public`
- one or more `file` parts

For each uploaded file the module:

1. creates an `Attachment` Chill entity through `ChillEngine`
2. stores the physical file in the archive
3. returns the created attachment DTO payload

## Download Endpoint

`GET /api/chill-attachment/attachment/download?guid={attachmentGuid}`

Behavior:

- loads the `Attachment` entity from the database
- resolves the archived file path
- returns the file using the stored original filename and mime type
- allows anonymous download when `Public == true`
- requires an authenticated user when `Public == false`

## Client Helpers

### `.NET`

`ChillSharp.Client` now includes attachment helpers:

```csharp
var post = new ChillDtoEntity
{
    Guid = postGuid,
    ChillType = "Model.Post"
};

var uploaded = await client.UploadAttachmentAsync(
    post,
    File.ReadAllBytes("contract.pdf"),
    "contract.pdf",
    "application/pdf",
    title: "Signed contract",
    description: "Customer-facing version",
    isPublic: false);

var attachments = await client.GetAttachmentsAsync(post);
var fileBytes = await client.DownloadAttachmentAsync(uploaded[0]);
```

Available overloads cover:

- upload from file path
- upload from `byte[]`
- upload from `Stream`
- download by attachment `Guid`
- download by attachment `ChillDtoEntity`

### TypeScript / Angular / React / Vue / Python

The generic client libraries under `extra-libs/` expose matching helpers:

- TypeScript: `uploadAttachment()`, `uploadAttachments()`, `getAttachments()`, `downloadAttachment()`
- Angular: same helpers through `ChillSharpNgClient` as `Observable` wrappers
- React and Vue: same helpers through the raw client returned by `useChillSharpClient()`
- Python: `upload_attachment()`, `upload_attachments()`, `get_attachments()`, `download_attachment()`

## Generic CRUD

Attachment metadata remains available through the standard Chill endpoints:

- `POST /api/chill/create`
- `POST /api/chill/find`
- `POST /api/chill/update`
- `POST /api/chill/delete`

Deleting an attachment through ChillSharp also removes the archived file from disk.
