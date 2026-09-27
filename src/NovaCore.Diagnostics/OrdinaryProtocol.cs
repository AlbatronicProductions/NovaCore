using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace NovaCore.Diagnostics;

public enum OrdinaryPhase : ulong
{
    Session=1, Frame=2, Update=3, Draw=4, Acquire=5, Record=6, Submit=7,
    Fence=8, Completed=9, Present=10, Error=11, Resource=12, Context=13,
    Publication=14, Callback=15, DeviceIdle=16, QueueIdle=17, Recreate=18,
    Window=19, Shutdown=20, Heartbeat=21
}
public enum OrdinaryEdge : ulong { Info=0, Enter=1, Return=2, Exception=3 }
[Flags] public enum OrdinaryFault : long { None=0, Overflow=1, Contention=2, IdentityCapacity=4, Io=8, Corruption=16, ObserverLost=32, Protocol=64 }

/// <summary>Wire layout shared with OrdinaryRecorder.h. No GPU ownership crosses it.</summary>
public static class OrdinaryProtocol
{
    public const long Magic=0x314D554D494D434E, Version=1;
    public const int HeaderBytes=4096, RecordBytes=256, Capacity=8192;
    public const long MappingBytes=HeaderBytes+(long)RecordBytes*Capacity;
    public const int Produced=8, Consumed=9, Durable=10, WorkerHeartbeat=11,
        ProducerFault=12, WorkerFault=13, Done=14, Ready=15, Dropped=16, ProducerHeartbeat=17, Gate=18;
    public static ulong Checksum(ReadOnlySpan<byte> bytes)
    {
        ulong h=14695981039346656037UL;
        foreach(byte b in bytes){h^=b;h=unchecked(h*1099511628211UL);}
        return h;
    }
    public static void Seal(Span<byte> record)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(record[240..],Checksum(record[..240]));
        record[..8].CopyTo(record[248..]);
    }
    public static bool Valid(ReadOnlySpan<byte> record,ulong sequence) => record.Length==RecordBytes &&
        BinaryPrimitives.ReadUInt64LittleEndian(record)==sequence &&
        BinaryPrimitives.ReadUInt64LittleEndian(record[248..])==sequence &&
        BinaryPrimitives.ReadUInt64LittleEndian(record[240..])==Checksum(record[..240]);
}

public sealed class OrdinaryEvent
{
    // Words0..29 are payload;30 checksum;31 commit. Copying occurs on observer only.
    public ulong[] Words {get;set;}=new ulong[30];
    public ulong Sequence=>Words[0];
    public OrdinaryPhase Phase=>(OrdinaryPhase)Words[4];
    public OrdinaryEdge Edge=>(OrdinaryEdge)Words[5];
    public long Result=>unchecked((long)Words[6]);
    public static OrdinaryEvent Read(ReadOnlySpan<byte> bytes)
    {
        var e=new OrdinaryEvent();for(int i=0;i<30;i++)e.Words[i]=BinaryPrimitives.ReadUInt64LittleEndian(bytes[(i*8)..]);return e;
    }
    public byte[] Encode()
    {
        var bytes=new byte[256];for(int i=0;i<30;i++)BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(i*8),Words[i]);OrdinaryProtocol.Seal(bytes);return bytes;
    }
}

/// <summary>Preallocated shared producer storage. Producer never waits for consumer or disk.</summary>
public sealed unsafe class OrdinaryMapping : IDisposable
{
    readonly MemoryMappedFile mapping;
    readonly MemoryMappedViewAccessor view;
    byte* pointer;
    public string Name {get;}
    public Guid Session {get;}
    public long OwnerPid=>Read(6);
    public long OwnerStartTicks=>Read(7);
    public OrdinaryMapping(string name,Guid session,bool create,int ownerPid=0,long ownerStartTicks=0)
    {
        Name=name;Session=session;
        mapping=create?MemoryMappedFile.CreateNew(name,OrdinaryProtocol.MappingBytes):MemoryMappedFile.OpenExisting(name);
        view=mapping.CreateViewAccessor(0,OrdinaryProtocol.MappingBytes);view.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
        if(create){new Span<byte>(pointer,(int)OrdinaryProtocol.MappingBytes).Clear();Write(0,OrdinaryProtocol.Magic);Write(1,1);Write(2,256);Write(3,OrdinaryProtocol.Capacity);
            var guid=session.ToByteArray();Write(4,BitConverter.ToInt64(guid,0));Write(5,BitConverter.ToInt64(guid,8));Write(6,ownerPid);Write(7,ownerStartTicks);}
        Span<byte> actual=stackalloc byte[16];BinaryPrimitives.WriteInt64LittleEndian(actual,Read(4));BinaryPrimitives.WriteInt64LittleEndian(actual[8..],Read(5));
        if(Read(0)!=OrdinaryProtocol.Magic||Read(1)!=1||Read(2)!=256||Read(3)!=OrdinaryProtocol.Capacity||new Guid(actual)!=session)
        {Dispose();throw new InvalidDataException("Minimum recorder session/schema mismatch.");}
    }
    public long Read(int word)=>Volatile.Read(ref ((long*)pointer)[word]);
    public void Write(int word,long value)=>Volatile.Write(ref ((long*)pointer)[word],value);
    public void Fault(OrdinaryFault fault,bool worker=false)=>Interlocked.Or(ref ((long*)pointer)[worker?13:12],(long)fault);
    public bool Emit(OrdinaryPhase phase,OrdinaryEdge edge=OrdinaryEdge.Info,long result=0,ulong operation=0,ulong submission=0,ulong resource=0,ulong birth=0)
    {
        Span<byte> bytes=stackalloc byte[256];bytes.Clear();Span<ulong> w=MemoryMarshal.Cast<byte,ulong>(bytes);
        w[4]=(ulong)phase;w[5]=(ulong)edge;w[6]=unchecked((ulong)result);w[7]=operation;w[8]=submission;w[13]=resource;w[14]=birth;
        return EmitBytes(bytes);
    }
    public bool EmitBytes(Span<byte> bytes)
    {
        if(Read(OrdinaryProtocol.Done)!=0)return false;
        if(bytes.Length!=256){Fault(OrdinaryFault.Protocol);return false;}
        if(Interlocked.CompareExchange(ref ((long*)pointer)[18],1,0)!=0){Fault(OrdinaryFault.Contention);Interlocked.Increment(ref ((long*)pointer)[16]);return false;}
        try{return Read(OrdinaryProtocol.Done)==0&&EmitLocked(bytes);}
        finally{Volatile.Write(ref ((long*)pointer)[18],0);}
    }
    // Seal producer admission under the same nonblocking lease as both producers.
    // The worker persists this final sequence later; closing never waits for disk.
    public bool Close(bool success)
    {
        if(Read(OrdinaryProtocol.Done)!=0)return false;
        if(Interlocked.CompareExchange(ref ((long*)pointer)[18],1,0)!=0){Fault(OrdinaryFault.Contention);return false;}
        try{
            if(Read(OrdinaryProtocol.Done)!=0)return false;
            long p=Read(8),consumed=Read(9);
            if(p<0||consumed<0||consumed>p){Fault(OrdinaryFault.Corruption);return false;}
            if(p>long.MaxValue-2||p-consumed>OrdinaryProtocol.Capacity-2){Fault(OrdinaryFault.Overflow);return false;}
            Span<byte> bytes=stackalloc byte[256];bytes.Clear();var words=MemoryMarshal.Cast<byte,ulong>(bytes);
            words[4]=(ulong)OrdinaryPhase.Shutdown;words[5]=(ulong)OrdinaryEdge.Enter;words[7]=ulong.MaxValue;
            if(!EmitLocked(bytes))return false;
            words[5]=(ulong)OrdinaryEdge.Return;words[6]=success?0:ulong.MaxValue;
            words[17]=1;words[18]=(ulong)p+2; // sealed-close contract and final ProducedSequence
            if(!EmitLocked(bytes))return false;
            Write(OrdinaryProtocol.Done,1);return true;
        }finally{Volatile.Write(ref ((long*)pointer)[18],0);}
    }
    private bool EmitLocked(Span<byte> bytes)
    {
            long produced=Read(8),qpc=Stopwatch.GetTimestamp();Write(17,qpc);
            long consumed=Read(9),durable=Read(10);
            if(produced<0||consumed<0||consumed>produced||durable<0||durable>produced){Fault(OrdinaryFault.Corruption);Interlocked.Increment(ref ((long*)pointer)[16]);return false;}
            if(produced==long.MaxValue||produced-consumed>=OrdinaryProtocol.Capacity){Fault(OrdinaryFault.Overflow);Interlocked.Increment(ref ((long*)pointer)[16]);return false;}
            long heartbeat=Read(11);if(Read(15)!=0&&(qpc-heartbeat)>Stopwatch.Frequency*5)Fault(OrdinaryFault.ObserverLost);
            ulong sequence=checked((ulong)produced+1);BinaryPrimitives.WriteUInt64LittleEndian(bytes,sequence);
            BinaryPrimitives.WriteInt64LittleEndian(bytes[8..],qpc);BinaryPrimitives.WriteInt64LittleEndian(bytes[232..],Read(12)|Read(13));OrdinaryProtocol.Seal(bytes);
            byte* slot=pointer+4096+(produced%OrdinaryProtocol.Capacity)*256;
            Volatile.Write(ref *(long*)(slot+248),0);bytes[..248].CopyTo(new Span<byte>(slot,248));Volatile.Write(ref *(long*)(slot+248),(long)sequence);Write(8,(long)sequence);return true;
    }
    public bool TryRead(long sequence,Span<byte> target)
    {
        byte* slot=pointer+4096+((sequence-1)%OrdinaryProtocol.Capacity)*256;
        if(Volatile.Read(ref *(long*)(slot+248))!=sequence)return false;
        new ReadOnlySpan<byte>(slot,256).CopyTo(target);
        return Volatile.Read(ref *(long*)(slot+248))==sequence&&OrdinaryProtocol.Valid(target,(ulong)sequence);
    }
    public void Dispose(){if(pointer!=null){view.SafeMemoryMappedViewHandle.ReleasePointer();pointer=null;}view.Dispose();mapping.Dispose();}
}
