using System.Buffers.Binary;

// Independent observer-side decoding. Phase 28 is informational: it never
// refreshes or replaces the existing Record/startup/completion deadlines.
internal sealed class RecordingCallProgress
{
    internal static readonly string[] Names=["None","FinalDraw","EndRenderPass","SceneLabelEnd","CaptureClockStart","CaptureClockFrequency",
        "HeaderBegin","HeaderClock","AuthorityScalars","BulkPhysical","BulkIndices","BulkVisibility","BulkCompacted","BulkCounters","BulkIndirect",
        "InputCamera","InputGpu","InputPresentation","InputPupils","InputPreparation","InputPublishedPupil","ResidencyScalars","InputResidency",
        "CatalogScan","InputCatalog","InputDemand","ReserveClock","ReserveSlot","SlotHeader","CaptureLabelBegin","ResetQueries","TimestampBefore",
        "BarrierBefore","CopyPhysical","CopyIndices","CopyVisibility","CopyCompacted","CopyCounters","CopyIndirect","BarrierAfter","TimestampAfter",
        "ScheduledMarker","CaptureLabelEnd","AuthorityMarker","DurableMarker","CaptureClockEnd","CostMarker","FrameTimestamp","EndCommandBuffer","IncomingPupilRead","AuthorityResources"];
    internal static readonly string[] ScalarNames=["None","Frame","RecordQpc","Swap","Generation","IncomingGeneration","TopologyHash","TopologyFamily",
        "PreparedPupil","CullPupil","RasterPupil","Vertices","Triangles","Draws","Dispatches","Groups","Width","Height","PhysicalGeneration",
        "TerrainVersion","IncomingVertices","IncomingTriangles","IncomingTopologyHash","Flags"];
    static readonly ulong[] ScalarKeys=[0,4,7,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,31];
    readonly ulong[] context=new ulong[32],arguments=new ulong[4];
    readonly bool requireStart;
    bool seen,finished,partial;
    ulong frame,command,ordinal,pending,lastReturned,lastReturnedOrdinal,serial,qpc,submitted,completed,thread,records;
    ulong protocol,scalarPending,scalarReturned,scalarHeaderIndex;
    public string? Failure {get;private set;}
    public ulong Pending=>pending;
    public ulong LastReturned=>lastReturned;
    public ulong PendingScalar=>scalarPending;
    public ulong LastReturnedScalar=>scalarReturned;
    public RecordingCallProgress(bool requireStart=true){this.requireStart=requireStart;}
    static ulong W(ReadOnlySpan<byte> bytes,int word)=>BinaryPrimitives.ReadUInt64LittleEndian(bytes[(word*8)..]);
    public void Observe(ReadOnlySpan<byte> bytes)
    {
        if(W(bytes,5)!=28||Failure!=null)return;
        var version=W(bytes,9);var edge=W(bytes,10);var op=W(bytes,11);var order=W(bytes,12);
        var returned=W(bytes,13);var returnedOrder=W(bytes,14);
        void Fail(string reason){Failure="recording-call protocol: "+reason;}
        if(version is not (1 or 2)||W(bytes,6)!=2||W(bytes,7)!=0||edge>(version==1?4ul:6ul)||op>=(ulong)Names.Length||returned>=(ulong)Names.Length||++records>160){Fail("invalid version/event/budget");return;}
        if(!seen){
            if(edge!=0&&requireStart){Fail("missing first interval context");return;}
            seen=true;partial=edge!=0;protocol=version;frame=W(bytes,2);command=W(bytes,19);
            for(int i=0;i<context.Length;i++)context[i]=W(bytes,19+i);
            if(partial){ordinal=order-(edge==1?1ul:0ul);lastReturned=returned;lastReturnedOrdinal=returnedOrder;if(edge==2)pending=op;}
        }
        else {
            if(finished||version!=protocol||frame!=W(bytes,2)||command!=W(bytes,19)){Fail("interval/frame/command ownership changed");return;}
            for(int i=0;i<context.Length;i++)if(((i is <18 or >21)||edge!=4)&&context[i]!=W(bytes,19+i)){Fail("prepared/resource ownership changed");return;}
        }
        serial=W(bytes,0);qpc=W(bytes,1);submitted=W(bytes,3);completed=W(bytes,4);thread=W(bytes,62);
        if(edge==0){if(records!=1||order!=0||op!=0){Fail("duplicate/invalid start");return;}}
        else if(edge==1){
            if(pending!=0||op==0||op>=(ulong)Names.Length||order!=ordinal+1||returned!=lastReturned||returnedOrder!=lastReturnedOrdinal){Fail("invalid call entry or last-return identity");return;}
            ordinal=order;pending=op;for(int i=0;i<4;i++)arguments[i]=W(bytes,15+i);
        }
        else if(edge==2){
            if(pending!=op||order!=ordinal||returned!=op||returnedOrder!=order){Fail("return without matching call");return;}
            if(scalarPending!=0||(protocol==2&&op==8&&scalarReturned!=23)){Fail("authority returned before scalar completion");return;}
            pending=0;lastReturned=op;lastReturnedOrdinal=order;
        }
        else if(edge==3){if(pending!=0||lastReturned!=48||returned!=lastReturned||returnedOrder!=lastReturnedOrdinal){Fail("incomplete interval finish");return;}finished=true;}
        else if(edge==4){
            if(pending!=0||lastReturned!=27||order!=ordinal||returned!=lastReturned||returnedOrder!=lastReturnedOrdinal){Fail("slot state outside reservation return");return;}
            for(int i=18;i<=21;i++)context[i]=W(bytes,19+i);
        }
        else if(edge is 5 or 6){
            var scalar=W(bytes,15);var key=W(bytes,16);
            if(pending!=8||op!=8||order!=ordinal||returned!=lastReturned||returnedOrder!=lastReturnedOrdinal||scalar is 0 or >23||key!=ScalarKeys[(int)scalar]){Fail("invalid scalar owner/key");return;}
            if(edge==5){if(scalarPending!=0||scalar!=scalarReturned+1){Fail("invalid scalar entry order");return;}scalarPending=scalar;scalarHeaderIndex=key;}
            else {if(scalarPending!=scalar||key!=scalarHeaderIndex){Fail("scalar return without entry");return;}scalarReturned=scalar;scalarPending=0;}
        }
    }
    public object Evidence=>new {seen,partial,finished,records,protocol,frame,commandBuffer=command,serial,qpc,thread,submitted,completed,
        pendingCall=Names[(int)pending],pendingOperation=pending,lastSuccessfullyReturnedCall=Names[(int)lastReturned],lastReturnedOperation=lastReturned,
        pendingScalar=ScalarNames[(int)scalarPending],pendingScalarOperation=scalarPending,lastReturnedScalar=ScalarNames[(int)scalarReturned],lastReturnedScalarOperation=scalarReturned,scalarHeaderIndex,
        ordinal,lastReturnedOrdinal,arguments,context,Failure};
}
