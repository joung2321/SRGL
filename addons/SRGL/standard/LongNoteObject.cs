namespace SRGL.Standard;

using Godot;
using SRGL.Common;

public partial class LongNoteObject : NoteObject
{
    [Export] private Sprite2D _sprite2D;
    [Export] private Texture2D[] _textures;
    
    protected override void OnInit(NoteVisualData visualData, int variationIndex)
    {
        _sprite2D.Position = Vector2.Zero;

        _sprite2D.Centered = true;
        _sprite2D.Offset = Vector2.Zero;
        _sprite2D.RegionEnabled = true;
        _sprite2D.RegionRect = new Rect2(0, 0, _sprite2D.Texture.GetWidth(), _sprite2D.Texture.GetHeight());

        if(0 <= variationIndex && variationIndex < _textures.Length) { _sprite2D.Texture = _textures[variationIndex]; }
        _sprite2D.Modulate = new Color(0.9f,0.9f,0.9f);
    }

    protected override void OnStateChanged()
    {
        switch(_state)
        {
            case NoteState.Idle:
            case NoteState.Released:
            _sprite2D.Modulate = new Color(0.9f,0.9f,0.9f);
            break;

            case NoteState.Holding:
            _sprite2D.Modulate = new Color(1,1,1);
            break;

            case NoteState.Miss:
            case NoteState.Broken:
            _sprite2D.Modulate = new Color(0.5f,0.5f,0.5f,0.5f);
            break;
        }
    }

    public override void UpdatePosition(double position, double userSpeedPxPerSec)
    {
        _sprite2D.Scale = new Vector2(1, (float)(_visualData.Length * userSpeedPxPerSec / _sprite2D.Texture.GetHeight()));

        if(_state == NoteState.Idle || _state == NoteState.Miss)
        {
            Position = _judgementPoint.Position + (float)(userSpeedPxPerSec * (_visualData.Position - position + _visualData.Length / 2)) * Vector2.Up;
        }
        else
        {
            double remainingLength = _visualData.Position + _visualData.Length - position;
            float h = (float)(_sprite2D.Texture.GetHeight() * remainingLength / _visualData.Length);

            _sprite2D.RegionRect = new Rect2(0, 0, _sprite2D.Texture.GetWidth(), h);
            Position = _judgementPoint.Position + (float)(userSpeedPxPerSec * (remainingLength / 2)) * Vector2.Up;
        }
    }
}