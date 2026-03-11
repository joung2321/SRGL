namespace SRGL.Standard;

using Godot;
using SRGL;

public partial class TapNoteObject : NoteObject
{
    [Export] private Sprite2D _sprite2D;
    [Export] private Texture2D[] _textures;

    public override void SetVisualVariation(int variationIndex)
    {
        if(0 <= variationIndex && variationIndex < _textures.Length)
        {
            _sprite2D.Texture = _textures[variationIndex];
        }
    }

    public override void UpdatePosition(double position, double userSpeedPxPerSec)
    {
        Position = _judgementPoint.Position + (float)(userSpeedPxPerSec * (_visualData.Position - position)) * Vector2.Up;
    }
}
