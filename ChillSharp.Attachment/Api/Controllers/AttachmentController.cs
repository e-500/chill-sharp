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

using ChillSharp.Api;
using ChillSharp.Attachment.Model;
using ChillSharp.Attachment.Services;
using ChillSharp.Auth.Api;
using ChillSharp.Dto;
using ChillSharp.Schema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Attachment.Api.Controllers;

/// <summary>
/// Uploads and downloads archived attachments.
/// </summary>
[ApiController]
[Route("api/chill-attachment")]
public sealed class AttachmentController : ControllerBase
{
    private readonly IChillContext _context;
    private readonly IChillAttachmentDbContext _attachmentContext;
    private readonly IChillAttachmentArchive _archive;
    private readonly IChillEntityAclService? _entityAclService;

    public AttachmentController(
        IChillContext context,
        IChillAttachmentDbContext attachmentContext,
        IChillAttachmentArchive archive,
        IChillEntityAclService? entityAclService = null,
        IChillSchemaResolverService? schemaResolver = null)
    {
        _context = context;
        _attachmentContext = attachmentContext;
        _archive = archive;
        _entityAclService = entityAclService;

        if (schemaResolver != null)
        {
            _context.RegisterSchemaService(schemaResolver);
        }
    }

    [AllowAnonymous]
    [HttpGet("attachment/download")]
    public async Task<IActionResult> Download([FromQuery] Guid guid, CancellationToken cancellationToken)
    {
        var attachment = await _attachmentContext.Attachments
            .FirstOrDefaultAsync(x => x.Guid == guid, cancellationToken);

        if (attachment == null)
        {
            return NotFound();
        }

        if (!attachment.Public)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized();
            }

            var authorizationResult = await EnsureEntityAccessAsync(ChillEntityAclAction.Query, cancellationToken);
            if (authorizationResult != null)
            {
                return authorizationResult;
            }
        }

        if (!_archive.Exists(attachment))
        {
            return NotFound();
        }

        var stream = _archive.OpenRead(attachment);
        var contentType = string.IsNullOrWhiteSpace(attachment.MimeType) ? "application/octet-stream" : attachment.MimeType;
        return File(stream, contentType, attachment.OriginalFilename, enableRangeProcessing: true);
    }

    [HttpPost("attachment/upload")]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload([FromForm] AttachmentUploadRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AttachToChillType))
        {
            return BadRequest("attachToChillType is required.");
        }

        if (request.AttachToGuid == Guid.Empty)
        {
            return BadRequest("attachToGuid is required.");
        }

        if (request.Files.Count == 0)
        {
            return BadRequest("At least one file is required.");
        }

        var authorizationResult = await EnsureEntityAccessAsync(ChillEntityAclAction.Create, cancellationToken);
        if (authorizationResult != null)
        {
            return authorizationResult;
        }

        var engine = new ChillEngine(_context);
        var createdAttachments = new List<ChillDtoEntity>();

        foreach (var file in request.Files.Where(x => x.Length > 0))
        {
            var originalFilename = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalFilename);
            var attachment = new FileMetadata
            {
                Guid = Guid.NewGuid(),
                CreatedAtUtc = DateTime.UtcNow,
                AttachToChillType = request.AttachToChillType.Trim(),
                AttachToGuid = request.AttachToGuid,
                OriginalFilename = string.IsNullOrWhiteSpace(originalFilename) ? $"{Guid.NewGuid():N}{extension}" : originalFilename,
                Extension = extension,
                MimeType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                Title = string.IsNullOrWhiteSpace(request.Title) ? Path.GetFileNameWithoutExtension(originalFilename) : request.Title.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                Public = request.Public
            };

            try
            {
                await using (var fileStream = file.OpenReadStream())
                {
                    await _archive.SaveAsync(attachment, fileStream, cancellationToken);
                }

                var createdAttachment = (FileMetadata)engine.Create(attachment);
                createdAttachments.Add(new ChillDtoEntity(_context, createdAttachment));
            }
            catch
            {
                _archive.DeleteIfExists(attachment);
                throw;
            }
        }

        return Ok(createdAttachments);
    }

    private async Task<IActionResult?> EnsureEntityAccessAsync(ChillEntityAclAction action, CancellationToken cancellationToken)
    {
        if (_entityAclService == null || User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var moduleName = typeof(FileMetadata).Namespace ?? "ChillSharp.Attachment.Model";
        var isAllowed = await _entityAclService.AuthorizeAsync(User, moduleName, nameof(FileMetadata), action, cancellationToken);
        return isAllowed ? null : Forbid();
    }
}

public sealed class AttachmentUploadRequest
{
    [FromForm(Name = "attachToChillType")]
    public string AttachToChillType { get; set; } = string.Empty;

    [FromForm(Name = "attachToGuid")]
    public Guid AttachToGuid { get; set; }

    [FromForm(Name = "title")]
    public string? Title { get; set; }

    [FromForm(Name = "description")]
    public string? Description { get; set; }

    [FromForm(Name = "public")]
    public bool Public { get; set; }

    [FromForm(Name = "file")]
    public List<IFormFile> Files { get; set; } = [];
}
