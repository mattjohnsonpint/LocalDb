﻿using LocalDb;

namespace TestBase;

#region TestBase

public abstract class TestBase
{
    static SqlInstance instance;

    static TestBase() =>
        instance = new(
            name: "TestBaseUsage",
            buildTemplate: TestDbBuilder.CreateTable);

    public Task<SqlDatabase> LocalDb(
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "") =>
        instance.Build(testFile, databaseSuffix, memberName);
}

public class Tests :
    TestBase
{
    [Fact]
    public async Task Test()
    {
        await using var database = await LocalDb();
        await TestDbBuilder.AddData(database);
        Assert.Single(await TestDbBuilder.GetData(database));
    }
}

#endregion