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

builder.Services.AddSingleton<SnapshotSpikeService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");

var commandApi = app.MapGroup("/api/command");

commandApi.MapGet("/state", (
    ProjectState state,
    CommandHistory history) =>
{
    return Results.Ok(new
    {
        projectState = state,
        canUndo = history.CanUndo,
        canRedo = history.CanRedo
    });
});

commandApi.MapPost("/config-items/{id:guid}/toggle", (
    Guid id,
    ProjectState state,
    CommandHistory history) =>
{
    var item = state.ConfigItems.FirstOrDefault(
        item => item.Id == id);

    if (item is null)
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

commandApi.MapDelete("/config-items/{id:guid}", (
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

commandApi.MapPost("/history/undo", (
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

commandApi.MapPost("/history/redo", (
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


var snapshotApi = app.MapGroup("/api/snapshot");

snapshotApi.MapGet("/state", (
    SnapshotSpikeService service) =>
{
    return Results.Ok(new
    {
        projectState = service.Project,
        canUndo = service.History.CanUndo,
        canRedo = service.History.CanRedo
    });
});

snapshotApi.MapPost(
    "/config-items/{id:guid}/toggle",
    (
        Guid id,
        SnapshotSpikeService service) =>
    {
        var itemExists =
            service.Project.ConfigItems.Any(
                item => item.Id == id);

        if (!itemExists)
        {
            return Results.NotFound();
        }

        service.History.Execute(
            service.Project,
            project =>
            {
                var item =
                    project.ConfigItems.First(
                        item => item.Id == id);

                item.Active = !item.Active;
            });

        return Results.Ok(new
        {
            projectState = service.Project,
            canUndo = service.History.CanUndo,
            canRedo = service.History.CanRedo
        });
    });

snapshotApi.MapDelete(
    "/config-items/{id:guid}",
    (
        Guid id,
        SnapshotSpikeService service) =>
    {
        var itemExists =
            service.Project.ConfigItems.Any(
                item => item.Id == id);

        if (!itemExists)
        {
            return Results.NotFound();
        }

        service.History.Execute(
            service.Project,
            project =>
            {
                project.ConfigItems.RemoveAll(
                    item => item.Id == id);
            });

        return Results.Ok(new
        {
            projectState = service.Project,
            canUndo = service.History.CanUndo,
            canRedo = service.History.CanRedo
        });
    });

snapshotApi.MapPost(
    "/history/undo",
    (SnapshotSpikeService service) =>
    {
        if (!service.History.Undo(service.Project))
        {
            return Results.Conflict(new
            {
                message = "Nothing to undo."
            });
        }

        return Results.Ok(new
        {
            projectState = service.Project,
            canUndo = service.History.CanUndo,
            canRedo = service.History.CanRedo
        });
    });

snapshotApi.MapPost(
    "/history/redo",
    (SnapshotSpikeService service) =>
    {
        if (!service.History.Redo(service.Project))
        {
            return Results.Conflict(new
            {
                message = "Nothing to redo."
            });
        }

        return Results.Ok(new
        {
            projectState = service.Project,
            canUndo = service.History.CanUndo,
            canRedo = service.History.CanRedo
        });
    });

app.Run();