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