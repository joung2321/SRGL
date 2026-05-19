namespace SRGLCE;

using Godot;
using SRGL;

[Tool]
public partial class EditorTimeSignature : EditorObject
{
    [Export] private Label _label;

    public void Render(RawChart.RawTimeSignature timeSignature, float positionX)
    {
        _label.Text = $"{timeSignature.Numerator}/{timeSignature.Denominator}";
        Position = new Vector2(positionX, -timeSignature.StartTick);
    }
}