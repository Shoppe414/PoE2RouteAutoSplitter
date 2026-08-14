# Custom Route — Exploration + Boss Rush

Define a route containing any supported mixture of **area** and **boss** objectives.

## Route file

Edit `poe2_mixed_route.txt` and copy it beside `LiveSplit.exe`.

```text
@start=G1_1
@order=unordered
area|G1_town
boss|the_bloated_miller
area|G1_2
boss|beira_of_the_rotten_pack
```

- `@start=<area id>` normally auto-starts when that area is detected. Special case: `@start=G1_1` arms the Riverbank start gate and timing begins on the Wounded Man final opening Client.txt line. Use `@start=manual` for manual start.
- `@order=unordered` accepts configured objectives in any order.
- `@order=ordered` accepts only the objective matching the current LiveSplit row.
- `area|<id>` uses the Exploration detector in `Client.txt`.
- `boss|<id>` uses BossWatcher's `poe2_boss_events.log`.

After editing the route, create a matching `.lss` with:

```powershell
.\Gen-MixedLayout.ps1 -RouteFile .\poe2_mixed_route.txt -OutputLss '.\My-Mixed.lss'
```

The supplied example layout has four rows and matches the supplied example route. Load only one ASL component at a time. Boss objectives require `BossWatcher` to be running.

## Optional area completion mode

Custom routes continue to use entry-based area objectives by default. Advanced hand-edited routes may add `@areaCompletion=successor` when the `area|...` lines form a deliberate progression sequence. In that mode, an area is armed when entered and completes only when the next listed area is entered; unrelated detours and revisits do not trigger it. The Setup UI does not enable this automatically for custom routes.

## Game Time

Game Time removes Path of Exile 2 `Client.txt` loading-screen durations automatically. GameTimeWatcher is only required if the optional manual-pause removal setting is enabled during setup.
