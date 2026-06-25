namespace SRGLCE;

using System;
using System.Collections.Generic;
using Godot;
using SRGL;
using SRGL.Common;
using SRGLCE.Common;

[Tool]
public partial class ChartInspector : VBoxContainer
{
    // inspectors (= Insp)
    private VBoxContainer _insp_md; // metadata

    private VBoxContainer _insp_startTick; // common field
    private VBoxContainer _insp_t; // tempo
    private VBoxContainer _insp_ts; // time signature
    private VBoxContainer _insp_svc; // sv change
    private VBoxContainer _insp_n; // note

    // fields
    private Dictionary<string, LineEdit> _lineEdits = new Dictionary<string, LineEdit>();
    private Dictionary<string, OptionButton> _optionButtons = new Dictionary<string, OptionButton>();

    public event Action Edited;

    private void OnTextChanged(string s) { Edited?.Invoke(); }
    private void OnItemSelected(long i) { Edited?.Invoke(); }
    
    private void CreateLineEdit(Control parent, string name)
    {
        HBoxContainer hBox = new HBoxContainer();
        Label label = new Label();
        LineEdit lineEdit = new LineEdit();

        label.Text = name;
        lineEdit.TextChanged += OnTextChanged;
        _lineEdits.Add(name, lineEdit);

        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        lineEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        hBox.AddChild(label);
        hBox.AddChild(lineEdit);
        parent.AddChild(hBox);
    }

    private void CreateOptionButton(Control parent, string name)
    {
        HBoxContainer hBox = new HBoxContainer();
        Label label = new Label();
        OptionButton optionButton = new OptionButton();

        label.Text = name;
        optionButton.ItemSelected += OnItemSelected;
        _optionButtons.Add(name, optionButton);

        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        optionButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        hBox.AddChild(label);
        hBox.AddChild(optionButton);
        parent.AddChild(hBox);
    }

    public override void _Ready()
    {
        Name = "Chart Inspector";

        // metadata
        _insp_md = new VBoxContainer();

        CreateLineEdit(_insp_md, "Title");
        CreateLineEdit(_insp_md, "Composer");
        CreateLineEdit(_insp_md, "Illustrator");
        CreateLineEdit(_insp_md, "Charter");
        CreateLineEdit(_insp_md, "DifficultyCategory");
        CreateLineEdit(_insp_md, "DifficultyLevel");
        CreateLineEdit(_insp_md, "Description");

        CreateLineEdit(_insp_md, "ImagePath");
        CreateLineEdit(_insp_md, "AudioPath");
        CreateLineEdit(_insp_md, "PPQN");
        CreateLineEdit(_insp_md, "EndOfTrack");
        CreateLineEdit(_insp_md, "OffsetUsec");

        AddChild(_insp_md);

        // common field
        _insp_startTick = new VBoxContainer();
        CreateLineEdit(_insp_startTick, "StartTick");
        AddChild(_insp_startTick);

        // tempo
        _insp_t = new VBoxContainer();
        CreateLineEdit(_insp_t, "BPM");
        AddChild(_insp_t);

        // time signature
        _insp_ts = new VBoxContainer();
        CreateLineEdit(_insp_ts, "Numerator");
        CreateLineEdit(_insp_ts, "Denominator");
        AddChild(_insp_ts);

        // sv change
        _insp_svc = new VBoxContainer();
        CreateLineEdit(_insp_svc, "Multiplier");
        CreateOptionButton(_insp_svc, "Interpolation");
        AddChild(_insp_svc);

        // note
        _insp_n = new VBoxContainer();
        CreateLineEdit(_insp_n, "EndTick");
        CreateLineEdit(_insp_n, "Lane");
        CreateLineEdit(_insp_n, "LogicType");
        CreateLineEdit(_insp_n, "VisualType");
        CreateLineEdit(_insp_n, "TickRate");
        AddChild(_insp_n);
        
        // uneditable fields
        _lineEdits["StartTick"].Editable = false;
        _lineEdits["Lane"].Editable = false;

        // setup OptionButton
        OptionButton interpolation = _optionButtons["Interpolation"];
        interpolation.Clear();
        foreach(InterpolationType it in Enum.GetValues(typeof(InterpolationType)))
        {
            interpolation.AddItem(it.ToString(), (int)it);
        }

        SetType(TypeMenu.Metadata);
        SetMode(ModeMenu.Input);
    }

    public void SetType(TypeMenu type)
    {
        _insp_startTick.Visible = type != TypeMenu.Metadata; // common field

        _insp_md.Visible  = type == TypeMenu.Metadata;
        _insp_t.Visible   = type == TypeMenu.Tempo;
        _insp_ts.Visible  = type == TypeMenu.TimeSignature;
        _insp_svc.Visible = type == TypeMenu.SvChange;
        _insp_n.Visible   = type == TypeMenu.Note;
    }

    public void SetMode(ModeMenu mode)
    {
        _lineEdits["StartTick"].Visible = mode == ModeMenu.Edit;
        _lineEdits["Lane"].Visible = mode == ModeMenu.Edit;
    }

    // ======== parsing ========
    public RawChart.RawTempo ParseTempo()
    {
        double.TryParse(_lineEdits["BPM"].Text, out double bpm);
        return new RawChart.RawTempo{ Bpm = bpm };
    }

    public RawChart.RawTimeSignature ParseTimeSignature()
    {
        int.TryParse(_lineEdits["Numerator"].Text, out int n);
        int.TryParse(_lineEdits["Denominator"].Text, out int d);
        return new RawChart.RawTimeSignature{ Numerator = n, Denominator = d };
    }

    public RawChart.RawSvChange ParseSvChange()
    {
        double.TryParse(_lineEdits["Multiplier"].Text, out double mul);
        return new RawChart.RawSvChange{ Multiplier = mul, Interpolation = (InterpolationType)_optionButtons["Interpolation"].GetSelectedId() };
    }

    public RawChart.RawNote ParseNote()
    {
        long.TryParse(_lineEdits["StartTick"].Text, out long st);
        long.TryParse(_lineEdits["EndTick"].Text, out long et);
        int.TryParse(_lineEdits["LogicType"].Text, out int lt);
        int.TryParse(_lineEdits["VisualType"].Text, out int vt);
        int.TryParse(_lineEdits["TickRate"].Text, out int tr);
        return new RawChart.RawNote
        {
            StartTick = st,
            EndTick = et,
            LogicType = lt,
            VisualType = vt,
            TickRate = tr,
            Options = 0
        };
    }

    // ======== inspection ========
    public void InspectTempo(RawChart.RawTempo value)
    {
        _lineEdits["StartTick"].Text = value.StartTick.ToString(); // common field
        _lineEdits["BPM"].Text = value.Bpm.ToString();
    }

    public void InspectTimeSignature(RawChart.RawTimeSignature value)
    {
        _lineEdits["StartTick"].Text = value.StartTick.ToString(); // common field
        _lineEdits["Numerator"].Text = value.Numerator.ToString();
        _lineEdits["Denominator"].Text = value.Denominator.ToString();
    }

    public void InspectSvChange(RawChart.RawSvChange value)
    {
        _lineEdits["StartTick"].Text = value.StartTick.ToString(); // common field
        _lineEdits["Multiplier"].Text = value.Multiplier.ToString();
        _optionButtons["Interpolation"].Selected = (int)value.Interpolation;
    }

    public void InspectNote(RawChart.RawNote value)
    {
        _lineEdits["StartTick"].Text = value.StartTick.ToString(); // common field
        _lineEdits["EndTick"].Text = value.EndTick.ToString();
        _lineEdits["Lane"].Text = value.Lane.ToString();
        _lineEdits["LogicType"].Text = value.LogicType.ToString();
        _lineEdits["VisualType"].Text = value.VisualType.ToString();
        _lineEdits["TickRate"].Text = value.TickRate.ToString();
    }
}
