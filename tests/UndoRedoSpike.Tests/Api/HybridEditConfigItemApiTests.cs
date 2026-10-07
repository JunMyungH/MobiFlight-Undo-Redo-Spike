using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

[TestClass]
public class HybridEditConfigItemApiTests
{
    [TestMethod]
    public async Task EditConfigItem_ApplyCreatesOneHistoryEntryAndUndoRestoresState()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        await Reset(client);

        var initialState =
            await GetState(client);

        var originalItem =
            initialState.ProjectState
                .ConfigItems[0];

        var editedName =
            $"{originalItem.Name} Edited";

        var editedActive =
            !originalItem.Active;

        var response =
            await client.PutAsJsonAsync(
                $"/api/hybrid/config-items/{originalItem.Id}",
                new
                {
                    name = editedName,
                    active = editedActive
                });

        response.EnsureSuccessStatusCode();

        var editedState =
            await response.Content
                .ReadFromJsonAsync<ApiStateResponse>();

        Assert.IsNotNull(
            editedState);

        var editedItem =
            editedState.ProjectState
                .ConfigItems
                .Single(
                    item =>
                        item.Id ==
                        originalItem.Id);

        Assert.AreEqual(
            editedName,
            editedItem.Name);

        Assert.AreEqual(
            editedActive,
            editedItem.Active);

        Assert.AreEqual(
            1,
            editedState.Diagnostics
                .UndoEntries);

        Assert.AreEqual(
            0,
            editedState.Diagnostics
                .RedoEntries);

        Assert.HasCount(
            1,
            editedState.Diagnostics
                .UndoEntryDetails);

        Assert.AreEqual(
            "Edit Config Item [replace Name + replace Active]",
            editedState.Diagnostics
                .UndoEntryDetails[0]);

        var undoResponse =
            await client.PostAsync(
                "/api/hybrid/history/undo",
                null);

        undoResponse.EnsureSuccessStatusCode();

        var undoResult =
            await undoResponse.Content
                .ReadFromJsonAsync<ApiHistoryResponse>();

        Assert.IsNotNull(
            undoResult);

        var restoredItem =
            undoResult.State
                .ProjectState
                .ConfigItems
                .Single(
                    item =>
                        item.Id ==
                        originalItem.Id);

        Assert.AreEqual(
            originalItem.Name,
            restoredItem.Name);

        Assert.AreEqual(
            originalItem.Active,
            restoredItem.Active);

        Assert.AreEqual(
            0,
            undoResult.State
                .Diagnostics
                .UndoEntries);

        Assert.AreEqual(
            1,
            undoResult.State
                .Diagnostics
                .RedoEntries);
    }

    [TestMethod]
    public async Task EditConfigItem_WithoutChangesDoesNotCreateHistoryEntry()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        await Reset(client);

        var initialState =
            await GetState(client);

        var originalItem =
            initialState.ProjectState
                .ConfigItems[0];

        var response =
            await client.PutAsJsonAsync(
                $"/api/hybrid/config-items/{originalItem.Id}",
                new
                {
                    name =
                        originalItem.Name,

                    active =
                        originalItem.Active
                });

        response.EnsureSuccessStatusCode();

        var state =
            await response.Content
                .ReadFromJsonAsync<ApiStateResponse>();

        Assert.IsNotNull(
            state);

        Assert.AreEqual(
            0,
            state.Diagnostics
                .UndoEntries);

        Assert.AreEqual(
            0,
            state.Diagnostics
                .RedoEntries);

        Assert.IsFalse(
            state.CanUndo);

        Assert.IsFalse(
            state.CanRedo);
    }

    [TestMethod]
    public async Task EditConfigItem_WithUnknownIdReturnsNotFound()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        await Reset(client);

        var missingId =
            Guid.NewGuid();

        var response =
            await client.PutAsJsonAsync(
                $"/api/hybrid/config-items/{missingId}",
                new
                {
                    name = "Missing Item",
                    active = true
                });

        Assert.AreEqual(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var state =
            await GetState(client);

        Assert.AreEqual(
            0,
            state.Diagnostics
                .UndoEntries);

        Assert.IsFalse(
            state.CanUndo);
    }

    private static async Task Reset(
        HttpClient client)
    {
        var response =
            await client.PostAsync(
                "/api/hybrid/experiment/reset/3",
                null);

        response.EnsureSuccessStatusCode();
    }

    private static async Task<ApiStateResponse>
        GetState(
            HttpClient client)
    {
        var state =
            await client.GetFromJsonAsync<ApiStateResponse>(
                "/api/hybrid/state");

        Assert.IsNotNull(
            state);

        return state;
    }
}

internal sealed class ApiStateResponse
{
    public ProjectState ProjectState
    {
        get;
        set;
    } = new();

    public bool CanUndo
    {
        get;
        set;
    }

    public bool CanRedo
    {
        get;
        set;
    }

    public ApiHistoryDiagnostics Diagnostics
    {
        get;
        set;
    } = new();
}

internal sealed class ApiHistoryDiagnostics
{
    public string Approach
    {
        get;
        set;
    } = string.Empty;

    public string Representation
    {
        get;
        set;
    } = string.Empty;

    public int UndoEntries
    {
        get;
        set;
    }

    public int RedoEntries
    {
        get;
        set;
    }

    public List<string> UndoEntryDetails
    {
        get;
        set;
    } = [];

    public List<string> RedoEntryDetails
    {
        get;
        set;
    } = [];

    public int? StoredConfigItemCopies
    {
        get;
        set;
    }
}

internal sealed class ApiHistoryResponse
{
    public ApiStateResponse State
    {
        get;
        set;
    } = new();
}