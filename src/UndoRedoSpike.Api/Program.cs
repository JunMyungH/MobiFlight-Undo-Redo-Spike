using System.Diagnostics;

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

builder.Services.AddSingleton<CommandSpikeService>();

builder.Services.AddSingleton<SnapshotSpikeService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");

var commandApi = app.MapGroup("/api/command");

commandApi.MapGet("/state", (
    CommandSpikeService service) =>
{
    return Results.Ok(CreateCommandResponse(service));
});

commandApi.MapPost("/config-items/{id:guid}/toggle", (
    Guid id,
    CommandSpikeService service) =>
{
    var item = service.Project.ConfigItems
            .FirstOrDefault(item => item.Id == id);

    if (item is null)
    {
        return Results.NotFound();
    }

    service.History.Execute(
        new ToggleActiveCommand(item));

    return Results.Ok(
        CreateCommandResponse(service));
});

commandApi.MapDelete("/config-items/{id:guid}", (
    Guid id,
    CommandSpikeService service) =>
{
    var exists =
        service.Project.ConfigItems.Any(
            item => item.Id == id);

    if (!exists)
    {
        return Results.NotFound();
    }

    service.History.Execute(
        new DeleteConfigItemCommand(
            service.Project,
            id));

    return Results.Ok(
        CreateCommandResponse(service));
});

commandApi.MapPost("/history/undo", (
    CommandSpikeService service) =>
{
    if (!service.History.Undo())
    {
        return Results.Conflict(new
        {
            message = "Nothing to undo."
        });
    }

    return Results.Ok(
        CreateCommandResponse(service));
});

commandApi.MapPost("/history/redo", (
    CommandSpikeService service) =>
{
    if (!service.History.Redo())
    {
        return Results.Conflict(new
        {
            message = "Nothing to redo."
        });
    }

    return Results.Ok(
        CreateCommandResponse(service));
});

commandApi.MapPost(
    "/experiment/reset/{itemCount:int}",
    (
        int itemCount,
        CommandSpikeService service) =>
    {
        if (itemCount is < 1 or > 10000)
        {
            return Results.BadRequest();
        }

        service.Reset(itemCount);

        return Results.Ok(
            CreateCommandResponse(service));
    });

commandApi.MapPost(
    "/experiment/toggles/{count:int}",
    (
        int count,
        CommandSpikeService service) =>
    {
        if (count is < 1 or > 10000)
        {
            return Results.BadRequest();
        }

        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < count; i++)
        {
            var index =
                i % service.Project.ConfigItems.Count;

            var item =
                service.Project.ConfigItems[index];

            service.History.Execute(
                new ToggleActiveCommand(item));
        }

        stopwatch.Stop();

        return Results.Ok(new
        {
            state = CreateCommandResponse(service),
            benchmark = new
            {
                operations = count,
                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

commandApi.MapPost(
    "/experiment/compound-edit",
    (CommandSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var item =
            service.Project.ConfigItems[0];

        service.History.Execute(
            new CompoundEditCommand(
                service.Project,
                item.Id));

        return Results.Ok(
            CreateCommandResponse(service));
    });

var snapshotApi = app.MapGroup("/api/snapshot");

snapshotApi.MapGet("/state", (
    SnapshotSpikeService service) =>
{
    return Results.Ok(CreateSnapshotResponse(service));
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

        return Results.Ok(CreateSnapshotResponse(service));
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

        return Results.Ok(CreateSnapshotResponse(service));
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

        return Results.Ok(CreateSnapshotResponse(service));
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

        return Results.Ok(CreateSnapshotResponse(service));
    });

snapshotApi.MapPost(
    "/experiment/reset/{itemCount:int}",
    (
        int itemCount,
        SnapshotSpikeService service) =>
    {
        if (itemCount is < 1 or > 10000)
        {
            return Results.BadRequest();
        }

        service.Reset(itemCount);

        return Results.Ok(
            CreateSnapshotResponse(service));
    });

snapshotApi.MapPost(
    "/experiment/toggles/{count:int}",
    (
        int count,
        SnapshotSpikeService service) =>
    {
        if (count is < 1 or > 10000)
        {
            return Results.BadRequest();
        }

        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < count; i++)
        {
            var index =
                i % service.Project.ConfigItems.Count;

            service.History.Execute(
                service.Project,
                project =>
                {
                    var item =
                        project.ConfigItems[index];

                    item.Active = !item.Active;
                });
        }

        stopwatch.Stop();

        return Results.Ok(new
        {
            state = CreateSnapshotResponse(service),
            benchmark = new
            {
                operations = count,
                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

snapshotApi.MapPost(
    "/experiment/compound-edit",
    (SnapshotSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        service.History.Execute(
            service.Project,
            project =>
            {
                var item =
                    project.ConfigItems[0];

                item.Name =
                    $"{item.Name} (Edited)";

                item.Active =
                    !item.Active;

                project.ConfigItems.RemoveAt(0);
                project.ConfigItems.Add(item);
            });

        return Results.Ok(
            CreateSnapshotResponse(service));
    });

static object CreateCommandResponse(
    CommandSpikeService service)
{
    return new
    {
        projectState = service.Project,
        canUndo = service.History.CanUndo,
        canRedo = service.History.CanRedo,

        diagnostics = new
        {
            approach = "command",
            representation = "Semantic commands",

            undoEntries =
                service.History.UndoCount,

            redoEntries =
                service.History.RedoCount,

            undoEntryDetails =
                service.History.UndoEntryTypes,

            redoEntryDetails =
                service.History.RedoEntryTypes,

            storedConfigItemCopies = (int?)null
        }
    };
}

static object CreateSnapshotResponse(
    SnapshotSpikeService service)
{
    return new
    {
        projectState = service.Project,
        canUndo = service.History.CanUndo,
        canRedo = service.History.CanRedo,

        diagnostics = new
        {
            approach = "snapshot",
            representation = "Full ProjectState snapshots",

            undoEntries = service.History.UndoCount,
            redoEntries = service.History.RedoCount,

            undoEntryDetails =
                service.History.UndoSnapshotSizes
                    .Select(count =>
                        $"ProjectState snapshot ({count} ConfigItems)")
                    .ToArray(),

            redoEntryDetails =
                service.History.RedoSnapshotSizes
                    .Select(count =>
                        $"ProjectState snapshot ({count} ConfigItems)")
                    .ToArray(),

            storedConfigItemCopies =
                (int?)service.History.StoredConfigItemCopies
        }
    };
}

app.Run();