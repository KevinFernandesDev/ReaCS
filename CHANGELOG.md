# 📦 Changelog

## [1.1.1] – 2025-05-26 — Link Tree Graph & LinkSO/ObservableScriptableObject Visualization

### ✨ Added

* **`LinkTreeGraphView`**:
  A fully recursive visual explorer for `LinkSO<TLeft, TRight>` relationships.

  * Starting from any `ObservableScriptableObject`, it renders a directional graph showing how objects are linked.
  * Highlights the root node.
  * Pressing `P` on a node pings the asset in the Project window.
  * Built-in **MiniMap**, zoom, and pan support.
  * Automatically centers view after generating the graph.

* **Auto-open Link Graph**:
  Selecting an `ObservableScriptableObject` in the **Project window** automatically opens the Link Tree Graph.

* **Traversal Enhancements**:

  * Prevents infinite cycles and redundant links.
  * Follows `TLeft ➔ TRight` directionality for visual clarity.
  * Skips links already visually expressed in the graph.
  * When a node is expanded, it spawns a new tree section rather than reusing global shared nodes.

### 🛠 Changed

* **SO Inspector UI**:

  * Removed the "Open Link Tree Graph" button.
  * Replaced with static display showing how many links are present for the current `ObservableScriptableObject`.

* **Graph Layout Improvements**:

  * Added `FrameGraphToCenter()` after layout generation to ensure nodes are centered and spaced correctly.
  * Fixed cases where nodes would render off-screen or to the top-left due to early layout timing.

### 🐛 Fixed

* 🧠 Nodes are no longer duplicated when expanding new links (unless explicitly allowed for visual clarity).
* ♻ The root node is no longer overridden or skipped during recursive expansion.
* 🎯 `PingObject` now works correctly for all node types and expansion states.
* 🎨 Nodes are placed consistently and logically with vertical spacing depending on number of links.


##  [1.1.0] 26/05/2025 — Entity-Based Binding Overhaul

### ✨ Added
- **`ComponentDataBinding<TSO, TUC>`**
  - Binds an `ObservableScriptableObject` (TSO) to a Unity Component (TUC).
  - Auto-resolves the Unity `component` via (TUC) generic and recommended `[RequireComponent]`.
  - Supports a single `dataSource` field with a `useAsTemplate` toggle.
    - If `dataSource` is not filled, instantiate (TSO) with no data and pool it.
    - If filled and `useAsTemplate` = true, instantiate (TSO) and copy data from `dataSource` to `data`, then pool
    - If filled and `useAsTemplate` = false, instantiate (TSO) and `data` = `dataSource` to use project-bound SO and 
      fields auto-save features so data is saved across playthroughs
  - Automatically registers to:
    - `SharedEntityIdService` (for shared `EntityId`)
    - `ComponentDataBindingService<TSO>`
    - `ReaCSIndexRegistry`

- **`SharedEntityIdService`**
  - Auto-assigns a stable `EntityId` to each GameObject hierarchy.
  - Used to group all components in a hierarchy into a logical entity.

- **`EntityId` struct**
  - Replaces `EntitySO` entirely.
  - Compact, Burst-safe, integer-based entity reference.

- **`ComponentDataBindingService<TSO>`**
  - Fast SO ➝ MonoBehaviour lookup at runtime.
  - Enables system-to-UnityComponent access via entity or SO.

- **`UnifiedObservableRegistry`**
  - Combines `ObservableRegistry` (editor) and `ReaCSIndexRegistry` (runtime).
  - Key methods:
    - `GetAll<T>()` auto-resolves editor/runtime mode.
    - `GetByEntity<T>(EntityId)`
    - `GetBindingsForEntity<T, TBinding>()`
    - `BuildNativeLookup<T, TField>()` — Burst-compatible NativeArray export.

- **`PoolService<T>`**
  - Replacement for `ReaCSPool<T>`.
  - Accessed via `Use<PoolService<T>>()` for clean pooling.

- **LinkSO cleanup logic** in `ReaCSIndexRegistry` and editor tool:
  - Removes broken links referencing null SOs.
  - Editor utility auto-cleans invalid LinkSO assets using `LinkedSOLookup`.

- **EntityId-to-ComponentDataBinding query path**
  - Systems can now go from `EntityId` ➝ `SO` ➝ `MonoBehaviour` ➝ UnityComponent in 1 step.

### 🛠 Changed
- `ComponentDataBinding<TSO>`:
  - Now supports optional `dataSource` with `useAsTemplate` toggle instead of separate static/template fields.
  - Only one SO field required.
- SOs that implement `IHasEntityId` now receive their `EntityId` automatically.
- Systems no longer require manual links or `EntitySO`-based ownership modeling.
- Renamed `ComponentDataBindingLookup<T>` to `ComponentDataBindingService<T>` for clarity and consistency with other `Use<T>()`-based services.

### 🧹 Removed / Deprecated
- ❌ `EntitySO` — replaced by `ReaCSEntityId`
- ❌ `IHasData`, `IHasStaticData` — replaced by `IHasDataSource<T>`
- ❌ `ReaCSPool<T>` — replaced by `PoolService<T>`
- Deprecated usage of `ObservableRegistry` directly in favor of `UnifiedObservableRegistry` for runtime queries.

### 🐛 Fixed
- 🧠 Runtime systems now persist across domain reloads in play mode.
- ✅ All systems and bindings auto-reconnect to SOs using new `SharedEntityIdService`.
- ✅ `OnChanged` / `MarkDirty` / visual graph pulses work after reloads.
- 🔁 Pooled SOs correctly re-acquire entity link and name after recycling.
