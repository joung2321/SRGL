namespace SRGL.Standard;

using Godot;
using SRGL;

public partial class BarlineObject : NoteObject
{
    public override void UpdatePosition(double position, double userSpeedPxPerSec)
    {
        Position = _judgementPoint.Position + (float)(userSpeedPxPerSec * (_visualData.Position - position)) * Vector2.Up;
    }
}
