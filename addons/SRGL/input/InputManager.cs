namespace SRGL;

using Godot;
using System;

public partial class InputManager : Node
{
    private ButtonStateMachine _bsm;

    public InputManager(ButtonStateMachine bsm)
    {
        ArgumentNullException.ThrowIfNull(bsm);
        _bsm = bsm;
    }

    public sealed override void _UnhandledKeyInput(InputEvent @event)
    {
        // store ticks first
        long ticksUsec = (long)Time.GetTicksUsec();

        if(@event is InputEventKey ek)
        {
            // ignore echo
            if(ek.Echo) { return; }

            if(_bsm.ReadKeyInput(ticksUsec, ek.Keycode, ek.Pressed))
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
            if(_bsm.ReadMouseButtonInput(ticksUsec, emb.ButtonIndex, emb.Pressed))
            { GetViewport().SetInputAsHandled(); }
        }
    }
    */
}