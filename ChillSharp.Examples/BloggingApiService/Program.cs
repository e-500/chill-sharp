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
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChillSharp.Examples.BloggingApiService
{
    internal class Program
	{
        private static void StartApiService()
        {
            var apiServer = Task.Run(() =>
            {
                // Activate BloggingContext (implements IChillContext)
                var ctx = new BloggingContext();
                ctx.Database.Migrate();
                
                // CREATE
                var builder = WebApplication.CreateBuilder(new string[0]);
                
                // ADD
                builder.Services.AddDbContext<BloggingContext>(options =>
                    options.UseSqlite($"Data Source={ctx.DbPath}"));
                builder.Services.AddChillApi<BloggingContext>();

                // BUILD
                var app = builder.Build();
                
                // MAP
                app.MapChillApi();
                app.MapGet("/", () => "BloggingApiService is running!");
                
                // RUN
                app.Run();
            });
            apiServer.Wait(5000);
        }

        static void Main(string[] args)
		{
            try
            {
                StartApiService();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();
        }
    }
}
