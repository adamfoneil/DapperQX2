using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DapperQX;

public static class Extensions
{
    public const string DefaultWhereScope = "global";

    public static void AddQueries(this IServiceCollection services)
    {
        var assembly = Assembly.GetCallingAssembly();
        var queryTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && 
                        t.BaseType?.IsGenericType == true && 
                        t.BaseType.GetGenericTypeDefinition() == typeof(Query<>));
        
        foreach (var type in queryTypes)
        {
            services.AddTransient(type);
        }
    }
}
