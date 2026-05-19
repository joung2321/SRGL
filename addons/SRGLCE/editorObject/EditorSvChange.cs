namespace SRGLCE;

using Godot;
using SRGL;

[Tool]
public partial class EditorSvChange : EditorObject
{
    [Export] private Label _label;

    public void Render(RawChart.RawSvChange svChange, float positionX)
    {
        char symbol;
        switch(svChange.Interpolation)
        {
            default:
            case SRGL.Common.InterpolationType.Step:
            symbol = '⎍';
            break;

            case SRGL.Common.InterpolationType.Linear:
            symbol = '／';
            break;
            
            case SRGL.Common.InterpolationType.Impulse:
            symbol = '↑';
            break;
        }

        _label.Text = $"x{svChange.Multiplier} {symbol}";
        Position = new Vector2(positionX, -svChange.StartTick);
    }
}