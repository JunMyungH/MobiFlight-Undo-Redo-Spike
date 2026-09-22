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

var commandHistory = new CommandHistory();

builder.Services.AddSingleton(projectState);
builder.Services.AddSingleton(commandHistory);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");

app.MapGet("/api/state", (ProjectState state, CommandHistory history) =>
{
    return Results.Ok(
        new
        {
            projectState = state,
            canUndo = history.CanUndo,
            canRedo = history.CanRedo
        }
    );
});

app.MapPost("/api/config-items/{id:guid}/toggle", (
    Guid id,
    ProjectState state,
    CommandHistory history) =>
{
    var item = state.ConfigItems.FirstOrDefault(
        item => item.Id == id);

    if (item == null)
    {
        return Results.NotFound();
    }

    history.Execute(
        new ToggleActiveCommand(item));

    return Results.Ok(new
    {
        projectState = state,
        canUndo = history.CanUndo,
        canRedo = history.CanRedo
    });
});

app.MapDelete("/api/config-items/{id:guid}", (
    Guid id,
    ProjectState state,
    CommandHistory history) =>
{
    var itemExists = state.ConfigItems.Any(
        item => item.Id == id);

    if (!itemExists)
    {
        return Results.NotFound();
    }

    history.Execute(
        new DeleteConfigItemCommand(state, id));

    return Results.Ok(new
    {
        projectState = state,
        canUndo = history.CanUndo,
        canRedo = history.CanRedo
    });
});

app.MapPost("/api/history/undo", (
    ProjectState state,
    CommandHistory history) =>
{
    if (!history.Undo())
    {
        return Results.Conflict(new
        {
            message = "Nothing to undo."
        });
    }

    return Results.Ok(new
    {
        projectState = state,
        canUndo = history.CanUndo,
        canRedo = history.CanRedo
    });
});

app.MapPost("/api/history/redo", (
    ProjectState state,
    CommandHistory history) =>
{
    if (!history.Redo())
    {
        return Results.Conflict(new
        {
            message = "Nothing to redo."
        });
    }

    return Results.Ok(new
    {
        projectState = state,
        canUndo = history.CanUndo,
        canRedo = history.CanRedo
    });
});

app.Run();