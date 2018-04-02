using System;
using System.IO;
using System.Threading.Tasks;
using Conduit.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Conduit.IntegrationTests
{
    public class SliceFixture : IDisposable
    {
        protected readonly IServiceScopeFactory scopeFactory;

        private readonly string DbName = Guid.NewGuid() + ".db";

        public SliceFixture()
        {
            AutoMapper.ServiceCollectionExtensions.UseStaticRegistration = false;
            var startup = new Startup();
            var services = new ServiceCollection();

            services.AddSingleton(new ConduitContext(DbName));

            startup.ConfigureServices(services);

            var provider = services.BuildServiceProvider();

            provider.GetRequiredService<ConduitContext>().Database.EnsureCreated();
            scopeFactory = provider.GetService<IServiceScopeFactory>();
        }

        public void Dispose()
        {
            File.Delete(DbName);
        }

        public async Task ExecuteScopeAsync(Func<IServiceProvider, Task> action)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                await action(scope.ServiceProvider);
            }
        }

        public async Task<T> ExecuteScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                return await action(scope.ServiceProvider);
            }
        }

        public Task ExecuteDbContextAsync(Func<ConduitContext, Task> action)
        {
            return ExecuteScopeAsync(sp => action(sp.GetService<ConduitContext>()));
        }

        public Task<T> ExecuteDbContextAsync<T>(Func<ConduitContext, Task<T>> action)
        {
            return ExecuteScopeAsync(sp => action(sp.GetService<ConduitContext>()));
        }

        public Task InsertAsync(params object[] entities)
        {
            return ExecuteDbContextAsync(db =>
            {
                foreach (var entity in entities)
                {
                    db.Add(entity);
                }
                return db.SaveChangesAsync();
            });
        }
    }
}