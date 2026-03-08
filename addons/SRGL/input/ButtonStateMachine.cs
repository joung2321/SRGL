namespace SRGL;

using Godot;

public abstract class ButtonStateMachine
{
    public delegate void ButtonEventHandler(long ticksUsec, int laneIndex, bool pressed);
    public event ButtonEventHandler ButtonEvent;

    protected void InvokeButtonEvent(long ticksUsec, int laneIndex, bool pressed)
    {
        ButtonEvent?.Invoke(ticksUsec, laneIndex, pressed);
    }

    /// <summary>
    /// Processes a rising or falling edge of a key input.<br/>
    /// Invokes the ButtonEvent when the state of at least one lane changes.
    /// </summary>
    /// <returns>True if the keycode corresponds to an assigned key; otherwise, false.</returns>
    public abstract bool ReadKeyInput(long ticksUsec, Key keycode, bool pressed);

    /*
    // [NOTE] Mouse input is disabled due to performance issues.
    /// <summary>
    /// Processes a rising or falling edge of a mouse button input.<br/>
    /// Invokes the ButtonEvent when the state of at least one lane changes.
    /// </summary>
    /// <returns>True if the buttonIndex corresponds to an assigned mouse button; otherwise, false.</returns>
    public abstract bool ReadMouseButtonInput(long ticksUsec, MouseButton buttonIndex, bool pressed);
    */
}