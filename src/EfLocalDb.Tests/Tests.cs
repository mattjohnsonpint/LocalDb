﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ApprovalTests;
using EfLocalDb;
using Xunit;
using Xunit.Abstractions;

public class Tests :
    XunitLoggingBase
{
    SqlInstance<TestDbContext> instance;

    [Fact]
    public async Task SeedData()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        using (var database = await instance.Build(new List<object> {entity}))
        {
            Assert.NotNull(database.Context.TestEntities.FindAsync(entity.Id));
        }
    }

    [Fact]
    public async Task AddData()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        using (var database = await instance.Build())
        {
            await database.AddData(entity);
            Assert.NotNull(database.Context.TestEntities.FindAsync(entity.Id));
        }
    }

    [Fact]
    public async Task WithFileAndNoDb()
    {
        new SqlInstance<TestDbContext>(
            constructInstance: builder => new TestDbContext(builder.Options),
            instanceSuffix: "EfWithFileAndNoDb");
        LocalDbApi.StopAndDelete("TestDbContext_EfWithFileAndNoDb");

        var instance = new SqlInstance<TestDbContext>(
            constructInstance: builder => new TestDbContext(builder.Options),
            instanceSuffix: "EfWithFileAndNoDb");

        var entity = new TestEntity
        {
            Property = "prop"
        };
        using (var database = await instance.Build(new List<object> {entity}))
        {
            Assert.NotNull(database.Context.TestEntities.FindAsync(entity.Id));
        }
    }

    [Fact]
    public async Task NoFileAndWithDb()
    {
        LocalDbApi.CreateInstance("TestDbContext_EfNoFileAndWithDb");
        var directory = DirectoryFinder.Find("EfNoFileAndWithDb");

        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }

        var instance = new SqlInstance<TestDbContext>(
            constructInstance: builder => new TestDbContext(builder.Options),
            instanceSuffix: "EfNoFileAndWithDb");

        var entity = new TestEntity
        {
            Property = "prop"
        };
        using (var database = await instance.Build(new List<object> {entity}))
        {
            Assert.NotNull(database.Context.TestEntities.FindAsync(entity.Id));
        }
    }


    [Fact]
    public async Task NoFileAndNoDb()
    {
        LocalDbApi.StopAndDelete("TestDbContext_EfNoFileAndNoDb");
        var directory = DirectoryFinder.Find("TestDbContext_EfNoFileAndNoDb");

        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }

        var instance = new SqlInstance<TestDbContext>(
            constructInstance: builder => new TestDbContext(builder.Options),
            instanceSuffix: "EfNoFileAndNoDb");

        var entity = new TestEntity
        {
            Property = "prop"
        };
        using (var database = await instance.Build(new List<object> {entity}))
        {
            Assert.NotNull(database.Context.TestEntities.FindAsync(entity.Id));
        }
    }

    [Fact]
    public async Task SuffixedContext()
    {
        var instance = new SqlInstance<TestDbContext>(
            constructInstance: builder => new TestDbContext(builder.Options),
            instanceSuffix: "theSuffix");

        var entity = new TestEntity
        {
            Property = "prop"
        };
        using (var database = await instance.Build(new List<object> {entity}))
        {
            Assert.NotNull(database.Context.TestEntities.FindAsync(entity.Id));
        }
    }

    [Fact]
    public async Task WithRebuildDbContext()
    {
        var instance1 = new SqlInstance<WithRebuildDbContext>(
            constructInstance: builder => new WithRebuildDbContext(builder.Options),
            requiresRebuild: dbContext => true);
        using (var database1 = await instance1.Build())
        {
            var entity = new TestEntity
            {
                Property = "prop"
            };
            await database1.AddData(entity);
        }

        var instance2 = new SqlInstance<WithRebuildDbContext>(
            constructInstance: builder => new WithRebuildDbContext(builder.Options),
            buildTemplate: x => throw new Exception(),
            requiresRebuild: dbContext => false);
        using (var database2 = await instance2.Build())
        {
            Assert.Empty(database2.Context.TestEntities);
        }
    }

    [Fact]
    public async Task Secondary()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        using (var database = await instance.Build())
        {
            using (var dbContext = database.NewDbContext())
            {
                dbContext.Add(entity);
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = database.NewDbContext())
            {
                Assert.NotNull(dbContext.TestEntities.FindAsync(entity.Id));
            }
        }
    }

    [Fact]
    public void DuplicateDbContext()
    {
        Register();
        var exception = Assert.Throws<Exception>(Register);
        Approvals.Verify(exception.Message);
    }

    static void Register()
    {
        SqlInstanceService<DuplicateDbContext>.Register(
            constructInstance: builder => new DuplicateDbContext(builder.Options));
    }

    [Fact]
    public async Task NewDbContext()
    {
        using (var database = await instance.Build())
        using (var dbContext = database.NewDbContext())
        {
            Assert.NotSame(database.Context, dbContext);
        }
    }

    [Fact]
    public async Task Simple()
    {
        var entity = new TestEntity
        {
            Property = "Item1"
        };
        using (var database = await instance.Build(new List<object> {entity}))
        {
            Assert.NotNull(database.Context.TestEntities.FindAsync(entity.Id));
            var settings = DbPropertyReader.Read(database.Connection, "Tests_Simple");
            Assert.NotEmpty(settings.Files);
        }
    }

    public Tests(ITestOutputHelper output) :
        base(output)
    {
        instance = new SqlInstance<TestDbContext>(
            builder => new TestDbContext(builder.Options));
    }
}