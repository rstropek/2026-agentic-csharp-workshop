namespace GraderApi.Tools;

public static class ToolSetup
{
    /// <summary>
    /// Adds the <see cref="ToolCatalog"/> and its tool classes (scoped, like the use-case services they call).
    /// </summary>
    public static IServiceCollection AddGraderTools(this IServiceCollection services)
    {
        services.AddSingleton<ToolCatalog>();
        foreach (var toolClass in ToolCatalog.ToolClasses)
        {
            services.AddScoped(toolClass);
        }

        return services;
    }
}
