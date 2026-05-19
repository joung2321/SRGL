namespace SRGLCE;

#if TOOLS
using Godot;

// entry point of SRGLCE
[Tool]
public partial class ChartEditorPlugin : EditorPlugin
{
    PackedScene _packed_mp = ResourceLoader.Load<PackedScene>("res://addons/SRGLCE/MainPanel.tscn");
    PackedScene _packed_ci = ResourceLoader.Load<PackedScene>("res://addons/SRGLCE/ChartInspector.tscn");

    // GUI
    MainPanel _mp;
    EditorDock _dock_ci;

	// when plugin enabled
    public override void _EnterTree()
    {
        // chart inspector
        ChartInspector ci = _packed_ci.Instantiate<ChartInspector>();
        _dock_ci = new EditorDock();
        _dock_ci.AddChild(ci);
        _dock_ci.DefaultSlot = EditorDock.DockSlot.RightUl;
        _dock_ci.AvailableLayouts = EditorDock.DockLayout.Horizontal | EditorDock.DockLayout.Floating;
        AddDock(_dock_ci);

        // main panel
        _mp = (MainPanel)_packed_mp.Instantiate();
        _mp.Init(ci); // before calling AddChild(MainPanel), pass ChartInspector to MainPanel
        EditorInterface.Singleton.GetEditorMainScreen().AddChild(_mp); // Add the main panel to the editor's main viewport.

        // Hide the main panel. Very much required.
        _MakeVisible(false);
    }

	// when plugin disabled
    public override void _ExitTree()
    {
        // main panel
        if(_mp != null)
        {
            EditorInterface.Singleton.GetEditorMainScreen().RemoveChild(_mp);
            _mp.Free();
            _mp = null;
        }

        // chart inspector
        if(_dock_ci != null)
        {
            RemoveDock(_dock_ci);
            _dock_ci.Free();
            _dock_ci = null;
        }
    }

    public override bool _HasMainScreen()
    {
        return true;
    }

    public override void _MakeVisible(bool visible)
    {
        if (_mp != null)
        {
            _mp.Visible = visible;
        }
    }

    public override string _GetPluginName()
    {
        return "SRGL Chart Editor";
    }

    public override Texture2D _GetPluginIcon()
    {
        // Must return some kind of Texture for the icon.
        return EditorInterface.Singleton.GetEditorTheme().GetIcon("CanvasLayer", "EditorIcons");
    }
}
#endif