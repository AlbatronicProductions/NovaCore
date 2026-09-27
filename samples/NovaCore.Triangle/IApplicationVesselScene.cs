using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Interop;

/// <summary>Presentation/application lifecycle only; each adapter retains its existing simulation owner.</summary>
internal interface IApplicationVesselScene : IDisposable
{
    ReusablePartVisuals Visuals {get;}
    int RenderCapacity {get;}
    FloridaContactPresentation? FloridaView {get;}
    PlayerFlightControlInput? PlayerInput {get;}
    bool UsesSolarCamera {get;}
    bool Failed {get;}
    ResolvedRenderSnapshot InitialSnapshot {get;}
    long? PhysicalEpochTicks => null;
    SceneObjectFocusObservation PrepareFocusObservation();
    void ApplyPlayerInput(in NativeInputState input);
    void AdvanceLive(bool startRequested=false);
    void BuildSubmission(in GpuCameraData camera,in UniversePosition root,RenderFrameSubmission submission);
    unsafe void WriteExhaustParameters(NativeRenderObject* objects,int count);
}
