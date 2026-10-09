[TestClass]
public class SnapshotHistoryTests
{
    [TestMethod]
    public void Undo_RestoresPreviousToggleState()
    {
        var item = new ConfigItem
        {
            Name = "Landing Light",
            Active = true
        };

        var project = new ProjectState
        {
            ConfigItems = [item]
        };

        var history = new SnapshotHistory();

        history.Execute(project, state =>
        {
            state.ConfigItems[0].Active = false;
        });

        Assert.IsFalse(project.ConfigItems[0].Active);

        history.Undo(project);

        Assert.IsTrue(project.ConfigItems[0].Active);
    }

    [TestMethod]
    public void Redo_RestoresResultingToggleState()
    {
        var project = new ProjectState
        {
            ConfigItems =
            [
                new ConfigItem
                {
                    Name = "Landing Light",
                    Active = true
                }
            ]
        };

        var history = new SnapshotHistory();

        history.Execute(project, state =>
        {
            state.ConfigItems[0].Active = false;
        });

        history.Undo(project);
        history.Redo(project);

        Assert.IsFalse(project.ConfigItems[0].Active);
    }

    [TestMethod]
    public void Undo_RestoresDeletedItemAtOriginalIndex()
    {
        var first = new ConfigItem { Name = "First" };
        var deleted = new ConfigItem { Name = "Delete Me" };
        var third = new ConfigItem { Name = "Third" };

        var deletedId = deleted.Id;

        var project = new ProjectState
        {
            ConfigItems =
            [
                first,
                deleted,
                third
            ]
        };

        var history = new SnapshotHistory();

        history.Execute(project, state =>
        {
            state.ConfigItems.RemoveAll(
                item => item.Id == deletedId);
        });

        Assert.HasCount(2, project.ConfigItems);

        history.Undo(project);

        Assert.HasCount(3, project.ConfigItems);
        Assert.AreEqual(
            deletedId,
            project.ConfigItems[1].Id);
    }

    [TestMethod]
    public void Undo_FollowsLifoOrder()
    {
        var project = new ProjectState
        {
            ConfigItems =
            [
                new ConfigItem
                {
                    Name = "First",
                    Active = false
                },
                new ConfigItem
                {
                    Name = "Second",
                    Active = false
                }
            ]
        };

        var history = new SnapshotHistory();

        history.Execute(project, state =>
        {
            state.ConfigItems[0].Active = true;
        });

        history.Execute(project, state =>
        {
            state.ConfigItems[1].Active = true;
        });

        history.Undo(project);

        Assert.IsTrue(project.ConfigItems[0].Active);
        Assert.IsFalse(project.ConfigItems[1].Active);

        history.Undo(project);

        Assert.IsFalse(project.ConfigItems[0].Active);
        Assert.IsFalse(project.ConfigItems[1].Active);
    }

    [TestMethod]
    public void NewActionAfterUndo_ClearsRedoStack()
    {
        var project = new ProjectState
        {
            ConfigItems =
            [
                new ConfigItem { Active = false },
                new ConfigItem { Active = false }
            ]
        };

        var history = new SnapshotHistory();

        history.Execute(project, state =>
        {
            state.ConfigItems[0].Active = true;
        });

        history.Undo(project);

        Assert.IsTrue(history.CanRedo);

        history.Execute(project, state =>
        {
            state.ConfigItems[1].Active = true;
        });

        Assert.IsFalse(history.CanRedo);
        Assert.AreEqual(0, history.RedoCount);
    }

    [TestMethod]
    public void UndoTo_GroupsMultipleActionsAndRedoRestoresThem()
    {
        var project =
            new ProjectState
            {
                ConfigItems =
                [
                    new ConfigItem
                {
                    Name = "First",
                    Active = false
                },
                new ConfigItem
                {
                    Name = "Second",
                    Active = false
                },
                new ConfigItem
                {
                    Name = "Third",
                    Active = false
                }
                ]
            };

        var history =
            new SnapshotHistory();

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[0].Active =
                    true;
            });

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[1].Active =
                    true;
            });

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[2].Active =
                    true;
            });

        var success =
            history.UndoTo(
                project,
                2);

        Assert.IsTrue(success);

        Assert.IsTrue(
            project.ConfigItems[0].Active);

        Assert.IsFalse(
            project.ConfigItems[1].Active);

        Assert.IsFalse(
            project.ConfigItems[2].Active);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.AreEqual(
            1,
            history.RedoCount);

        Assert.AreEqual(
            "History Jump (2 actions)",
            history.RedoEntryDetails[0]);

        history.Redo(project);

        Assert.IsTrue(
            project.ConfigItems[0].Active);

        Assert.IsTrue(
            project.ConfigItems[1].Active);

        Assert.IsTrue(
            project.ConfigItems[2].Active);

        Assert.AreEqual(
            3,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);
    }

    [TestMethod]
    public void NestedHistoryJump_UsesSemanticActionCount()
    {
        var project =
            new ProjectState
            {
                ConfigItems =
                [
                    new ConfigItem
                {
                    Name = "First",
                    Active = false
                },
                new ConfigItem
                {
                    Name = "Second",
                    Active = false
                },
                new ConfigItem
                {
                    Name = "Third",
                    Active = false
                }
                ]
            };

        var history =
            new SnapshotHistory();

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[0]
                    .Active = true;
            });

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[1]
                    .Active = true;
            });

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[2]
                    .Active = true;
            });

        history.UndoTo(
            project,
            2);

        Assert.AreEqual(
            "History Jump (2 actions)",
            history.RedoEntryDetails[0]);

        history.Undo(
            project);

        Assert.AreEqual(
            0,
            history.UndoCount);

        Assert.AreEqual(
            2,
            history.RedoCount);

        history.RedoTo(
            project,
            2);

        Assert.IsTrue(
            project.ConfigItems[0].Active);

        Assert.IsTrue(
            project.ConfigItems[1].Active);

        Assert.IsTrue(
            project.ConfigItems[2].Active);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);

        Assert.AreEqual(
            3,
            history.NextUndoActionCount);

        Assert.AreEqual(
            "History Jump (3 actions)",
            history.UndoEntryDetails[0]);
    }

    [TestMethod]
    public void Execute_StoresSemanticActionName()
    {
        var project = new ProjectState
        {
            ConfigItems =
            [
                new ConfigItem
                {
                    Name = "Landing Light",
                    Active = false
                }
            ]
        };

        var history = new SnapshotHistory();

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[0].Active = true;
            },
            "Toggle Active"
            );

        Assert.AreEqual(1, history.UndoCount);
    }

    [TestMethod]
    public void UndoRedo_PreservesSemanticActionName()
    {
        var project = new ProjectState
        {
            ConfigItems =
            [
                new ConfigItem
                {
                    Name = "Landing Light",
                    Active = false
                }
            ]
        };

        var history = new SnapshotHistory();

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[0].Active = true;
            },
            "Toggle Active");

        Assert.IsTrue(history.Undo(project));
        Assert.AreEqual(0, history.UndoCount);
        Assert.AreEqual(1, history.RedoCount);
        Assert.AreEqual(
            "Toggle Active",
            history.RedoEntryDetails[0]);
        Assert.IsTrue(history.Redo(project));
        Assert.AreEqual(
            "Toggle Active",
            history.UndoEntryDetails[0]);
        Assert.IsTrue(project.ConfigItems[0].Active);
    }

    [TestMethod]
    public void Execute_NoOpDoesNotCreateHistoryEntry()
    {
        var project = new ProjectState
        {
            ConfigItems =
            [
                new ConfigItem
                {
                    Name = "Landing Light",
                    Active = false
                }
            ]
        };

        var history = new SnapshotHistory();

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[0].Name =
                    "Landing Light";
                state.ConfigItems[0].Active = false;
            },
            "Edit Config Item");

        Assert.AreEqual(0, history.UndoCount);
        Assert.AreEqual(0, history.RedoCount);
        Assert.IsFalse(history.CanUndo);
    }

    [TestMethod]
    public void Execute_NoOpPreserveExistingRedoHistory()
    {
        var project = new ProjectState
        {
            ConfigItems =
            [
                new ConfigItem
                {
                    Name = "Landing Light",
                    Active = false
                }
            ]
        };

        var history = new SnapshotHistory();

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[0].Active = true;
            },
            "Toggle Active");

        history.Undo(project);

        Assert.IsTrue(history.CanRedo);

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[0].Name =
                    "Landing Light";
            },
            "No-op Edit");

        Assert.AreEqual(0, history.UndoCount);
        Assert.AreEqual(1, history.RedoCount);
        Assert.AreEqual(
            "Toggle Active",
            history.RedoEntryDetails[0]);
    }

    [TestMethod]
    public void Execute_FailedMutationRestoresProjectAndHistory()
    {
        var project = new ProjectState
        {
            ConfigItems =
            [
                new ConfigItem
            {
                Name = "First",
                Active = false
            },
            new ConfigItem
            {
                Name = "Second",
                Active = false
            }
            ]
        };

        var history = new SnapshotHistory();

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[0].Active = true;
            },
            "Toggle First");

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[1].Active = true;
            },
            "Toggle Second");

        history.Undo(project);

        var undoBefore =
            history.UndoEntryDetails.ToArray();

        var redoBefore =
            history.RedoEntryDetails.ToArray();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => history.Execute(
                project,
                state =>
                {
                    state.ConfigItems[0].Name =
                        "Broken Name";

                    state.ConfigItems.RemoveAt(1);

                    throw new InvalidOperationException(
                        "Simulated mutation failure");
                },
                "Failed Edit"));

        Assert.HasCount(2, project.ConfigItems);

        Assert.AreEqual(
            "First",
            project.ConfigItems[0].Name);

        Assert.AreEqual(
            "Second",
            project.ConfigItems[1].Name);

        Assert.IsTrue(project.ConfigItems[0].Active);
        Assert.IsFalse(project.ConfigItems[1].Active);

        CollectionAssert.AreEqual(
            undoBefore,
            history.UndoEntryDetails.ToArray());

        CollectionAssert.AreEqual(
            redoBefore,
            history.RedoEntryDetails.ToArray());
    }

    [TestMethod]
    public void HistoryJump_PreservesOriginalActionNames()
    {
        var project = new ProjectState
        {
            ConfigItems =
            [
                new ConfigItem { Name = "First" },
                new ConfigItem { Name = "Second" },
                new ConfigItem { Name = "Third" }
            ]
        };

        var history = new SnapshotHistory();

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[0].Active = true;
            },
            "Toggle First");

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[1].Active = true;
            },
            "Toggle Second");

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems[2].Active = true;
            },
            "Toggle Third");

        Assert.IsTrue(history.UndoTo(project, 2));

        Assert.AreEqual(
            "History Jump (2 actions)",
            history.RedoEntryDetails[0]);

        Assert.IsTrue(history.Redo(project));

        CollectionAssert.AreEqual(
            new[]
            {
                "Toggle Third",
                "Toggle Second",
                "Toggle First"
            },
            history.UndoEntryDetails.ToArray());

        Assert.IsTrue(history.UndoTo(project, 3));
        Assert.IsTrue(history.Redo(project));

        CollectionAssert.AreEqual(
            new[]
            {
                "Toggle Third",
                "Toggle Second",
                "Toggle First"
            },
            history.UndoEntryDetails.ToArray());
    }
}