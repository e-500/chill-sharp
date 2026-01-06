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