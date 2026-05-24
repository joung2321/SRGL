namespace SRGLCE;

using Godot;
using System;
using System.Collections.Generic;
using SRGL;
using SRGLCE.Common;

public partial class ChartRenderer : Node2D
{
    // constants
    private const int MinZoomPercent = 5;
    private const int MaxZoomPercent = 1600;
    private const float PxPerPPQN = 256f;
    
    private readonly Color BarlineColor = Colors.Orange;
    private readonly Color GridColor = new Color(1, 1, 1, 0.25f);
    // private readonly Color InvalidGridColor = Colors.Red;

    // reference
    private ChartModel _cm;
    private Control _parent;

    // object pools (= OP)
    private ObjectPool<EditorTempo> _op_t; // Tempo
    private ObjectPool<EditorTimeSignature> _op_ts; // Time Signature
    private ObjectPool<EditorSvChange> _op_svc; // Sv Change
    private ObjectPool<EditorNote> _op_n; // Note

    // spawned editor objects (= SEO)
    private List<EditorTempo> _seo_t; // Tempo
    private List<EditorTimeSignature> _seo_ts; // Time Signature
    private List<EditorSvChange> _seo_svc; // Sv Change
    private List<EditorNote> _seo_n; // Note

    // render range
    private long _minRenderTick;
    private long _maxRenderTick;

    // scroll and zoom
    private long _scrollTick = 0;
    private int _zoomXPercent = 100;
    private int _zoomYPercent = 100;

    // mode and selection
    private TypeMenu _type = TypeMenu.Metadata;
    private long _selectedIndex = -1;
    private int _selectedLane = -1;

    // public properties
    public int GridDivision = 0; // <= 0: divide by Denominator-th note, > 0: divide by GridDivision-th note
    public float LaneWidth = 128f;
    public float MinGridWidth = 64f; // width of grid with no lane
    public float HalfGridWidth => (_cm != null && _cm.LaneCount > 0)? _cm.LaneCount * LaneWidth / 2: MinGridWidth / 2;

    public void Init(Control parent, ChartModel cm)
    {
        _parent = parent;
        _cm = cm;
    }

    public override void _Ready()
    {
        // object pools (= OP)
        _op_t = new ObjectPool<EditorTempo>(this, "res://addons/SRGLCE/editorObject/EditorTempo.tscn");
        _op_ts = new ObjectPool<EditorTimeSignature>(this, "res://addons/SRGLCE/editorObject/EditorTimeSignature.tscn");
        _op_svc = new ObjectPool<EditorSvChange>(this, "res://addons/SRGLCE/editorObject/EditorSvChange.tscn");
        _op_n = new ObjectPool<EditorNote>(this, "res://addons/SRGLCE/editorObject/EditorNote.tscn");

        // spawned editor objects (= SEO)
        _seo_t = new List<EditorTempo>(32);
        _seo_ts = new List<EditorTimeSignature>(32);
        _seo_svc = new List<EditorSvChange>(32);
        _seo_n = new List<EditorNote>(256);
    }
    
    // scroll
    public void ResetScroll() { _scrollTick = 0; }
    public void ScrollUp()   { _scrollTick += (long)(100f / _zoomYPercent * _cm.PPQN / 2); }
    public void ScrollDown() { _scrollTick -= (long)(100f / _zoomYPercent * _cm.PPQN / 2); }

    // zoom
    public void ResetZoom() { _zoomXPercent = _zoomYPercent = 100; }
    public void ZoomX(int deltaZoomPercent) { _zoomXPercent = Math.Clamp(_zoomXPercent + deltaZoomPercent, MinZoomPercent, MaxZoomPercent); }
    public void ZoomY(int deltaZoomPercent) { _zoomYPercent = Math.Clamp(_zoomYPercent + deltaZoomPercent, MinZoomPercent, MaxZoomPercent); }

    // mode and selection
    public void Select(TypeMenu type, long index, int lane = -1) { _type = type; _selectedIndex = index; _selectedLane = lane; }
    public void Deselect() { _selectedIndex = -1; _selectedLane = -1; }

    private void UpdateTransform()
    {
        float scaleX = _zoomXPercent / 100f;
        float scaleY = _zoomYPercent * PxPerPPQN / (100f * _cm.PPQN); // (_zoomYPercent / 100f) * (PxPerPPQN / PPQN)

        Scale = new Vector2(scaleX, scaleY);
        Position = _parent.Size / 2 + scaleY * _scrollTick * Vector2.Down;
    }

    private void CalculateRenderRange()
    {
        _minRenderTick = (long)((Position.Y - _parent.Size.Y) / Scale.Y);
        _maxRenderTick = (long)(Position.Y / Scale.Y) + 1; // + 1: better than using Mathf.Round()
    }

    /// <param name="localMousePosX">ChartRenderer.GetLocalMousePosition().X</param>
    public int SnapToLane(float localMousePosX)
    {
        int lane = Mathf.FloorToInt((localMousePosX + HalfGridWidth) / LaneWidth);
        return Math.Clamp(lane, -1, _cm.LaneCount);
    }

    /// <summary>
    /// Snaps to the bottom grid.
    /// </summary>
    /// <param name="localMousePosY">ChartRenderer.GetLocalMousePosition().Y</param>
    public long SnapToGrid(float localMousePosY, bool snapToGrid = true)
    {
        long tick = (long)Mathf.Round(-localMousePosY);
        if(!snapToGrid) { return tick; }

        // snap to grid
        if(tick <= 0) { return 0; } // trivial
        else
        {
            int index = _cm.IndexOfTimeSignatureAt(tick);

            if(index >= 0) { return tick; } // exact match
            else
            {
                index = ~index;
                if(index < _cm.TimeSignatures.Count) { index--; }
                else { index = _cm.TimeSignatures.Count - 1; }

                // current time signature
                RawChart.RawTimeSignature ts = _cm.TimeSignatures[index];
                long tpm = ts.Numerator * 4 * _cm.PPQN / ts.Denominator; // ticks per measure
                long tpg; // ticks per grid

                // tpg
                if(GridDivision <= 0) { tpg = 4 * _cm.PPQN / ts.Denominator; }
                else { tpg = 4 * _cm.PPQN / GridDivision; }

                // snap tick to grid
                return ts.StartTick + (tick - ts.StartTick) / tpm * tpm + (tick - ts.StartTick) % tpm / tpg * tpg;
            }
        }
    }

    // draw barlines, measure numbers, and lines of beats (Denominator-th or GridDivision-th)
    private void DrawGrid()
    {
        if(_cm == null) { return; }

        float halfLineLength = HalfGridWidth;
        Vector2 invScale = new Vector2(1/Scale.X, 1/Scale.Y);

        // ======== vertical lines ========
        long bottomPosY = -Math.Max(0, _minRenderTick);
        for(int i=0; i<=_cm.LaneCount; i++)
        {
            float posX = (_cm.LaneCount > 0)? -halfLineLength + i * LaneWidth: 0;
            DrawLine(new Vector2(posX, bottomPosY), new Vector2(posX, -_maxRenderTick), GridColor);
        }

        // ======== horizontal lines ========
        long measureNumber = 0;
        for(int i=0; i<_cm.TimeSignatures.Count; i++)
        {
            // current time signature
            RawChart.RawTimeSignature ts = _cm.TimeSignatures[i];
            long currTick = ts.StartTick; // snap to current time signature
            long endTick;
            long tpm = ts.Numerator * 4 * _cm.PPQN / ts.Denominator; // ticks per measure
            long tpg; // ticks per grid

            // endTick
            if(i+1 < _cm.TimeSignatures.Count) { endTick = _cm.TimeSignatures[i+1].StartTick; }
            else { endTick = long.MaxValue; }

            // tpg
            if(GridDivision <= 0) { tpg = 4 * _cm.PPQN / ts.Denominator; }
            else { tpg = 4 * _cm.PPQN / GridDivision; }

            // skip current time signature
            if(endTick <= _minRenderTick)
            {
                long skippedMeasures = (endTick - ts.StartTick) / tpm;
                if((endTick - ts.StartTick) % tpm != 0) { skippedMeasures++; } // incomplete measure

                measureNumber += skippedMeasures;
                continue;
            }

            // stop drawing
            if(ts.StartTick > _maxRenderTick) { return; }

            // optimize currTick
            if(currTick < _minRenderTick)
            {
                long skippedMeasures = (_minRenderTick - currTick) / tpm;

                measureNumber += skippedMeasures;
                currTick += skippedMeasures * tpm;
            }

            while(currTick < endTick && currTick <= _maxRenderTick)
            {
                Color lineColor;

                // determine line color
                if((currTick - ts.StartTick) % tpm == 0) // is barline?
                {
                    lineColor = BarlineColor;
                    measureNumber++;

                    // draw measure number
                    DrawSetTransform(new Vector2(halfLineLength, -currTick), 0, invScale);
                    DrawString(ThemeDB.FallbackFont, Vector2.Zero, measureNumber.ToString(), modulate: lineColor);
                    DrawSetTransform(Vector2.Zero, 0, Vector2.One); // reset transform
                }
                else { lineColor = GridColor; }

                // draw line
                DrawLine(new Vector2(-halfLineLength, -currTick), new Vector2(halfLineLength, -currTick), lineColor);

                // increase currTick
                long prevTick = currTick; // safety to prevent infinite loop

                if((currTick - ts.StartTick) / tpm != (currTick + tpg - ts.StartTick) / tpm) // new measure?
                {
                    currTick = ts.StartTick + (currTick + tpg - ts.StartTick) / tpm * tpm;
                }
                else { currTick += tpg; }

                if(prevTick == currTick) { break; }
            }
        }
    }

    private void DrawTempos()
    {
        if(_cm == null) { return; }

        float posX = -HalfGridWidth;
        Vector2 invScale = new Vector2(1/Scale.X, 1/Scale.Y);
        IList<RawChart.RawTempo> list = _cm.Tempos;

        int count = 0; // the number of activated editor objects
        int start = _cm.IndexOfTempoAt(_minRenderTick);
        if(start < 0) { start = ~start; }

        // draw tempos in render range
        for(int i = start; i < list.Count && list[i].StartTick <= _maxRenderTick; i++, count++)
        {
            // spawn new object if needed
            if(!(count < _seo_t.Count)) { _seo_t.Add(_op_t.Spawn()); }

            _seo_t[count].Scale = invScale;
            _seo_t[count].Render(list[i], posX);
            _seo_t[count].SetActive(true);
            _seo_t[count].SetSelected(_type == TypeMenu.Tempo && _selectedIndex == i);
        }

        // hide unused objects
        for(int i=count; i<_seo_t.Count; i++)
        {
            if(!_seo_t[i].Visible) { break; }
            else { _seo_t[i].SetActive(false); }
        }
    }

    private void DrawTimeSignatures()
    {
        if(_cm == null) { return; }
        
        Vector2 invScale = new Vector2(1/Scale.X, 1/Scale.Y);
        IList<RawChart.RawTimeSignature> list = _cm.TimeSignatures;

        int count = 0; // the number of activated editor objects
        int start = _cm.IndexOfTimeSignatureAt(_minRenderTick);
        if(start < 0) { start = ~start; }

        // draw time signatures in render range
        for(int i = start; i < list.Count && list[i].StartTick <= _maxRenderTick; i++, count++)
        {
            // spawn new object if needed
            if(!(count < _seo_ts.Count)) { _seo_ts.Add(_op_ts.Spawn()); }

            _seo_ts[count].Scale = invScale;
            _seo_ts[count].Render(list[i], 0);
            _seo_ts[count].SetActive(true);
            _seo_ts[count].SetSelected(_type == TypeMenu.TimeSignature && _selectedIndex == i);
        }
        
        // hide unused objects
        for(int i=count; i<_seo_ts.Count; i++)
        {
            if(!_seo_ts[i].Visible) { break; }
            else { _seo_ts[i].SetActive(false); }
        }
    }

    private void DrawSvChanges()
    {
        if(_cm == null) { return; }

        float posX = HalfGridWidth;
        Vector2 invScale = new Vector2(1/Scale.X, 1/Scale.Y);
        IList<RawChart.RawSvChange> list = _cm.SvChanges;

        int count = 0; // the number of activated editor objects
        int start = _cm.IndexOfSvChangeAt(_minRenderTick);
        if(start < 0) { start = ~start; }

        // draw Sv changes in render range
        for(int i = start; i < list.Count && list[i].StartTick <= _maxRenderTick; i++, count++)
        {
            // spawn new object if needed
            if(!(count < _seo_svc.Count)) { _seo_svc.Add(_op_svc.Spawn()); }

            _seo_svc[count].Scale = invScale;
            _seo_svc[count].Render(list[i], posX);
            _seo_svc[count].SetActive(true);
            _seo_svc[count].SetSelected(_type == TypeMenu.SvChange && _selectedIndex == i);
        }

        // hide unused objects
        for(int i=count; i<_seo_svc.Count; i++)
        {
            if(!_seo_svc[i].Visible) { break; }
            else { _seo_svc[i].SetActive(false); }
        }
    }

    private void DrawNotes()
    {
        if(_cm == null) { return; }

        int count = 0; // the number of activated editor objects
        float invScaleY = 1 / Scale.Y;

        // for each lane...
        for(int laneIndex = 0; laneIndex < _cm.LaneCount; laneIndex++)
        {
            float posX = (-0.5f * _cm.LaneCount + 0.5f + laneIndex) * LaneWidth;
            IList<RawChart.RawNote> list = _cm.GetLane(laneIndex);

            int start = _cm.IndexOfNoteAt(_minRenderTick, laneIndex);
            if(start < 0) { start = ~start; }
            if(start > 0 && list[start-1].EndTick >= _minRenderTick) { start--; } // consider long note
            
            // draw tempos in render range
            for(int i = start; i < list.Count && list[i].StartTick <= _maxRenderTick; i++, count++)
            {
                // spawn new object if needed
                if(!(count < _seo_n.Count)) { _seo_n.Add(_op_n.Spawn()); }

                _seo_n[count].Render(list[i], posX, invScaleY);
                _seo_n[count].SetActive(true);
                _seo_n[count].SetSelected(_type == TypeMenu.Note && _selectedIndex == i && _selectedLane == laneIndex);
            }
        }
        
        // hide unused objects
        for(int i=count; i<_seo_n.Count; i++)
        {
            if(!_seo_n[i].Visible) { break; }
            else { _seo_n[i].SetActive(false); }
        }
    }

    public override void _Draw()
    {
        if(_cm == null) { return; }

        UpdateTransform();
        CalculateRenderRange();

        DrawGrid();

        DrawTempos();
        DrawTimeSignatures();
        DrawSvChanges();
        DrawNotes();
    }
}
