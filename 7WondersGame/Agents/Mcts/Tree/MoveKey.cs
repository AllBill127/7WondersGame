using _7WondersGame.Engine.Moves.Dtos;

namespace _7WondersGame.Agents.Mcts.Tree;

/// <summary>The moves made at a node (one in a sequential tree, everyone pending in DUCT): the key of the child.</summary>
internal readonly struct MoveKey : IEquatable<MoveKey>
{
    private readonly (int Seat, Command Move)[] _moves;

    public MoveKey(IEnumerable<(int Seat, Command Move)> moves) => _moves = moves.ToArray();

    public bool Equals(MoveKey other) => _moves.SequenceEqual(other._moves);
    public override bool Equals(object? obj) => obj is MoveKey k && Equals(k);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var m in _moves)
            hash.Add(m);
        return hash.ToHashCode();
    }
}
