using SquarePayments.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
// NOTE - I am using environment variables for convenience here. In a real app you would use appSettings.json, Azure secrets or the like
SquareData data = new() {
  Environment = "sandbox",
  AccessToken = Environment.GetEnvironmentVariable("SquareAccessToken") ?? "",
  AppId = Environment.GetEnvironmentVariable("SquareAppId") ?? "",
  LocationId = Environment.GetEnvironmentVariable("SquareLocationId") ?? ""
};
builder.Services.AddSingleton(data);
builder.Services.AddTransient<SquareHelper>();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment()) {
  app.UseExceptionHandler("/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
