using System.Diagnostics;
using UndoRedoSpike.Api.Services;

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

builder.Services.AddSingleton<PatchSpikeService>();

builder.Services.AddSingleton<HybridSpikeService>();

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

var patchApi = app.MapGroup("/api/patch");

patchApi.MapGet(
    "/state",
    (PatchSpikeService service) =>
    {
        return Results.Ok(
            CreatePatchResponse(service));
    });

patchApi.MapPost(
    "/config-items/{id:guid}/toggle",
    (
        Guid id,
        PatchSpikeService service) =>
    {
        var item =
            service.Project.ConfigItems
                .FirstOrDefault(
                    item => item.Id == id);

        if (item is null)
        {
            return Results.NotFound();
        }

        var transaction =
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                    item.Id,
                    item.Active,
                    !item.Active)
            ]);

        service.History.Execute(
            service.Project,
            transaction);

        return Results.Ok(
            CreatePatchResponse(service));
    });

patchApi.MapDelete(
    "/config-items/{id:guid}",
    (
        Guid id,
        PatchSpikeService service) =>
    {
        var index =
            service.Project.ConfigItems.FindIndex(
                item => item.Id == id);

        if (index < 0)
        {
            return Results.NotFound();
        }

        var item =
            service.Project.ConfigItems[index];

        var transaction =
            new PatchTransaction(
            [
                new RemoveConfigItemPatch(
                    item,
                    index)
            ]);

        service.History.Execute(
            service.Project,
            transaction);

        return Results.Ok(
            CreatePatchResponse(service));
    });

patchApi.MapPost(
    "/history/undo",
    (PatchSpikeService service) =>
    {
        if (!service.History.Undo(
            service.Project))
        {
            return Results.Conflict(new
            {
                message = "Nothing to undo."
            });
        }

        return Results.Ok(
            CreatePatchResponse(service));
    });

patchApi.MapPost(
    "/history/redo",
    (PatchSpikeService service) =>
    {
        if (!service.History.Redo(
            service.Project))
        {
            return Results.Conflict(new
            {
                message = "Nothing to redo."
            });
        }

        return Results.Ok(
            CreatePatchResponse(service));
    });

patchApi.MapPost(
    "/experiment/compound-edit",
    (PatchSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var item =
            service.Project.ConfigItems[0];

        var lastIndex =
            service.Project.ConfigItems.Count - 1;

        var transaction =
            new PatchTransaction(
            [
                new ReplaceNamePatch(
                    item.Id,
                    item.Name,
                    $"{item.Name} (Edited)"),

                new ReplaceActivePatch(
                    item.Id,
                    item.Active,
                    !item.Active),

                new MoveConfigItemPatch(
                    item.Id,
                    0,
                    lastIndex)
            ]);

        service.History.Execute(
            service.Project,
            transaction);

        return Results.Ok(
            CreatePatchResponse(service));
    });

patchApi.MapPost(
    "/experiment/reset/{itemCount:int}",
    (
        int itemCount,
        PatchSpikeService service) =>
    {
        if (itemCount is < 1 or > 10000)
        {
            return Results.BadRequest();
        }

        service.Reset(itemCount);

        return Results.Ok(
            CreatePatchResponse(service));
    });

patchApi.MapPost(
    "/experiment/toggles/{count:int}",
    (
        int count,
        PatchSpikeService service) =>
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

            var transaction =
                new PatchTransaction(
                [
                    new ReplaceActivePatch(
                        item.Id,
                        item.Active,
                        !item.Active)
                ]);

            service.History.Execute(
                service.Project,
                transaction);
        }

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreatePatchResponse(service),

            benchmark = new
            {
                operations = count,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

var hybridApi = app.MapGroup("/api/hybrid");

hybridApi.MapPost(
    "/config-items/{id:guid}/toggle",
    (
        Guid id,
        HybridSpikeService service) =>
    {
        var item =
            service.Project.ConfigItems
                .FirstOrDefault(
                    item => item.Id == id);

        if (item is null)
        {
            return Results.NotFound();
        }

        var entry =
            new HybridHistoryEntry(
                "Toggle Active",
                new PatchTransaction(
                [
                    new ReplaceActivePatch(
                        item.Id,
                        item.Active,
                        !item.Active)
                ]));

        service.History.Execute(
            service.Project,
            entry);

        return Results.Ok(
            CreateHybridResponse(service));
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

static object CreatePatchResponse(
    PatchSpikeService service)
{
    return new
    {
        projectState = service.Project,
        canUndo = service.History.CanUndo,
        canRedo = service.History.CanRedo,

        diagnostics = new
        {
            approach = "patch",
            representation = "Generic state patches",

            undoEntries =
                service.History.UndoCount,

            redoEntries =
                service.History.RedoCount,

            undoEntryDetails =
                service.History.UndoEntryDetails,

            redoEntryDetails =
                service.History.RedoEntryDetails,

            storedConfigItemCopies = (int?)null
        }
    };
}

static object CreateHybridResponse(
    HybridSpikeService service)
{
    return new
    {
        projectState = service.Project,
        canUndo = service.History.CanUndo,
        canRedo = service.History.CanRedo,

        diagnostics = new
        {
            approach = "hybrid",
            representation = "Semantic actions + generic patches",

            undoEntries =
                service.History.UndoCount,

            redoEntries =
                service.History.RedoCount,

            undoEntryDetails =
                service.History.UndoEntryDetails,

            redoEntryDetails =
                service.History.RedoEntryDetails,

            storedConfigItemCopies = (int?)null
        }
    };
}

app.Run();