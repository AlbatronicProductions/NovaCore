using System.Text.Json;
using NovaCore.Core;
using NovaCore.Graphics;

internal static class ModularGreyboxAssetTests
{
    internal static void Run()
    {
        const string root="assets/vehicles/modular-starter";
        using var catalog=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root,"catalog.json")));var count=0;var meshes=0;var triangles=0;
        static Double3 V(JsonElement v)=>new(v.GetProperty("x").GetDouble(),v.GetProperty("y").GetDouble(),v.GetProperty("z").GetDouble());
        static void Need(bool value,string why){if(!value)throw new InvalidDataException("Modular visual: "+why);}
        foreach(var definition in catalog.RootElement.GetProperty("definitions").EnumerateArray())
        {
            var asset=definition.GetProperty("construction").GetProperty("asset");var id=asset.GetProperty("id").GetString()!;
            var path=Path.Combine(root,asset.GetProperty("relativePath").GetString()!);
            var value=PartVisualLoader.Load(path,id+"/1",asset.GetProperty("sha256").GetString()!);
            var glb=File.ReadAllBytes(path);var jsonLength=BitConverter.ToInt32(glb,12);var binaryStart=28+jsonLength;
            using var metadata=JsonDocument.Parse(glb.AsMemory(20,jsonLength));var m=metadata.RootElement;
            foreach(var accessor in m.GetProperty("accessors").EnumerateArray().Where(a=>a.GetProperty("componentType").GetInt32()==5126))
            {
                var view=m.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];var offset=binaryStart+view.GetProperty("byteOffset").GetInt32();
                var vertices=accessor.GetProperty("count").GetInt32();
                for(var axis=0;axis<3;axis++)
                {
                    var minimum=float.PositiveInfinity;var maximum=float.NegativeInfinity;
                    for(var v=0;v<vertices;v++){var component=BitConverter.ToSingle(glb,offset+v*12+axis*4);minimum=Math.Min(minimum,component);maximum=Math.Max(maximum,component);}
                    Need(accessor.GetProperty("min")[axis].GetDouble()==minimum&&accessor.GetProperty("max")[axis].GetDouble()==maximum,"bounds match actual quantized FLOAT extrema");
                }
            }
            Need(value.Meshes.Length>0,"renderable asset");
            foreach(var attachment in definition.GetProperty("attachments").EnumerateArray())
            {
                var socket=value.Sockets.Single(s=>s.Name=="SOCKET_"+attachment.GetProperty("id").GetString());
                var frame=attachment.GetProperty("frame");var r=frame.GetProperty("rotation");
                Need((socket.Point-V(frame.GetProperty("position"))).LengthSquared<1e-20,"socket material-frame position");
                var outward=new Double3(r.GetProperty("a").GetDouble(),r.GetProperty("d").GetDouble(),r.GetProperty("g").GetDouble());
                Need((socket.Outward-outward).LengthSquared<1e-20,"socket outward orientation");
            }
            if(id=="nc.greybox.engine")Need(value.GimbalPivot==Double3.Zero&&value.Meshes.Count(m=>m.Gimballed)==2,"head/bell share authored pivot; neck/flange fixed");
            if(id=="nc.greybox.block")Need(value.Meshes.Count(m=>m.Name.StartsWith("MESH_exit-",StringComparison.Ordinal))==4,"four visible independent nozzle exits");
            count++;meshes+=value.Meshes.Length;triangles+=value.Meshes.Sum(m=>m.TriangleCount);
        }
        Need(count==6,"exactly six visual assets");
        Console.WriteLine($"Modular greybox assets PASS: {count} assets, {meshes} meshes, {triangles} triangles; strict production loader, no GPU/player acceptance claim");
    }
}
