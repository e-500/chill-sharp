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
using ChillSharp.Client;
using ChillSharp.Client.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using ChillDtoEntity = ChillSharp.Client.Dto.ChillDtoEntity;

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

        private static ChillDtoEntity CreateBlog(ChillSharpClient Client)
        {
            // Create a new Blog
            var blog = new ChillDtoEntity();
            // Use partial namespace: depends to BloggingContext.GetChillTypePrefix() implementation 
            blog.ChillType = "Model.Blog";
            // Client side id generation
            blog.Guid = Guid.NewGuid();
            blog.Properties.Add("Name", "My new exciting blog");
            blog.Properties.Add("Url", "https//wy-exciting-blog.com");
            // All the blog creation operations are ecapsulated in an internal transaction
            // So, if it fails at some points, there's nothing to clean up.
            return Client.Create(blog);
        }

        private static ChillDtoEntity CreateFirstBlogPost(ChillSharpClient Client, ChillDtoEntity Blog)
        {
            // Create first posts
            var post1 = new ChillDtoEntity();
            // Use partial namespace: depends to BloggingContext.GetChillTypePrefix() implementation 
            post1.ChillType = "Model.Post";
            // Client side id generation
            post1.Guid = Guid.NewGuid();
            post1.Properties.Add("Title", "My first post");
            post1.Properties.Add("Content", "Lorem, ipsum dolor sit amet consectetur adipisicing elit. " +
                "Pariatur nesciunt in neque! Eos esse illum necessitatibus voluptate quae quisquam? " +
                "Officiis ullam doloribus ut repellat sed fugit enim ad impedit quisquam.");
            // Link post to the blog
            // Set reference (FK-less) using object mock for efficiency
            post1.Properties.Add("Blog", Blog.Mock());
            // Create post
            return Client.Create(post1);
        }

        private static List<ChillOperation> CreateTwentyPosts(ChillSharpClient Client, ChillDtoEntity Blog)
        {
            // Create 20 posts
            List<ChillOperation> chunk = new List<ChillOperation>();
            // Explicitly open a TRANSACTION
            chunk.Add(new ChillOperation() { Verb = ChillOperationVerb.TRANSACTION });
            for (int i = 0; i < 20; i++)
            {
                var postN = new ChillDtoEntity();
                // Use partial namespace: depends to BloggingContext.GetChillTypePrefix() implementation 
                postN.ChillType = "Model.Post";
                // Client side id generation
                postN.Guid = Guid.NewGuid();
                postN.Properties.Add("Title", $"Post #{i}");
                postN.Properties.Add("Content", "Lorem, ipsum dolor sit amet consectetur adipisicing elit. " +
                    "Pariatur nesciunt in neque! Eos esse illum necessitatibus voluptate quae quisquam? " +
                    "Officiis ullam doloribus ut repellat sed fugit enim ad impedit quisquam.");
                // Link post to the blog
                // Set reference (FK-less) using object mock for efficiency
                postN.Properties.Add("Blog", Blog.Mock());
                // Add to chunk
                chunk.Add(new ChillOperation() { Entity = postN, Verb = ChillOperationVerb.CREATE });
            }
            // Explicitly COMMIT the transaction
            chunk.Add(new ChillOperation() { Verb = ChillOperationVerb.COMMIT });
            // Save posts in a single transaction
            return Client.Chunk(chunk);
        }

        private static void PrintBlogByGuid(ChillSharpClient Client, Guid BlogGuid)
        {
            var qry = new ChillDtoQuery();
            qry.ChillType = "Query.BlogQuery";
            // Require title and other properties
            qry.ResultProperties = ChillDtoProperty.FromStrings(new string[] { "Name", "PostTitles" });
            // Perform a subquery to get Post properties
            var subP = ChillDtoProperty.FromStrings(new string[] { "Title", "Content" });
            subP.Add(new ChillDtoProperty()
            {
                PropertyName = "Blog",
                SubProperties = ChillDtoProperty.FromStrings(new string[] { "Name" })
            });
            qry.ResultProperties.Add(new ChillDtoProperty() { 
                PropertyName = "Posts", 
                SubProperties = subP
            });
            qry.Properties.Add("Guid", BlogGuid);
            qry = Client.Query(qry);
            var blog = qry.Results.FirstOrDefault();
            if (blog == null)
            {
                Console.WriteLine("Blog not found");
                return;
            }
            Console.WriteLine($"Blog: {blog.Properties.GetValueOrDefault("Name")}");
            var posts = blog.Properties.GetValueOrDefault("Posts");
            //if (posts == null)
            //{
            //    Console.WriteLine("No posts found");
            //    return;
            //}
            var postList = ((JsonElement)posts).Deserialize<List<ChillDtoEntity>>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            foreach (var post in postList)
            {
                Console.WriteLine($"*** {post.Properties.GetValueOrDefault("Title")} ***");
                Console.WriteLine($"  {post.Properties.GetValueOrDefault("Content")}");
            }
        }

        static void Main(string[] args)
		{
            try
            {
                StartApiService();
                ChillSharpClient cli = new ChillSharpClient("http://localhost:5000/api/chill");
                var blog = CreateBlog(cli);
                var post1 = CreateFirstBlogPost(cli, blog);
                CreateTwentyPosts(cli, blog);

                PrintBlogByGuid(cli, blog.Guid);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();
        }
    }
}
