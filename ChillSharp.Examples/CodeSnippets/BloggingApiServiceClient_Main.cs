static void Main(string[] args)
{
    try
    {
        // Init client
        ChillSharpClient cli = new ChillSharpClient("http://localhost:5000/api/chill");

        // Creating a blog
        Console.WriteLine("Creating a new blog");
        var blog = CreateBlog(cli);

        // [...]
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }

    Console.ReadLine();
}