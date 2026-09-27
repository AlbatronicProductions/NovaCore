using NovaCore.Diagnostics;
try
{
    if(args.Length==4&&args[0]=="observe"){int code=OrdinaryObserver.Run(args[1],Guid.Parse(args[2]),args[3]);if(RuntimeRetention.IsRuntimeSession(Guid.Parse(args[2]),args[3]))RuntimeRetention.Schedule();return code;}
    if(args.Length==1&&args[0]=="retain-runtime"){var report=RuntimeRetention.RunAndReport();Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report));return report.Failures.Length==0?0:2;}
    if(args.Length==1&&args[0]=="inspect-runtime-index"){Console.WriteLine(RuntimeRetention.InspectForensicIndex());return 0;}
    if(args.Length==3&&args[0]=="retain-runtime-after"){
        try{using var parent=System.Diagnostics.Process.GetProcessById(int.Parse(args[1],System.Globalization.CultureInfo.InvariantCulture));if(parent.StartTime.ToUniversalTime().Ticks==long.Parse(args[2],System.Globalization.CultureInfo.InvariantCulture))parent.WaitForExit(30000);}catch(ArgumentException){}
        var report=RuntimeRetention.RunAndReport();Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report));return report.Failures.Length==0?0:2;
    }
    if(args.Length==2&&args[0]=="pin-runtime"){RuntimeRetention.Pin(Guid.ParseExact(args[1],"N"));Console.WriteLine("Pinned runtime session "+args[1]);return 0;}
    if(args.Length==2&&args[0]=="recover-runtime-capsule"){var r=RuntimeRetention.RecoverCapsule(Guid.ParseExact(args[1],"N"));Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{r.Session,r.DurableSequence,r.Complete,r.Terminal,r.State.Faults,r.State.Dropped,r.State.Open,r.State.Pending}));return 0;}
    if(args.Length==3&&args[0]=="recover"){var r=OrdinaryJournal.Recover(args[1],Guid.Parse(args[2]));OrdinaryObserver.WriteReport(args[1],r);Console.WriteLine($"DurableSequence={r.DurableSequence}; {r.Terminal}");return 0;}
    Console.Error.WriteLine("Usage: NovaCore.Recorder recover <session-directory> <session-guid>");return 1;
}
catch(Exception e){Console.Error.WriteLine(e);return 2;}
