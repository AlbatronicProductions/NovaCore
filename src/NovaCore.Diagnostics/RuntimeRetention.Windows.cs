using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NovaCore.Diagnostics;

public sealed partial class RuntimeRetention
{
    const uint Read=0x80000000,Delete=0x10000,Attributes=0x80,Backup=0x02000000,NoFollow=0x00200000;
    [StructLayout(LayoutKind.Sequential)] struct FileInfoNative {public uint Attributes;public System.Runtime.InteropServices.ComTypes.FILETIME Created,Accessed,Written;public uint Volume,SizeHigh,SizeLow,Links,IndexHigh,IndexLow;}
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern SafeFileHandle CreateFileW(string name,uint access,uint share,IntPtr security,uint creation,uint flags,IntPtr template);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern SafeFileHandle CreateFileTransactedW(string name,uint access,uint share,IntPtr security,uint creation,uint flags,IntPtr template,SafeFileHandle transaction,IntPtr version,IntPtr context);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool GetFileInformationByHandle(SafeFileHandle file,out FileInfoNative info);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern uint GetFinalPathNameByHandleW(SafeFileHandle file,char[] buffer,uint count,uint flags);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool SetFileInformationByHandle(SafeFileHandle file,int kind,ref int info,uint size);
    [DllImport("KtmW32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern SafeFileHandle CreateTransaction(IntPtr attributes,IntPtr id,uint options,uint isolation,uint flags,uint timeout,string description);
    [DllImport("KtmW32.dll",SetLastError=true)] static extern bool CommitTransaction(SafeFileHandle transaction);
    [DllImport("KtmW32.dll",SetLastError=true)] static extern bool RollbackTransaction(SafeFileHandle transaction);
    static void Need(bool ok,string operation){if(!ok)throw new IOException(operation,new Win32Exception(Marshal.GetLastWin32Error()));}
    static FileInfoNative Info(SafeFileHandle h){Need(GetFileInformationByHandle(h,out var info),"Read file identity");return info;}
    static SafeFileHandle Open(string path,bool directory,bool delete,SafeFileHandle? transaction=null,bool write=false)
    {
        uint flags=NoFollow|(directory?Backup:0),access=Attributes|(directory?0:Read)|(delete?Delete:0)|(write?0x40000000u:0);
        // Deny path replacement for the complete lifetime of the capability.
        var h=transaction is null?CreateFileW(path,access,directory?3u:1u,IntPtr.Zero,3,flags,IntPtr.Zero):CreateFileTransactedW(path,access,directory?3u:1u,IntPtr.Zero,3,flags,IntPtr.Zero,transaction,IntPtr.Zero,IntPtr.Zero);
        if(h.IsInvalid){h.Dispose();throw new IOException("Cannot lock "+Path.GetFileName(path),new Win32Exception(Marshal.GetLastWin32Error()));}
        try{
            var i=Info(h);if((i.Attributes&(uint)FileAttributes.ReparsePoint)!=0||((i.Attributes&(uint)FileAttributes.Directory)!=0)!=directory||!directory&&i.Links!=1)throw new InvalidDataException("Reparse, hard-link or type ambiguity.");
            char[] name=new char[32768];uint n=GetFinalPathNameByHandleW(h,name,(uint)name.Length,0);Need(n>0&&n<name.Length,"Resolve opened identity");
            string actual=new string(name,0,(int)n);if(!actual.Equals("\\\\?\\"+Path.GetFullPath(path).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Opened path escaped fixed runtime tree.");
            return h;
        }catch{h.Dispose();throw;}
    }
    List<SafeFileHandle> Anchor()
    {
        var handles=new List<SafeFileHandle>();
        try{
            var parts=new Stack<string>();for(var p=new DirectoryInfo(root);p.Parent is not null;p=p.Parent)parts.Push(p.FullName);
            while(parts.Count>0)handles.Add(Open(parts.Pop(),true,false));
            return handles;
        }catch{foreach(var h in handles)h.Dispose();throw;}
    }
    static void DisposeAll(IEnumerable<SafeFileHandle> handles){foreach(var h in handles)h.Dispose();}
    // Called only with a GUID selected by this fixed-root owner. No recursive
    // delete and no external path/root parameter exists in the retention API.
    void Retire(Guid id,Action<string>? cut)
    {
        using var tx=CreateTransaction(IntPtr.Zero,IntPtr.Zero,0,0,0,30000,"NovaCore validated runtime session retirement");
        Need(!tx.IsInvalid,"Transactional retirement unavailable; preserve");
        var handles=new List<SafeFileHandle>();bool committed=false;
        try{
            string path=SessionPath(id);
            var directory=Open(path,true,true,tx);handles.Add(directory);
            var names=Directory.GetFileSystemEntries(path).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray();
            if(!RawNames(names,false))throw new InvalidDataException("Pin or unexpected session content; preserve.");
            foreach(var name in names)handles.Add(Open(Path.Combine(path,name!),false,true,tx));
            // Locks exclude active leases/writers and any metadata replacement.
            var current=Inspect(id,false);
            if(!current.Eligible)throw new InvalidDataException("Eligibility changed: "+current.Reason);
            using var capsule=current.Clean?null:Capsule(id,cut);
            cut?.Invoke("validated");
            for(int i=1;i<handles.Count;i++){int yes=1;Need(SetFileInformationByHandle(handles[i],4,ref yes,4),"Stage retirement");cut?.Invoke("file-staged");}
            // Close transacted handles before commit; deletion is still invisible
            // outside the transaction and rollback restores the complete set.
            for(int i=1;i<handles.Count;i++)handles[i].Dispose();
            int remove=1;Need(SetFileInformationByHandle(directory,4,ref remove,4),"Stage whole session directory");directory.Dispose();
            cut?.Invoke("before-commit");Need(CommitTransaction(tx),"Commit retirement");committed=true;
        }finally{DisposeAll(handles);if(!committed)RollbackTransaction(tx);}
    }
}
