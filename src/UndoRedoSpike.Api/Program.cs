var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var projectState = new ProjectState
{
    ConfigItems =
    [
        new ConfigItem
        {
            Name = "Landing Light",
            Active = true
        },
        new ConfigItem
        {
            Name = "Gear Indicator",
            Active = false
        },
        new ConfigItem
        {
            Name = "Flaps",
            Active = true
        }
    ]
};

builder.Services.AddSingleton(projectState);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");

app.MapGet("/api/state", (ProjectState state) =>
{
    return Results.Ok(state);
});

app.Run();