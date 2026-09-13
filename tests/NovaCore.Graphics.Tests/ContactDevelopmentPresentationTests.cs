using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Simulation.Time;

internal static class ContactDevelopmentPresentationTests
{
    internal static void Run()
    {
        foreach(var tilted in new[]{false,true})
        {
            using var scene=new ContactDevelopmentScene(tilted);
            Check(scene.WindowStatus.EndsWith("READY - prepared; Space starts 20-second episode",StringComparison.Ordinal),"full ready status");
            var initial=scene.PresentedTranslation;
            var submission=new RenderFrameSubmission(2);
            var camera=new UniversePosition(new(6,4,8),new ReferenceFrameId(1));
            scene.BuildSubmission(default,camera,submission);
            Check(submission.ObjectCount==2 && submission.Batches[0].Mesh==MeshHandle.ContactQualificationBody,"independent qualification meshes");
            Check(scene.InitialSnapshot.Objects[0].Scale==new Double3(2,1,1) && scene.InitialSnapshot.Objects[1].Scale==new Double3(128,2,128),"exact physical geometry scales");
            scene.Advance(new(16665));Check(scene.Frontier==0 && scene.PresentedTranslation==initial,"unfunded presentation holds canonical state");
            scene.Advance(new(1));Check(scene.Frontier==1 && scene.PresentedTranslation.Epoch.Ticks==16666,"published pose only");
            var published=scene.PresentedTranslation;
            scene.BuildSubmission(default,new UniversePosition(new(-20,12,7),new(1)),submission);
            Check(scene.PresentedTranslation==published && scene.Frontier==1,"camera has no physical authority");
            scene.Advance(new(20_000_000-16666));Check(scene.Frontier==5,"large elapsed time respects four-step budget");
            for(var i=0;i<299;i++)scene.Advance(SimulationDuration.Zero);
            Check(scene.Frontier==1200 && scene.PresentedTranslation.Epoch.Ticks==20_000_000 && scene.Status.StartsWith("COMPLETED",StringComparison.Ordinal),"bounded completion");
            var copiedStatus=scene.WindowStatus;GC.Collect();
            Check(scene.WindowStatus==copiedStatus && !copiedStatus.Contains("RUNNING",StringComparison.Ordinal),"completion copy lifetime and status");
            published=scene.PresentedTranslation;var rotation=scene.PresentedRotation;
            scene.Advance(new(123));scene.BuildSubmission(default,camera,submission);
            Check(scene.PresentedTranslation==published && scene.PresentedRotation==rotation,"completed endpoint held without extrapolation");
        }
        using(var failed=new ContactDevelopmentScene(false))
        {
            failed.Advance(new(-1));
            Check(failed.WindowStatus.Contains("FAILED",StringComparison.Ordinal)&&!failed.WindowStatus.Contains("COMPLETED",StringComparison.Ordinal),"terminal refusal not completion");
            var status=failed.WindowStatus;failed.Advance(new(20_000_000));Check(failed.WindowStatus==status,"terminal status retained");
        }
        foreach(var name in new[]{"contact-centered","contact-tilted"})
            Check(SampleOptions.TryParse(["--scene="+name],out var options,out _)&&options.Scene==name&&!options.UseProductionEarth,"opt-in scene parsing");
        Console.WriteLine("PASS Contact development canonical presentation and lifecycle");
        Article();
    }
    private static void Article()
    {
        foreach(var tilted in new[]{false,true})
        {
            using var scene=new ContactDevelopmentScene(tilted,engineeringArticle:true);
            Check(scene.WindowStatus.Contains("Engineering spacecraft article",StringComparison.Ordinal) && scene.Status.StartsWith("READY",StringComparison.Ordinal),"article labeled READY");
            Check(scene.InitialSnapshot.Count==4,"three article children plus slab");
            var submission=new RenderFrameSubmission(4);
            var camera=new UniversePosition(new(6,4,8),new ReferenceFrameId(1));
            void Geometry()
            {
                // Independent authored oracle; do not call the article's COM helper.
                ReadOnlySpan<Double3> sizes=[new(1.5,1,2.5),new(.5,1,1.5),new(.5,1,1.5)];
                ReadOnlySpan<Double3> centres=[new(0,0,-.1),new(-1,0,.4),new(1,0,.4)];
                for(var child=0;child<3;child++)
                {
                    var actual=scene.PresentedBody(child);
                    var expected=scene.PresentedTranslation.PositionRoot+scene.PresentedRotation.OrientationLocalToParent.Rotate(centres[child]);
                    Check(actual.RootPosition.Value==expected && actual.RootOrientation==scene.PresentedRotation.OrientationLocalToParent &&
                        actual.Scale==sizes[child] && actual.Mesh==MeshHandle.ContactQualificationBody,"article rendered geometry/COM applied exactly once");
                }
                scene.BuildSubmission(default,camera,submission);
                Check(submission.ObjectCount==4,"article single submission includes complete finite body and slab");
            }
            Geometry();var initial=scene.PresentedTranslation;
            scene.Advance(new(16665));Check(scene.Frontier==0 && scene.PresentedTranslation==initial,"article unfunded hold");
            scene.Advance(new(1));Check(scene.Frontier==1,"article first canonical endpoint");Geometry();
            var pose=scene.PresentedTranslation;var rotation=scene.PresentedRotation;
            scene.BuildSubmission(default,new UniversePosition(new(-20,12,7),new(1)),submission);
            Check(scene.Frontier==1 && scene.PresentedTranslation==pose && scene.PresentedRotation==rotation,"article camera nonmutation");
            scene.Advance(new(20_000_000-16666));Check(scene.Frontier==5,"article bounded delayed catch-up");
            for(var i=0;i<299;i++)scene.Advance(SimulationDuration.Zero);
            Check(scene.Frontier==1200 && scene.PresentedTranslation.Epoch.Ticks==20_000_000 && scene.Status.StartsWith("COMPLETED",StringComparison.Ordinal),"article canonical completion");
            Geometry();pose=scene.PresentedTranslation;rotation=scene.PresentedRotation;
            scene.Advance(new(123456));scene.BuildSubmission(default,camera,submission);
            Check(scene.Frontier==1200 && scene.PresentedTranslation==pose && scene.PresentedRotation==rotation,"article final copied endpoint hold");
        }
        using(var failed=new ContactDevelopmentScene(false,engineeringArticle:true))
        {
            var pose=failed.PresentedTranslation;failed.Advance(new(-1));
            Check(failed.Status.StartsWith("FAILED",StringComparison.Ordinal) && failed.PresentedTranslation==pose,"article failure holds authoritative endpoint");
        }
        foreach(var name in new[]{"article-centered","article-tilted"})
            Check(SampleOptions.TryParse(["--scene="+name],out var options,out _)&&options.Scene==name&&!options.UseProductionEarth,"article opt-in route");
        Console.WriteLine("PASS Engineering article copied geometry, COM, camera, bounded catch-up and final hold");
    }
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
