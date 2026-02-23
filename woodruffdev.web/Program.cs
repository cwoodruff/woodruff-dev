var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<WoodruffDev.Services.IBlogService, WoodruffDev.Services.BlogService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Serve blog post images from the BlogPosts content directory
var blogPostsPath = Path.Combine(builder.Environment.ContentRootPath, "BlogPosts");
if (Directory.Exists(blogPostsPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(blogPostsPath),
        RequestPath = "/blog-content"
    });
}

app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
