using DotNetFrameworkToolkit.Modules.DataAccess.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Data.SqlServerCe;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace DotNetFrameworkToolkit.Tests;

[TestClass]
public class SqlServerCeDatabaseTests
{
    private string _directory;
    private string _path;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "ToolkitSqlCeTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "database.sdf");
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Deleting the directory also verifies that disposed connections release their file handles.
        Directory.Delete(_directory, true);
    }

    [TestMethod]
    public void ConnectionStringNormalizesAndEncodesTheEntirePathWithoutCreatingFiles()
    {
        string path = Path.Combine(_directory, "sub", "..", "database;Password=injected.sdf");
        string connectionString = SqlServerCeDatabase.BuildConnectionString(path);
        SqlCeConnectionStringBuilder builder = new(connectionString);
        Assert.AreEqual(Path.GetFullPath(path), builder.DataSource);
        Assert.AreEqual(string.Empty, builder.Password);
        Assert.AreEqual(0, Directory.GetFileSystemEntries(_directory).Length);
    }

    [TestMethod]
    public void UncPathCanBeEncodedWithoutAccessingTheShare()
    {
        string path = @"\\server\share\folder\database.sdf";
        SqlCeConnectionStringBuilder builder = new(SqlServerCeDatabase.BuildConnectionString(path));
        Assert.AreEqual(path, builder.DataSource);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("relative.sdf")]
    [DataRow(@"C:relative.sdf")]
    [DataRow(@"\current-drive.sdf")]
    [DataRow(@"|DataDirectory|\database.sdf")]
    public void AmbiguousPathsAreRejected(string path)
    {
        Assert.ThrowsException<ArgumentException>(() => SqlServerCeDatabase.BuildConnectionString(path));
    }

    [TestMethod]
    public void NullArgumentsAreRejected()
    {
        Assert.ThrowsException<ArgumentNullException>(() => SqlServerCeDatabase.BuildConnectionString(null));
        Assert.ThrowsException<ArgumentNullException>(() => SqlServerCeDatabase.CreateDatabase(null));
        Assert.ThrowsException<ArgumentNullException>(() => SqlServerCeDatabase.OpenConnection(null));
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("Password=test-only")]
    [DataRow("Data Source=relative.sdf")]
    [DataRow(@"Data Source=|DataDirectory|\database.sdf")]
    public void ConnectionStringsRequireAnExplicitAbsoluteDataSource(string connectionString)
    {
        Assert.ThrowsException<ArgumentException>(() => SqlServerCeDatabase.CreateDatabase(connectionString));
        Assert.ThrowsException<ArgumentException>(() => SqlServerCeDatabase.OpenConnection(connectionString));
        Assert.AreEqual(0, Directory.GetFiles(_directory).Length);
    }

    [DataTestMethod]
    [DataRow("database.sdf")]
    [DataRow("database;semi.sdf")]
    [DataRow("資料庫.sdf")]
    [DataRow("database.data")]
    [TestCategory("SqlCeIntegration")]
    public void CreatedDatabaseHasNoApplicationTablesAndConnectionIsCallerOwned(string fileName)
    {
        _path = Path.Combine(_directory, fileName);
        string connectionString = SqlServerCeDatabase.BuildConnectionString(_path);
        SqlServerCeDatabase.CreateDatabase(connectionString);
        Assert.IsTrue(File.Exists(_path));
        using (SqlCeConnection connection = SqlServerCeDatabase.OpenConnection(connectionString))
        {
            Assert.AreEqual(ConnectionState.Open, connection.State);
            using SqlCeCommand command = new("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'TABLE'", connection);
            Assert.AreEqual(0, Convert.ToInt32(command.ExecuteScalar()));
        }
        File.Move(_path, _path + ".moved");
    }

    [TestMethod]
    [TestCategory("SqlCeIntegration")]
    public void ApplicationControlsKeysParameterizedCommandsAndTransactions()
    {
        string connectionString = SqlServerCeDatabase.BuildConnectionString(_path);
        SqlServerCeDatabase.CreateDatabase(connectionString);
        using SqlCeConnection connection = SqlServerCeDatabase.OpenConnection(connectionString);
        using (SqlCeCommand create = new("CREATE TABLE Sample (Code nvarchar(30) PRIMARY KEY, Value nvarchar(100) NOT NULL)", connection))
        {
            create.ExecuteNonQuery();
        }
        using (SqlCeTransaction transaction = connection.BeginTransaction())
        {
            Insert(connection, transaction, "consumer-key", "O'Brien; DROP TABLE Sample;");
            transaction.Commit();
        }
        using (SqlCeTransaction transaction = connection.BeginTransaction())
        {
            Insert(connection, transaction, "rolled-back", "discarded");
            transaction.Rollback();
        }
        using SqlCeCommand query = new("SELECT Value FROM Sample WHERE Code = @code", connection);
        query.Parameters.Add("@code", SqlDbType.NVarChar, 30).Value = "consumer-key";
        Assert.AreEqual("O'Brien; DROP TABLE Sample;", query.ExecuteScalar());
        using SqlCeCommand count = new("SELECT COUNT(*) FROM Sample", connection);
        Assert.AreEqual(1, Convert.ToInt32(count.ExecuteScalar()));
    }

    [TestMethod]
    [TestCategory("SqlCeIntegration")]
    public void ExistingDatabaseIsNotOverwritten()
    {
        string connectionString = SqlServerCeDatabase.BuildConnectionString(_path);
        SqlServerCeDatabase.CreateDatabase(connectionString);
        using (SqlCeConnection connection = SqlServerCeDatabase.OpenConnection(connectionString))
        {
            using SqlCeCommand create = new("CREATE TABLE KeepMe (Code int PRIMARY KEY)", connection);
            create.ExecuteNonQuery();
        }
        Assert.ThrowsException<SqlCeException>(() => SqlServerCeDatabase.CreateDatabase(connectionString));
        using SqlCeConnection reopened = SqlServerCeDatabase.OpenConnection(connectionString);
        using SqlCeCommand query = new("SELECT COUNT(*) FROM KeepMe", reopened);
        Assert.AreEqual(0, Convert.ToInt32(query.ExecuteScalar()));
    }

    [TestMethod]
    [TestCategory("SqlCeIntegration")]
    public void OpeningMissingDatabaseDoesNotCreateIt()
    {
        string connectionString = SqlServerCeDatabase.BuildConnectionString(_path);
        Assert.ThrowsException<SqlCeException>(() => SqlServerCeDatabase.OpenConnection(connectionString));
        Assert.IsFalse(File.Exists(_path));
        SqlServerCeDatabase.CreateDatabase(connectionString);
        using SqlCeConnection connection = SqlServerCeDatabase.OpenConnection(connectionString);
        Assert.AreEqual(ConnectionState.Open, connection.State);
    }

    [TestMethod]
    [TestCategory("SqlCeIntegration")]
    public void FailedOpenPreservesInvalidFileAndReleasesItsHandle()
    {
        File.WriteAllText(_path, "not a database");
        Assert.ThrowsException<SqlCeException>(() => SqlServerCeDatabase.OpenConnection(SqlServerCeDatabase.BuildConnectionString(_path)));
        Assert.AreEqual("not a database", File.ReadAllText(_path));
        File.Delete(_path);
        Assert.IsFalse(File.Exists(_path));
    }

    [TestMethod]
    [TestCategory("SqlCeIntegration")]
    public void CreationDoesNotSelectOrCreateParentDirectories()
    {
        string missingParent = Path.Combine(_directory, "consumer-directory");
        string path = Path.Combine(missingParent, "database.sdf");
        Assert.ThrowsException<SqlCeException>(() => SqlServerCeDatabase.CreateDatabase(SqlServerCeDatabase.BuildConnectionString(path)));
        Assert.IsFalse(Directory.Exists(missingParent));
    }

    [TestMethod]
    [TestCategory("SqlCeIntegration")]
    public void ProviderOptionsIncludingPasswordArePreserved()
    {
        SqlCeConnectionStringBuilder builder = new(SqlServerCeDatabase.BuildConnectionString(_path))
        {
            Password = "test-only-password"
        };
        SqlServerCeDatabase.CreateDatabase(builder.ConnectionString);
        using (SqlCeConnection connection = SqlServerCeDatabase.OpenConnection(builder.ConnectionString))
        {
            Assert.AreEqual(ConnectionState.Open, connection.State);
        }
        Assert.ThrowsException<SqlCeException>(() => SqlServerCeDatabase.OpenConnection(SqlServerCeDatabase.BuildConnectionString(_path)));
        builder.Password = "incorrect-test-password";
        Assert.ThrowsException<SqlCeException>(() => SqlServerCeDatabase.OpenConnection(builder.ConnectionString));
    }

    [TestMethod]
    [TestCategory("SqlCeIntegration")]
    public void TestsLoadThePinnedManagedProviderAndMatchingPrivateNativeEngine()
    {
        SqlServerCeDatabase.CreateDatabase(SqlServerCeDatabase.BuildConnectionString(_path));
        string output = Path.GetDirectoryName(typeof(SqlServerCeDatabaseTests).Assembly.Location);
        string provider = typeof(SqlCeConnection).Assembly.Location;
        Assert.AreEqual(output, Path.GetDirectoryName(provider), "A globally installed provider must not replace the pinned test provider.");
        string restoredProvider = Path.GetFullPath(Path.Combine(output, "..", "..", "..", "packages", "System.Data.SqlServerCe_unofficial.4.0.8482.1", "lib", "net20", "System.Data.SqlServerCe.dll"));
        using (SHA256 hash = SHA256.Create())
        {
            CollectionAssert.AreEqual(hash.ComputeHash(File.ReadAllBytes(restoredProvider)), hash.ComputeHash(File.ReadAllBytes(provider)));
        }
        Assert.AreEqual("v2.0.50727", typeof(SqlCeConnection).Assembly.ImageRuntimeVersion);
        using Process process = Process.GetCurrentProcess();
        ProcessModule native = process.Modules.Cast<ProcessModule>().Single(module => string.Equals(module.ModuleName, "sqlceme40.dll", StringComparison.OrdinalIgnoreCase));
        string architecture = Environment.Is64BitProcess ? "amd64" : "x86";
        Assert.AreEqual(Path.Combine(output, architecture, "sqlceme40.dll"), native.FileName, true);
        Assert.AreEqual("4.0.8482.1", native.FileVersionInfo.FileVersion);
    }

    private static void Insert(SqlCeConnection connection, SqlCeTransaction transaction, string code, string value)
    {
        using SqlCeCommand command = new("INSERT INTO Sample (Code, Value) VALUES (@code, @value)", connection, transaction);
        command.Parameters.Add("@code", SqlDbType.NVarChar, 30).Value = code;
        command.Parameters.Add("@value", SqlDbType.NVarChar, 100).Value = value;
        command.ExecuteNonQuery();
    }
}
