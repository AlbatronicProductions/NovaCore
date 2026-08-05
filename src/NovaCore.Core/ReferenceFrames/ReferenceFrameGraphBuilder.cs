namespace NovaCore.Core.ReferenceFrames;

/// <summary>Construction-only mutable builder for a deterministic immutable reference-frame graph.</summary>
public sealed class ReferenceFrameGraphBuilder
{
    private readonly List<ReferenceFrameNode> _nodes = [];
    private readonly Dictionary<ReferenceFrameId, int> _indices = [];

    public int Count => _nodes.Count;

    /// <summary>Adds a node in deterministic insertion order. A non-root parent must already exist.</summary>
    public void Add(in ReferenceFrameNode node)
    {
        if (string.IsNullOrWhiteSpace(node.DiagnosticName)) throw new ArgumentException("Frame diagnostic name is required.", nameof(node));
        if (_indices.ContainsKey(node.Id)) throw new ArgumentException("Duplicate reference-frame ID.", nameof(node));
        if (node.ParentId is { } parent && !_indices.ContainsKey(parent)) throw new ArgumentException("Frame parent must already exist.", nameof(node));
        _indices.Add(node.Id, _nodes.Count);
        _nodes.Add(node);
    }

    public void Add(in ReferenceFrameDefinition definition) =>
        Add(new ReferenceFrameNode(definition.Id, definition.ParentId, definition.Kind, definition.DiagnosticName));

    /// <summary>Freezes the insertion-ordered topology into allocation-free traversal arrays.</summary>
    public ReferenceFrameGraph Build() => new(_nodes.ToArray());
}
