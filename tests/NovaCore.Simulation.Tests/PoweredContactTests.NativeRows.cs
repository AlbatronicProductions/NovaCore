// TEST OBSERVER ONLY. Generated from BEPU commit f73164bb3c9ca733eb3329f1f6b1cea4e216ece7.
// Copyright Bepu Entertainment LLC; Apache-2.0 license: external/bepu/2.5.0-beta.29/LICENSE.txt.
// Native arithmetic recovered byte-for-byte after removing recording calls and reversing names.
using BepuPhysics;
using BepuPhysics.Constraints;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;
using System.Numerics;
using System.Runtime.CompilerServices;
    public struct PoweredObservedFunctions : IOneBodyConstraintFunctions<Contact4OneBodyPrestepData, Contact4AccumulatedImpulses>
    {
        public static bool RequiresIncrementalSubstepUpdates => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementallyUpdateForSubstep(in Vector<float> dt, in BodyVelocityWide velocityA, ref Contact4OneBodyPrestepData prestep)
        {
            PenetrationLimitOneBody.UpdatePenetrationDepth(dt, prestep.Contact0.OffsetA, prestep.Normal, velocityA, ref prestep.Contact0.Depth);
            PenetrationLimitOneBody.UpdatePenetrationDepth(dt, prestep.Contact1.OffsetA, prestep.Normal, velocityA, ref prestep.Contact1.Depth);
            PenetrationLimitOneBody.UpdatePenetrationDepth(dt, prestep.Contact2.OffsetA, prestep.Normal, velocityA, ref prestep.Contact2.Depth);
            PenetrationLimitOneBody.UpdatePenetrationDepth(dt, prestep.Contact3.OffsetA, prestep.Normal, velocityA, ref prestep.Contact3.Depth);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WarmStart(in Vector3Wide positionA, in QuaternionWide orientationA, in BodyInertiaWide inertiaA, ref Contact4OneBodyPrestepData prestep, ref Contact4AccumulatedImpulses accumulatedImpulses, ref BodyVelocityWide wsvA)
        {
            PoweredWorkRecorder.Begin(true);
            Helpers.BuildOrthonormalBasis(prestep.Normal, out var x, out var z);
            PoweredObservedFrictionHelpers.ComputeFrictionCenter(prestep.Contact0.OffsetA, prestep.Contact1.OffsetA, prestep.Contact2.OffsetA, prestep.Contact3.OffsetA, prestep.Contact0.Depth, prestep.Contact1.Depth, prestep.Contact2.Depth, prestep.Contact3.Depth, out var offsetToManifoldCenterA);
            PoweredWorkRecorder.Record(0, 0, wsvA, inertiaA, accumulatedImpulses);
            TangentFrictionOneBody.WarmStart(x, z, offsetToManifoldCenterA, inertiaA, accumulatedImpulses.Tangent, ref wsvA);
            PoweredWorkRecorder.Record(0, 1, wsvA, inertiaA, accumulatedImpulses);
            PoweredWorkRecorder.Record(1, 0, wsvA, inertiaA, accumulatedImpulses);
            PenetrationLimitOneBody.WarmStart(inertiaA, prestep.Normal, prestep.Contact0.OffsetA, accumulatedImpulses.Penetration0, ref wsvA);
            PoweredWorkRecorder.Record(1, 1, wsvA, inertiaA, accumulatedImpulses);
            PoweredWorkRecorder.Record(2, 0, wsvA, inertiaA, accumulatedImpulses);
            PenetrationLimitOneBody.WarmStart(inertiaA, prestep.Normal, prestep.Contact1.OffsetA, accumulatedImpulses.Penetration1, ref wsvA);
            PoweredWorkRecorder.Record(2, 1, wsvA, inertiaA, accumulatedImpulses);
            PoweredWorkRecorder.Record(3, 0, wsvA, inertiaA, accumulatedImpulses);
            PenetrationLimitOneBody.WarmStart(inertiaA, prestep.Normal, prestep.Contact2.OffsetA, accumulatedImpulses.Penetration2, ref wsvA);
            PoweredWorkRecorder.Record(3, 1, wsvA, inertiaA, accumulatedImpulses);
            PoweredWorkRecorder.Record(4, 0, wsvA, inertiaA, accumulatedImpulses);
            PenetrationLimitOneBody.WarmStart(inertiaA, prestep.Normal, prestep.Contact3.OffsetA, accumulatedImpulses.Penetration3, ref wsvA);
            PoweredWorkRecorder.Record(4, 1, wsvA, inertiaA, accumulatedImpulses);
            PoweredWorkRecorder.Record(5, 0, wsvA, inertiaA, accumulatedImpulses);
            TwistFrictionOneBody.WarmStart(prestep.Normal, inertiaA, accumulatedImpulses.Twist, ref wsvA);
            PoweredWorkRecorder.Record(5, 1, wsvA, inertiaA, accumulatedImpulses);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Solve(in Vector3Wide positionA, in QuaternionWide orientationA, in BodyInertiaWide inertiaA, float dt, float inverseDt, ref Contact4OneBodyPrestepData prestep, ref Contact4AccumulatedImpulses accumulatedImpulses, ref BodyVelocityWide wsvA)
        {
            PoweredWorkRecorder.Begin(false);
            //Note that we solve the penetration constraints before the friction constraints.
            //This makes the friction constraints more authoritative, since they happen last.
            //It's a pretty minor effect either way, but penetration constraints have error correction feedback- penetration depth.
            //Friction is velocity only and has no error correction, so introducing error there might cause drift.
            SpringSettingsWide.ComputeSpringiness(prestep.MaterialProperties.SpringSettings, dt, out var positionErrorToVelocity, out var effectiveMassCFMScale, out var softnessImpulseScale);
            var inverseDtWide = new Vector<float>(inverseDt);
            PoweredWorkRecorder.Record(1, 0, wsvA, inertiaA, accumulatedImpulses);
            PenetrationLimitOneBody.Solve(inertiaA, prestep.Normal, prestep.Contact0.OffsetA, prestep.Contact0.Depth, positionErrorToVelocity, effectiveMassCFMScale, prestep.MaterialProperties.MaximumRecoveryVelocity, inverseDtWide, softnessImpulseScale, ref accumulatedImpulses.Penetration0, ref wsvA);
            PoweredWorkRecorder.Record(1, 1, wsvA, inertiaA, accumulatedImpulses);
            PoweredWorkRecorder.Record(2, 0, wsvA, inertiaA, accumulatedImpulses);
            PenetrationLimitOneBody.Solve(inertiaA, prestep.Normal, prestep.Contact1.OffsetA, prestep.Contact1.Depth, positionErrorToVelocity, effectiveMassCFMScale, prestep.MaterialProperties.MaximumRecoveryVelocity, inverseDtWide, softnessImpulseScale, ref accumulatedImpulses.Penetration1, ref wsvA);
            PoweredWorkRecorder.Record(2, 1, wsvA, inertiaA, accumulatedImpulses);
            PoweredWorkRecorder.Record(3, 0, wsvA, inertiaA, accumulatedImpulses);
            PenetrationLimitOneBody.Solve(inertiaA, prestep.Normal, prestep.Contact2.OffsetA, prestep.Contact2.Depth, positionErrorToVelocity, effectiveMassCFMScale, prestep.MaterialProperties.MaximumRecoveryVelocity, inverseDtWide, softnessImpulseScale, ref accumulatedImpulses.Penetration2, ref wsvA);
            PoweredWorkRecorder.Record(3, 1, wsvA, inertiaA, accumulatedImpulses);
            PoweredWorkRecorder.Record(4, 0, wsvA, inertiaA, accumulatedImpulses);
            PenetrationLimitOneBody.Solve(inertiaA, prestep.Normal, prestep.Contact3.OffsetA, prestep.Contact3.Depth, positionErrorToVelocity, effectiveMassCFMScale, prestep.MaterialProperties.MaximumRecoveryVelocity, inverseDtWide, softnessImpulseScale, ref accumulatedImpulses.Penetration3, ref wsvA);
            PoweredWorkRecorder.Record(4, 1, wsvA, inertiaA, accumulatedImpulses);
            Helpers.BuildOrthonormalBasis(prestep.Normal, out var x, out var z);
            var premultipliedFrictionCoefficient = new Vector<float>(1f / 4f) * prestep.MaterialProperties.FrictionCoefficient;
            var maximumTangentImpulse = premultipliedFrictionCoefficient * (accumulatedImpulses.Penetration0 + accumulatedImpulses.Penetration1 + accumulatedImpulses.Penetration2 + accumulatedImpulses.Penetration3);
            PoweredObservedFrictionHelpers.ComputeFrictionCenter(prestep.Contact0.OffsetA, prestep.Contact1.OffsetA, prestep.Contact2.OffsetA, prestep.Contact3.OffsetA, prestep.Contact0.Depth, prestep.Contact1.Depth, prestep.Contact2.Depth, prestep.Contact3.Depth, out var offsetToManifoldCenterA);
            PoweredWorkRecorder.Record(0, 0, wsvA, inertiaA, accumulatedImpulses);
            TangentFrictionOneBody.Solve(x, z, offsetToManifoldCenterA, inertiaA, maximumTangentImpulse, ref accumulatedImpulses.Tangent, ref wsvA);
            PoweredWorkRecorder.Record(0, 1, wsvA, inertiaA, accumulatedImpulses);
            var maximumTwistImpulse = premultipliedFrictionCoefficient * (
                accumulatedImpulses.Penetration0 * Vector3Wide.Distance(offsetToManifoldCenterA, prestep.Contact0.OffsetA) +
                accumulatedImpulses.Penetration1 * Vector3Wide.Distance(offsetToManifoldCenterA, prestep.Contact1.OffsetA) +
                accumulatedImpulses.Penetration2 * Vector3Wide.Distance(offsetToManifoldCenterA, prestep.Contact2.OffsetA) +
                accumulatedImpulses.Penetration3 * Vector3Wide.Distance(offsetToManifoldCenterA, prestep.Contact3.OffsetA));
            PoweredWorkRecorder.Record(5, 0, wsvA, inertiaA, accumulatedImpulses);
            TwistFrictionOneBody.Solve(prestep.Normal, inertiaA, maximumTwistImpulse, ref accumulatedImpulses.Twist, ref wsvA);
            PoweredWorkRecorder.Record(5, 1, wsvA, inertiaA, accumulatedImpulses);
        }
    }


internal static class PoweredObservedFrictionHelpers {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ComputeFrictionCenter(
            in Vector3Wide offsetA0, in Vector3Wide offsetA1, in Vector3Wide offsetA2, in Vector3Wide offsetA3,
            in Vector<float> depth0,in Vector<float> depth1,in Vector<float> depth2,in Vector<float> depth3, out Vector3Wide center)
        {
            //This can sometimes cause a weird center of friction. That's a bit strange, but the alternative is often stranger:
            //Without this, if one contact is active and the other is speculative, friction will use the manifold center as halfway between the two points. If something is holding
            //the inactive contact side up and swinging it around, the existence of speculative contacts would make friction work against the free swinging.
            var weight0 = Vector.ConditionalSelect(Vector.LessThan(depth0, Vector<float>.Zero), Vector<float>.Zero, Vector<float>.One);
            var weight1 = Vector.ConditionalSelect(Vector.LessThan(depth1, Vector<float>.Zero), Vector<float>.Zero, Vector<float>.One);
            var weight2 = Vector.ConditionalSelect(Vector.LessThan(depth2, Vector<float>.Zero), Vector<float>.Zero, Vector<float>.One);
            var weight3 = Vector.ConditionalSelect(Vector.LessThan(depth3, Vector<float>.Zero), Vector<float>.Zero, Vector<float>.One);
            var weightSum = weight0 + weight1 + weight2 + weight3;
            var useFallback = Vector.Equals(weightSum, Vector<float>.Zero);
            weightSum = Vector.ConditionalSelect(useFallback, new Vector<float>(4), weightSum);
            var inverseWeightSum = Vector<float>.One / weightSum;
            weight0 = Vector.ConditionalSelect(useFallback, inverseWeightSum, weight0 * inverseWeightSum);
            weight1 = Vector.ConditionalSelect(useFallback, inverseWeightSum, weight1 * inverseWeightSum);
            weight2 = Vector.ConditionalSelect(useFallback, inverseWeightSum, weight2 * inverseWeightSum);
            weight3 = Vector.ConditionalSelect(useFallback, inverseWeightSum, weight3 * inverseWeightSum);
            Vector3Wide.Scale(offsetA0, weight0, out var a0Contribution);
            Vector3Wide.Scale(offsetA1, weight1, out var a1Contribution);
            Vector3Wide.Scale(offsetA2, weight2, out var a2Contribution);
            Vector3Wide.Scale(offsetA3, weight3, out var a3Contribution);
            Vector3Wide.Add(a0Contribution, a1Contribution, out var a0a1);
            Vector3Wide.Add(a2Contribution, a3Contribution, out var a2a3);
            Vector3Wide.Add(a0a1, a2a3, out center);
        }
}
internal sealed class PoweredObservedProcessor : OneBodyContactTypeProcessor<Contact4OneBodyPrestepData, Contact4AccumulatedImpulses, PoweredObservedFunctions> {}
