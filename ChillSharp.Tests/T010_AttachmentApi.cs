using ChillSharp.Attachment.Model;
using ChillSharp.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ClientDto = ChillSharp.Client.Dto;

namespace ChillSharp.Tests;

[TestClass]
public sealed class AttachmentApi
{
    [TestMethod]
    public async Task Step001_UploadCreatesAttachmentEntityAndStoresArchiveFile()
    {
        AttachmentApiHost.EnsureStarted();

        using var client = AttachmentApiHost.CreateClient(authenticated: true);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Post"), "attachToChillType");
        form.Add(new StringContent(Guid.NewGuid().ToString()), "attachToGuid");
        form.Add(new StringContent("Contract"), "title");
        form.Add(new StringContent("Signed PDF"), "description");
        form.Add(new StringContent("true"), "public");
        form.Add(new StreamContent(new MemoryStream("hello attachment"u8.ToArray()))
        {
            Headers =
            {
                ContentType = new MediaTypeHeaderValue("text/plain")
            }
        }, "file", "hello.txt");

        using var response = await client.PostAsync("http://localhost:6013/api/chill-attachment/attachment/upload", form);
        if (!response.IsSuccessStatusCode)
        {
            Assert.Fail($"Upload failed with {(int)response.StatusCode} {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }

        var created = await response.Content.ReadFromJsonAsync<List<ChillSharp.Dto.ChillDtoEntity>>();
        Assert.IsNotNull(created);
        Assert.AreEqual(1, created.Count);

        var attachmentGuid = created[0].Guid;

        await using var context = AttachmentApiHost.CreateDbContext();
        var persisted = await context.Attachments.FirstAsync(x => x.Guid == attachmentGuid);
        Assert.AreEqual("hello.txt", persisted.OriginalFilename);
        Assert.AreEqual(".txt", persisted.Extension);
        Assert.AreEqual("text/plain", persisted.MimeType);
        Assert.AreEqual("Post", persisted.AttachToChillType);
        Assert.IsTrue(persisted.Public);

        var archivePath = AttachmentApiHost.BuildArchivePath(persisted);
        Assert.IsTrue(File.Exists(archivePath));
        Assert.AreEqual("hello attachment", await File.ReadAllTextAsync(archivePath));
    }

    [TestMethod]
    public async Task Step002_PublicDownloadAllowsAnonymousAndPrivateDownloadRequiresAuth()
    {
        AttachmentApiHost.EnsureStarted();

        var publicGuid = Guid.NewGuid();
        var privateGuid = Guid.NewGuid();
        var attachedGuid = Guid.NewGuid();

        await using (var context = AttachmentApiHost.CreateDbContext())
        {
            var publicAttachment = new ChillSharp.Attachment.Model.Attachment
            {
                Guid = publicGuid,
                AttachToChillType = "Post",
                AttachToGuid = attachedGuid,
                OriginalFilename = "public.txt",
                Extension = ".txt",
                MimeType = "text/plain",
                Title = "Public",
                Description = "Public attachment",
                Public = true,
                CreatedAtUtc = new DateTime(2026, 4, 15, 8, 0, 0, DateTimeKind.Utc)
            };

            var privateAttachment = new ChillSharp.Attachment.Model.Attachment
            {
                Guid = privateGuid,
                AttachToChillType = "Post",
                AttachToGuid = attachedGuid,
                OriginalFilename = "private.txt",
                Extension = ".txt",
                MimeType = "text/plain",
                Title = "Private",
                Description = "Private attachment",
                Public = false,
                CreatedAtUtc = new DateTime(2026, 4, 15, 8, 0, 0, DateTimeKind.Utc)
            };

            context.Attachments.Add(publicAttachment);
            context.Attachments.Add(privateAttachment);
            await context.SaveChangesAsync();

            await File.WriteAllTextAsync(AttachmentApiHost.BuildArchivePath(publicAttachment), "public file");
            await File.WriteAllTextAsync(AttachmentApiHost.BuildArchivePath(privateAttachment), "private file");
        }

        using var anonymousClient = AttachmentApiHost.CreateClient(authenticated: false);
        using var publicResponse = await anonymousClient.GetAsync($"http://localhost:6013/api/chill-attachment/attachment/download?guid={publicGuid}");
        Assert.AreEqual(HttpStatusCode.OK, publicResponse.StatusCode);
        Assert.AreEqual("public file", await publicResponse.Content.ReadAsStringAsync());

        using var privateAnonymousResponse = await anonymousClient.GetAsync($"http://localhost:6013/api/chill-attachment/attachment/download?guid={privateGuid}");
        Assert.AreEqual(HttpStatusCode.Unauthorized, privateAnonymousResponse.StatusCode);

        using var authenticatedClient = AttachmentApiHost.CreateClient(authenticated: true);
        using var privateAuthenticatedResponse = await authenticatedClient.GetAsync($"http://localhost:6013/api/chill-attachment/attachment/download?guid={privateGuid}");
        Assert.AreEqual(HttpStatusCode.OK, privateAuthenticatedResponse.StatusCode);
        Assert.AreEqual("private file", await privateAuthenticatedResponse.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Step003_AttachmentEntityIsExposedBySchemaList()
    {
        AttachmentApiHost.EnsureStarted();

        using var client = AttachmentApiHost.CreateClient(authenticated: true);
        var schemaList = await client.GetFromJsonAsync<List<ChillSharp.Schema.Contracts.ChillDtoSchemaListItem>>(
            "http://localhost:6013/api/chill-schema/get-schema-list");

        Assert.IsNotNull(schemaList);
        Assert.IsTrue(schemaList.Any(x => string.Equals(x.Name, "Attachment", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task Step004_ClientHelpersUploadAndListAttachments()
    {
        AttachmentApiHost.EnsureStarted();

        var post = new ClientDto.ChillDtoEntity
        {
            Guid = Guid.NewGuid(),
            ChillType = "Model.Post"
        };

        var client = AttachmentApiHost.CreateChillClient(authenticated: true);
        var created = await client.UploadAttachmentAsync(
            post,
            "hello attachment from client"u8.ToArray(),
            "client-upload.txt",
            "text/plain",
            title: "Client contract",
            description: "Uploaded through ChillSharp.Client",
            isPublic: false);

        Assert.AreEqual(1, created.Count);
        Assert.AreEqual(AttachmentApiHost.NormalizeAttachmentChillType(created[0].ChillType), AttachmentApiHost.NormalizeAttachmentChillType("Attachment"));

        var attachments = await client.GetAttachmentsAsync(post);
        Assert.AreEqual(1, attachments.Count);
        Assert.AreEqual(created[0].Guid, attachments[0].Guid);
        Assert.AreEqual("Model.Post", attachments[0].GetString("AttachToChillType"));
        Assert.AreEqual(post.Guid.ToString(), attachments[0].GetValue("AttachToGuid")?.ToString());
        Assert.AreEqual("client-upload.txt", attachments[0].GetString("OriginalFilename"));
    }

    [TestMethod]
    public async Task Step005_ClientHelpersDownloadAttachmentByGuidAndEntity()
    {
        AttachmentApiHost.EnsureStarted();

        var attachmentGuid = Guid.NewGuid();
        var attachedGuid = Guid.NewGuid();

        await using (var context = AttachmentApiHost.CreateDbContext())
        {
            var attachment = new ChillSharp.Attachment.Model.Attachment
            {
                Guid = attachmentGuid,
                AttachToChillType = "Model.Post",
                AttachToGuid = attachedGuid,
                OriginalFilename = "download.txt",
                Extension = ".txt",
                MimeType = "text/plain",
                Title = "Download",
                Description = "Download helper",
                Public = false,
                CreatedAtUtc = new DateTime(2026, 4, 15, 8, 0, 0, DateTimeKind.Utc)
            };

            context.Attachments.Add(attachment);
            await context.SaveChangesAsync();
            await File.WriteAllTextAsync(AttachmentApiHost.BuildArchivePath(attachment), "download helper content");
        }

        var client = AttachmentApiHost.CreateChillClient(authenticated: true);
        var downloadedByGuid = await client.DownloadAttachmentAsync(attachmentGuid);
        CollectionAssert.AreEqual("download helper content"u8.ToArray(), downloadedByGuid);

        var attachmentEntity = new ClientDto.ChillDtoEntity
        {
            Guid = attachmentGuid,
            ChillType = "Attachment"
        };

        var downloadedByEntity = await client.DownloadAttachmentAsync(attachmentEntity);
        CollectionAssert.AreEqual(downloadedByGuid, downloadedByEntity);
    }

    private static class AttachmentApiHost
    {
        private static readonly object SyncRoot = new();
        private static readonly string DatabasePath = Path.Combine(Path.GetTempPath(), "ChillSharpTestContext", "attachment-api-host.db");
        private static readonly string ArchiveRoot = Path.Combine(Path.GetTempPath(), "ChillSharpTestContext", "attachment-archive");
        private static bool _apiServiceUpAndRunning;

        public static void EnsureStarted()
        {
            if (_apiServiceUpAndRunning)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (_apiServiceUpAndRunning)
                {
                    return;
                }

                var apiServer = Task.Run(() =>
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
                    Directory.CreateDirectory(ArchiveRoot);
                    if (File.Exists(DatabasePath))
                    {
                        File.Delete(DatabasePath);
                    }

                    if (Directory.Exists(ArchiveRoot))
                    {
                        Directory.Delete(ArchiveRoot, recursive: true);
                    }

                    Directory.CreateDirectory(ArchiveRoot);

                    var ctx = CreateDbContext();
                    ctx.Database.EnsureDeleted();
                    ctx.Database.EnsureCreated();

                    var builder = WebApplication.CreateBuilder(Array.Empty<string>());
                    builder.WebHost.UseUrls("http://localhost:6013");
                    builder.Logging.ClearProviders();
                    builder.Services.AddDbContext<EF.DummyContext>(options =>
                        options.UseSqlite($"Data Source={DatabasePath}"));
                    builder.Services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestHeaderAuthenticationHandler>("Test", _ => { });
                    builder.Services.AddAuthorization();
                    builder.Services.AddChillApi<EF.DummyContext>(options =>
                    {
                        options.ProtectedApi = true;
                        options.EnableAuthApi = false;
                    });
                    builder.Services.Configure<ChillSharp.Attachment.Services.ChillAttachmentOptions>(options =>
                    {
                        options.ArchiveRoot = ArchiveRoot;
                    });

                    var app = builder.Build();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.MapChillApi();
                    app.Run();
                });

                apiServer.Wait(5000);
                _apiServiceUpAndRunning = true;
            }
        }

        public static EF.DummyContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={DatabasePath}")
                .Options;
            return new EF.DummyContext(options);
        }

        public static HttpClient CreateClient(bool authenticated)
        {
            var client = new HttpClient();
            if (authenticated)
            {
                client.DefaultRequestHeaders.Add("X-Test-User", "attachment-user");
            }

            return client;
        }

        public static ChillSharp.Client.ChillSharpClient CreateChillClient(bool authenticated)
        {
            return new ChillSharp.Client.ChillSharpClient(
                "http://localhost:6013/api/chill",
                () => CreateClient(authenticated));
        }

        public static string NormalizeAttachmentChillType(string chillType)
        {
            return chillType?.Split('.').LastOrDefault() ?? string.Empty;
        }

        public static string BuildArchivePath(ChillSharp.Attachment.Model.Attachment attachment)
        {
            var path = ChillSharp.Attachment.Services.ChillAttachmentArchive.BuildAttachmentPath(
                ArchiveRoot,
                attachment.AttachToChillType,
                attachment.Guid,
                attachment.Extension,
                attachment.CreatedAtUtc);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return path;
        }
    }

    private sealed class TestHeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestHeaderAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-User", out var userId) || string.IsNullOrWhiteSpace(userId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, userId.ToString())
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
