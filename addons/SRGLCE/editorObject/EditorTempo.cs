namespace SRGLCE;

using Godot;
using SRGL;

[Tool]
public partial class EditorTempo : EditorObject
{
    [Export] private Label _label;

    public void Render(RawChart.RawTempo tempo, float positionX)
    {
        _label.Text = $"♩ = {tempo.Bpm}";
        Position = new Vector2(positionX, -tempo.StartTick);
    }
}