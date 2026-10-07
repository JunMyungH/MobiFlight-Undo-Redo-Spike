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
    var operations =
        service.History
            .NextUndoActionCount;

    var stopwatch =
        Stopwatch.StartNew();

    var success =
        service.History.Undo();

    stopwatch.Stop();

    if (!success)
    {
        return Results.Conflict(new
        {
            message = "Nothing to undo."
        });
    }

    return Results.Ok(new
    {
        state = CreateCommandResponse(service),

        benchmark = new
        {
            operations,
            elapsedMilliseconds =
                stopwatch.Elapsed.TotalMilliseconds
        }
    });
});

commandApi.MapPost("/history/redo", (
    CommandSpikeService service) =>
{
    var operations =
        service.History
            .NextRedoActionCount;

    var stopwatch =
        Stopwatch.StartNew();

    var success =
        service.History.Redo();

    stopwatch.Stop();

    if (!success)
    {
        return Results.Conflict(new
        {
            message = "Nothing to redo."
        });
    }

    return Results.Ok(new
    {
        state = CreateCommandResponse(service),

        benchmark = new
        {
            operations,
            elapsedMilliseconds =
                stopwatch.Elapsed.TotalMilliseconds
        }
    });
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

commandApi.MapPost(
    "/experiment/bulk-toggle",
    (CommandSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var command =
            new BulkToggleCommand(
                service.Project.ConfigItems);

        service.History.Execute(command);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreateCommandResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

commandApi.MapPost(
    "/experiment/bulk-delete",
    (CommandSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var selectedItems =
            service.Project.ConfigItems
                .Where(item => item.Active)
                .ToList();

        if (selectedItems.Count == 0)
        {
            return Results.BadRequest(new
            {
                message = "No active ConfigItems to delete."
            });
        }

        var command =
            new BulkDeleteCommand(
                service.Project,
                selectedItems);

        service.History.Execute(command);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreateCommandResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

commandApi.MapPost(
    "/experiment/duplicate-first",
    (CommandSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0) 
        {
            return Results.BadRequest();
        }

        var source =
            service.Project.ConfigItems[0];

        var stopwatch = Stopwatch.StartNew();

        service.History.Execute(
            new DuplicateConfigItemCommand(
                service.Project,
                source.Id));
        stopwatch.Stop();

        return Results.Ok(new
        {
            state = CreateCommandResponse(service),

            benchmark = new
            {
                operations = 1,
                elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

commandApi.MapPost(
    "/experiment/move-first-to-last",
    (CommandSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count < 2)
        {
            return Results.BadRequest();
        }

        var item =
            service.Project.ConfigItems[0];

        var lastIndex =
            service.Project.ConfigItems.Count - 1;

        var stopwatch = Stopwatch.StartNew();

        service.History.Execute(
            new MoveConfigItemCommand(
                service.Project,
                item.Id,
                0,
                lastIndex));
        stopwatch.Stop();

        return Results.Ok(new
        {
            state = CreateCommandResponse(service),

            benchmark = new
            {
                operations = 1,
                elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

commandApi.MapPost(
    "/history/undo-to/{steps:int}",
    (
        int steps,
        CommandSpikeService service) =>
    {
        if (
            steps < 1 ||
            steps >
                service.History.UndoCount)
        {
            return Results.BadRequest(
                new
                {
                    message =
                        "Invalid undo history target."
                });
        }

        var stopwatch =
            Stopwatch.StartNew();

        var success =
            service.History.UndoTo(
                steps);

        stopwatch.Stop();

        if (!success)
        {
            return Results.Conflict();
        }

        return Results.Ok(
            new
            {
                state =
                    CreateCommandResponse(
                        service),

                benchmark =
                    new
                    {
                        operations =
                            steps,

                        elapsedMilliseconds =
                            stopwatch.Elapsed
                                .TotalMilliseconds
                    }
            });
    });

commandApi.MapPost(
    "/history/redo-to/{steps:int}",
    (
        int steps,
        CommandSpikeService service) =>
    {
        if (
            steps < 1 ||
            steps >
                service.History.RedoCount)
        {
            return Results.BadRequest(
                new
                {
                    message =
                        "Invalid redo history target."
                });
        }

        var stopwatch =
            Stopwatch.StartNew();

        var success =
            service.History.RedoTo(
                steps);

        stopwatch.Stop();

        if (!success)
        {
            return Results.Conflict();
        }

        return Results.Ok(
            new
            {
                state =
                    CreateCommandResponse(
                        service),

                benchmark =
                    new
                    {
                        operations =
                            steps,

                        elapsedMilliseconds =
                            stopwatch.Elapsed
                                .TotalMilliseconds
                    }
            });
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
        var stopwatch =
        Stopwatch.StartNew();

        var success =
            service.History.Undo(
                service.Project);

        stopwatch.Stop();

        if (!success)
        {
            return Results.Conflict(new
            {
                message = "Nothing to undo."
            });
        }

        return Results.Ok(new
        {
            state =
                CreateSnapshotResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

snapshotApi.MapPost(
    "/history/redo",
    (SnapshotSpikeService service) =>
    {
        var stopwatch =
        Stopwatch.StartNew();

        var success =
            service.History.Redo(
                service.Project);

        stopwatch.Stop();

        if (!success)
        {
            return Results.Conflict(new
            {
                message = "Nothing to redo."
            });
        }

        return Results.Ok(new
        {
            state =
                CreateSnapshotResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
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

snapshotApi.MapPost(
    "/experiment/bulk-toggle",
    (SnapshotSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        service.History.Execute(
            service.Project,
            project =>
            {
                foreach (var item in project.ConfigItems)
                {
                    item.Active =
                        !item.Active;
                }
            });

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreateSnapshotResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

snapshotApi.MapPost(
    "/experiment/bulk-delete",
    (SnapshotSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var selectedIds =
            service.Project.ConfigItems
                .Where(item => item.Active)
                .Select(item => item.Id)
                .ToHashSet();

            if (selectedIds.Count == 0)
            {
                return Results.BadRequest(new
                {
                    message = "No active ConfigItems to delete."
                });
            }

        service.History.Execute(
            service.Project,
            project =>
            {
                project.ConfigItems.RemoveAll(
                    item => selectedIds.Contains(item.Id));
            });

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreateSnapshotResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

snapshotApi.MapPost(
    "/experiment/duplicate-first",
    (SnapshotSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch = Stopwatch.StartNew();

        service.History.Execute(
            service.Project,
            project =>
            {
                var source = project.ConfigItems[0];
                var copy = new ConfigItem
                {
                    Name = $"{source.Name} (Copy)",
                    Active = source.Active
                };
                
                project.ConfigItems.Insert(
                    1, copy);
            });

        stopwatch.Stop();

        return Results.Ok(new
        {
            state = CreateSnapshotResponse(service),

            benchmark = new
            {
                operations = 1,
                elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

snapshotApi.MapPost(
    "/experiment/move-first-to-last",
    (SnapshotSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count < 2)
        {
            return Results.BadRequest();
        }

        var stopwatch = Stopwatch.StartNew();

        service.History.Execute(
            service.Project,
            project =>
            {
                var item = project.ConfigItems[0];
                project.ConfigItems.RemoveAt(0);
                project.ConfigItems.Add(item);
            });

        stopwatch.Stop();

        return Results.Ok(new
        {
            state = CreateSnapshotResponse(service),

            benchmark = new
            {
                operations = 1,
                elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

snapshotApi.MapPost(
    "/history/undo-to/{steps:int}",
    (
        int steps,
        SnapshotSpikeService service) =>
    {
        if (
            steps < 1 ||
            steps >
                service.History.UndoCount)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var success =
            service.History.UndoTo(
                service.Project,
                steps);

        stopwatch.Stop();

        if (!success)
        {
            return Results.Conflict();
        }

        return Results.Ok(
            new
            {
                state =
                    CreateSnapshotResponse(
                        service),

                benchmark =
                    new
                    {
                        operations =
                            steps,

                        elapsedMilliseconds =
                            stopwatch.Elapsed
                                .TotalMilliseconds
                    }
            });
    });

snapshotApi.MapPost(
    "/history/redo-to/{steps:int}",
    (
        int steps,
        SnapshotSpikeService service) =>
    {
        if (
            steps < 1 ||
            steps >
                service.History.RedoCount)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var success =
            service.History.RedoTo(
                service.Project,
                steps);

        stopwatch.Stop();

        if (!success)
        {
            return Results.Conflict();
        }

        return Results.Ok(
            new
            {
                state =
                    CreateSnapshotResponse(
                        service),

                benchmark =
                    new
                    {
                        operations =
                            steps,

                        elapsedMilliseconds =
                            stopwatch.Elapsed
                                .TotalMilliseconds
                    }
            });
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
        var stopwatch =
        Stopwatch.StartNew();

        var success =
            service.History.Undo(
                service.Project);

        stopwatch.Stop();

        if (!success)
        {
            return Results.Conflict(new
            {
                message = "Nothing to undo."
            });
        }

        return Results.Ok(new
        {
            state =
                CreatePatchResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

patchApi.MapPost(
    "/history/redo",
    (PatchSpikeService service) =>
    {
        var stopwatch =
        Stopwatch.StartNew();

        var success =
            service.History.Redo(
                service.Project);

        stopwatch.Stop();

        if (!success)
        {
            return Results.Conflict(new
            {
                message = "Nothing to redo."
            });
        }

        return Results.Ok(new
        {
            state =
                CreatePatchResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
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
    "/experiment/bulk-toggle",
    (PatchSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var operations =
            service.Project.ConfigItems
                .Select(item =>
                    (IPatchOperation)
                    new ReplaceActivePatch(
                        item.Id,
                        item.Active,
                        !item.Active))
                .ToList();

        var transaction =
            new PatchTransaction(
                operations);

        service.History.Execute(
            service.Project,
            transaction);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreatePatchResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

patchApi.MapPost(
    "/experiment/bulk-delete",
    (PatchSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var targets =
            service.Project.ConfigItems
                .Select((item, index) => new
                {
                    Item = item,
                    Index = index
                })
                .Where(entry => entry.Item.Active)
                .OrderByDescending(
                    entry => entry.Index)
                .ToList();

        if (targets.Count == 0)
        {
            return Results.BadRequest(new
            {
                message = "No active ConfigItems to delete."
            });
        }

        var operations =
            targets
                .Select(entry =>
                    (IPatchOperation)
                    new RemoveConfigItemPatch(
                        entry.Item,
                        entry.Index))
                .ToList();

        var transaction =
            new PatchTransaction(
                operations);

        service.History.Execute(
            service.Project,
            transaction);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreatePatchResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

patchApi.MapPost(
    "/experiment/duplicate-first",
    (PatchSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }
        var source = service.Project.ConfigItems[0];
        var copy = new ConfigItem
        {
            Name = $"{source.Name} (Copy)",
            Active = source.Active
        };
        var stopwatch = Stopwatch.StartNew();
        var transaction =
            new PatchTransaction(
            [
                new AddConfigItemPatch(
                    copy,
                    1)
            ]);

        service.History.Execute(
            service.Project,
            transaction);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state = CreatePatchResponse(service),

            benchmark = new
            {
                operations = 1,
                elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

patchApi.MapPost(
    "/experiment/move-first-to-last",
    (PatchSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count < 2)
        {
            return Results.BadRequest();
        }
        var item = service.Project.ConfigItems[0];
        var lastIndex = service.Project.ConfigItems.Count - 1;
        var stopwatch = Stopwatch.StartNew();
        var transaction =
            new PatchTransaction(
            [
                new MoveConfigItemPatch(
                    item.Id,
                    0,
                    lastIndex)
            ]);

        service.History.Execute(
            service.Project,
            transaction);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state = CreatePatchResponse(service),

            benchmark = new
            {
                operations = 1,
                elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

patchApi.MapPost(
    "/experiment/bulk-toggle-reference",
    (PatchSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var operations =
            service.Project.ConfigItems
                .Select(item =>
                    (IPatchOperation)
                    new ReplaceActiveReferencePatch(
                        item,
                        item.Active,
                        !item.Active))
                .ToList();

        var transaction =
            new PatchTransaction(
                operations);

        service.History.Execute(
            service.Project,
            transaction);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreatePatchResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

patchApi.MapPost(
    "/experiment/bulk-toggle-indexed",
    (PatchSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var itemIndex =
            service.Project.ConfigItems
                .ToDictionary(
                    item => item.Id);

        var operations =
            service.Project.ConfigItems
                .Select(item =>
                    (IPatchOperation)
                    new ReplaceActiveIndexedPatch(
                        itemIndex,
                        item.Id,
                        item.Active,
                        !item.Active))
                .ToList();

        var transaction =
            new PatchTransaction(
                operations);

        service.History.Execute(
            service.Project,
            transaction);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreatePatchResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

patchApi.MapPost(
    "/experiment/bulk-delete-indexed",
    (PatchSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var targets =
            service.Project.ConfigItems
                .Select((item, index) => new
                {
                    Item = item,
                    Index = index
                })
                .Where(entry =>
                    entry.Item.Active)
                .OrderByDescending(
                    entry => entry.Index)
                .ToList();

        if (targets.Count == 0)
        {
            return Results.BadRequest(new
            {
                message =
                    "No active ConfigItems to delete."
            });
        }

        var operations =
            targets
                .Select(entry =>
                    (IPatchOperation)
                    new RemoveConfigItemByIndexPatch(
                        entry.Item,
                        entry.Index))
                .ToList();

        var transaction =
            new PatchTransaction(
                operations);

        service.History.Execute(
            service.Project,
            transaction);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreatePatchResponse(service),

            benchmark = new
            {
                operations = 1,
                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

patchApi.MapPost(
    "/history/undo-to/{steps:int}",
    (
        int steps,
        PatchSpikeService service) =>
    {
        if (
            steps < 1 ||
            steps >
                service.History.UndoCount)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var success =
            service.History.UndoTo(
                service.Project,
                steps);

        stopwatch.Stop();

        if (!success)
        {
            return Results.Conflict();
        }

        return Results.Ok(
            new
            {
                state =
                    CreatePatchResponse(service),

                benchmark =
                    new
                    {
                        operations =
                            steps,

                        elapsedMilliseconds =
                            stopwatch.Elapsed
                                .TotalMilliseconds
                    }
            });
    });

patchApi.MapPost(
    "/history/redo-to/{steps:int}",
    (
        int steps,
        PatchSpikeService service) =>
    {
        if (
            steps < 1 ||
            steps >
                service.History.RedoCount)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var success =
            service.History.RedoTo(
                service.Project,
                steps);

        stopwatch.Stop();

        if (!success)
        {
            return Results.Conflict();
        }

        return Results.Ok(
            new
            {
                state =
                    CreatePatchResponse(service),

                benchmark =
                    new
                    {
                        operations =
                            steps,

                        elapsedMilliseconds =
                            stopwatch.Elapsed
                                .TotalMilliseconds
                    }
            });
    });

var hybridApi = app.MapGroup("/api/hybrid");

hybridApi.MapGet(
    "/state",
    (HybridSpikeService service) =>
    {
        return Results.Ok(
            CreateHybridResponse(service));
    });

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

hybridApi.MapPut(
    "/config-items/{id:guid}",
    (
        Guid id,
        EditConfigItemRequest request,
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

        var operations =
            new List<IPatchOperation>();

        if (item.Name != request.Name)
        {
            operations.Add(
                new ReplaceNamePatch(
                    item.Id,
                    item.Name,
                    request.Name));
        }

        if (item.Active != request.Active)
        {
            operations.Add(
                new ReplaceActivePatch(
                    item.Id,
                    item.Active,
                    request.Active));
        }

        if (operations.Count == 0)
        {
            return Results.Ok(
                CreateHybridResponse(service));
        }

        var entry =
            new HybridHistoryEntry(
                "Edit Config Item",
                new PatchTransaction(
                    operations));

        service.History.Execute(
            service.Project,
            entry);

        return Results.Ok(
            CreateHybridResponse(service));
    });

hybridApi.MapDelete(
    "/config-items/{id:guid}",
    (
        Guid id,
        HybridSpikeService service) =>
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

        var entry =
            new HybridHistoryEntry(
                "Delete Config Item",
                new PatchTransaction(
                    [
                        new RemoveConfigItemPatch(
                            item,
                            index)
                    ]));

        service.History.Execute(
            service.Project,
            entry);

        return Results.Ok(
            CreateHybridResponse(service));
    }
    );

hybridApi.MapPost(
    "/history/undo",
    (HybridSpikeService service) =>
    {
        var stopwatch =
        Stopwatch.StartNew();

        var success =
            service.History.Undo(
                service.Project);

        stopwatch.Stop();

        if (!success)
        {
            return Results.Conflict(new
            {
                message = "Nothing to undo."
            });
        }

        return Results.Ok(new
        {
            state =
               CreateHybridResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                   stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

hybridApi.MapPost(
    "/history/redo",
    (HybridSpikeService service) =>
    {
        var stopwatch =
            Stopwatch.StartNew();

        var success =
            service.History.Redo(
                service.Project);

        stopwatch.Stop();

        if (!success)
        {
            return Results.Conflict(new
            {
                message = "Nothing to redo."
            });
        }

        return Results.Ok(new
        {
            state =
               CreateHybridResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                   stopwatch.Elapsed.TotalMilliseconds
            }
        });

    });

hybridApi.MapPost(
    "/experiment/compound-edit",
    (HybridSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var item =
            service.Project.ConfigItems[0];
        
        var lastIndex =
            service.Project.ConfigItems.Count - 1;

        var entry =
            new HybridHistoryEntry(
                "Compound Edit",
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
                    ]));
        service.History.Execute(
            service.Project,
            entry);

        return Results.Ok(
            CreateHybridResponse(service));
    });

hybridApi.MapPost(
    "/experiment/reset/{itemCount:int}",
    (
        int itemCount,
        HybridSpikeService service) =>
    {
        if (itemCount is < 1 or > 10000)
        {
            return Results.BadRequest();
        }

        service.Reset(itemCount);

        return Results.Ok(
            CreateHybridResponse(service));
    });

hybridApi.MapPost(
    "/experiment/bulk-toggle",
    (HybridSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var operations =
            service.Project.ConfigItems
                .Select(item =>
                    (IPatchOperation)
                    new ReplaceActivePatch(
                        item.Id,
                        item.Active,
                        !item.Active))
                .ToList();

        var entry =
            new HybridHistoryEntry(
                "Bulk Toggle",
                new PatchTransaction(
                    operations));

        service.History.Execute(
            service.Project,
            entry);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreateHybridResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

hybridApi.MapPost(
    "/experiment/bulk-delete",
    (HybridSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var targets =
            service.Project.ConfigItems
                .Select((item, index) => new
                {
                    Item = item,
                    Index = index
                })
                .Where(entry => entry.Item.Active)
                .OrderByDescending(
                    entry => entry.Index)
                .ToList();

        if (targets.Count == 0)
        {
            return Results.BadRequest(new
            {
                message = "No active ConfigItems to delete."
            });
        }

        var operations =
            targets
                .Select(entry =>
                    (IPatchOperation)
                    new RemoveConfigItemPatch(
                        entry.Item,
                        entry.Index))
                .ToList();

        var entry =
            new HybridHistoryEntry(
                "Bulk Delete",
                new PatchTransaction(
                    operations));

        service.History.Execute(
            service.Project,
            entry);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreateHybridResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

hybridApi.MapPost(
    "/experiment/duplicate-first",
    (HybridSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }
        var source = service.Project.ConfigItems[0];
        var copy = new ConfigItem
        {
            Name = $"{source.Name} (Copy)",
            Active = source.Active
        };
        var stopwatch = Stopwatch.StartNew();
        var entry =
            new HybridHistoryEntry(
                "Duplicate Config Item",
                new PatchTransaction(
            [
                new AddConfigItemPatch(
                    copy,
                    1)
            ]));

        service.History.Execute(
            service.Project,
            entry);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state = CreateHybridResponse(service),

            benchmark = new
            {
                operations = 1,
                elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

hybridApi.MapPost(
    "/experiment/move-first-to-last",
    (HybridSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count < 2)
        {
            return Results.BadRequest();
        }
        var item = service.Project.ConfigItems[0];
        var lastIndex = service.Project.ConfigItems.Count - 1;
        var stopwatch = Stopwatch.StartNew();
        var entry =
            new HybridHistoryEntry(
                "Move Config Item",
                new PatchTransaction(
            [
                new MoveConfigItemPatch(
                    item.Id,
                    0,
                    lastIndex)
            ]));

        service.History.Execute(
            service.Project,
            entry);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state = CreateHybridResponse(service),

            benchmark = new
            {
                operations = 1,
                elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

hybridApi.MapPost(
    "/experiment/bulk-toggle-reference",
    (HybridSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var operations =
            service.Project.ConfigItems
                .Select(item =>
                    (IPatchOperation)
                    new ReplaceActiveReferencePatch(
                        item,
                        item.Active,
                        !item.Active))
                .ToList();

        var entry =
            new HybridHistoryEntry(
                "Bulk Toggle (Direct Reference)",
                new PatchTransaction(
                    operations));

        service.History.Execute(
            service.Project,
            entry);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreateHybridResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

hybridApi.MapPost(
    "/experiment/bulk-toggle-indexed",
    (HybridSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var itemIndex =
            service.Project.ConfigItems
                .ToDictionary(
                    item => item.Id);

        var operations =
            service.Project.ConfigItems
                .Select(item =>
                    (IPatchOperation)
                    new ReplaceActiveIndexedPatch(
                        itemIndex,
                        item.Id,
                        item.Active,
                        !item.Active))
                .ToList();

        var entry =
            new HybridHistoryEntry(
                "Bulk Toggle (Indexed Lookup)",
                new PatchTransaction(
                    operations));

        service.History.Execute(
            service.Project,
            entry);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreateHybridResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

hybridApi.MapPost(
    "/experiment/bulk-delete-indexed",
    (HybridSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var targets =
            service.Project.ConfigItems
                .Select((item, index) => new
                {
                    Item = item,
                    Index = index
                })
                .Where(entry =>
                    entry.Item.Active)
                .OrderByDescending(
                    entry => entry.Index)
                .ToList();

        if (targets.Count == 0)
        {
            return Results.BadRequest(new
            {
                message =
                    "No active ConfigItems to delete."
            });
        }

        var operations =
            targets
                .Select(entry =>
                    (IPatchOperation)
                    new RemoveConfigItemByIndexPatch(
                        entry.Item,
                        entry.Index))
                .ToList();

        var entry =
            new HybridHistoryEntry(
                "Bulk Delete (Stored Index)",
                new PatchTransaction(
                    operations));

        service.History.Execute(
            service.Project,
            entry);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreateHybridResponse(service),

            benchmark = new
            {
                operations = 1,
                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

hybridApi.MapPost(
    "/experiment/bulk-delete-snapshot",
    (HybridSpikeService service) =>
    {
        if (service.Project.ConfigItems.Count == 0)
        {
            return Results.BadRequest();
        }

        var selectedIds =
            service.Project.ConfigItems
                .Where(item => item.Active)
                .Select(item => item.Id)
                .ToHashSet();

        if (selectedIds.Count == 0)
        {
            return Results.BadRequest(new
            {
                message =
                    "No active ConfigItems to delete."
            });
        }

        var stopwatch =
            Stopwatch.StartNew();

        var entry =
            new HybridHistoryEntry(
                "Bulk Delete",
                new HybridSnapshotOperation(
                    project =>
                    {
                        project.ConfigItems.RemoveAll(
                            item =>
                                selectedIds.Contains(
                                    item.Id));
                    }));

        service.History.Execute(
            service.Project,
            entry);

        stopwatch.Stop();

        return Results.Ok(new
        {
            state =
                CreateHybridResponse(service),

            benchmark = new
            {
                operations = 1,

                elapsedMilliseconds =
                    stopwatch.Elapsed.TotalMilliseconds
            }
        });
    });

hybridApi.MapPost(
    "/history/undo-to/{steps:int}",
    (
        int steps,
        HybridSpikeService service) =>
    {
        if (
            steps < 1 ||
            steps >
                service.History.UndoCount)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var success =
            service.History.UndoTo(
                service.Project,
                steps);

        stopwatch.Stop();

        if (!success)
        {
            return Results.Conflict();
        }

        return Results.Ok(
            new
            {
                state =
                    CreateHybridResponse(service),

                benchmark =
                    new
                    {
                        operations =
                            steps,

                        elapsedMilliseconds =
                            stopwatch.Elapsed
                                .TotalMilliseconds
                    }
            });
    });

hybridApi.MapPost(
    "/history/redo-to/{steps:int}",
    (
        int steps,
        HybridSpikeService service) =>
    {
        if (
            steps < 1 ||
            steps >
                service.History.RedoCount)
        {
            return Results.BadRequest();
        }

        var stopwatch =
            Stopwatch.StartNew();

        var success =
            service.History.RedoTo(
                service.Project,
                steps);

        stopwatch.Stop();

        if (!success)
        {
            return Results.Conflict();
        }

        return Results.Ok(
            new
            {
                state =
                    CreateHybridResponse(service),

                benchmark =
                    new
                    {
                        operations =
                            steps,

                        elapsedMilliseconds =
                            stopwatch.Elapsed
                                .TotalMilliseconds
                    }
            });
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
                service.History
                    .UndoEntryDetails,

            redoEntryDetails =
                service.History
                    .RedoEntryDetails,

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
            representation = "Semantic actions + Patch/Snapshot strategies",

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

public sealed record EditConfigItemRequest(
    string Name,
    bool Active);

public partial class Program
{
}