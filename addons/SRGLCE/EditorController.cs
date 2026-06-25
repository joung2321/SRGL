namespace SRGLCE;

using System;
using Godot;
using SRGL;
using SRGLCE.Common;

public partial class EditorController : Node
{
    private TypeMenu _type = TypeMenu.Metadata;
    private ModeMenu _mode = ModeMenu.Input;

    private Control _mp; // MainPanel as Control node
    private ChartModel _cm;
    private ChartRenderer _cr;
    private ChartInspector _ci;

    private FileDialog _fd;
    private PopupMenu _pm_file;
    private OptionButton _ob_type;
    private OptionButton _ob_mode;
    private OptionButton _ob_division;
    private LineEdit _le_gridDivision;

    // selection
    private int _selectedIndex;
    private int _selectedLane;

    // public properties
    public TypeMenu Type => _type;
    public ModeMenu Mode => _mode;

    // ======== setter ========
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
        if(_mode != mode)
        {
            Deselect();
            _mode = mode;
            _ob_mode.Select((int)mode);
            _ci.SetMode(mode);
        }
    }
    // ======== end ========

    // ======== lifecycle of EditorController ========
    public void Init(Control mainPanel, ChartModel cm, ChartRenderer cr, ChartInspector ci)
    {
        _mp = mainPanel;
        _cm = cm;
        _cr = cr;
        _ci = ci;

        // subscribe event
        if(cm != null && cr != null && ci != null) { _ci.Edited += OnInspectorEdited; }
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

        // menu bar
        MenuBar mb = (MenuBar)_mp.FindChild("MenuBar");
        if(mb == null) { return; }

        // "File" PopupMenu
        _pm_file = mb.GetNode<PopupMenu>("File");
        if(_pm_file != null)
        {
            _pm_file.Clear();
            foreach(FileMenu x in Enum.GetValues(typeof(FileMenu))) { _pm_file.AddItem(x.ToString(), (int)x); }
            _pm_file.IdPressed += OnFileMenuPressed;
        }

        // "Type" OptionButton
        _ob_type = (OptionButton)_mp.FindChild("Type");
        if(_ob_type != null)
        {
            _ob_type.Clear();
            foreach(TypeMenu x in Enum.GetValues(typeof(TypeMenu))) { _ob_type.AddItem(x.ToString(), (int)x); }            
            _ob_type.Selected = (int)_type;
            _ob_type.ItemSelected += OnTypeSelected;
        }

        // "Mode" OptionButton
        _ob_mode = (OptionButton)_mp.FindChild("Mode");
        if(_ob_mode != null)
        {
            _ob_mode.Clear();
            foreach(ModeMenu x in Enum.GetValues(typeof(ModeMenu))) { _ob_mode.AddItem(x.ToString(), (int)x); }
            _ob_mode.Selected = (int)_mode;
            _ob_mode.ItemSelected += OnModeSelected;
        }

        // "Division" OptionButton
        _ob_division = (OptionButton)_mp.FindChild("Division");
        if(_ob_division != null)
        {
            _ob_division.Clear();
            foreach(DivisionMenu x in Enum.GetValues(typeof(DivisionMenu))) { _ob_division.AddItem(x.ToString(), (int)x); }
            _ob_division.Selected = (int)DivisionMenu.ByBeat;
            _ob_division.ItemSelected += OnDivisionSelected;
        }

        // "GridDivision" LineEdit
        _le_gridDivision = (LineEdit)_mp.FindChild("CustomDivision");
        _le_gridDivision.Editable = false;
        _le_gridDivision.TextChanged += OnCustomDivisionChanged;
    }

    public override void _ExitTree()
    {
        // unsubscribe event
        if(_ci != null) { _ci.Edited -= OnInspectorEdited; }

        if(_fd != null) { _fd.FileSelected -= OnFileSelected; }
        if(_pm_file != null) { _pm_file.IdPressed -= OnFileMenuPressed; }

        if(_ob_type != null) { _ob_type.ItemSelected -= OnTypeSelected; }
        if(_ob_mode != null) { _ob_mode.ItemSelected -= OnModeSelected; }
        if(_ob_division != null) { _ob_division.ItemSelected -= OnDivisionSelected; }
        if(_le_gridDivision != null) { _le_gridDivision.TextChanged -= OnCustomDivisionChanged; }
    }
    // ======== end ========

    /// <param name="localMousePos">ChartRenderer.GetLocalMousePosition()</param>
    public void Insert(Vector2 localMousePos)
    {
        long tick = _cr.LocalPosYToTick(localMousePos.Y, true);
        int lane = _cr.LocalPosXToLane(localMousePos.X);

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
            RawChart.RawNote n = _ci.ParseNote();
            long et = (n.EndTick > n.StartTick)? tick + (n.EndTick - n.StartTick): 0; // end tick
            _cm.InsertNote(n with { StartTick = tick, Lane = lane, EndTick = et });
            break;
        }
    }

    // inspects selected object
    private void Inspect()
    {
        if(_selectedIndex < 0) { return; }
        
        switch(_type)
        {
            case TypeMenu.Tempo:
            _ci.InspectTempo(_cm.Tempos[_selectedIndex]);
            break;

            case TypeMenu.TimeSignature:
            _ci.InspectTimeSignature(_cm.TimeSignatures[_selectedIndex]);
            break;

            case TypeMenu.SvChange:
            _ci.InspectSvChange(_cm.SvChanges[_selectedIndex]);
            break;

            case TypeMenu.Note:
            _ci.InspectNote(_cm.GetLane(_selectedLane)[_selectedIndex]);
            break;
        }
    }

    /// <param name="localMousePos">ChartRenderer.GetLocalMousePosition()</param>
    public void Select(Vector2 localMousePos)
    {
        long tick = _cr.LocalPosYToTick(localMousePos.Y, false);
        int lane = _cr.LocalPosXToLane(localMousePos.X);
        int index = -1;
        int count = 0;
        
        switch(_type)
        {
            case TypeMenu.Tempo:
            index = _cm.IndexOfTempoAt(tick);
            count = _cm.Tempos.Count;
            break;

            case TypeMenu.TimeSignature:
            index = _cm.IndexOfTimeSignatureAt(tick);
            count = _cm.TimeSignatures.Count;
            break;

            case TypeMenu.SvChange:
            index = _cm.IndexOfSvChangeAt(tick);
            count = _cm.SvChanges.Count;
            break;

            case TypeMenu.Note:
            if(lane < 0 || lane >= _cm.LaneCount) { return; }
            index = _cm.IndexOfNoteAt(tick, lane);
            count = _cm.GetLane(lane).Count;
            break;
        }
        
        if(index < 0) { index = ~index - 1; }
        if(index >= count) { index = -1; }

        _selectedIndex = index;
        _selectedLane = lane;
        _cr.Select(_type, index, lane);

        // inspect selected object
        Inspect();
    }

    public void Deselect()
    {
        _selectedIndex = -1;
        _selectedLane = -1;
        _cr.Deselect();
    }

    public void RemoveSelected()
    {
        if(_selectedIndex < 0) { return; }

        switch(_type)
        {
            case TypeMenu.Tempo:
            RawChart.RawTempo t = _cm.Tempos[_selectedIndex];
            _cm.RemoveTempo(t);
            break;

            case TypeMenu.TimeSignature:
            RawChart.RawTimeSignature ts = _cm.TimeSignatures[_selectedIndex];
            _cm.RemoveTimeSignature(ts);
            break;

            case TypeMenu.SvChange:
            RawChart.RawSvChange svc = _cm.SvChanges[_selectedIndex];
            _cm.RemoveSvChange(svc);
            break;

            case TypeMenu.Note:
            RawChart.RawNote n = _cm.GetLane(_selectedLane)[_selectedIndex];
            _cm.RemoveNote(n);
            break;
        }

        Deselect();
    }

    /// <param name="localMousePosY">ChartRenderer.GetLocalMousePosition()</param>
    public void EditNoteEndTick(float localMousePosY)
    {
        if(_mode != ModeMenu.Edit || _selectedIndex == -1) { return; }

        long tick = _cr.LocalPosYToTick(localMousePosY, true);
        RawChart.RawNote n = _cm.GetLane(_selectedLane)[_selectedIndex];

        if(tick <= n.StartTick) { tick = 0; }

        _cm.UpdateNote(n, n with { EndTick = tick });
        Inspect();
    }

    // ======== GUI callbacks ========
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

    private void OnTypeSelected(long id)
    {
        _type = (TypeMenu)id;
        _ci.SetType(_type);
    }

    private void OnModeSelected(long id)
    {
        _mode = (ModeMenu)id;
        _ci.SetMode(_mode);
    }

    private void OnDivisionSelected(long id)
    {
        Array arr = Enum.GetValues(typeof(DivisionMenu));
        DivisionMenu division = (DivisionMenu)arr.GetValue(id);
        
        if(division != DivisionMenu.Custom)
        {
            _le_gridDivision.Editable = false;
            _cr.GridDivision = (int)division;
        }
        else
        {
            _le_gridDivision.Editable = true;
            int.TryParse(_le_gridDivision.Text, out int customDivision);
            _cr.GridDivision = customDivision;
        }

        _cr.QueueRedraw();
    }

    private void OnCustomDivisionChanged(string s)
    {
        int.TryParse(_le_gridDivision.Text, out int customDivision);
        _cr.GridDivision = customDivision;

        _cr.QueueRedraw();
    }

    private void OnFileMenuPressed(long id)
    {
        // clear selection
        Deselect();

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

    private void OnInspectorEdited()
    {
        if(_mode == ModeMenu.Input || _selectedIndex < 0) { return; }

        switch(_type)
        {
            case TypeMenu.Metadata:
            break;

            case TypeMenu.Tempo:
            RawChart.RawTempo t = _cm.Tempos[_selectedIndex];
            RawChart.RawTempo new_t = _ci.ParseTempo() with { StartTick = t.StartTick };
            _cm.UpdateTempo(t, new_t);
            break;

            case TypeMenu.TimeSignature:
            RawChart.RawTimeSignature ts = _cm.TimeSignatures[_selectedIndex];
            RawChart.RawTimeSignature new_ts = _ci.ParseTimeSignature() with { StartTick = ts.StartTick };
            _cm.UpdateTimeSignature(ts, new_ts);
            break;

            case TypeMenu.SvChange:
            RawChart.RawSvChange svc = _cm.SvChanges[_selectedIndex];
            RawChart.RawSvChange new_svc = _ci.ParseSvChange() with { StartTick = svc.StartTick };
            _cm.UpdateSvChange(svc, new_svc);
            break;

            case TypeMenu.Note:
            RawChart.RawNote n = _cm.GetLane(_selectedLane)[_selectedIndex];
            RawChart.RawNote new_n = _ci.ParseNote() with { StartTick = n.StartTick };
            _cm.UpdateNote(n, new_n);
            break;
        }

        _cr.QueueRedraw();
    }
    // ======== end ========
}
