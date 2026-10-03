using DotNetFrameworkToolkit.Core;
using DotNetFrameworkToolkit.Modules.ComponentModel;
using DotNetFrameworkToolkit.Modules.DataAccess;
using DotNetFrameworkToolkit.Modules.DependencyInjection;
using DotNetFrameworkToolkit.Modules.FileSystem;
using DotNetFrameworkToolkit.Modules.Logging;
using DotNetFrameworkToolkit.Modules.UserAccess;
using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Diagnostics;
using System.IO;

namespace ConsumerSmoke;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            Require(Environment.Version.Major == 2, "The consumer must execute on CLR 2.0.");
            Require(typeof(object).Assembly.ImageRuntimeVersion == "v2.0.50727", "Unexpected core runtime metadata.");
            Require(IntPtr.Size == int.Parse(args[0]), "Unexpected process architecture.");
            Console.WriteLine("Runtime: " + Environment.Version + "; pointer size: " + IntPtr.Size);
            Console.WriteLine("Toolkit: " + typeof(ProcessResult<int>).Assembly.Location);
            DependencyInjection();
            ResultsAndValidation();
            Logging();
            Credentials();
            FileSystem();
            Database();
            Console.WriteLine("PASS: DI, results, validation, logging scopes, credential persistence, filesystem writes and SQL CE.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void DependencyInjection()
    {
        ServiceCollectionPNP services = new ServiceCollectionPNP();
        services.AddScoped<Session>();
        IServiceProvider provider = services.BuildServiceProvider();
        using (IDisposable owner = (IDisposable)provider)
        {
            IServiceScopeFactory factory = (IServiceScopeFactory)provider.GetService(typeof(IServiceScopeFactory));
            Session first;
            using (IServiceScope scope = factory.CreateScope())
            {
                first = (Session)scope.ServiceProvider.GetService(typeof(Session));
                Require(ReferenceEquals(first, scope.ServiceProvider.GetService(typeof(Session))), "Scoped identity was lost.");
            }
            Require(first.IsDisposed, "The scope did not dispose its service.");
            using (IServiceScope scope = factory.CreateScope())
            {
                Require(!ReferenceEquals(first, scope.ServiceProvider.GetService(typeof(Session))), "Scopes shared a service.");
            }
        }
    }

    private static void ResultsAndValidation()
    {
        ProcessResult<int, LookupError> success = ProcessResult<int, LookupError>.Success(42);
        int value;
        Require(success.TryGet(out value) && value == 42, "The successful value was lost.");
        ProcessResult<int, LookupError> missing = ProcessResult<int, LookupError>.Failure(LookupError.Missing);
        Require(!missing.TryGet(out value) && missing.Error == LookupError.Missing, "The modeled failure was lost.");
        Dictionary<string, string[]> messages = new Dictionary<string, string[]>();
        messages.Add("Name", new string[] { "A name is required." });
        ValidationResult<string> invalid = new ValidationResult<string>(string.Empty, messages, new List<string>());
        Require(!invalid.IsValid, "Invalid input was accepted.");
        NameModel model = new NameModel();
        model.ValidateName(string.Empty);
        Require(model.HasErrors, "Observable validation did not report an error.");
        model.ValidateName("Alice");
        Require(!model.HasErrors, "Observable validation did not clear the error.");
    }

    private static void Logging()
    {
        using (InMemorySinkPNP sink = new InMemorySinkPNP())
        using (ScopeSink capture = new ScopeSink())
        using (LoggerPNP logger = new LoggerPNP(LogLevel.Information, sink, capture))
        {
            using (IDisposable scope = logger.BeginScope("Import"))
            {
                logger.LogInformation("Imported {Count} records", 3);
                string[] scopes = (string[])capture.Last.ExtendedProperties["Scopes"];
                Require(scopes.Length == 1 && scopes[0] == "Import", "Scope context was lost.");
            }
            Require(sink.Logs.Count == 1 && sink.Logs[0].Contains("3"), "Logging did not reach the sink.");
            logger.LogInformation("Finished");
            Require(!capture.Last.ExtendedProperties.ContainsKey("Scopes"), "Disposed scope context remained active.");
        }
    }

    private static void Credentials()
    {
        UserAuthenticator authenticator = new UserAuthenticator(new CryptographyConfig
        {
            SaltLength = 16,
            NewUserWorkFactor = 1000,
            MaxVerificationWorkFactor = 2000
        });
        CryptographyCredential created = authenticator.CreateUserCredentials("pässword");
        // Persist all three values; Base64 is a storage encoding, not encryption.
        string salt = Convert.ToBase64String(created.LoginSalt);
        string hash = Convert.ToBase64String(created.LoginHash);
        int workFactor = created.LoginWorkFactor;
        CryptographyCredential restored = new CryptographyCredential
        {
            LoginSalt = Convert.FromBase64String(salt),
            LoginHash = Convert.FromBase64String(hash),
            LoginWorkFactor = workFactor
        };
        Require(authenticator.VerifyCredentials(restored, "pässword"), "Stored credentials did not round trip.");
        Require(!authenticator.VerifyCredentials(restored, "wrong"), "An incorrect password was accepted.");
    }

    private static void FileSystem()
    {
        string directory = Path.Combine(Path.GetTempPath(), "toolkit-files-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (LoggerPNP logger = new LoggerPNP(LogLevel.None))
            {
                FileSystemAccess files = new FileSystemAccess(logger);
                Require(files.WriteFile(new string[] { "first" }, "content.txt", directory).IsSuccessful, "Initial file write failed.");
                Require(files.WriteFile(new string[] { "replacement" }, "content.txt", directory).IsSuccessful, "File overwrite failed.");
                string path = Path.Combine(directory, "content.txt");
                string[] lines = File.ReadAllLines(path);
                Require(lines.Length == 1 && lines[0] == "replacement", "File overwrite lost content.");
                Require(Directory.GetFiles(directory).Length == 1, "File write left staging files.");
            }
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private static void Database()
    {
        string path = Path.Combine(Path.GetTempPath(), "toolkit-consumer-" + Guid.NewGuid().ToString("N") + ".sdf");
        try
        {
            string connectionString = SqlServerCeDatabase.BuildConnectionString(path);
            SqlServerCeDatabase.CreateDatabase(connectionString);
            using (SqlCeConnection connection = SqlServerCeDatabase.OpenConnection(connectionString))
            using (SqlCeCommand command = connection.CreateCommand())
            {
                // Schema and primary keys belong to the consuming application.
                command.CommandText = "CREATE TABLE Notes (NoteId int NOT NULL PRIMARY KEY, Text nvarchar(100) NOT NULL)";
                command.ExecuteNonQuery();
                command.CommandText = "INSERT INTO Notes (NoteId, Text) VALUES (@id, @text)";
                command.Parameters.AddWithValue("@id", 1);
                command.Parameters.AddWithValue("@text", "Hello");
                command.ExecuteNonQuery();
                command.Parameters.Clear();
                command.CommandText = "SELECT Text FROM Notes WHERE NoteId = 1";
                Require((string)command.ExecuteScalar() == "Hello", "SQL CE did not return the stored value.");
                bool privateEngineLoaded = false;
                using (Process process = Process.GetCurrentProcess())
                {
                    foreach (ProcessModule module in process.Modules)
                    {
                        if (string.Equals(module.ModuleName, "sqlceqp40.dll", StringComparison.OrdinalIgnoreCase))
                        {
                            string architecture = IntPtr.Size == 4 ? "x86" : "amd64";
                            string expected = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, architecture), "sqlceqp40.dll");
                            Require(string.Equals(module.FileName, expected, StringComparison.OrdinalIgnoreCase), "SQL CE loaded a global native engine.");
                            Console.WriteLine("Native engine: " + module.FileName);
                            privateEngineLoaded = true;
                        }
                    }
                }
                Require(privateEngineLoaded, "The private SQL CE engine was not loaded.");
            }
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

public sealed class Session : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

internal enum LookupError
{
    None,
    Missing
}

internal sealed class NameModel : ObservableValidator
{
    public void ValidateName(string name)
    {
        List<string> errors = new List<string>();
        if (name.Length == 0)
        {
            errors.Add("A name is required.");
        }
        SetErrorsForProperty("Name", errors);
    }
}

internal sealed class ScopeSink : Microsoft.Practices.EnterpriseLibrary.Logging.TraceListeners.CustomTraceListener
{
    public Microsoft.Practices.EnterpriseLibrary.Logging.LogEntry Last { get; private set; }

    public override void Write(string message)
    {
    }

    public override void WriteLine(string message)
    {
    }

    public override void TraceData(TraceEventCache cache, string source, TraceEventType type, int id, object data)
    {
        Last = (Microsoft.Practices.EnterpriseLibrary.Logging.LogEntry)data;
    }
}
