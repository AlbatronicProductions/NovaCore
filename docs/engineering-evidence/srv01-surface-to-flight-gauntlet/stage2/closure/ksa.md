# Current KSA gate: bounded Stage 2 closure

Actual read-only directory: E:\Kitten Space Agency. Rechecked2026-09-17. Build
2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9. All three installed DLL
hashes match the parent Stage2 design (KSA A03E9815...AA8; BEPU77185E51...FA7;
utilities E0A1528D...C68). See parent design for full hashes/token reproduction.
An independent reader freshly inspected these installed methods; no copied
NovaCore account substituted for current implementation.

| Responsibility | Current source / token | Direct live history | Comparison |
|---|---|---|---|
| Thrust enters native velocity | UpdateActiveNozzles06001C59; callback060006BD resolves handle to staged vehicle, force/mass and inverse tensor*torque; Simulate0600069D |4549 outer-step derivative/resource work;5174 activation/feed/shutdown | ADOPT ordinary retained solve; ADAPT canonical prepared command |
| Source properties through solve | FullPhysicsConstrainedStep06001C19; consume06001C5B/06001B46; recompute06001B43 afterward |4549 once-per-outer-step consumption;5434 shared correct drain ownership | ADOPT order; ADAPT exact two-store arithmetic |
| Body/child refresh | Push06000688; CopyToBepu06001B40; UpdateShape0600068E/06002F19 |4428 COM/body-radius refresh | ADOPT same body/shape; ADAPT original material-O continuity |
| Prepared/ready result | constructor06001C4D binds vehicle; Prepare06001BC4 clears ready; PublishResults06001BF8 sets only on no failure; Apply06001BEA requires ready |4549 staged work;5434 lifetime/graph corrections | ADAPT: NovaCore must retain exact stale/revision/replay protections; KSA ready is a Boolean, not CAS |
| Post-burn publication |06002F31 copies NewKinematicStates and optional properties, then06001AB3/0600147C/06003F46/06003F1C copies module state |5434 actual drain consistency | ADAPT atomic physical/resources/properties. KSA typed-state length check logs/skips mismatch; not atomic rollback |
| COM/reference coordinates |06001B32–34 and0600068B direct native copies; new COM child offset pushed next sync |4428 distinguishes body/geometry bounds | ADAPT NovaCore banked O-law. No claim KSA has NovaCore projection remainders |

Authenticated live-changelog directly reread in Codex browser:

-4549: https://discord.com/channels/1260011486735241329/1260112103134724146/1512355160775721020
-4428: https://discord.com/channels/1260011486735241329/1260112103134724146/1506430180674113696
-5174: https://discord.com/channels/1260011486735241329/1260112103134724146/1534461444010016902
-5434: https://discord.com/channels/1260011486735241329/1260112103134724146/1548873113746411602

Supersession: current live view also exposed5441–5448. Editor readouts, renderer,
save lifetime and dynamic ground-clutter fixes do not supersede the freshly read
installed vessel owner chain.5448 hull/COM corrections describe ground clutter,
not NovaCore's authored boxes. Newer history is not claimed installed.

Gate PASS for the equivalent remaining Stage2 responsibilities; no broad research,
KSA writes/assets, new numerical mechanism or departure permission.
