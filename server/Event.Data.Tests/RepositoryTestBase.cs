using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Event.Data.Contexts;
using Serilog;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Storage;

namespace Event.Data.Tests
{
    public abstract class RepositoryTestBase : IDisposable
    {
        protected readonly EventDbContext Context;
        private readonly IDbContextTransaction _transaction;

        static RepositoryTestBase()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            string logPath = Path.Combine(projectDir, "test_results.log");

            try
            {
                if (File.Exists(logPath))
                {
                    File.Delete(logPath);
                }
            }
            catch
            {
                // Silently swallow errors if file is locked or inaccessible during static setup
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}")
                .CreateLogger();
        }

        protected RepositoryTestBase()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string apiDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Event.API"));
            string appSettingsPath = Path.Combine(apiDir, "appsettings.Development.json");

            var config = new ConfigurationBuilder()
                .AddJsonFile(appSettingsPath, optional: true, reloadOnChange: false)
                .Build();

            string connectionString = config.GetConnectionString("DefaultConnection");
            
            var options = new DbContextOptionsBuilder<EventDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            Context = new EventDbContext(options);
            _transaction = Context.Database.BeginTransaction();
        }

        protected void LogSubTest(string repoName, string testCaseName, bool success, string? details = null)
        {
            string result = success ? "Successful" : "Failed";
            Log.Information("Repo: {RepoName}, Test Case: {TestCaseName}, Result: {ResultStatus}{Details}", 
                repoName, testCaseName, result, details != null ? $" ({details})" : "");
        }

        protected void LogTestDetail(
            string repoName, 
            string functionName, 
            string scenario, 
            object? input, 
            object? output, 
            bool success, 
            string? errorMessage = null)
        {
            string result = success ? "SUCCESS" : "FAILED";
            
            var jsonOptions = new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = false,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };

            string inputJson = input != null ? System.Text.Json.JsonSerializer.Serialize(input, jsonOptions) : "null";
            string outputJson = output != null ? System.Text.Json.JsonSerializer.Serialize(output, jsonOptions) : "null";

            Log.Information(
                "\n========================================\n" +
                "REPO: {RepoName}\n" +
                "FUNCTION: {FunctionName}\n" +
                "CASE: {Scenario}\n" +
                "INPUT: {InputData}\n" +
                "OUTPUT: {OutputData}\n" +
                "RESULT: {ResultStatus}\n" +
                (success ? "" : "ERROR: " + errorMessage + "\n") +
                "========================================\n",
                repoName, functionName, scenario, inputJson, outputJson, result);
        }

        public void Dispose()
        {
            try
            {
                _transaction.Rollback();
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to rollback test transaction: {Message}", ex.Message);
            }
            finally
            {
                _transaction.Dispose();
                Context.Dispose();
            }
            Log.Information("----------------------------------------------------------------------\n");
        }
    }
}
