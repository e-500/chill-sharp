/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core 
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

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

            if (args.Length == 0)
                args = new string[] { "--urls=https://localhost:5000/" };

            var apiServer = Task.Run(() =>
            {
                var builder = WebApplication.CreateBuilder(args);
                builder.Services.AddChillApi<DummyContext>();
                var app = builder.Build();
                app.MapChillApi();
                app.Run();
            });
            apiServer.Wait();
        }
    }
}