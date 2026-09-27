# Command and data boundary

Data is an explicit bidirectional service graph. A design independently selects a command-capable ControlPart; that selection need not equal the structural root. CanCommand reports whether a target part shares the selected control part's Data component. Missing selection yields no command reachability.

Reachability is a fact, not a runtime capability. Engine-issued identity and generation must still authorize an actual command at Stage6. A camera focus change cannot change this selection or grant authority. Power alone grants neither data connectivity nor command authority; data alone supplies no energy.

Installed KSA has no equivalent connector Data bit; controllability derives from control-module presence/override. NovaCore intentionally differs because the accepted article and Project Control explicitly require cross-stage command/data with no cross-stage electrical power. This extension preserves the existing camera/control separation.
