namespace SRGLCE;

using System;
using Godot;
using SRGLCE.Common;

public partial class EditorController : Node
{
    private TypeMenu _type = TypeMenu.Metadata;
    private ModeMenu _mode = ModeMenu.Input;

    private Control _mp; // MainPanel as Node class
    private ChartModel _cm;
    private ChartRenderer _cr;
    private ChartInspector _ci;

    private FileDialog _fd;
    private PopupMenu _pm_file;
    private OptionButton _ob_type;
    // private PopupMenu _pm_grid;
    private OptionButton _ob_mode;
    private LineEdit _le_gridDivision;

    // selection
    long _selectedIndex;
    int _selectedLane;

    // public properties
    public TypeMenu Type => _type;
    public ModeMenu Mode => _mode;

    public void SetType(TypeMenu type)
    {
        if(_type != type)
        {
            Deselect();
            _type = type;
            _ob_type.Select((int)type);
            _ci.SetType(type);
        }
    }
    public void SetMode(ModeMenu mode)
    {
        _mode = mode; _ob_mode.Select((int)mode);
        _ci.SetMode(mode);
    }
    
    public void Init(Control mainPanel, ChartModel cm, ChartRenderer cr, ChartInspector ci)
    {
        _mp = mainPanel;
        _cm = cm;
        _cr = cr;
        _ci = ci;
    }

    public override void _ExitTree()
    {
        // unsubscribe event
        _fd.FileSelected -= OnFileSelected;
        _pm_file.IdPressed -= OnFileMenuPressed;
    }

    public override void _Ready()
    {
        // file dialog
        _fd = new FileDialog();
        _fd.DisplayMode = FileDialog.DisplayModeEnum.List;
        _fd.Access = FileDialog.AccessEnum.Filesystem;
        _fd.AddFilter("*.json");
        _fd.InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen;
        _fd.FileSelected += OnFileSelected;
        
        AddChild(_fd);

        // ======== MenuBar ========
        MenuBar mb = (MenuBar)_mp.FindChild("MenuBar");

        // "File" PopupMenu
        _pm_file = mb.GetNode<PopupMenu>("File");

        if(_pm_file != null)
        {
            _pm_file.Clear();

            foreach(FileMenu x in Enum.GetValues(typeof(FileMenu)))
            {
                _pm_file.AddItem(x.ToString(), (int)x);
            }

            _pm_file.IdPressed += OnFileMenuPressed;
        }

        // "Type" OptionButton
        _ob_type = (OptionButton)_mp.FindChild("Type");

        if(_ob_type != null)
        {
            _ob_type.Clear();
            foreach(TypeMenu x in Enum.GetValues(typeof(TypeMenu)))
            {
                _ob_type.AddItem(x.ToString(), (int)x);
            }
        }
        _ob_type.Selected = (int)_mode;
        _ob_type.ItemSelected += OnTypeSelected;
        // ======== end ========

        // "Mode" OptionButton
        _ob_mode = (OptionButton)_mp.FindChild("Mode");

        if(_ob_mode != null)
        {
            _ob_mode.Clear();
            foreach(ModeMenu x in Enum.GetValues(typeof(ModeMenu)))
            {
                _ob_mode.AddItem(x.ToString(), (int)x);
            }
        }
        _ob_mode.Selected = (int)_mode;
        _ob_mode.ItemSelected += OnModeSelected;

        // "GridDivision" LineEdit
        _le_gridDivision = (LineEdit)_mp.FindChild("GridDivision");
        _le_gridDivision.Editable = false;
    }

    private void OnFileSelected(string path)
    {
        switch(_fd.FileMode)
        {
            case FileDialog.FileModeEnum.OpenFile:
            _cm.Load(path);
            _cr.ResetScroll();
            _cr.QueueRedraw();
            break;

            case FileDialog.FileModeEnum.SaveFile:
            Godot.FileAccess file = Godot.FileAccess.Open(path, FileAccess.ModeFlags.Write);
            file.StoreString(_cm.Serialize());
            file.Close();
            break;
        }
    }

    private void OnTypeMenuPressed(long id)
    {
        _type = (TypeMenu)id;
        _cr.Deselect();
        _ci.SetType(_type);
    }

    private void OnTypeSelected(long id) { _type = (TypeMenu)id; }

    private void OnModeSelected(long id) { _mode = (ModeMenu)id; }

    private void OnFileMenuPressed(long id)
    {
        switch((FileMenu)id)
        {
            case FileMenu.New:
            _cm.LoadDefaultChart();
            _cr.QueueRedraw();
            break;

            case FileMenu.Open:
            _fd.FileMode = FileDialog.FileModeEnum.OpenFile;
            _fd.PopupFileDialog();
            break;

            case FileMenu.Save:
            _fd.FileMode = FileDialog.FileModeEnum.SaveFile;
            _fd.PopupFileDialog();
            break;

            case FileMenu.SaveAs:
            GD.Print(_cm.Serialize());
            break;
        }
    }

    /// <param name="localMousePos">ChartRenderer.GetLocalMousePosition()</param>
    public void Insert(Vector2 localMousePos)
    {
        long tick = _cr.SnapToGrid(localMousePos.Y, true);
        int lane = _cr.SnapToLane(localMousePos.X);

        switch(_type)
        {
            case TypeMenu.Tempo:
            _cm.InsertTempo(_ci.ParseTempo() with { StartTick = tick });
            break;

            case TypeMenu.TimeSignature:
            _cm.InsertTimeSignature(_ci.ParseTimeSignature() with { StartTick = tick });
            break;

            case TypeMenu.SvChange:
            _cm.InsertSvChange(_ci.ParseSvChange() with { StartTick = tick });
            break;

            case TypeMenu.Note:
            _cm.InsertNote(new SRGL.RawChart.RawNote{ StartTick = tick, Lane = lane });
            break;
        }
    }

    /// <param name="localMousePos">ChartRenderer.GetLocalMousePosition()</param>
    public void Select(Vector2 localMousePos)
    {
        long tick = _cr.SnapToGrid(localMousePos.Y, false);
        int lane = _cr.SnapToLane(localMousePos.X);
        int index = -1;

        switch(_type)
        {
            case TypeMenu.Tempo:
            index = _cm.IndexOfTempoAt(tick);
            break;

            case TypeMenu.TimeSignature:
            index = _cm.IndexOfTimeSignatureAt(tick);
            break;

            case TypeMenu.SvChange:
            index = _cm.IndexOfSvChangeAt(tick);
            break;

            case TypeMenu.Note:
            if(lane < 0 || lane >= _cm.LaneCount) { return; }
            index = _cm.IndexOfNoteAt(tick, lane);
            break;
        }

        if(index < 0) { index = ~index - 1; }
        _cr.Select(_type, index, lane);
    }

    public void Deselect()
    {
        _selectedIndex = -1;
        _selectedLane = -1;
        _cr.Deselect();
    }
}
