using ServiceMonitor.AWS;
using ServiceMonitor.Cloud;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAppRunner, AppRunner>();
builder.Services.AddScoped<IInstance, AWSInstance>();
builder.Services.AddRazorPages();

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();
});

app.Run();