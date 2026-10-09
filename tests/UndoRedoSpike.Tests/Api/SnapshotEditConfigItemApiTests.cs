using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

[TestClass]
public class SnapshotEditConfigItemApiTests
{
    [TestMethod]
    public async Task Apply_CreatesOneSemanticHistoryEntry()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using var client = factory.CreateClient();

        await Reset(client);

        var initial = await GetState(client);
        var original = initial.ProjectState.ConfigItems[0];

        var newName = "Updated Landing Light";
        var newActive = !original.Active;

        var response = await client.PutAsJsonAsync(
            $"/api/snapshot/config-items/{original.Id}",
            new
            {
                name = newName,
                active = newActive
            });

        response.EnsureSuccessStatusCode();

        var state = await response.Content
            .ReadFromJsonAsync<ApiStateResponse>();

        Assert.IsNotNull(state);

        var edited = state.ProjectState.ConfigItems
            .Single(item => item.Id == original.Id);

        Assert.AreEqual(newName, edited.Name);
        Assert.AreEqual(newActive, edited.Active);

        Assert.AreEqual(
            1,
            state.Diagnostics.UndoEntries);

        Assert.AreEqual(
            0,
            state.Diagnostics.RedoEntries);

        Assert.AreEqual(
            "Edit Config Item",
            state.Diagnostics.UndoEntryDetails[0]);
    }

    [TestMethod]
    public async Task Apply_UndoAndRedoRestoreBothFields()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using var client = factory.CreateClient();

        await Reset(client);

        var initial = await GetState(client);
        var original = initial.ProjectState.ConfigItems[0];

        var newName = "Taxi Light";
        var newActive = !original.Active;

        var applyResponse = await client.PutAsJsonAsync(
            $"/api/snapshot/config-items/{original.Id}",
            new
            {
                name = newName,
                active = newActive
            });

        applyResponse.EnsureSuccessStatusCode();

        var undoResponse = await client.PostAsync(
            "/api/snapshot/history/undo",
            null);

        undoResponse.EnsureSuccessStatusCode();

        var undoResult = await undoResponse.Content
            .ReadFromJsonAsync<ApiHistoryResponse>();

        Assert.IsNotNull(undoResult);

        var restored = undoResult.State
            .ProjectState.ConfigItems
            .Single(item => item.Id == original.Id);

        Assert.AreEqual(original.Name, restored.Name);
        Assert.AreEqual(original.Active, restored.Active);

        Assert.AreEqual(
            "Edit Config Item",
            undoResult.State.Diagnostics
                .RedoEntryDetails[0]);

        var redoResponse = await client.PostAsync(
            "/api/snapshot/history/redo",
            null);

        redoResponse.EnsureSuccessStatusCode();

        var redoResult = await redoResponse.Content
            .ReadFromJsonAsync<ApiHistoryResponse>();

        Assert.IsNotNull(redoResult);

        var reapplied = redoResult.State
            .ProjectState.ConfigItems
            .Single(item => item.Id == original.Id);

        Assert.AreEqual(newName, reapplied.Name);
        Assert.AreEqual(newActive, reapplied.Active);

        Assert.AreEqual(
            "Edit Config Item",
            redoResult.State.Diagnostics
                .UndoEntryDetails[0]);
    }

    [TestMethod]
    public async Task Apply_WithoutChangesDoesNotCreateHistory()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using var client = factory.CreateClient();

        await Reset(client);

        var initial = await GetState(client);
        var original = initial.ProjectState.ConfigItems[0];

        var response = await client.PutAsJsonAsync(
            $"/api/snapshot/config-items/{original.Id}",
            new
            {
                name = original.Name,
                active = original.Active
            });

        response.EnsureSuccessStatusCode();

        var state = await response.Content
            .ReadFromJsonAsync<ApiStateResponse>();

        Assert.IsNotNull(state);

        Assert.AreEqual(
            0,
            state.Diagnostics.UndoEntries);

        Assert.AreEqual(
            0,
            state.Diagnostics.RedoEntries);

        Assert.IsFalse(state.CanUndo);
        Assert.IsFalse(state.CanRedo);
    }

    [TestMethod]
    public async Task Apply_UnknownIdReturnsNotFound()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using var client = factory.CreateClient();

        await Reset(client);

        var response = await client.PutAsJsonAsync(
            $"/api/snapshot/config-items/{Guid.NewGuid()}",
            new
            {
                name = "Missing",
                active = true
            });

        Assert.AreEqual(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var state = await GetState(client);

        Assert.AreEqual(
            0,
            state.Diagnostics.UndoEntries);
    }

    [TestMethod]
    public async Task Apply_NoOpPreservesRedoHistory()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using var client = factory.CreateClient();

        await Reset(client);

        var initial = await GetState(client);
        var original = initial.ProjectState.ConfigItems[0];

        var update = await client.PutAsJsonAsync(
            $"/api/snapshot/config-items/{original.Id}",
            new
            {
                name = "Modified",
                active = original.Active
            });

        update.EnsureSuccessStatusCode();

        var undo = await client.PostAsync(
            "/api/snapshot/history/undo",
            null);

        undo.EnsureSuccessStatusCode();

        var response = await client.PutAsJsonAsync(
            $"/api/snapshot/config-items/{original.Id}",
            new
            {
                name = original.Name,
                active = original.Active
            });

        response.EnsureSuccessStatusCode();

        var state = await response.Content
            .ReadFromJsonAsync<ApiStateResponse>();

        Assert.IsNotNull(state);

        Assert.AreEqual(
            0,
            state.Diagnostics.UndoEntries);

        Assert.AreEqual(
            1,
            state.Diagnostics.RedoEntries);

        Assert.AreEqual(
            "Edit Config Item",
            state.Diagnostics.RedoEntryDetails[0]);
    }

    private static async Task Reset(HttpClient client)
    {
        var response = await client.PostAsync(
            "/api/snapshot/experiment/reset/3",
            null);

        response.EnsureSuccessStatusCode();
    }

    private static async Task<ApiStateResponse> GetState(
        HttpClient client)
    {
        var result =
            await client.GetFromJsonAsync<ApiStateResponse>(
                "/api/snapshot/state");

        Assert.IsNotNull(result);

        return result;
    }
}
