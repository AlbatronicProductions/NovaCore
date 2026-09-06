using NovaCore.Graphics;

internal static class CacheLifecycleTests
{
    internal static void Run()
    {
        var repository = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
        var fixtureRoot = Path.Combine(repository,"tests","fixtures","terrain");
        Check(TerrainAssetManifestFile.TryLoad(Path.Combine(fixtureRoot,"tiny-global.json"),out var manifest,out _),"manifest");
        var source = Path.Combine(fixtureRoot,"tiny-global.nccube");
        var temporaryRoot = Path.Combine(Path.GetTempPath(),"novacore-cache-lifecycle-"+Guid.NewGuid().ToString("N"));
        var cache = Path.Combine(temporaryRoot,"explicit-cache");
        var previousOverride = Environment.GetEnvironmentVariable(TerrainAssetRepository.CacheEnvironmentVariable);
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            Check(TerrainCacheCleanup.Inspect(cache,TimeSpan.Zero).Candidates.Count==0&&!Directory.Exists(cache),"missing root creates nothing");
            Environment.SetEnvironmentVariable(TerrainAssetRepository.CacheEnvironmentVariable,cache);
            Check(TerrainAssetRepository.CacheRoot(repository)==cache&&TerrainAssetRepository.CacheRoot(repository,temporaryRoot)==temporaryRoot,"explicit override precedence");
            var published = TerrainAssetCache.PublishFromFile(manifest,source,cache);
            Check(published.IsValid,"bounded install publication");
            var final = published.Path;
            string Stage(int owner= int.MaxValue)
            {
                var path=final+$".incomplete-{owner}-{Guid.NewGuid():N}";
                File.WriteAllBytes(path,[1,2,3]);File.SetLastWriteTimeUtc(path,DateTime.UtcNow-TimeSpan.FromDays(2));return path;
            }
            var abandoned=Stage();var active=Stage(Environment.ProcessId);var recent=Stage();File.SetLastWriteTimeUtc(recent,DateTime.UtcNow);
            var unknown=final+".incomplete-unknown";File.WriteAllBytes(unknown,[7]);
            var unrelated=Path.Combine(temporaryRoot,"unrelated.incomplete");File.WriteAllBytes(unrelated,[8]);
            var download=Path.Combine(cache,".downloads","old.nccube.incomplete");Directory.CreateDirectory(Path.GetDirectoryName(download)!);File.WriteAllBytes(download,[9]);
            var unreferenced=Path.Combine(Path.GetDirectoryName(final)!,"unreferenced.nccube");File.Copy(source,unreferenced);
            var report=TerrainCacheCleanup.Inspect(cache,TimeSpan.FromDays(1));
            Check(report.Candidates.Count==1&&report.Candidates[0].Path==abandoned&&report.ReclaimableBytes==3,"only dead-owner publication temporary with recovery copy selected");
            Check(File.Exists(abandoned)&&File.Exists(final),"dry run is read only");
            Check(TerrainCacheCleanup.TryRemove(cache,report.Candidates[0],out _),"verified abandoned duplicate removed by handle");
            Check(!File.Exists(abandoned)&&File.Exists(active)&&File.Exists(recent)&&File.Exists(unknown)&&File.Exists(download)&&File.Exists(unrelated)&&File.Exists(unreferenced),"only exact candidate deleted");
            Check(TerrainAssetCache.Verify(manifest,final).IsValid,"current final object remains verified");
            var changed=Stage();var changedCandidate=TerrainCacheCleanup.Inspect(cache,TimeSpan.FromDays(1)).Candidates.Single();
            File.WriteAllBytes(changed,[4,5,6]);File.SetLastWriteTimeUtc(changed,changedCandidate.LastWriteUtc);
            Check(!TerrainCacheCleanup.TryRemove(cache,changedCandidate,out _)&&File.Exists(changed),"same-size changed bytes invalidate prior report");File.Delete(changed);
            var locked=Stage();var lockedCandidate=TerrainCacheCleanup.Inspect(cache,TimeSpan.FromDays(1)).Candidates.Single();
            using(var held=new FileStream(locked,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            {
                Check(TerrainCacheCleanup.Inspect(cache,TimeSpan.FromDays(1)).Candidates.Count==0,"active writer handle retained");
                Check(!TerrainCacheCleanup.TryRemove(cache,lockedCandidate,out _),"writer acquired after report prevents apply");
            }
            File.SetAttributes(locked,FileAttributes.ReadOnly);
            Check(!TerrainCacheCleanup.TryRemove(cache,lockedCandidate,out _)&&TerrainCacheCleanup.Inspect(cache,TimeSpan.Zero).Candidates.All(c=>c.Path!=locked),"readonly temporary retained");
            File.SetAttributes(locked,FileAttributes.Normal);File.Delete(locked);
            var recovery=Stage();var recoveryCandidate=TerrainCacheCleanup.Inspect(cache,TimeSpan.FromDays(1)).Candidates.Single();
            File.Delete(final);
            Check(TerrainCacheCleanup.Inspect(cache,TimeSpan.FromDays(1)).Candidates.Count==0&&!TerrainCacheCleanup.TryRemove(cache,recoveryCandidate,out _),"missing recovery authority prevents cleanup");
            Check(TerrainAssetCache.PublishFromFile(manifest,source,cache).IsValid,"install restores missing cache from retained fixture");
            using(var corrupt=new FileStream(final,FileMode.Open,FileAccess.Write)){corrupt.Position=corrupt.Length-1;corrupt.WriteByte(99);}
            Check(TerrainCacheCleanup.Inspect(cache,TimeSpan.FromDays(1)).Candidates.Count==0&&!TerrainCacheCleanup.TryRemove(cache,recoveryCandidate,out _),"corrupt recovery authority prevents cleanup");
            Check(TerrainAssetCache.PublishFromFile(manifest,source,cache).IsValid,"verified atomic install repairs corrupt final occupant");
            var second=Stage();report=TerrainCacheCleanup.Inspect(cache,TimeSpan.FromDays(1));
            Check(report.Candidates.Count==2&&report.Candidates.Select(c=>c.Path).SequenceEqual(report.Candidates.Select(c=>c.Path).Order(StringComparer.Ordinal)),"deterministic exact candidate set");
            Check(TerrainCacheCleanup.TryRemove(cache,report.Candidates[0],out _)&&File.Exists(report.Candidates[1].Path),"interrupted cleanup leaves unprocessed file intact");
            Check(TerrainCacheCleanup.TryRemove(cache,report.Candidates[1],out _)&&TerrainAssetCache.Verify(manifest,final).IsValid,"resume removes remaining temporary without partial final state");
            var fresh=Stage();var freshCandidate=TerrainCacheCleanup.Inspect(cache,TimeSpan.FromDays(1)).Candidates.Single();
            Check(!TerrainCacheCleanup.TryRemove(temporaryRoot,freshCandidate,out _)&&File.Exists(fresh),"report cannot be applied against another root");
            var link=Path.Combine(temporaryRoot,"cache-link");
            try
            {
                Directory.CreateSymbolicLink(link,cache);
                Check(TerrainCacheCleanup.Inspect(link,TimeSpan.Zero).Candidates.Count==0,"reparse root refused");
                Directory.Delete(link);
            }
            catch(Exception exception) when(exception is UnauthorizedAccessException or IOException)
            {
                // Directory junctions exercise the same reparse exclusion without symlink privileges.
                var start=new System.Diagnostics.ProcessStartInfo("powershell.exe"){UseShellExecute=false,CreateNoWindow=true};
                start.ArgumentList.Add("-NoProfile");start.ArgumentList.Add("-Command");
                start.ArgumentList.Add("New-Item -ItemType Junction -Path '"+link.Replace("'","''")+"' -Target '"+cache.Replace("'","''")+"' | Out-Null");
                using var process=System.Diagnostics.Process.Start(start)!;process.WaitForExit();
                Check(process.ExitCode==0,"bounded junction creation");
                Check(TerrainCacheCleanup.Inspect(link,TimeSpan.Zero).Candidates.Count==0,"junction root refused");
                Directory.Delete(link);
            }
            Check(TerrainAssetCache.RemoveStaleIncompleteFiles(cache,TimeSpan.FromDays(1))==1,"existing API uses same narrowed safe policy");
            Check(TerrainAssetCache.Verify(manifest,final).IsValid&&File.Exists(unrelated),"production/recovery and outside content protected");
            Console.WriteLine("Cache lifecycle: dry run, owner/handle/read-only safety, changed reports, recovery hash, interruption, override containment and atomic install PASS; bounded 5032-byte asset.");
        }
        finally
        {
            Environment.SetEnvironmentVariable(TerrainAssetRepository.CacheEnvironmentVariable,previousOverride);
            Directory.Delete(temporaryRoot,true);
        }
    }
    private static void Check(bool condition,string message){if(!condition)throw new Exception("Cache lifecycle: "+message);}
}
