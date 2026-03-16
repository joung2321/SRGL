namespace SRGL.Standard;

using Godot;
using System;
using SRGL.Common;

public partial class LongNoteObject : NoteObject
{
    [Export] private Sprite2D _sprite2D;
    [Export] private Texture2D[] _textures;
    
    protected override void OnInit(NoteVisualData visualData, int variationIndex)
    {
        _sprite2D.Centered = true;
        _sprite2D.RegionEnabled = true;
        _sprite2D.RegionRect = new Rect2(0, 0, _sprite2D.Texture.GetWidth(), _sprite2D.Texture.GetHeight());

        if(0 <= variationIndex && variationIndex < _textures.Length)
        {
            _sprite2D.Texture = _textures[variationIndex];
        }
    }

    public override void UpdatePosition(double position, double userSpeedPxPerSec)
    {
        throw new NotImplementedException();
    }
}