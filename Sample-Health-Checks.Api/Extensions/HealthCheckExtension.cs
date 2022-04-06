using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;

namespace Sample_Health_Checks.Api.Extensions
{
    public static class HealthCheckExtension
    {
        public static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                    .AddSqlServer(
                        connectionString: configuration.GetConnectionString("SqlServer"),
                        name: "SQL Server",
                        tags: GetDatabaseTags(),
                        failureStatus: HealthStatus.Unhealthy
                    )
                    .AddCosmosDb(
                        connectionString: configuration.GetConnectionString("CosmosDb"),
                        database: configuration["CosmosDb:DatabaseName"],
                        name: "Cosmos DB",
                        tags: GetDatabaseTags(),
                        failureStatus: HealthStatus.Unhealthy
                    );

            services.AddHealthChecksUI()
                    .AddInMemoryStorage();
        }

        public static void UseHealthChecksExtension(this IApplicationBuilder app)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/healthcheck",
                    new HealthCheckOptions
                    {
                        AllowCachingResponses = false,
                        ResponseWriter = async (context, report) =>
                        {
                            await context.Response.WriteAsync(JsonSerializer.Serialize(
                                new
                                {
                                    Status = report.Status.ToString(),
                                    TotalDuration = report.TotalDuration.ToString(),
                                    Monitors = report.Entries.Select(s => 
                                        new 
                                        { 
                                            Resource = s.Key, 
                                            Status = Enum.GetName(typeof(HealthStatus), s.Value.Status), 
                                            s.Value.Tags 
                                        })
                                })
                            );
                        }
                    }
                );
            });

            app.UseHealthChecks("/healthcheck", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.UseHealthChecksUI(options =>
            {
                options.UIPath = "/healthcheck-ui";
            });
        }

        private static string[] GetDatabaseTags()
            => new string[] { "Database", "Infrastructure" };
    }
}