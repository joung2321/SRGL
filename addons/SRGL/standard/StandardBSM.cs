namespace SRGL.Standard;

using Godot;
using System.Collections.Generic;

// input manager for standard VSRG (Vertical Scrolling Rhythm Game)
public partial class StandardInputMapper : InputMapper
{
    private Dictionary<Key, int> _keyMap;

    public StandardInputMapper() { _keyMap = new Dictionary<Key, int>(); }
    
    public void AssignKey(Key keycode, int laneIndex)
    {
        if(laneIndex < 0)
        {
            GD.PrintErr("laneIndex can NOT be negative.");
            return;
        }
        else { _keyMap.Add(keycode, laneIndex); }
    }

    public override bool ReadKeyInput(long ticksUsec, Key keycode, bool pressed)
    {
        if(_keyMap.TryGetValue(keycode, out int laneIndex))
        {
            InvokeButtonEvent(ticksUsec, laneIndex, pressed);
            return true;
        }
        else { return false; }
    }

    /*
    // [NOTE] Mouse input is disabled due to performance issues.
    public override bool ReadMouseButtonInput(long ticksUsec, MouseButton buttonIndex, bool pressed)
    {
        return false;
    }
    */
}