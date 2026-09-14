using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Transactions;

internal static partial class EngineeringContactArticleTests
{
    internal static void EngineProposalNonActuation()
    {
        foreach (var (tilted, moving) in new[] { (false,false), (true,false), (true,true) })
        {
            var expected = ContactSequence(tilted, moving);
            using var f = new Fixture(tilted, moving);
            Check(f.Engine.PrepareSpacecraftCommands(Craft,1200,out var commands)==SpacecraftCommandStatus.Accepted,"article engine command binding");
            Check(IdealEngineDefinition.TryCreate(1,1,new(0,2,0),Double3.UnitX,1200,3000,true,out var definition)==EnginePreparationStatus.Ready,"article engine definition");
            Check(f.Engine.BindSingleEnginePreparation(commands!,definition,out var authority)==EnginePreparationStatus.Ready,"article conditional engine binding");
            RunEngineProposals(f, commands!, authority!, expected);
            Console.WriteLine($"ENGINE_ARTICLE PASS tilted={tilted} moving={moving} endpoint_bits=1200/1200 original_geometry_gates=PASS same_world=PASS no_force_applied=PASS");
        }
    }
    private static void RunEngineProposals(Fixture f, SpacecraftCommandAuthority commands, EnginePreparationAuthority authority,
        NovaCore.Simulation.Spacecraft.Translation.SpacecraftMotion[] expected)
    {
        var generation=f.World.Generation;var timeline=f.Clock.Timeline.Revision;ulong sequence=0;
        for(var step=1;step<=1200;step++)
        {
            Send(SpacecraftCommandIntent.Ignite());Send(SpacecraftCommandIntent.Shutdown());Send(SpacecraftCommandIntent.Ignite());
            Send(SpacecraftCommandIntent.ThrottlePosition((step&1)==0?.25:.75));
            Check(f.Engine.CloseSpacecraftCommandBoundary(commands,out var requested)==SpacecraftCommandStatus.BoundaryReady,"article boundary closure");
            Check(f.Source.TryEndpoint(step,out var target),"article exact target");
            var clock=f.Engine.CaptureContinuationClock();var revision=f.Engine.State.Revision;var receipt=f.Receipt;
            f.Engine.State.Spacecraft.TryGetTranslation(Craft,out var linearBefore,out var massBefore);
            f.Engine.State.Spacecraft.TryGetRigidBody(Craft,out var angularBefore);
            Check(f.World.Read(f.Engine,f.Configuration,f.Receipt,out var privateBefore)==LocalContactStatus.Success,"private before proposal");
            Check(f.Engine.PrepareSingleEngineActuation(authority,target,out var token)==EnginePreparationStatus.Prepared &&
                f.Engine.PreviewSingleEngineActuation(authority,token,out var preview)==EnginePreparationStatus.Preview &&
                preview.EngineTransitionCount==3 && preview.ProposedThrustNewtons>0,"sealed nonzero conditional engine demand");
            f.Engine.State.Spacecraft.TryGetTranslation(Craft,out var linearAfter,out var massAfter);
            f.Engine.State.Spacecraft.TryGetRigidBody(Craft,out var angularAfter);
            Check(f.Engine.CaptureContinuationClock()==clock && f.Engine.State.Revision==revision && f.Clock.Timeline.Revision==timeline &&
                linearBefore==linearAfter && angularBefore==angularAfter && massBefore==massAfter && f.World.Generation==generation && f.Receipt.Equals(receipt) &&
                f.Engine.ObserveSpacecraftCommands(commands,out var afterCommand)==SpacecraftCommandStatus.Accepted && afterCommand.State==requested.Requested,
                "proposal leaves canonical/command/clock/debt/mass/private identity unchanged");
            Check(f.World.Read(f.Engine,f.Configuration,f.Receipt,out var privateAfter)==LocalContactStatus.Success &&
                CommandSameBits(privateBefore.Motion,privateAfter.Motion),"proposal does not even privately step dynamics");
            Check(f.Engine.DiscardSingleEngineProposal(authority,token)==EnginePreparationStatus.Retired,"explicit unused proposal retirement");
            Check(f.Credit(target.Ticks-f.Clock.CurrentTime.Ticks).Status==ContactHostCreditStatus.Accepted,"banked credit");
            Check(f.World.Step(f.Engine,f.Configuration,f.Receipt,target,out var next)==LocalContactStatus.Success,"banked private step");
            f.Receipt=next;
            var publication=f.Engine.PublishPersistentContact(f.World,f.Configuration,new(next,f.Engine.CaptureContinuationClock()));
            Check(publication.Status==PersistentContactPublicationStatus.Published,"banked publication");
            Check(f.World.Read(f.Engine,f.Configuration,f.Receipt,out var actual)==LocalContactStatus.Success &&
                CommandSameBits(actual.Motion,expected[step-1]),"proposals preserve every non-actuated contact endpoint bit");
            Check(CommandSameBits(publication.Observation.Translation.PositionRoot,actual.Motion.PositionRoot) &&
                CommandSameBits(publication.Observation.Translation.VelocityRoot,actual.Motion.VelocityRoot) &&
                CommandSameBits(publication.Observation.Rotation.OrientationLocalToParent,actual.Motion.BodyToRoot) &&
                CommandSameBits(publication.Observation.Rotation.AngularVelocityBody,actual.Motion.AngularVelocityBody),"canonical exact paired bits");
            Check(f.World.Generation==generation && f.Clock.Timeline.Revision==timeline && f.Engine.State.Revision.Value==(ulong)step &&
                f.Engine.ProcessedPersistentContactCount==step && f.Clock.PendingSimulationDebt.Ticks==0,"revision/history/debt/world contract unchanged");
        }
        void Send(SpacecraftCommandIntent intent)
        {
            var admission=f.Engine.AdmitSpacecraftCommand(commands,1,++sequence,intent);
            Check(admission.Status==SpacecraftCommandStatus.Accepted && admission.EffectiveEpoch==f.Clock.CurrentTime,"article request at current zero-debt boundary");
            Check(f.Engine.CommitNextSpacecraftCommand(commands).Status is SpacecraftCommandStatus.Committed or SpacecraftCommandStatus.NoChange,"article requested command commit");
        }
    }
}
