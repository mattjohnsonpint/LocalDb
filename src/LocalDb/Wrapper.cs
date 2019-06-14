﻿using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

class Wrapper
{
    string directory;
    string masterConnection;
    string instance;

    public Wrapper(string instance, string directory)
    {
        this.instance = instance;
        masterConnection = $"Data Source=(LocalDb)\\{instance};Database=master";
        this.directory = directory;
        Directory.CreateDirectory(directory);
        ServerName = $@"(LocalDb)\{instance}";
        Trace.WriteLine($@"Creating LocalDb instance. Server Name: {ServerName}");
    }

    public readonly string ServerName;

    public void DetachTemplate()
    {
        var commandText = "EXEC sp_detach_db 'template', 'true';";
        try
        {
            using (var connection = new SqlConnection(masterConnection))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = commandText;
                command.ExecuteNonQuery();
            }
        }
        catch (Exception exception)
        {
            throw new Exception(
                innerException: exception,
                message: $@"Failed to {nameof(DetachTemplate)}
{nameof(directory)}: {directory}
{nameof(masterConnection)}: {masterConnection}
{nameof(instance)}: {instance}
{nameof(commandText)}: {commandText}
");
        }
    }

    public void Purge()
    {
        var commandText = @"
declare @command nvarchar(max)
set @command = ''

select @command = @command
+ '

begin try
  alter database [' + [name] + '] set single_user with rollback immediate;
end try
begin catch
end catch;

drop database [' + [name] + '];

'
from [master].[sys].[databases]
where [name] not in ('master', 'model', 'msdb', 'tempdb');
execute sp_executesql @command";
        try
        {
            using (var connection = new SqlConnection(masterConnection))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = commandText;
                command.ExecuteNonQuery();
            }
        }
        catch (Exception exception)
        {
            throw new Exception(
                innerException: exception,
                message: $@"Failed to {nameof(Purge)}
{nameof(directory)}: {directory}
{nameof(masterConnection)}: {masterConnection}
{nameof(instance)}: {instance}
{nameof(commandText)}: {commandText}
");
        }
    }

    public Task<string> CreateDatabaseFromTemplate(string name)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return InnerCreateDatabaseFromTemplate(name);
        }
        finally
        {
            Trace.WriteLine($"LocalDB CreateDatabaseFromTemplate: {stopwatch.ElapsedMilliseconds}ms");
        }
    }

    Task<string> InnerCreateDatabaseFromTemplate(string name)
    {
        if (string.Equals(name, "template", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("The database name 'template' is reserved.");
        }

        var dataFile = Path.Combine(directory, $"{name}.mdf");
        if (File.Exists(dataFile))
        {
            throw new Exception($"The database name '{name}' has already been used.");
        }

        var templateDataFile = Path.Combine(directory, "template.mdf");

        var copyTask = FileExtensions.Copy(templateDataFile, dataFile);

        return CreateDatabaseFromFile(name, copyTask);
    }

    public bool TemplateFileExists()
    {
        var dataFile = Path.Combine(directory, "template.mdf");
        return File.Exists(dataFile);
    }

    public string RestoreTemplate()
    {
        var dataFile = Path.Combine(directory, "template.mdf");
        var commandText = $@"
create database [template] on
(
    name = [template],
    filename = '{dataFile}',
    size = 10MB,
    fileGrowth = 5MB
)
for attach;
";
        try
        {
            using (var connection = new SqlConnection(masterConnection))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
        catch (Exception exception)
        {
            throw BuildException("template", exception, nameof(RestoreTemplate), dataFile, commandText);
        }

        // needs to be pooling=false so that we can immediately detach and use the files
        return $"Data Source=(LocalDb)\\{instance};Database=template;MultipleActiveResultSets=True;Pooling=false";
    }

    public async Task<string> CreateDatabaseFromFile(string name, Task fileCopyTask)
    {
        var dataFile = Path.Combine(directory, $"{name}.mdf");
        var commandText = $@"
create database [{name}] on
(
    name = [{name}],
    filename = '{dataFile}',
    size = 10MB,
    fileGrowth = 5MB
)
for attach;

alter database [{name}]
    modify file (name=template, newname='{name}')
alter database [{name}]
    modify file (name=template_log, newname='{name}_log')
";
        try
        {
            using (var connection = new SqlConnection(masterConnection))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                await connection.OpenAsync();
                await fileCopyTask;
                await command.ExecuteNonQueryAsync();
            }
        }
        catch (Exception exception)
        {
            throw BuildException(name, exception, nameof(CreateDatabaseFromFile), dataFile, commandText);
        }

        return $"Data Source=(LocalDb)\\{instance};Database={name};MultipleActiveResultSets=True";
    }

    public string CreateDatabase()
    {
        var dataFile = Path.Combine(directory, "template.mdf");
        var commandText = $@"
create database [template] on
(
    name = [template],
    filename = '{dataFile}',
    size = 10MB,
    fileGrowth = 5MB
);
";
        try
        {
            using (var connection = new SqlConnection(masterConnection))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
        catch (Exception exception)
        {
            throw new Exception(
                innerException: exception,
                message: $@"Failed to {nameof(CreateDatabase)}
{nameof(directory)}: {directory}
{nameof(masterConnection)}: {masterConnection}
{nameof(instance)}: {instance}
{nameof(dataFile)}: {dataFile}
{nameof(commandText)}: {commandText}
");
        }

        return $"Data Source=(LocalDb)\\{instance};Database=template;MultipleActiveResultSets=True;Pooling=false";
    }

    public void Start()
    {
        RunLocalDbCommand($"create \"{instance}\" -s");
        //RunLocalDbCommand($"start \"{instance}\"");
    }

    public void DeleteInstance()
    {
        RunLocalDbCommand($"stop \"{instance}\"");
        RunLocalDbCommand($"delete \"{instance}\"");
        DeleteFiles();
    }

    public void DeleteFiles(string exclude = null)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            if (exclude != null)
            {
                if (Path.GetFileNameWithoutExtension(file) == exclude)
                {
                    continue;
                }
            }

            File.Delete(file);
        }
    }

    void RunLocalDbCommand(string command)
    {
        var startInfo = new ProcessStartInfo("sqllocaldb", command)
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        try
        {
            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    var readToEnd = process.StandardError.ReadToEnd();
                    throw new Exception($"ExitCode: {process.ExitCode}. Output: {readToEnd}");
                }
            }
        }
        catch (Exception exception)
        {
            throw new Exception(
                innerException: exception,
                message: $@"Failed to {nameof(RunLocalDbCommand)}
{nameof(directory)}: {directory}
{nameof(masterConnection)}: {masterConnection}
{nameof(instance)}: {instance}
{nameof(command)}: sqllocaldb {command}
");
        }
    }

    Exception BuildException(string name, Exception exception, string methodName, string dataFile, string commandText)
    {
        return new Exception(
            innerException: exception,
            message: $@"Failed to {methodName}
{nameof(directory)}: {directory}
{nameof(masterConnection)}: {masterConnection}
{nameof(instance)}: {instance}
{nameof(name)}: {name}
{nameof(dataFile)}: {dataFile}
{nameof(commandText)}: {commandText}
");
    }
}