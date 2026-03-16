using ChillSharp.EF.ServiceModel.I18n;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Tests.EF
{
    public partial class DummyContext : ChillSharp.I18n.IChillI18nDbContext
    {
        public DbSet<Text> Texts { get; set; }
    }
}
