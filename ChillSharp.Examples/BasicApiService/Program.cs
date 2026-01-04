using ChillSharp.Api;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Examples.BasicApiService
{
    public class Program
    {
        /// <summary>
        /// This is only a dummy EF Core database context used for demo purposes.
        /// </summary>
        private class DummyContext : DbContext, IChillContext
        {
            public string GetChillTypePrefix()
            {
                return "ChillSharp.Examples.BasicApiService";
            }
        }

        /// <summary>
        /// Basic API Service demo
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("Starting ChillSharp Basic API Service example... ");

            var apiServer = Task.Run(() =>
            {
                var builder = WebApplication.CreateBuilder(args);
                builder.Services.AddChillApi<DummyContext>();
                var app = builder.Build();
                app.MapChillApi();
                app.Run();
            });
            apiServer.Wait(5000);

            Console.WriteLine("ChillSharp Basic API Service example is running.");
        }
    }
}