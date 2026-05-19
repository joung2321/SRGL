namespace SRGLCE;

using Godot;
using SRGL;

[Tool]
public partial class EditorNote : EditorObject
{
    [Export] private Sprite2D _head; // head of long note
    [Export] private Sprite2D _body; // body of long note

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public void Render(RawChart.RawNote note, float positionX, float invParentScaleY)
    {
        long length = note.EndTick - note.StartTick;

        // transform itself
        Position = new Vector2(positionX, -note.StartTick);

        // transform head
        _head.Scale = new Vector2(1, invParentScaleY);

        // transform body
        if(length > 0)
        {
            _body.Scale = new Vector2(1, length / _body.Texture.GetHeight());
            _body.Position = new Vector2(0, -length / 2f);
            _body.Visible = true;
        }
        else { _body.Visible = false; }
    }
}