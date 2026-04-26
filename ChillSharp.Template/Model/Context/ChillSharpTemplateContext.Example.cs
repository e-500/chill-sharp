using ChillSharp.Template.Model;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Template;

public partial class ChillSharpTemplateContext
{
    public DbSet<Example> Examples => Set<Example>();
}
