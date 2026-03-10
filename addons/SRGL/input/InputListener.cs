namespace SRGL;

using Godot;
using System;

public partial class InputListener : Node
{
    private InputMapper _im;

    public InputListener(InputMapper im)
    {
        ArgumentNullException.ThrowIfNull(im);
        _im = im;
    }

    public sealed override void _UnhandledKeyInput(InputEvent @event)
    {
        // store ticks first
        long ticksUsec = (long)Time.GetTicksUsec();

        if(@event is InputEventKey ek)
        {
            // ignore echo
            if(ek.Echo) { return; }

            if(_im.ReadKeyInput(ticksUsec, ek.Keycode, ek.Pressed))
            { GetViewport().SetInputAsHandled(); }
        }
    }

    /*
    // [NOTE] Mouse input is disabled due to performance issues.
    public sealed override void _UnhandledInput(InputEvent @event)
    {
        // store ticks first
        long ticksUsec = (long)Time.GetTicksUsec();

        if(@event is InputEventMouseButton emb)
        {
            if(_im.ReadMouseButtonInput(ticksUsec, emb.ButtonIndex, emb.Pressed))
            { GetViewport().SetInputAsHandled(); }
        }
    }
    */
}