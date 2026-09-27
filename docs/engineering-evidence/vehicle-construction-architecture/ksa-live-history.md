# Authenticated KSA history — checked 2026-09-20

Read-only Discord live-changelog, authenticated after Project Control sign-in. Latest visible revision5467. Installed source is5438. Revision labels take precedence over Discord posting dates;5434 was posted after the installed file version date. Newer entries below are **NEWER HISTORY — NOT CURRENT INSTALLED BEHAVIOR**.

- [Revision 4994](https://discord.com/channels/1260011486735241329/1260112103134724146/1529715754809688156) (2026-07-23): Connector capability domains and explicit engine feed containers/connectors introduced; electricity/service-fluid defaults permissive.
- [Revision 4918](https://discord.com/channels/1260011486735241329/1260112103134724146/1526773903622672447) (2026-07-14): Same-stage fuel-link reachability and decoupler branch restrictions corrected.
- [Revision 4925](https://discord.com/channels/1260011486735241329/1260112103134724146/1526801294973472799) (2026-07-15): Save load ordering corrected: resource managers rebuilt after tank contents applied.
- [Revision 4939](https://discord.com/channels/1260011486735241329/1260112103134724146/1527143939969384449) (2026-07-15): Disabled propellant stores excluded; editor entry distinguishes unconfigured from drained stores.
- [Revision 4958](https://discord.com/channels/1260011486735241329/1260112103134724146/1528603524840030243) (2026-07-19): Availability-based staging estimate and engine farthest-first by stage; estimate superseded by4966.
- [Revision 4959](https://discord.com/channels/1260011486735241329/1260112103134724146/1528603528803778611) (2026-07-19): Flow-rule persistence across save/load.
- [Revision 4966](https://discord.com/channels/1260011486735241329/1260112103134724146/1528957279096012994) (2026-07-20): Planner uses event-driven concurrent depletion instead of assigning whole reachable tanks to first sequence.
- [Revision 5022](https://discord.com/channels/1260011486735241329/1260112103134724146/1530713652024443026) (2026-07-25): Save/load and exiting editor no longer automatically refill consumed resources.
- [Revision 5080](https://discord.com/channels/1260011486735241329/1260112103134724146/1531892380301787227) (2026-07-29): Fuel-link edits trigger reconfiguration; deleted ports no dangling edges.
- [Revision 5128](https://discord.com/channels/1260011486735241329/1260112103134724146/1533637200137162935) (2026-08-02): Editor rotations changed to part-local axes.
- [Revision 5172](https://discord.com/channels/1260011486735241329/1260112103134724146/1534431004146536620) (2026-08-05): Fuel hose geometry edited through route nodes; does not replace explicit flow relation.
- [Revision 5328](https://discord.com/channels/1260011486735241329/1260112103134724146/1539789250126413926) (2026-08-19): Electrical connected components and rotating battery cursor replace per-consumer resource graphs; old power graph diagnostic only.
- [Revision 5331](https://discord.com/channels/1260011486735241329/1260112103134724146/1539882019348287551) (2026-08-20): Uniform scale supersedes triaxial; module-level sequences and self-nominated inert mass; duplicate module IDs diagnosed.
- [Revision 5363](https://discord.com/channels/1260011486735241329/1260112103134724146/1542054861997350944) (2026-08-26): Power switch ownership and bounds prevent detached stale control.
- [Revision 5386](https://discord.com/channels/1260011486735241329/1260112103134724146/1544482567175274648) (2026-09-01): Editor save validation and overwrite lifetime corrected.
- [Revision 5412](https://discord.com/channels/1260011486735241329/1260112103134724146/1546296424269090828) (2026-09-06): Authored mass added to collider-bearing battery/solar/gear/decoupler parts; visuals/colliders alone not mass.
- [Revision 5434](https://discord.com/channels/1260011486735241329/1260112103134724146/1548873113746411602) (2026-09-13): Reachability/reactant-level shares, disabled stores, pooled-graph double disposal and placement-dependent flow fixed. History describes shared planner/live drain path; current5438 method traces still show distinct live sequential consumption and planner atomic event preparation. Source wins.
- [Revision 5441](https://discord.com/channels/1260011486735241329/1260112103134724146/1549215441472331887) (2026-09-14; NEWER HISTORY): Editor engine inspector displays design-point quantities.
- [Revision 5454](https://discord.com/channels/1260011486735241329/1260112103134724146/1550369807260590142) (2026-09-18; NEWER HISTORY): Overwrite failure preserves old save and dirty flag; path scope guard.
- [Revision 5457](https://discord.com/channels/1260011486735241329/1260112103134724146/1550450712499200031) (2026-09-18; NEWER HISTORY): Per-part render dirtiness and high-part-count allocation work.
- [Revision 5464](https://discord.com/channels/1260011486735241329/1260112103134724146/1551394196353589269) (2026-09-20; NEWER HISTORY): Non-vehicle targets survive save/load.
- [Revision 5465](https://discord.com/channels/1260011486735241329/1260112103134724146/1551403512531783742) (2026-09-20; NEWER HISTORY): Broader lazy PartTree derivation and one-per-tree-per-frame flow-manager rebuild; batched structural work. Installed5438 already has recomputeDerivedData parameters: do not attribute first introduction to5465.
- [Revision 5467](https://discord.com/channels/1260011486735241329/1260112103134724146/1551407223245242470) (2026-09-20; NEWER HISTORY): Runtime generated glint deregistration fixes lifetime leak while preserving catalog-lifetime assets.

No raw history dump retained. Installed ownership traces and the explicit supersession notes above decide implementation, not historical performance claims.

