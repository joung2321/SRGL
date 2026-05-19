namespace SRGLCE;

using System;
using Godot;
using SRGL;
using SRGL.Common;
using SRGLCE.Common;

[Tool]
public partial class ChartInspector : Control
{
    // inspectors (= Insp)
    [Export] private HBoxContainer _insp_startTick; // common field

    [Export] private VBoxContainer _insp_t; // tempo
    [Export] private VBoxContainer _insp_ts; // time signature
    [Export] private VBoxContainer _insp_svc; // sv change
    [Export] private VBoxContainer _insp_n; // note
    
    // line edit (= LE), option button (= OB)
    private LineEdit _le_startTick; // common field

    private LineEdit _le_bpm;
    private LineEdit _le_numerator;
    private LineEdit _le_denominator;
    private LineEdit _le_multiplier;
    private OptionButton _ob_interpolation;

    private LineEdit _le_endTick;
    private LineEdit _le_lane;
    private LineEdit _le_logicType;
    private LineEdit _le_visualType;
    private LineEdit _le_tickRate;

    public override void _Ready()
    {
        _le_startTick = (LineEdit)FindChild("StartTick"); // common field
        _le_startTick.Editable = false;

        _le_bpm = (LineEdit)FindChild("Bpm");
        _le_numerator = (LineEdit)FindChild("Numerator");
        _le_denominator = (LineEdit)FindChild("Denominator");
        _le_multiplier = (LineEdit)FindChild("Multiplier");
        _ob_interpolation = (OptionButton)FindChild("Interpolation");

        foreach(InterpolationType it in Enum.GetValues(typeof(InterpolationType)))
        {
            _ob_interpolation.AddItem(it.ToString(), (int)it);
        }

        SetType(TypeMenu.Metadata);
    }

    public void SetType(TypeMenu type)
    {
        _insp_startTick.Visible = type != TypeMenu.Metadata;

        _insp_t.Visible   = type == TypeMenu.Tempo;
        _insp_ts.Visible  = type == TypeMenu.TimeSignature;
        _insp_svc.Visible = type == TypeMenu.SvChange;
        _insp_n.Visible   = type == TypeMenu.Note;
    }

    public void SetMode(ModeMenu mode)
    {
        _le_startTick.Visible = mode == ModeMenu.Edit;
    }

    public RawChart.RawTempo ParseTempo()
    {
        double.TryParse(_le_bpm.Text, out double bpm);
        return new RawChart.RawTempo{ Bpm = bpm };
    }

    public RawChart.RawTimeSignature ParseTimeSignature()
    {
        int.TryParse(_le_numerator.Text, out int n);
        int.TryParse(_le_denominator.Text, out int d);
        return new RawChart.RawTimeSignature{ Numerator = n, Denominator = d };
    }

    public RawChart.RawSvChange ParseSvChange()
    {
        double.TryParse(_le_multiplier.Text, out double mul);
        return new RawChart.RawSvChange{ Multiplier = mul, Interpolation = (InterpolationType)_ob_interpolation.GetSelectedId() };
    }
}
