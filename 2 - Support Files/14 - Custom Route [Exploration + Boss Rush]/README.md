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

- `@start=<area id>` auto-starts when that area is detected. Use `@start=manual` for manual start.
- `@order=unordered` accepts configured objectives in any order.
- `@order=ordered` accepts only the objective matching the current LiveSplit row.
- `area|<id>` uses the Exploration detector in `Client.txt`.
- `boss|<id>` uses BossWatcher's `poe2_boss_events.log`.

After editing the route, create a matching `.lss` with:

```powershell
.\Generate-MixedLayout.ps1 -RouteFile .\poe2_mixed_route.txt -OutputLss '.\Path of Exile 2 - My Mixed Route.lss'
```

The supplied example layout has four rows and matches the supplied example route. Load only one ASL component at a time. Boss objectives require `BossWatcher [Boss Rush Detection]` to be running.
