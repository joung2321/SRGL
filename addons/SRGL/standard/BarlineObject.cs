namespace SRGL.Standard;

using Godot;

using SRGL;
using SRGL.Common;

public partial class BarlineObject : NoteObject
{
    public override void UpdatePosition(double position, double userSpeedPxPerSec)
    {
        Position = _judgementPoint.Position + (float)(userSpeedPxPerSec * (_visualData.Position - position)) * Vector2.Up;
    }

    public override void SetVisualVariation(int variationIndex) {}
    protected override void OnStateChanged(NoteState state) {}
}
