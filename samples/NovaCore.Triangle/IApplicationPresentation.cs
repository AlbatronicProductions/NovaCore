using NovaCore.Core.Camera;
using NovaCore.Graphics;
using NovaCore.Interop;

// Presentation bridge only. The existing renderer loop and canonical craft
// session retain all camera, clock, input, contact and resource authority.
internal unsafe interface IApplicationPresentation
{
    ReusablePartVisuals Visuals { get; }
    int RenderCapacity { get; }
    NativeApplicationViewport* Viewport { get; }
    NativeRuntime.EditorMessageCallback Preprocess { get; }
    ConstructionFlightScene? Flight { get; }
    bool Editing { get; }
    bool Paused { get; }
    bool LoadingCancelled => false;
    void LoadingProgress(string stage,bool ready=false) { }
    void Attach(SolarSystemScene solar,CameraState camera);
    void BeginFrame(in NativeInputState input);
    void PresentEditor(NativeFrameSubmission* frame);
    // Opt-in engineering observation only; no ownership or mutation of borrowed buffers.
    void ObservePresentation(NativeFrameSubmission* frame) { }
}
