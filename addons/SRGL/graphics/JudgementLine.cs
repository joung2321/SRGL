namespace SRGL;

using Godot;

public partial class JudgementLine : Node2D
{
    // children of this node
    [Export] private Node2D _center;
    [Export] private Node2D[] _lanes;

    public override void _Ready()
    {
        // TODO: validate that _center and _lanes are children of this node
    }
    
    protected virtual int ConvertLaneIndex(int laneIndex) { return laneIndex; }

    public Node2D GetJudgementPoint(int laneIndex)
    {
        laneIndex = ConvertLaneIndex(laneIndex);
        
        if(0 <= laneIndex && laneIndex < _lanes.Length) { return _lanes[laneIndex]; }
        else { return _center; }
    }
}