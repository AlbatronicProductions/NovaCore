using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Transactions;

internal static partial class EngineeringContactArticleTests
{
    private static bool CommandSameBits(double a, double b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);
    private static bool CommandSameBits(Double3 a, Double3 b) => CommandSameBits(a.X, b.X) && CommandSameBits(a.Y, b.Y) && CommandSameBits(a.Z, b.Z);
    private static bool CommandSameBits(DoubleQuaternion a, DoubleQuaternion b) => CommandSameBits(a.X, b.X) && CommandSameBits(a.Y, b.Y) && CommandSameBits(a.Z, b.Z) && CommandSameBits(a.W, b.W);
    private static bool CommandSameBits(SpacecraftMotion a, SpacecraftMotion b) => a == b && CommandSameBits(a.PositionRoot, b.PositionRoot) &&
        CommandSameBits(a.VelocityRoot, b.VelocityRoot) && CommandSameBits(a.BodyToRoot, b.BodyToRoot) && CommandSameBits(a.AngularVelocityBody, b.AngularVelocityBody);
    internal static void CommandNonActuation()
    {
        foreach (var (tilted, moving) in new[] { (false, false), (true, false), (true, true) })
        {
            // Unmodified banked scenario is the independent physical baseline, including its geometry gates.
            var expected = ContactSequence(tilted, moving);
            using var f = new Fixture(tilted, moving);
            Check(f.Engine.PrepareSpacecraftCommands(Craft, 1200, out var authority) == SpacecraftCommandStatus.Accepted, "command owner over banked article");
            var generation = f.World.Generation; var timeline = f.Clock.Timeline.Revision;
            for (var step = 1; step <= 1200; step++)
            {
                var command = (step % 4) switch
                {
                    0 => SpacecraftCommandIntent.Ignite(),
                    1 => SpacecraftCommandIntent.Axes(new(.5, -.7, 1), new(1, -.2, .3)),
                    2 => SpacecraftCommandIntent.ThrottlePosition((step & 4) == 0 ? .3 : .9),
                    _ => SpacecraftCommandIntent.Shutdown()
                };
                var admitted = f.Engine.AdmitSpacecraftCommand(authority!, 1, (ulong)step, command);
                Check(admitted.Status == SpacecraftCommandStatus.Accepted && admitted.EffectiveEpoch == f.Clock.CurrentTime, "zero debt at copied physical endpoint");
                var consumed = f.Engine.CommitNextSpacecraftCommand(authority!);
                Check(consumed.Status is SpacecraftCommandStatus.Committed or SpacecraftCommandStatus.NoChange, "command installed without actuation");
                Check(f.Engine.CloseSpacecraftCommandBoundary(authority!, out var requested) == SpacecraftCommandStatus.BoundaryReady &&
                    requested.Requested == consumed.Observation.State, "copy handoff before interval");
                Check(f.Source.TryEndpoint(step, out var target), "existing exact target");
                Check(f.Credit(target.Ticks - f.Clock.CurrentTime.Ticks).Status == ContactHostCreditStatus.Accepted, "unchanged host-credit contract");
                Check(f.World.Step(f.Engine, f.Configuration, f.Receipt, target, out var next) == LocalContactStatus.Success, "unchanged one private step");
                f.Receipt = next;
                var publication = f.Engine.PublishPersistentContact(f.World, f.Configuration, new(next, f.Engine.CaptureContinuationClock()));
                Check(publication.Status == PersistentContactPublicationStatus.Published, "unchanged exact publisher and acknowledgement");
                // Compare the same stored/export boundary on both sides. The general evaluator returns the NEW
                // physical revision and normalizes its observation; the staged motion carries the prior revision.
                Check(f.World.Read(f.Engine, f.Configuration, f.Receipt, out var actual) == LocalContactStatus.Success &&
                    CommandSameBits(actual.Motion, expected[step - 1]), "non-actuating command preserves every stored endpoint bit");
                var committed = publication.Observation;
                Check(CommandSameBits(committed.Translation.PositionRoot, actual.Motion.PositionRoot) && CommandSameBits(committed.Translation.VelocityRoot, actual.Motion.VelocityRoot) &&
                    CommandSameBits(committed.Rotation.OrientationLocalToParent, actual.Motion.BodyToRoot) && CommandSameBits(committed.Rotation.AngularVelocityBody, actual.Motion.AngularVelocityBody),
                    "exact canonical paired publication agrees with stored endpoint");
                Check(f.World.Generation == generation && f.Clock.Timeline.Revision == timeline && f.Engine.State.Revision.Value == (ulong)step &&
                    f.Engine.ProcessedPersistentContactCount == step && f.Clock.PendingSimulationDebt.Ticks == 0, "physical revision/history/debt/world identity preserved");
            }
            Console.WriteLine($"COMMAND_ARTICLE PASS tilted={tilted} moving={moving} endpoint_bits=1200/1200 original_geometry_gates=PASS same_world=PASS");
        }
    }
}
