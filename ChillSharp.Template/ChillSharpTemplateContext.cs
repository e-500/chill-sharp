using ChillSharp;
using ChillSharp.Attachment;
using ChillSharp.Auth;
using ChillSharp.I18n;
using ChillSharp.Schema;
using ChillSharp.Schema.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Template;

public partial class ChillSharpTemplateContext : IdentityDbContext<IdentityUser>, IChillContext, IChillAuthDbContext, IChillI18nDbContext, IChillAttachmentDbContext, IChillSchemaDbContext
{
    public const string DefaultConnectionString = "Data Source=chill-sharp-template.db";

    public DbSet<ChillSchemaEntry> SchemaEntries => Set<ChillSchemaEntry>();
    public DbSet<ChillEntityOptionsEntry> EntityOptionsEntries => Set<ChillEntityOptionsEntry>();
    public DbSet<ChillMenuItemEntry> MenuItems => Set<ChillMenuItemEntry>();

    public ChillSharpTemplateContext()
    {
    }

    public ChillSharpTemplateContext(DbContextOptions<ChillSharpTemplateContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite(DefaultConnectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddChillAuthModel();
        modelBuilder.AddChillI18nModel();
        modelBuilder.AddChillAttachmentModel();
        modelBuilder.AddChillSchemaModel();
        OnModelCreatingPartial(modelBuilder);
    }

    public string GetChillTypePrefix()
    {
        return "ChillSharp.Template";
    }

    public string GetPrimaryCultureName()
    {
        return "en-US";
    }

    public string GetSecondaryCultureName()
    {
        return "it-IT";
    }

    public string GetCurrentUserName()
    {
        return Environment.UserName;
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
