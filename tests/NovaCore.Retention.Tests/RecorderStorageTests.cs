using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NovaCore.Diagnostics;

internal static class RecorderStorageTests
{
    internal static void Run(RuntimeRetention owner,Func<long,Guid> create,Action<bool,string> check)
    {
        long limit=RuntimeRetention.TotalWarningBytes;string root=owner.QualificationPath,ack=Path.Combine(root,"storage-acknowledgement.json");
        check(limit==419430400&&RuntimeRetention.RuntimeCapBytes==536870912,"400 MiB preventive warning and 512 MiB hard cap");
        foreach(long n in new[]{limit-1,limit,limit+1})check(RuntimeRetention.ShouldWarn(n,0,null)==(n>limit),"exact warning boundary "+n);
        var ids=new List<Guid>();string Raw(Guid id)=>Path.Combine(root,id.ToString("N"));
        byte[] Hashes(Guid id)=>SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n",Directory.GetFiles(Raw(id)).Order(StringComparer.Ordinal).Select(p=>Path.GetFileName(p)+Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)))))));
        try{
            for(int i=0;i<5;i++){var id=create(DateTime.UtcNow.AddHours(-5-i).Ticks);ids.Add(id);owner.PinSession(id);}
            var first=owner.Notice();check(first.Warn&&first.Report.After.TotalBytes>limit,"actual >512 MiB warning path");
            var a=first.Report.After;
            check(a.TotalBytes==a.CleanBytes+a.OrdinaryAbnormalBytes+a.ExemptRawBytes+first.Report.OtherRetainedBytes&&a.ExemptRawBytes>=a.ActiveBytes,"storage categories disjoint and reconcile total");
            owner.Acknowledge(first.Report);check(!owner.Notice().Warn,"acknowledged unchanged storage does not nag");
            var ackBytes=File.ReadAllBytes(ack);
            check(!RuntimeRetention.ShouldWarn(a.TotalBytes+10*1024*1024,a.ExemptRawBytes,ackBytes),"ordinary/capsule/control growth does not rearm acknowledged warning");
            check(!RuntimeRetention.ShouldWarn(a.TotalBytes,a.ExemptRawBytes+RuntimeRetention.ProtectedGrowthBytes-1,ackBytes),"sub-event-window protected growth does not nag");
            check(RuntimeRetention.ShouldWarn(a.TotalBytes+RuntimeRetention.ProtectedGrowthBytes,a.ExemptRawBytes+RuntimeRetention.ProtectedGrowthBytes,ackBytes),"material protected growth rearms");
            foreach(byte[] bad in new[]{Encoding.UTF8.GetBytes("{"),new byte[4097],Encoding.UTF8.GetBytes("{\"payload\":\"AA==\",\"payload\":\"AQ==\",\"sha256\":\"bad\"}")})
                check(RuntimeRetention.ShouldWarn(a.TotalBytes,a.ExemptRawBytes,bad),"malformed/oversized/duplicate acknowledgement fails open");
            var hashes=ids.ToDictionary(id=>id,Hashes);var maintained=owner.Execute();
            check(ids.All(id=>Hashes(id).SequenceEqual(hashes[id]))&&maintained.Deleted.Length==0&&RuntimeRetention.Describe(maintained,true).ManualReviewRequired,"safe maintenance preserves >512 MiB protected evidence and requests manual review");
            check(maintained.Decisions.Where(d=>d.Pinned).All(d=>d.Session!=Guid.Empty&&d.Bytes>0&&d.Reason.Length>0),"protected listing includes identity pin size classification and reason");
            // A new protected session appearing after the displayed snapshot is
            // not silently acknowledged by a later click on the older warning.
            var fresh=create(DateTime.UtcNow.Ticks);ids.Add(fresh);owner.PinSession(fresh);
            bool staleNotice=false;try{owner.Acknowledge(first.Report);}catch(InvalidDataException){staleNotice=true;}
            check(staleNotice&&owner.Notice().Warn,"acknowledgement refuses stale displayed snapshot; concurrent protected increase remains visible");
            var critical=ids[0];string term=Path.Combine(Raw(critical),"termination.json");File.Delete(term);
            OrdinaryTermination.Write(Raw(critical),critical,int.MaxValue,1,true,27,DateTime.UtcNow.Ticks);
            owner.ReviewSession(critical,"Read-only analysis; no positive noncritical judgment",Encoding.UTF8.GetBytes("Keep this synthetic incident."),false);
            var decision=owner.Execute().Decisions.Single(d=>d.Session==critical);
            check(decision.Pinned&&decision.Classification!="ORDINARY","review/seal alone neither unpins nor reclassifies");
            owner.UnpinSession(critical);decision=owner.Execute().Decisions.Single(d=>d.Session==critical);
            check(!decision.Pinned&&!decision.Eligible&&decision.Classification!="ORDINARY","UNPIN removes explicit pin only; criticality remains protected");
            bool refused=false;try{owner.Reclassify(critical);}catch(InvalidDataException){refused=true;}check(refused,"reclassification requires positive noncritical evidence");
            owner.ReviewSession(critical,"Intentional CPU fixture exit, positively not a crash",Encoding.UTF8.GetBytes("The source fixture deliberately exits27; no player/GPU exposure."),true);
            string perf=Path.Combine(Raw(critical),"performance.json");var original=File.ReadAllBytes(perf);File.WriteAllText(perf,"{\"changed\":true}");
            refused=false;try{owner.Reclassify(critical);}catch(InvalidDataException){refused=true;}check(refused,"stale review cannot reclassify changed raw");
            File.WriteAllBytes(perf,original);owner.Reclassify(critical);decision=owner.Execute().Decisions.Single(d=>d.Session==critical);
            check(decision.Classification=="ORDINARY"&&!decision.Clean&&decision.Eligible,"explicit reclassification keeps incident raw on capsule-only path");
            var kept=Hashes(critical);var under=owner.Execute(requested:critical);
            check(!under.Deleted.Contains(critical)&&Hashes(critical).SequenceEqual(kept),"RETIRE RAW cannot bypass under-budget/newest-useful rule");
            // Drop below threshold in this disposable fixture, then cross again.
            foreach(var id in ids.Skip(1).ToArray()){RetentionAdversaries.DeleteFixtureTree(root,Raw(id));ids.Remove(id);}
            check(!owner.Notice().Warn,"observed below-threshold storage rearms without warning");
            var armed=File.ReadAllBytes(ack);check(RuntimeRetention.ShouldWarn(limit+1,0,armed),"subsequent threshold recross warns");
        }
        finally{
            foreach(var id in ids){if(Directory.Exists(Raw(id)))RetentionAdversaries.DeleteFixtureTree(root,Raw(id));foreach(string prefix in new[]{"review-","classification-"}){var p=Path.Combine(root,prefix+id.ToString("N")+".json");if(File.Exists(p))File.Delete(p);}}
            if(File.Exists(ack))File.Delete(ack);
        }
    }
}

