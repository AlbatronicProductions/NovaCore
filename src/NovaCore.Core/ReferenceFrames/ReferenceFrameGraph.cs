namespace NovaCore.Core.ReferenceFrames;

/// <summary>
/// Immutable structural hierarchy for reference frames. It owns identity and parent-child topology only;
/// evaluated transforms, velocities, simulation time, and rendering remain outside this type.
/// </summary>
public sealed class ReferenceFrameGraph
{
    private const int NoIndex = -1;
    private readonly ReferenceFrameNode[] _nodes;
    private readonly int[] _parentIndices;
    private readonly int[] _childOffsets;
    private readonly int[] _childCounts;
    private readonly int[] _childIndices;
    private readonly int[] _rootIndices;
    private readonly int[] _depthFirstIndices;
    private readonly Dictionary<ReferenceFrameId, int> _indices;

    internal ReferenceFrameGraph(ReferenceFrameNode[] nodes)
    {
        _nodes = nodes;
        _indices = new Dictionary<ReferenceFrameId, int>(nodes.Length);
        _parentIndices = new int[nodes.Length];
        Array.Fill(_parentIndices, NoIndex);
        for (var index = 0; index < nodes.Length; index++)
        {
            var node = nodes[index];
            if (!_indices.TryAdd(node.Id, index)) throw new ArgumentException("Duplicate reference-frame ID.", nameof(nodes));
            if (string.IsNullOrWhiteSpace(node.DiagnosticName)) throw new ArgumentException("Frame diagnostic name is required.", nameof(nodes));
            if (node.ParentId is { } parent)
            {
                if (!_indices.TryGetValue(parent, out var parentIndex)) throw new ArgumentException("Frame parent must already exist.", nameof(nodes));
                _parentIndices[index] = parentIndex;
            }
        }

        _childCounts = new int[nodes.Length];
        var roots = new List<int>();
        for (var index = 0; index < nodes.Length; index++)
            if (_parentIndices[index] == NoIndex) roots.Add(index); else _childCounts[_parentIndices[index]]++;

        _childOffsets = new int[nodes.Length];
        var childTotal = 0;
        for (var index = 0; index < nodes.Length; index++)
        {
            _childOffsets[index] = childTotal;
            childTotal = checked(childTotal + _childCounts[index]);
        }
        _childIndices = new int[childTotal];
        var nextChildIndices = (int[])_childOffsets.Clone();
        for (var index = 0; index < nodes.Length; index++)
            if (_parentIndices[index] != NoIndex) _childIndices[nextChildIndices[_parentIndices[index]]++] = index;

        _rootIndices = roots.ToArray();
        _depthFirstIndices = BuildDepthFirstIndices();
    }

    public int Count => _nodes.Length;
    public int RootCount => _rootIndices.Length;

    public bool TryGetNode(ReferenceFrameId id, out ReferenceFrameNode node)
    {
        if (_indices.TryGetValue(id, out var index)) { node = _nodes[index]; return true; }
        node = default;
        return false;
    }

    public bool TryGetParent(ReferenceFrameId id, out ReferenceFrameNode parent)
    {
        if (_indices.TryGetValue(id, out var index) && _parentIndices[index] != NoIndex) { parent = _nodes[_parentIndices[index]]; return true; }
        parent = default;
        return false;
    }

    /// <summary>Returns true only for a strict ancestor relationship.</summary>
    public bool IsAncestorOf(ReferenceFrameId ancestor, ReferenceFrameId descendant)
    {
        if (!_indices.TryGetValue(ancestor, out var ancestorIndex) || !_indices.TryGetValue(descendant, out var current)) return false;
        while ((current = _parentIndices[current]) != NoIndex)
            if (current == ancestorIndex) return true;
        return false;
    }

    public ReferenceFrameNodeSequence GetRoots() => new(_nodes, _rootIndices, 0, _rootIndices.Length);

    public ReferenceFrameNodeSequence GetChildren(ReferenceFrameId parent)
    {
        if (!_indices.TryGetValue(parent, out var parentIndex)) return default;
        return new(_nodes, _childIndices, _childOffsets[parentIndex], _childCounts[parentIndex]);
    }

    /// <summary>Returns nodes in deterministic insertion-derived depth-first order.</summary>
    public ReferenceFrameNodeSequence TraverseDepthFirst() => new(_nodes, _depthFirstIndices, 0, _depthFirstIndices.Length);

    /// <summary>Returns strict ancestors from immediate parent through the root without allocation.</summary>
    public ReferenceFrameAncestorSequence GetAncestors(ReferenceFrameId descendant) =>
        _indices.TryGetValue(descendant, out var index) ? new(this, index) : default;

    private int[] BuildDepthFirstIndices()
    {
        var result = new int[_nodes.Length];
        var stack = new Stack<int>(_rootIndices.Length);
        for (var root = _rootIndices.Length - 1; root >= 0; root--) stack.Push(_rootIndices[root]);
        var count = 0;
        while (stack.TryPop(out var current))
        {
            result[count++] = current;
            var start = _childOffsets[current];
            for (var child = _childCounts[current] - 1; child >= 0; child--) stack.Push(_childIndices[start + child]);
        }
        return result;
    }

    /// <summary>Value-type, allocation-free view over a frozen deterministic node index sequence.</summary>
    public readonly struct ReferenceFrameNodeSequence(ReferenceFrameNode[]? nodes, int[]? indices, int start, int count)
    {
        public int Count => count;
        public Enumerator GetEnumerator() => new(nodes, indices, start, count);

        public struct Enumerator(ReferenceFrameNode[]? nodes, int[]? indices, int start, int count)
        {
            private int _offset = -1;
            public ReferenceFrameNode Current => nodes![indices![start + _offset]];
            public bool MoveNext() => ++_offset < count;
        }
    }

    /// <summary>Value-type, allocation-free strict-ancestry traversal view.</summary>
    public readonly struct ReferenceFrameAncestorSequence(ReferenceFrameGraph? graph, int descendantIndex)
    {
        public Enumerator GetEnumerator() => new(graph, descendantIndex);

        public struct Enumerator(ReferenceFrameGraph? graph, int descendantIndex)
        {
            private int _current = descendantIndex;
            public ReferenceFrameNode Current => graph!._nodes[_current];
            public bool MoveNext()
            {
                if (graph is null || _current == NoIndex) return false;
                _current = graph._parentIndices[_current];
                return _current != NoIndex;
            }
        }
    }
}
