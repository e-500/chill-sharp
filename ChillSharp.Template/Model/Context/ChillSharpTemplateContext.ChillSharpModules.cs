using ChillSharp.Auth.Model;
using ChillSharp.I18n.Model;
using Microsoft.EntityFrameworkCore;
using ChillSharp.Attachment.Model;

namespace ChillSharp.Template;

public partial class ChillSharpTemplateContext
{
    public new DbSet<AuthUser> Users => Set<AuthUser>();
    public new DbSet<AuthRole> Roles => Set<AuthRole>();
    public new DbSet<AuthUserRole> UserRoles => Set<AuthUserRole>();
    public DbSet<AuthPermissionRule> PermissionRules => Set<AuthPermissionRule>();
    public DbSet<AuthRefreshToken> RefreshTokens => Set<AuthRefreshToken>();
    public DbSet<Text> Texts => Set<Text>();
    public DbSet<FileMetadata> Attachments => Set<FileMetadata>();
}
