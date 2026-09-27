using System.Text.Json;

// Also works after an interrupted session: never trusts a clean-exit summary.
internal static class CausalJournal
{
    internal static IEnumerable<byte[]> Records(string directory)
    {
        foreach(string name in new[]{"breadcrumbs.bin","termination-tail.bin"}){
            string path=Path.Combine(directory,name);if(name=="termination-tail.bin"&&!File.Exists(path))continue;
            using var file=File.Open(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
            var header=new byte[128];file.ReadExactly(header);long H(int n)=>BitConverter.ToInt64(header,n*8);
            if(H(0)!=0x314c41535541434e||H(1)!=2||H(2)!=512||H(15) is <1 or >131072||file.Length!=128+H(15)*512)
                throw new InvalidDataException("Journal schema/size mismatch: "+name);
            for(long slot=0;slot<H(15);slot++){var bytes=new byte[512];file.ReadExactly(bytes);yield return bytes;}
        }
    }
    internal static int Inspect(string directory)
    {
        directory=Path.GetFullPath(directory);
        using var stream=File.Open(Path.Combine(directory,"breadcrumbs.bin"),FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
        var header=new byte[128];stream.ReadExactly(header);
        long H(int n)=>BitConverter.ToInt64(header,n*8);
        if(H(0)!=0x314c41535541434e||H(1)!=2||H(2)!=512||stream.Length!=128+H(15)*512||H(15) is <1 or >131072)
            throw new InvalidDataException("Journal schema/size mismatch.");
        var records=new List<byte[]>();int bad=0,empty=0;
        foreach(var bytes in Records(directory))
        {
            long serial=BitConverter.ToInt64(bytes,0);if(serial==0){empty++;continue;}
            ulong checksum=14695981039346656037;
            for(int i=0;i<504;i++)if(i<488||i>=496){checksum^=bytes[i];checksum=unchecked(checksum*1099511628211);}
            if(serial!=BitConverter.ToInt64(bytes,504)||checksum!=BitConverter.ToUInt64(bytes,488)){bad++;continue;}
            records.Add(bytes);
        }
        records.Sort((a,b)=>BitConverter.ToInt64(a,0).CompareTo(BitConverter.ToInt64(b,0)));
        var open=new Dictionary<long,long>();var maximum=new Dictionary<long,double>();var errors=new List<object>();
        var snapshots=new Queue<object>();long first=0,last=0,gaps=0,frame=0,submitted=0,completed=0;double maxGpu=0;
        long firstQpc=0,lastQpc=0,maxVisible=0,maxTotal=0,maxLod=0;
        var memoryBudgets=new Dictionary<long,(long Usage,long Budget,long Heap)>();long[]? capabilities=null;
        var recordingCalls=new RecordingCallProgress(requireStart:false);
        foreach(var record in records)
        {
            recordingCalls.Observe(record);
            long W(int word)=>BitConverter.ToInt64(record,word*8);
            long D(int word)=>W(9+word);
            double F(int word)=>BitConverter.ToDouble(record,(9+word)*8);
            if(first==0){first=W(0);firstQpc=W(1);}else if(W(0)!=last+1)gaps+=Math.Max(1,W(0)-last-1);
            last=W(0);lastQpc=W(1);frame=W(2);submitted=W(3);completed=W(4);
            long phase=W(5),kind=W(6),result=W(7);
            if(kind==0)open[phase]=W(1);
            if(kind==1&&open.Remove(phase,out long began)){double ms=(W(1)-began)*1000d/H(5);maximum[phase]=Math.Max(maximum.GetValueOrDefault(phase),ms);}
            if(result<0||phase==21&&result!=0)errors.Add(new{serial=last,frame,phase,result});
            if(phase==14&&kind==2&&D(0)==7)maxGpu=Math.Max(maxGpu,F(2));
            if(phase==14&&kind==2&&D(0)==3){maxVisible=Math.Max(maxVisible,D(2));maxTotal=Math.Max(maxTotal,D(3));maxLod=Math.Max(maxLod,D(1));}
            if(phase==18&&kind==2)capabilities=new[]{D(0),D(1),D(2)};
            if(phase==20&&kind==2)memoryBudgets[D(0)]=(D(1),D(2),D(3));
            if(phase==14&&kind==2&&D(0) is 0 or 1)
            {
                if(snapshots.Count==64)snapshots.Dequeue();
                snapshots.Enqueue(new{serial=last,frame,submitted,completed,gpuCompleted=D(0)==1,width=D(1),height=D(2),
                    camera=new[]{F(4),F(5),F(6)},altitude=F(7),forward=new[]{F(8),F(9),F(10)},level=D(13),triangles=D(14),
                    visible=D(15),vertices=D(17),draws=D(27),dispatches=D(28),groups=D(29),swapGeneration=W(8)});
            }
        }
        var report=new{schema=2,scope="checksum-verified retained rolling journal; older overwritten history is unavailable",
            recordCount=records.Count,emptySlots=empty,badSlots=bad,gaps,firstSerial=first,lastSerial=last,
            headerEmitted=H(6),headerReceived=H(7),headerDurable=H(8),retainedSeconds=(lastQpc-firstQpc)/(double)H(5),
            frame,submitted,completed,maximumOperationMilliseconds=maximum,unreturnedPhases=open.Keys,errors,
            maximumGpuMilliseconds=maxGpu,maximumVisibleTriangles=maxVisible,maximumBaseTriangles=maxTotal,maximumLevel=maxLod,lastSnapshots=snapshots,
            recordingCalls=recordingCalls.Evidence,capabilities,memoryBudgets=memoryBudgets.Select(p=>new{heap=p.Key,usage=p.Value.Usage,budget=p.Value.Budget,size=p.Value.Heap})};
        var json=JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true,NumberHandling=System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals});
        File.WriteAllText(Path.Combine(directory,"analysis.json"),json);
        Console.WriteLine(JsonSerializer.Serialize(new{records=records.Count,bad,gaps,first,last,frame,submitted,completed,maxGpu,maxVisible,maxTotal,maxLod,maximum,errors}));
        return records.Count>0&&bad==0&&gaps==0&&recordingCalls.Failure==null?0:3;
    }
}
