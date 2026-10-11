using ChillSharp.Annotations;
using ChillSharp.EF;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ChillSharp.Tests.EF.Model
{
    [ChillEntity(
        UniquePropertyKeyString: "D658348B-1788-4C1E-A071-E5A5AC215C13",
        PrimaryLanguageLabel: "CQRS CRUD item",
        SecondaryLanguageLabel: "Elemento CQRS CRUD")]
    public sealed class CqrsCrudItem : ChillEntity
    {
        [Key]
        public override Guid Guid { get; set; }

        [ChillProperty]
        public string Name { get; set; } = string.Empty;
    }
}

namespace ChillSharp.Tests.EF.Query
{
    [ChillEntity(
        UniquePropertyKeyString: "E8682181-7C03-476C-8B7A-8EF9B2A4AB46",
        PrimaryLanguageLabel: "CQRS CRUD item query",
        SecondaryLanguageLabel: "Ricerca elemento CQRS CRUD")]
    public sealed class CqrsCrudItemQuery : ChillQuery
    {
        public override IQueryable<IChillEntity> OnQuery(IChillContext Context, bool LightweightRequired = false)
        {
            var context = (DummyContext)Context;
            var query = context.CqrsCrudItems.AsQueryable();
            if (Guid.HasValue)
                query = query.Where(item => item.Guid == Guid.Value);
            return query;
        }
    }
}

namespace ChillSharp.Tests.EF.Commands
{
    public sealed class RenameCqrsCrudItemCommand : IChillCommand
    {
        [ChillProperty]
        public Guid ItemGuid { get; set; }

        [ChillProperty]
        public string Name { get; set; } = string.Empty;

        [ChillProperty]
        public string Result { get; set; } = string.Empty;

        public void OnCommand(IChillContext Context)
        {
            var context = (DummyContext)Context;
            var item = context.CqrsCrudItems.Single(item => item.Guid == ItemGuid);
            item.Name = Name;
            context.SaveChanges();
            Result = item.Name;
        }
    }
}