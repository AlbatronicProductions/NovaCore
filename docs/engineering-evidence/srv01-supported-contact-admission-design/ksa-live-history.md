# Direct live engineering history

Read 2026-09-17 through the authenticated existing in-app browser, Kitten Space
Agency / **#live-changelog**, server 1260011486735241329, channel
1260112103134724146. Channel topic says these are main-branch commits appearing
in a following build. No messages, reactions, settings or history were modified.
Searches: `in:live-changelog physics`, `cluster`, `support`, `landed`, `4549`, and
`after:2026-09-12`. Dates below are the Discord display dates. Concise paraphrases,
not retained bulk chat or source dumps.

| Revision/date and direct message | Relevant change / reason | Current-source reconciliation |
|---|---|---|
| [4136, Apr 16](https://discord.com/channels/1260011486735241329/1260112103134724146/1494540216482926693) | Persistent local vehicle update clusters prepare permanent parallel BEPU arenas | Installed task/bubble/sim lifetime implements retained ownership; older cluster ownership is not assumed current |
| [4428, May 19](https://discord.com/channels/1260011486735241329/1260112103134724146/1506430180674113696) | COM change had failed to refresh body bounds; landed rail transition reset positional surface parameters | Current source separates mass-to-geometry offsets/bounds from body and surface state; this motivates preserving material-frame identity |
| [4549, Jun 5](https://discord.com/channels/1260011486735241329/1260112103134724146/1512355160775721020) | Reuse active-nozzle flow, evaluate derivatives and consume resource once per outer update rather than each collision step | FullPhysicsConstrainedStep directly shows this cadence; not evidence of exact NovaCore exhaustion |
| [4646, Jun 17](https://discord.com/channels/1260011486735241329/1260112103134724146/1516883816554168393) | Terrain-contact observation moved to BEPU; contact bits distinguish terrain/ocean while settling and on-rails behavior remain separate | Current fresh contact flags / Situation / constrained selection are separate responsibilities |
| [5173, Aug 5](https://discord.com/channels/1260011486735241329/1260112103134724146/1534447088291811359) | Sleeping bodies had stale bounds and could become uncollidable | Retained body property/bounds updates must remain correct; sleep is not disposal |
| [5174, Aug 5](https://discord.com/channels/1260011486735241329/1260112103134724146/1534461444010016902) | Firing engines could incorrectly enter landed state; availability and motor shutdown defects | Current eligibility explicitly distinguishes commands, active actuators and settled state; support alone cannot authorize a rail/free-flight handoff |
| [5177, Aug 5](https://discord.com/channels/1260011486735241329/1260112103134724146/1534637795740750058) | Repeated control execution inside physics caused overshoot/chatter/excess consumption | One owner must not execute/debit the same input through two consumers |
| [5217, Aug 8](https://discord.com/channels/1260011486735241329/1260112103134724146/1535554540617859124) | Per-vehicle jobs and shape-registry lock redesigned around real update access | Current global-shape update phase and worker task are production, not a lock per canonical property |
| [5333, Aug 20](https://discord.com/channels/1260011486735241329/1260112103134724146/1539911148168544268) | Conservative merge policy caused unnecessary merges/slowdowns; work moved off render critical section | Current VehicleUpdateTask owns worker merge logic |
| [5341, Aug 20](https://discord.com/channels/1260011486735241329/1260112103134724146/1540207649734529147) | Replaced thrashing naive split/merge logic; worker now intakes orphans; task owns bubbles | Supersedes a simplistic reading of old persistent-cluster history; directly matches current IntakeOrphans/Run |
| [5421, Sep 9](https://discord.com/channels/1260011486735241329/1260112103134724146/1547117063749902347) | Permit mid-frame merge/split where appropriate | Current horizon stepping/merge helpers are present. Not a requirement to add multi-vehicle scheduling to a one-body NovaCore slice |
| [5434, Sep 13](https://discord.com/channels/1260011486735241329/1260112103134724146/1548873113746411602) | Mixed-reactant drain/share/own-part reachability fixes; shared flight/planner drain and corrected pooled-graph lifetime | Resource owner and graph lifetime matter; current part/module drain remains outside BEPU authority |
| [5435, Sep 13](https://discord.com/channels/1260011486735241329/1260112103134724146/1548902968160550994) | Scaled child parts lost physical scale on reload | Fixed assembled geometry/physical identity must be bound, not inferred from root alone; save redesign not in this ticket |
| [5448, Sep 17](https://discord.com/channels/1260011486735241329/1260112103134724146/1550158043986010133), [continuation](https://discord.com/channels/1260011486735241329/1260112103134724146/1550158045143634085) | Dynamic ground-clutter promotion, COM/hull/impulse defects, terrain sampling and dispatcher race changes | Newer than installed manifest range. Specific clutter changes and shared dispatcher fixes are disclosed; no evidence here replaces the assembled-vehicle task/ready/apply owner. Do not claim these fixes are installed or qualified |

The installed release changelog and live Discord use different revision/date
labels for some entries (e.g. mass-geometry entry is installed 5411 vs live 5412).
We cite exact channel messages as history and method bodies/hashes as current
implementation. No equality of revision label alone is used to establish code.

History explains persistence, worker ownership, property correctness and avoidance
of duplicate evolution. It does not establish NovaCore exact-resource arithmetic,
publication atomicity or cross-platform determinism, nor certify KSA bug freedom.
