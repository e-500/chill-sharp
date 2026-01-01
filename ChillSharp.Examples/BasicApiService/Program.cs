using ChillSharp.Api;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Examples.BasicApiService
{
    public class Program
    {
        private class DummyContext : DbContext, IChillContext
        {
            public string GetChillTypePrefix()
            {
                return "ChillSharp.Examples.BasicApiService";
            }
        }

        static void Main(string[] args)
        {
            // This is a library that uses Microsoft.NET.Sdk.Web
            // ChillSharp are looking to optimize the imports without using the whole SDK
            // If you have any ideas, please give me a feedback.

            Console.WriteLine("Running a ChillSharp Asp.Net.Core library example...");

            var apiServer = Task.Run(() =>
            {
                var builder = WebApplication.CreateBuilder(args);
                builder.Services.AddChillApi<DummyContext>();
                var app = builder.Build();
                app.MapChillApi();
                app.Run();
            });
            apiServer.Wait(5000);

            // Ready
        }
    }
}