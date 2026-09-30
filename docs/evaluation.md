# Undo/Redo Approach Evaluation

This document summarizes the findings from the current Undo/Redo spike implementations and the exploratory benchmark experiments performed so far.

The evaluated approaches are:

- Command
- Snapshot
- Patch
- Hybrid

The current Hybrid prototype represents:

```
Semantic Action + PatchTransaction
```

It does not yet select between Patch and Snapshot representations depending on the mutation type.

## Comparison

| Criterion | Command | Snapshot | Patch | Hybrid |
|---|---|---|---|---|
| Stored history data | Action-specific command state such as target reference, previous/resulting values, deleted object and original index | Full cloned ProjectState for each history entry | PatchTransaction containing operation-specific before/after data | Semantic action name + PatchTransaction containing operation-specific data |
| History granularity | One semantic command per committed user action | One snapshot per committed mutation | One PatchTransaction per committed user action | One semantic HybridHistoryEntry per committed user action |
| Undo complexity | Each command must implement its own inverse logic | Generic state restoration; no action-specific inverse logic required | Each patch type implements its inverse; transactions compose multiple patches | Delegates reversal to PatchTransaction while retaining semantic action metadata |
| Redo complexity | Each command implements explicit Redo behavior | Generic snapshot restoration | Re-applies the stored PatchTransaction | Re-applies the stored PatchTransaction |
| Object identity | Can preserve object identity when the command stores/restores the original object | Current implementation restores cloned objects, so reference identity is not preserved | Can preserve identity when patches retain original object references | Inherits object-identity behavior from the underlying patches |
| Ordering restoration | Must explicitly store and restore collection position when required | Collection order is contained in the snapshot | Explicitly represented by operations such as move or remove with stored index | Inherits explicit ordering restoration from the underlying patches |
| Compound edits | Requires a dedicated command or additional command-specific state and logic | Naturally represented by one snapshot regardless of mutation complexity | Multiple reusable patches can be grouped into one PatchTransaction | Multiple patches are grouped into one semantic history entry |
| Broad / bulk edits | One command can represent the complete user action, but the command must explicitly store all required reversal data | One full-state snapshot represents the whole bulk mutation | One transaction can contain many fine-grained patches | One semantic entry can contain many fine-grained patches |
| Create / duplicate | Dedicated DuplicateConfigItemCommand stores the created item and insertion position | Generic mutation is captured by a full-state snapshot | AddConfigItemPatch stores the created item and insertion index | Semantic `Duplicate Config Item` entry delegates reversal to AddConfigItemPatch |
| Move / reorder | Dedicated MoveConfigItemCommand stores item identity and source/target positions | Generic list mutation is captured by a full-state snapshot | MoveConfigItemPatch stores item identity and source/target positions | Semantic `Move Config Item` entry delegates reversal to MoveConfigItemPatch |
| Draft handling | Not evaluated; current prototype records committed backend actions | Not evaluated; current prototype records committed backend actions | Not evaluated; current prototype records committed backend actions | Not evaluated; current prototype records committed backend actions |
| Side effects | Not evaluated; current prototype only mutates ProjectState | Not evaluated; current prototype only restores ProjectState | Not evaluated; current prototype only represents ProjectState mutations | Not evaluated; current prototype only represents ProjectState mutations |
| Memory implications | Primarily proportional to the reversal data required by each action | Proportional to snapshot scope × history depth; full-state prototype copies every ConfigItem for each entry | Primarily proportional to the number and size of stored patch operations | Similar to Patch plus semantic action metadata |
| Implementation effort | Simple for small actions, but each new action may require a new command and custom Execute/Undo/Redo logic | Undo/Redo mechanism is generic, but requires cloning/restoration infrastructure | Requires reusable patch types and transaction handling; existing patches can be reused across actions | Adds semantic history abstraction around Patch; new actions can reuse existing patch types when applicable |
| Extensibility | Strong semantic model, but action count can increase the number of command classes | New mutations generally require no new history type as long as the snapshot contains the affected state | New actions can compose existing operations, but new mutation kinds may require new patch types | New semantic actions can reuse existing patches; broader mutation strategies such as targeted Snapshot are not yet evaluated |

## Current Mutation Coverage

The spike now contains implementations for the following mutation patterns:

```
Update
-> Toggle Active

Delete
-> Delete Config Item

Compound
-> Name + Active + Move

Broad Update
-> Bulk Toggle

Broad Delete
-> Bulk Delete

Create
-> Duplicate Config Item

Move / Reorder
-> Move First -> Last
```

Create and Move / Reorder are now represented in all four approaches. No performance conclusions are drawn for those two scenarios yet because the benchmark data in this document focuses on Toggle and Delete workloads.

## Benchmark Methodology

All timing measurements are exploratory spike measurements and should not be interpreted as production-grade performance benchmarks.

The original Bulk Action measurements were collected five times per approach and operation. Several result sets contain clear warm-up/runtime outliers, especially in early measurements. For that reason, median values are used for comparison.

A later Direct-Reference control experiment used two measurement procedures:

1. five Execute measurements, followed by five Undo measurements, followed by five Redo measurements;
2. repeated independent cycles of `Reset -> Execute -> Undo -> Redo`.

The second procedure is considered the better methodology because each cycle starts from the same initial state and history depth. Therefore, the cycle-based medians are used as the primary Direct-Reference comparison. The sequential results are retained as supporting raw data.

For future experiments, the preferred measurement procedure is:

```
1 unrecorded warm-up cycle
-> Reset
-> Execute
-> Undo
-> Redo

then 10 measured cycles:
Reset
-> Execute
-> Undo
-> Redo
-> record all three values
```

The median should remain the primary summary statistic.

The later Indexed Lookup and Stored-Index Bulk Delete experiments used 10 measured independent cycles. Indexed Bulk Toggle includes dictionary construction in the Execute timing.

## Performance Observations

### Earlier Individual Toggle Baseline

The earlier experiment executed 100 individual Toggle actions while varying ProjectState size.

Median execution time:

| Approach | 100 Items | 1000 Items |
|---|---:|---:|
| Command | 0.006 ms | 0.006 ms |
| Snapshot | 0.902 ms | 6.995 ms |
| Patch | 0.038 ms | 0.038 ms |
| Hybrid | 0.070 ms | 0.042 ms |

The important observation was the effect of ProjectState size.

The full-state Snapshot prototype cloned the complete ProjectState once for every individual Toggle action. Its cost therefore increased substantially as the state grew from 100 to 1000 ConfigItems.

Command, Patch, and Hybrid did not show comparable state-size-dependent growth for this localized workload.

### Bulk Toggle

Bulk Toggle changes the Active state of every ConfigItem but records the complete user action as one history entry.

#### 100 Items - Median

| Approach | Execute | Undo | Redo |
|---|---:|---:|---:|
| Command | 0.0150 ms | 0.0065 ms | 0.0068 ms |
| Snapshot | 0.0220 ms | 0.0300 ms | 0.0343 ms |
| Patch - ID lookup | 0.0400 ms | 0.0412 ms | 0.0390 ms |
| Hybrid - ID lookup | 0.0610 ms | 0.0433 ms | 0.0404 ms |

#### 1000 Items - Median

| Approach | Execute | Undo | Redo |
|---|---:|---:|---:|
| Command | 0.0628 ms | 0.0333 ms | 0.0199 ms |
| Snapshot | 0.0913 ms | 0.2571 ms | 0.2677 ms |
| Patch - ID lookup | 1.7906 ms | 4.6173 ms | 4.3340 ms |
| Hybrid - ID lookup | 1.8936 ms | 2.8551 ms | 1.8171 ms |

The current Command implementation scales well for this scenario because BulkToggleCommand stores direct ConfigItem references and before/after values, then restores them with direct assignments.

Snapshot behaves differently from the earlier individual-toggle experiment. A Bulk Toggle is one committed action, so the current implementation creates one complete ProjectState snapshot rather than one snapshot per item. This makes full-state Snapshot comparatively competitive for a broad single action even though it still copies the complete state.

The original Patch and Hybrid implementations create one ReplaceActivePatch per ConfigItem. ReplaceActivePatch locates its target by ID using a linear search through ProjectState.ConfigItems. A broad mutation therefore performs many repeated list searches.

At this point, the original benchmark could not distinguish whether the observed cost came mainly from:

```
many Patch operations
```

or:

```
many Patch operations
+
one linear target lookup per Patch
```

A control experiment was therefore added.

## Direct-Reference Patch Control Experiment

### Purpose

ReplaceActiveReferencePatch keeps the same fine-grained representation:

```
one Patch operation per ConfigItem
```

but removes repeated ID lookup by storing a direct ConfigItem reference.

Conceptually:

```
Original Patch
Guid
-> linear search through ConfigItems
-> modify item

Direct-Reference control
ConfigItem reference
-> modify item directly
```

This experiment is intended to isolate target-resolution cost. Direct object references are not yet proposed as the final production design because references can become stale if another history representation replaces object instances.

### Primary Results - Independent Reset / Execute / Undo / Redo Cycles

#### 100 Items

| Approach | Execute | Undo | Redo |
|---|---:|---:|---:|
| Patch - direct reference | 0.0127 ms | 0.0081 ms | 0.0082 ms |
| Hybrid - direct reference | 0.0151 ms | 0.0083 ms | 0.0086 ms |

#### 1000 Items

| Approach | Execute | Undo | Redo |
|---|---:|---:|---:|
| Patch - direct reference | 0.0470 ms | 0.0281 ms | 0.0286 ms |
| Hybrid - direct reference | 0.0446 ms | 0.0274 ms | 0.0290 ms |

### 1000-Item Comparison With Original ID Lookup

| Approach | Execute | Undo | Redo |
|---|---:|---:|---:|
| Patch - ID lookup | 1.7906 ms | 4.6173 ms | 4.3340 ms |
| Patch - direct reference | 0.0470 ms | 0.0281 ms | 0.0286 ms |
| Hybrid - ID lookup | 1.8936 ms | 2.8551 ms | 1.8171 ms |
| Hybrid - direct reference | 0.0446 ms | 0.0274 ms | 0.0290 ms |

In this prototype, removing repeated linear lookup reduced the median 1000-item Patch timings by approximately:

```
Execute: 38x
Undo:    164x
Redo:    152x
```

For Hybrid, the corresponding differences were approximately:

```
Execute: 42x
Undo:    104x
Redo:     63x
```

These ratios describe this specific implementation and benchmark setup; they should not be generalized as universal speed-up factors.

The stronger conclusion is algorithmic:

> In the current prototype, repeated linear target lookup was the dominant cost in the original 1000-item Bulk Toggle implementation.

Keeping 1000 fine-grained Patch operations while eliminating the linear lookup reduced the operation to roughly 0.03-0.05 ms in the cycle-based measurements.

Therefore, the number of Patch operations alone did not explain the original millisecond-level results.

### Comparison Against Command and Snapshot

For the 1000-item Bulk Toggle:

| Approach | Execute | Undo | Redo |
|---|---:|---:|---:|
| Command | 0.0628 ms | 0.0333 ms | 0.0199 ms |
| Snapshot | 0.0913 ms | 0.2571 ms | 0.2677 ms |
| Patch - direct reference | 0.0470 ms | 0.0281 ms | 0.0286 ms |
| Hybrid - direct reference | 0.0446 ms | 0.0274 ms | 0.0290 ms |

After removing the repeated lookup, Patch and Hybrid returned to the same small timing range as Command and were below Snapshot in this particular broad-toggle experiment.

This changes the interpretation of the earlier Bulk Toggle benchmark.

The earlier result does **not** support the conclusion:

```
Broad mutation
-> Patch is inherently inefficient
-> Snapshot should therefore be preferred
```

The evidence instead supports:

```
Broad mutation performance
depends strongly on target-resolution strategy.

Fine-grained Patch history can remain inexpensive
when targets can be resolved efficiently.
```

### Why Direct References Are Only a Control

Direct references provide a useful control because they eliminate lookup almost completely, but they introduce an important design risk.

For example:

```
Patch stores reference to ConfigItem A
-> another mechanism restores/clones ProjectState
-> current ProjectState now contains ConfigItem A'
-> stored reference still points to old ConfigItem A
```

This is especially relevant if a future Hybrid implementation mixes Patch and Snapshot entries.

Therefore, the Direct-Reference Patch is evidence about lookup cost, not yet the preferred production representation.

## Indexed ID Lookup Control

A follow-up experiment kept Guid-based logical identity but replaced repeated linear lookup with a dictionary-based lookup.

Median results:

| Items | Approach | Execute | Undo | Redo |
|---:|---|---:|---:|---:|
| 100 | Patch - indexed | 0.0254 ms | 0.0120 ms | 0.0136 ms |
| 100 | Hybrid - indexed | 0.0210 ms | 0.0154 ms | 0.0102 ms |
| 1000 | Patch - indexed | 0.0902 ms | 0.0477 ms | 0.0526 ms |
| 1000 | Hybrid - indexed | 0.1114 ms | 0.0535 ms | 0.0521 ms |

For 1000 items, indexed lookup is much faster than the original linear ID lookup while retaining Guid-based targeting. It is somewhat slower than direct references, which is expected because dictionary lookup adds overhead. Execute also includes the cost of creating the dictionary.

This supports the conclusion that the poor scaling of the original Bulk Toggle Patch/Hybrid implementation was mainly caused by repeated linear target lookup rather than by the number of Patch operations itself.

## Bulk Delete

Bulk Delete removes all active ConfigItems and records the complete deletion as one history entry.

#### 100 Items - Median

| Approach | Execute | Undo | Redo |
|---|---:|---:|---:|
| Command | 0.0687 ms | 0.0340 ms | 0.0527 ms |
| Snapshot | 0.0654 ms | 0.0438 ms | 0.0281 ms |
| Patch | 0.2810 ms | 0.0139 ms | 0.0298 ms |
| Hybrid | 0.0810 ms | 0.0141 ms | 0.0374 ms |

#### 1000 Items - Median

| Approach | Execute | Undo | Redo |
|---|---:|---:|---:|
| Command | 0.1632 ms | 0.0775 ms | 2.4196 ms |
| Snapshot | 0.1484 ms | 0.1405 ms | 0.1409 ms |
| Patch | 1.3968 ms | 0.0806 ms | 1.9467 ms |
| Hybrid | 1.3031 ms | 0.0459 ms | 1.3286 ms |

The original Patch/Hybrid implementation searched the list by ID before every removal. A stored-index control removed that repeated search while preserving the same broad Delete action.

#### 1000 Items - Stored Index Control

| Approach | Execute | Undo | Redo |
|---|---:|---:|---:|
| Patch - stored index | 0.2912 ms | 0.0616 ms | 0.0697 ms |
| Hybrid - stored index | 0.2283 ms | 0.1110 ms | 0.0519 ms |

Removing repeated lookup greatly reduced Execute and especially Redo cost. Unlike Bulk Toggle, Bulk Delete still performs repeated `RemoveAt` / `Insert` operations, so collection mutation and element shifting remain part of the cost.

The Hybrid Undo median was higher in this run than in the earlier baseline, but the algorithm is still the same stored-index insertion path; this single difference is not treated as a separate architectural finding.

## Raw Bulk Measurements

Each row contains five measurements in milliseconds in execution order.

### Bulk Toggle - Execute

| Items | Approach | Runs |
|---|---|---|
| 100 | Command | 0.0260, 0.0130, 0.0150, 0.0160, 0.0110 |
| 100 | Snapshot | 0.0250, 0.0220, 0.0180, 0.0180, 0.0230 |
| 100 | Patch | 0.0400, 0.0890, 0.0390, 0.0390, 0.0910 |
| 100 | Hybrid | 0.0600, 0.0700, 0.2230, 0.0320, 0.0610 |
| 1000 | Command | 1.0774, 0.0729, 0.0585, 0.0470, 0.0628 |
| 1000 | Snapshot | 0.5891, 0.0913, 0.1021, 0.0907, 0.0861 |
| 1000 | Patch | 3.0586, 3.3539, 1.7906, 1.7740, 1.7773 |
| 1000 | Hybrid | 1.8936, 1.7366, 2.2803, 2.2559, 1.7823 |

### Bulk Toggle - Undo

| Items | Approach | Runs |
|---|---|---|
| 100 | Command | 0.0065, 0.0068, 0.0063, 0.0062, 0.0083 |
| 100 | Snapshot | 0.3270, 0.0293, 0.0300, 0.0292, 0.0302 |
| 100 | Patch | 0.4753, 0.0372, 0.0412, 0.0366, 0.1356 |
| 100 | Hybrid | 0.1849, 0.0891, 0.0430, 0.0325, 0.0433 |
| 1000 | Command | 0.3435, 0.0550, 0.0269, 0.0245, 0.0333 |
| 1000 | Snapshot | 0.8881, 0.2187, 0.1721, 0.2571, 0.3651 |
| 1000 | Patch | 9.0421, 4.7426, 1.8236, 1.8708, 4.6173 |
| 1000 | Hybrid | 2.5705, 2.8551, 1.8231, 7.3479, 5.1991 |

### Bulk Toggle - Redo

| Items | Approach | Runs |
|---|---|---|
| 100 | Command | 0.2448, 0.0551, 0.0067, 0.0059, 0.0068 |
| 100 | Snapshot | 0.1258, 0.0339, 0.0343, 0.0263, 0.0457 |
| 100 | Patch | 0.1200, 0.0308, 0.0390, 0.0412, 0.0377 |
| 100 | Hybrid | 0.1234, 0.0367, 0.0404, 0.1258, 0.0350 |
| 1000 | Command | 0.2713, 0.0543, 0.0199, 0.0194, 0.0184 |
| 1000 | Snapshot | 0.2677, 0.1747, 0.4008, 0.1682, 0.4234 |
| 1000 | Patch | 5.1339, 1.8468, 9.2158, 2.2535, 4.3340 |
| 1000 | Hybrid | 1.8171, 1.8323, 1.6972, 4.0323, 1.7351 |

### Bulk Delete - Execute

| Items | Approach | Runs |
|---|---|---|
| 100 | Command | 2.6013, 0.0961, 0.0687, 0.0337, 0.0347 |
| 100 | Snapshot | 1.0944, 0.0692, 0.0654, 0.0408, 0.0487 |
| 100 | Patch | 1.2381, 0.5029, 0.1355, 0.2810, 0.0682 |
| 100 | Hybrid | 0.2325, 0.0734, 0.2028, 0.0792, 0.0810 |
| 1000 | Command | 0.1876, 0.1758, 0.1520, 0.1632, 0.1440 |
| 1000 | Snapshot | 0.1420, 0.1783, 0.1484, 0.1698, 0.1410 |
| 1000 | Patch | 1.3968, 1.3716, 1.3326, 1.4874, 1.4519 |
| 1000 | Hybrid | 1.2501, 1.3072, 1.2353, 1.3031, 1.5433 |

### Bulk Delete - Undo

| Items | Approach | Runs |
|---|---|---|
| 100 | Command | 0.0311, 0.0340, 0.0265, 0.0403, 0.1723 |
| 100 | Snapshot | 0.0695, 0.0317, 0.0301, 0.0475, 0.0438 |
| 100 | Patch | 0.0139, 0.0234, 0.0128, 0.0139, 0.0098 |
| 100 | Hybrid | 0.0114, 0.0218, 0.0110, 0.0712, 0.0141 |
| 1000 | Command | 0.0588, 0.0994, 0.0775, 0.0511, 0.0844 |
| 1000 | Snapshot | 0.1305, 0.2882, 0.1342, 0.1405, 0.2907 |
| 1000 | Patch | 0.0918, 0.0806, 0.0886, 0.0707, 0.0494 |
| 1000 | Hybrid | 0.0459, 0.0579, 0.0395, 0.0459, 0.0823 |

### Bulk Delete - Redo

| Items | Approach | Runs |
|---|---|---|
| 100 | Command | 0.0527, 0.1058, 0.0404, 0.0395, 0.1043 |
| 100 | Snapshot | 0.0518, 0.0235, 0.0242, 0.0281, 0.0558 |
| 100 | Patch | 0.0298, 0.0375, 0.0365, 0.0220, 0.0246 |
| 100 | Hybrid | 0.0270, 0.0374, 0.0438, 0.0693, 0.0231 |
| 1000 | Command | 3.0641, 2.2170, 2.4535, 1.4715, 2.4196 |
| 1000 | Snapshot | 0.2364, 0.1409, 0.2692, 0.1361, 0.1350 |
| 1000 | Patch | 4.9678, 2.3710, 1.2086, 1.9467, 1.2381 |
| 1000 | Hybrid | 1.3286, 3.7622, 1.2076, 1.2010, 3.3895 |

## Raw Direct-Reference Measurements

### Sequential Measurement Procedure

In this procedure, five Execute measurements were taken consecutively, then five Undo measurements, then five Redo measurements.

#### 100 Items - Patch Reference

```
Execute: 0.0159, 0.0151, 0.0198, 0.0176, 0.0238
Undo:    0.4228, 0.0198, 0.0149, 0.0729, 0.0116
Redo:    0.0097, 0.0105, 0.0134, 0.0755, 0.0099
```

Median:

```
Execute: 0.0176 ms
Undo:    0.0198 ms
Redo:    0.0105 ms
```

#### 100 Items - Hybrid Reference

```
Execute: 0.2098, 0.0173, 0.0262, 0.0227, 0.0133
Undo:    0.1544, 0.0099, 0.0091, 0.0098, 0.0140
Redo:    0.1002, 0.0125, 0.0092, 0.0104, 0.0103
```

Median:

```
Execute: 0.0227 ms
Undo:    0.0099 ms
Redo:    0.0104 ms
```

#### 1000 Items - Patch Reference

```
Execute: 0.0539, 0.1162, 0.0404, 0.0372, 0.0381
Undo:    0.0273, 0.0223, 0.0246, 0.0391, 0.0226
Redo:    0.0279, 0.0218, 0.0287, 0.0226, 0.0297
```

Median:

```
Execute: 0.0404 ms
Undo:    0.0246 ms
Redo:    0.0279 ms
```

#### 1000 Items - Hybrid Reference

```
Execute: 0.0514, 0.0420, 0.0378, 0.0639, 0.0453
Undo:    0.0254, 0.0219, 0.0720, 0.0325, 0.0259
Redo:    0.0295, 0.0277, 0.0307, 0.0291, 0.0204
```

Median:

```
Execute: 0.0453 ms
Undo:    0.0259 ms
Redo:    0.0291 ms
```

### Independent Cycle Measurement Procedure

Each group below represents five independent `Reset -> Execute -> Undo -> Redo` cycles.

#### 100 Items - Patch Reference

```
Cycle 1: Execute 0.0124 | Undo 0.0073 | Redo 0.0081
Cycle 2: Execute 0.0150 | Undo 0.0124 | Redo 0.0082
Cycle 3: Execute 0.1712 | Undo 0.0184 | Redo 0.0104
Cycle 4: Execute 0.0116 | Undo 0.0081 | Redo 0.0077
Cycle 5: Execute 0.0127 | Undo 0.0074 | Redo 0.0108
```

Median:

```
Execute: 0.0127 ms
Undo:    0.0081 ms
Redo:    0.0082 ms
```

#### 100 Items - Hybrid Reference

```
Cycle 1: Execute 0.0151 | Undo 0.0083 | Redo 0.0130
Cycle 2: Execute 0.0114 | Undo 0.0094 | Redo 0.0080
Cycle 3: Execute 0.0154 | Undo 0.0077 | Redo 0.0129
Cycle 4: Execute 0.0125 | Undo 0.0077 | Redo 0.0086
Cycle 5: Execute 0.0159 | Undo 0.0131 | Redo 0.0081
```

Median:

```
Execute: 0.0151 ms
Undo:    0.0083 ms
Redo:    0.0086 ms
```

#### 1000 Items - Patch Reference

```
Cycle 1: Execute 0.0482 | Undo 0.0241 | Redo 0.0265
Cycle 2: Execute 0.0470 | Undo 0.0267 | Redo 0.0312
Cycle 3: Execute 0.0578 | Undo 0.0391 | Redo 0.0288
Cycle 4: Execute 0.0461 | Undo 0.0281 | Redo 0.0286
Cycle 5: Execute 0.0452 | Undo 0.0305 | Redo 0.0206
```

Median:

```
Execute: 0.0470 ms
Undo:    0.0281 ms
Redo:    0.0286 ms
```

#### 1000 Items - Hybrid Reference

```
Cycle 1: Execute 0.0349 | Undo 0.0274 | Redo 0.0290
Cycle 2: Execute 0.0446 | Undo 0.0311 | Redo 0.0316
Cycle 3: Execute 0.0426 | Undo 0.0293 | Redo 0.0285
Cycle 4: Execute 0.0448 | Undo 0.0271 | Redo 0.0239
Cycle 5: Execute 0.0581 | Undo 0.0246 | Redo 0.0327
```

Median:

```
Execute: 0.0446 ms
Undo:    0.0274 ms
Redo:    0.0290 ms
```

The two procedures produce broadly similar 1000-item Direct-Reference results, which supports the lookup-cost interpretation. The independent-cycle procedure remains preferable for future comparisons.

## History Representation

For the Compound Edit experiment, all approaches represented one committed user action as one Undo history entry.

Command:

```
CompoundEditCommand
```

Snapshot:

```
ProjectState snapshot (3 ConfigItems)
```

Patch:

```
replace Name + replace Active + move ConfigItem
```

Hybrid:

```
Compound Edit [replace Name + replace Active + move ConfigItem]
```

For Bulk Toggle:

Command:

```
BulkToggleCommand
```

Snapshot:

```
ProjectState snapshot
```

Patch:

```
replace Active + replace Active + ...
```

Hybrid:

```
Bulk Toggle [replace Active + replace Active + ...]
```

For Duplicate:

Command:

```
DuplicateConfigItemCommand
```

Snapshot:

```
ProjectState snapshot
```

Patch:

```
add ConfigItem
```

Hybrid:

```
Duplicate Config Item [add ConfigItem]
```

For Move / Reorder:

Command:

```
MoveConfigItemCommand
```

Snapshot:

```
ProjectState snapshot
```

Patch:

```
move ConfigItem
```

Hybrid:

```
Move Config Item [move ConfigItem]
```

Command provides the clearest domain-level representation but requires action-specific reversal logic.

Snapshot stores previous state without requiring knowledge of individual mutations but does not preserve the semantic meaning of the action.

Patch exposes concrete mutations and allows reusable operations to be composed into transactions.

Hybrid retains both the semantic user action and the concrete Patch operations used to reverse it.

## State Size and History Storage

The current Snapshot implementation clones the complete ProjectState before every committed action.

For the earlier 100-individual-Toggle experiment:

```
100 ConfigItems
-> 10,000 stored ConfigItem copies

1000 ConfigItems
-> 100,000 stored ConfigItem copies
```

This growth came from both state size and history depth: 100 committed actions produced 100 snapshots.

For one Bulk Toggle, however:

```
100 ConfigItems
-> one snapshot containing 100 ConfigItems

1000 ConfigItems
-> one snapshot containing 1000 ConfigItems
```

Snapshot cost therefore depends on both snapshot scope and the number of committed history entries.

Command, Patch, and current Hybrid do not store a complete ProjectState copy for each action. Their history storage is mainly related to the reversal data required for that mutation.

For broad Patch/Hybrid actions, the number of low-level operations can grow with the number of affected items, but the Direct-Reference experiment shows that operation count alone did not create the earlier millisecond-level timing behavior.

Formal byte-level memory measurements are still missing.

## Current Findings

### Command

Command gives each history entry clear domain meaning.

It works naturally for local semantic actions such as Toggle Active, Delete Config Item, Duplicate Config Item, and Move Config Item.

Bulk actions can also remain one semantic history entry.

For Bulk Toggle, BulkToggleCommand stores direct ConfigItem references and before/after values, avoiding repeated target lookup during reversal.

The main trade-off is implementation effort: new actions may require new command classes and custom Execute/Undo/Redo logic.

CompoundEditCommand demonstrates how action-specific state grows when one semantic action changes multiple properties and collection positions.

BulkDeleteCommand also shows that implementation detail matters: Undo can restore stored items by index, while Redo currently searches again before removal.

### Snapshot

Snapshot provides the most generic reversal mechanism in the current spike.

A mutation can affect several properties or collection positions without requiring action-specific inverse logic.

Observed disadvantages of the current full-state implementation include:

- storage grows with snapshot scope and history depth
- restore replaces object instances and therefore does not preserve C# reference identity
- history does not describe semantic user intent

The individual-toggle benchmark showed the cost of cloning complete state for every small action.

The Bulk Action benchmark showed the opposite side of the trade-off: one broad action only creates one snapshot, making Snapshot comparatively competitive in that scenario.

The current spike still evaluates only complete ProjectState snapshots; targeted snapshots have not yet been implemented.

### Patch

Patch represents mutations as reusable low-level operations composed into a PatchTransaction.

The original Bulk Toggle and Bulk Delete results were strongly affected by repeated linear target lookup. Direct-reference, indexed-ID, and stored-index controls showed that much of this cost can be removed without changing the one-action / one-history-entry model.

Indexed lookup is particularly relevant because it retains Guid-based logical identity while avoiding repeated full-list scans.

Patch still requires explicit mutation-specific operations, and structural list changes such as Bulk Delete retain collection-shifting cost even after lookup is improved.

### Hybrid

The current Hybrid prototype is still:

```
Semantic Action + PatchTransaction
```

The benchmark controls show that its performance is largely inherited from the underlying Patch implementation; the semantic wrapper itself does not appear to be the main cost.

Hybrid has not yet demonstrated mixed history representations. The next useful step is therefore to let one semantic history support both Patch-backed and Snapshot-backed entries.

## Not Yet Evaluated

The following areas remain open:

- Draft-level Undo versus committed transaction Undo
- Cancel / Escape behavior relative to Undo history
- Native text-field Undo versus global Project Undo
- Side effects outside ProjectState
- Import / Merge-like mutations
- Targeted Snapshot scope
- Mixed Patch / Snapshot Hybrid history
- Formal byte-level memory measurements
- Production-level persistent target resolver / index

## Next Evaluation: Hybrid v2

The lookup experiments are sufficient for the current spike. The next step is to test whether one Hybrid history can support more than one reversal representation.

Minimal Hybrid v2 target:

```
Local / fine-grained mutation
-> PatchTransaction

Broad structural mutation
-> Snapshot-backed entry
```

Bulk Delete is a useful first Snapshot-backed Hybrid action because the current experiments already show different trade-offs between Patch and Snapshot for that workload.

The goal is not to declare Snapshot the best representation for Bulk Delete. The goal is to verify that one semantic Hybrid history can choose different reversal representations while keeping a single user-level Undo/Redo stack.

### Planned Steps

1. Introduce a common Hybrid history operation abstraction.
2. Keep existing Patch-backed entries working unchanged.
3. Add a Snapshot-backed Hybrid entry.
4. Use Snapshot-backed history for one Bulk Delete experiment.
5. Verify Execute / Undo / Redo correctness and one semantic history entry.
6. Update the final evaluation with representation, identity, storage, and implementation trade-offs.

After Hybrid v2, the remaining high-value work is memory/history-size comparison and final documentation. Additional lookup microbenchmarks are not required unless a new implementation question appears.

## Cross-Approach Observation

The experiments increasingly show that the suitability of an Undo/Redo representation depends on several independent factors:

```
mutation shape
+
history granularity
+
target-resolution strategy
+
state-copy scope
+
identity requirements
+
implementation complexity
```

Localized mutations favor compact reversal data.

Broad actions do not automatically favor Snapshot. The direct-reference, indexed-ID, and stored-index controls show that implementation details such as target resolution can dominate benchmark results.

The next architectural question is therefore no longer lookup performance, but whether Hybrid can combine multiple reversal representations cleanly in one semantic history.
