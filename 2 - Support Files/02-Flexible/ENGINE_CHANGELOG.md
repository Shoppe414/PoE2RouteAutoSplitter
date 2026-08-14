# Changelog

## v0.3.0

Milestone transition from ordered routes to flexible first-visit area pools.

### Added
- Hierarchical Scriptable Auto Splitter settings:
  - Act 1
  - Act 2
  - Act 3
  - Act 4
  - Interludes
- Individual checkbox for every one of the 97 default campaign area splits.
- Unordered first-visit completion tracking.
- Dynamic LiveSplit slot naming based on actual completion order.
- Independent subgroup completion counters in the status file.
- `poe2_area_groups.txt` reference file.
- Flexible 97-slot + Ziggurat `.lss`.
- Unordered Client.txt replay test tool.

### Changed
- `poe2_route.txt` is no longer used by the v0.3 script.
- No area is treated as "next expected."
- Revisits are ignored based on a completed-area set rather than route position.
- Act 4 and all three Interludes may be progressed in arbitrary order without desynchronizing the script.
- Ziggurat finish logic is generalized for both:
  - Cuachic-as-final-area runs;
  - Cuachic-completed-earlier runs.

### Preserved
- Client.txt area parsing.
- Riverbank auto-start.
- validation CSV and unknown-area capture.
- explicit Ziggurat finish behavior derived from stable v0.2.15.
- Deserted Post remains reference-only/non-splittable.

### Known limitations
- Default `.lss` assumes all 97 area settings are enabled.
- Manual Skip Segment is not supported by the unordered completion model.
- PB comparison rows are slot-based when route order changes.
