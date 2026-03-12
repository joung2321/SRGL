namespace SRGL;

using Godot;
using SRGL.Common;

public partial class JudgementLine : Node2D
{
    // children of this node
    [Export] private Node2D _center;
    [Export] private Node2D[] _judgementPoints;

    public override void _Ready()
    {
        // verify that _center and _lanes are children of this node
        Verifier v = new Verifier();

        v.Ensure(_center.GetParent() == this, () => $"{_center.Name} is NOT a child of JudgementLine.");
        foreach(Node2D judgementPoint in _judgementPoints)
        {
            v.Ensure(judgementPoint.GetParent() == this, $"{judgementPoint.Name} is NOT a child of JudgementLine.");
        }

        v.ThrowIfInvalid();
    }
    
    protected virtual int ConvertLaneIndex(int laneIndex) { return laneIndex; }

    public Node2D GetJudgementPoint(int laneIndex)
    {
        laneIndex = ConvertLaneIndex(laneIndex);
        
        if(0 <= laneIndex && laneIndex < _judgementPoints.Length) { return _judgementPoints[laneIndex]; }
        else { return _center; }
    }
}