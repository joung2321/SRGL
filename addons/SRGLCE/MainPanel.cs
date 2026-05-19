namespace SRGLCE;

using Godot;

// main screen of SRGLCE
[Tool]
public partial class MainPanel : Control
{
    // model
    private ChartModel _cm;

    // view
    private ChartRenderer _cr;
    private ChartInspector _ci;

    // controller
    private EditorController _ec;
    
    public void Init(ChartInspector ci) { _ci = ci; }

    public override void _Ready()
    {
        // confugure Control node
        ClipContents= true;
        SetAnchorsPreset(LayoutPreset.FullRect);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        MouseFilter = MouseFilterEnum.Pass;

        // model
        _cm = new ChartModel();

        // view
        _cr = new ChartRenderer();
        _cr.Init(this, _cm);
        AddChild(_cr);

        // controller
        _ec = new EditorController();
        _ec.Init(this, _cm, _cr, _ci);
        AddChild(_ec);
    }

    // ======== keyboard input handler with modifier ========
    private void HandleKey(Key keycode)
    {
        switch(keycode)
        {
            case Key.Home:
            _cr.ResetScroll();
            break;
            
            case Key k when Key.Key0 <= k && k <= Key.Key9:
            _cr.GridDivision = (int)(k - Key.Key0);
            break;
        }
    }

    private void HandleCtrlKey(Key keycode)
    {
        switch(keycode)
        {
            case Key.Key0:
            case Key.Kp0:
            _cr.ResetZoom();
            break;
        }
    }

    // keyboard input router
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if(@event is not InputEventKey ek || !ek.Pressed || ek.Echo) { return; }

        switch((ek.CtrlPressed, ek.ShiftPressed))
        {
            case (false, false):
            HandleKey(ek.Keycode);
            break;

            case (true, false):
            HandleCtrlKey(ek.Keycode);
            break;
        }

        _cr.QueueRedraw();
    }

    // ======== mouse input handler with modifier ========
    private void HandleMouseButton(MouseButton buttonIndex)
    {
        switch(buttonIndex)
        {
            case MouseButton.Left:
            _ec.OnMouseButtonPressed(_cr.GetLocalMousePosition());
            break;

            case MouseButton.WheelUp:
            _cr.ScrollUp();
            break;

            case MouseButton.WheelDown:
            _cr.ScrollDown();
            break;
        }
    }

    private void HandleCtrlMouseButton(MouseButton buttonIndex)
    {
        switch(buttonIndex)
        {
            case MouseButton.WheelUp:
            _cr.ZoomY(5);
            break;

            case MouseButton.WheelDown:
            _cr.ZoomY(-5);
            break;
        }
    }

    private void HandleCtrlShiftMouseButton(MouseButton buttonIndex)
    {
        switch(buttonIndex)
        {
            case MouseButton.WheelUp:
            _cr.ZoomX(5);
            break;

            case MouseButton.WheelDown:
            _cr.ZoomX(-5);
            break;
        }
    }

    // mouse input router
    public override void _GuiInput(InputEvent @event)
    {
        if(@event is not InputEventMouseButton emb || !emb.Pressed || emb.IsEcho()) { return; }

        switch((emb.CtrlPressed, emb.ShiftPressed))
        {
            case (false, false):
            HandleMouseButton(emb.ButtonIndex);
            break;

            case (true, false):
            HandleCtrlMouseButton(emb.ButtonIndex);
            break;

            case (true, true):
            HandleCtrlShiftMouseButton(emb.ButtonIndex);
            break;
        }

        _cr.QueueRedraw();
    }
}
