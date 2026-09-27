// CPU-only independent input path: reconstruct the entire live prefix, then
// compare its canonical encoded state with checkpoint + journal recovery.
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using NovaCore.Diagnostics;
var raw=File.ReadAllBytes(Path.Combine(args[0],"live-events.bin"));
var info=JsonDocument.Parse(File.ReadAllText(Path.Combine(args[0],"session.json"))).RootElement;
var recovered=OrdinaryJournal.Recover(Path.Combine(args[0],"retained-session"),info.GetProperty("session").GetGuid());
var state=new OrdinaryState();
for(int i=0;i<raw.Length;i+=256){var bytes=raw.AsSpan(i,256);if(!OrdinaryProtocol.Valid(bytes,(ulong)(i/256+1)))throw new InvalidDataException("Live event integrity");state.Apply(OrdinaryEvent.Read(bytes));}
state.LastProducedObserved=(ulong)(raw.Length/256);
var a=OrdinaryCheckpoint.Encode(state);var b=OrdinaryCheckpoint.Encode(recovered.State);
if(!recovered.Complete||!a.SequenceEqual(b))throw new InvalidDataException("Recovered and full live reduction differ");
Console.WriteLine(JsonSerializer.Serialize(new{judgment="PASS",events=state.Sequence,recovered.Complete,canonicalStateBytes=a.Length,canonicalStateSha256=Convert.ToHexString(SHA256.HashData(a)),fullLiveReductionEqualsRecoveredState=true}));
