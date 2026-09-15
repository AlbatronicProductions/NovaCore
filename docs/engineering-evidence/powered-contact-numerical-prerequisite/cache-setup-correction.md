> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Cache probe setup correction

First Debug execution stopped before solver work: expected four contacts/one constraint. One reporting-only diagnostic execution captured contacts=0, constraints=0, awake=true, pose=(0,.5,0), identity orientation. No numerical kernel ran.

The cold cache probe supplied no acceleration or velocity to bounding-box prediction while placing the body exactly tangent to the slab. Its ordinary speculative-margin constructor permits zero minimum margin. This did not establish an overlapping/contact-generating pair. A motionless tangent fixture was not a valid setup for the intended cache witness.

Bounded correction: supply explicit existing gravity -9.81 m/s^2 to the prediction callback, as the accepted contact path does. Prediction's temporary velocity is discarded. Geometry, source pose, body mass/inertia, material, maximum margin and eight-iteration setting remain unchanged. No Solve is called and the actual body must remain unchanged. This corrects the missing prepared prediction input, not contact quality or a numerical acceptance threshold.

Retain both failed attempts. Then execute one corrected Debug and one Release cache witness; no retry-to-green campaign. The original preregistration remains preserved; this dated-by-execution amendment describes the exact change before the corrected runs.
