namespace SRGLCE;

using Godot;
using SRGL;

[Tool]
public partial class EditorObject : PoolableNode2D
{
    public void SetSelected(bool isSelected)
    {
        if(isSelected) { Modulate = Colors.Gold; }
        else { Modulate = Colors.White; }
    }

    // ======== implementation of PoolableNode2D ========
    protected override void _OnDespawn() {}
    protected override void _OnSpawn() {}

    protected override void _SetActive(bool active)
    {
        Visible = active;
        ProcessMode = active? ProcessModeEnum.Inherit: ProcessModeEnum.Disabled;
    }
    // ======== end ========
}