using ExamPlatform.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5001";

// HttpClient for SSR pages
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Named HttpClient for the proxy
builder.Services.AddHttpClient("api", c => c.BaseAddress = new Uri(apiBaseUrl));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

// Simple proxy: forward all /api/* requests from the browser to the API server
app.Map("/api/{**path}", async (HttpContext context, IHttpClientFactory factory, string path) =>
{
    var client = factory.CreateClient("api");
    var method = new HttpMethod(context.Request.Method);
    var targetUri = new Uri(client.BaseAddress!, $"/api/{path}{context.Request.QueryString}");
    var req = new HttpRequestMessage(method, targetUri);

    // Copy request body for POST/PUT
    if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
    {
        req.Content = new StreamContent(context.Request.Body);
        if (context.Request.ContentType is not null)
            req.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
    }

    var response = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
    context.Response.StatusCode = (int)response.StatusCode;

    foreach (var header in response.Headers)
        context.Response.Headers[header.Key] = header.Value.ToArray();
    foreach (var header in response.Content.Headers)
        context.Response.Headers[header.Key] = header.Value.ToArray();

    context.Response.Headers.Remove("transfer-encoding");

    await response.Content.CopyToAsync(context.Response.Body);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ExamPlatform.Web.Client._Imports).Assembly);

app.Run();
