﻿using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EfLocalDb
{
    public class SqlDatabase<TDbContext>
        where TDbContext : DbContext
    {
        Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance;

        public SqlDatabase(string connection, Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance)
        {
            this.constructInstance = constructInstance;
            Connection = connection;
        }

        public string Connection { get; }

        public async Task AddSeed(params object[] entities)
        {
            Guard.AgainstNull(nameof(entities), entities);
            using (var sqlConnection = new SqlConnection(Connection))
            {
                var openAsync = sqlConnection.OpenAsync();
                var builder = DefaultOptionsBuilder.Build<TDbContext>();
                builder.UseSqlServer(sqlConnection);
                using (var dbContext = constructInstance(builder))
                {
                    dbContext.AddRange(entities);
                    await openAsync;
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        public TDbContext NewDbContext()
        {
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseSqlServer(Connection);
            return constructInstance(builder);
        }
    }
}