using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal sealed partial class LocalContactWorld
{
    // Exactly three owned convex children. No builder recentering/inertia calculation:
    // NovaCore already qualified both the COM-relative transforms and canonical inertia.
    private static TypedIndex CreateArticleShape(Shapes shapes, BufferPool pool, EngineeringContactArticle article,
        bool failAfterChildrenForTest = false)
    {
        pool.Take<CompoundChild>(EngineeringContactArticle.ChildCount, out var children);
        var created = 0;
        try
        {
            for (var i = 0; i < EngineeringContactArticle.ChildCount; i++)
            {
                var child = article.Child(i); var d = child.Dimensions;
                var shape = shapes.Add(new Box((float)d.X, (float)d.Y, (float)d.Z));
                children[i] = new CompoundChild(new RigidPose(ToFloat(article.ChildCentreBody(i)), Quaternion.Identity), shape);
                created++;
            }
            // Narrow construction-cleanup seam; never enabled by normal world creation.
            if (failAfterChildrenForTest) throw new InvalidOperationException("Article construction cleanup witness.");
            return shapes.Add(new Compound(children)); // Compound owns the buffer after this succeeds.
        }
        catch
        {
            for (var i = 0; i < created; i++) shapes.RemoveAndDispose(children[i].ShapeIndex, pool);
            pool.Return(ref children);
            throw;
        }
    }

    internal static (int ReclaimedChildSlots, ulong RemainingPoolBytes) VerifyArticleConstructionCleanupForTest(EngineeringContactArticle article)
    {
        var testPool = new BufferPool(16384);
        var shapes = new Shapes(testPool, 4);
        var mask = 0;
        try
        {
            try { CreateArticleShape(shapes, testPool, article, failAfterChildrenForTest: true); }
            catch (InvalidOperationException)
            {
                // Removed children must release all three slots, rather than leaking them in the batch.
                for (var i = 0; i < 3; i++) mask |= 1 << shapes.Add(new Box(1, 1, 1)).Index;
            }
        }
        finally { shapes.Dispose(); testPool.Clear(); }
        return (mask, testPool.GetTotalAllocatedByteCount());
    }

    internal EngineeringBox ReadArticleChildForTest(int index)
    {
        ref var child = ref simulation.Shapes.GetShape<Compound>(bodyShape.Index).Children[index];
        ref var box = ref simulation.Shapes.GetShape<Box>(child.ShapeIndex.Index);
        var q = child.LocalOrientation;
        return new(new(box.Width, box.Height, box.Length), FromFloat(child.LocalPosition),
            new(q.X, q.Y, q.Z, q.W), configuration.Article!.Child(index).MassKilograms);
    }
}
