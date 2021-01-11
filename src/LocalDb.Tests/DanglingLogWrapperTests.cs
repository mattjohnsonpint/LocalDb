using System;
using Microsoft.Data.SqlClient;
using Xunit;

public class DanglingLogWrapperTests
{
    [Fact]
    public void Run()
    {
        var name = "DanglingLogWrapperTests";
        LocalDbApi.StopAndDelete(name);
        Wrapper instance = new(s => new SqlConnection(s), name, DirectoryFinder.Find(name));
        instance.Start(Guid.NewGuid().ToString(), TestDbBuilder.CreateTable);
    }
}