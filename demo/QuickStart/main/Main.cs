using Godot;

using SRGL;
using SRGL.Common;
using SRGL.Standard;
using SRGL.Misc;

public partial class Main : Node
{
    [Export] private JudgementLine _judgementLine;
    [Export] private ComboCounter _comboCounter;

    // logic
    private SongPlayer _sp;
    private JudgementQueue _jq;
    private LogicManager _lm;

    // visual
    private NoteManager _nm;
    private EffectManager _em;

    public override void _Ready()
    {
        // ======== logic ========
        // load chart
        RawChart rc = RawChartLoader.Load("res://chart/quickStart.json");
        Chart c = new Chart(rc);

        // load audio
        _sp = new SongPlayer(this);
        _sp.LoadSong(c.AudioPath);

        // define timing window (reference: Arcaea)
        TimingWindow tw = new TimingWindow();
        tw.Partition(25 * 1_000); // pure (exact)
        tw.Partition(50 * 1_000); // pure
        tw.Partition(100 * 1_000); // far
        tw.RadiusUsec = 120 * 1_000; // lost

        // define judgement logic
        _jq = new JudgementQueue(c.LaneCount, tw);
        _jq.AddStrategy(0, new TapNoteJudgementStrategy());

        // define key map
        StandardInputMapper sim = new StandardInputMapper();
        sim.AssignKey(Key.D, 0);
        sim.AssignKey(Key.F, 1);
        sim.AssignKey(Key.J, 2);
        sim.AssignKey(Key.K, 3);

        // user offset [us]
        long userOffsetUsec = 0;

        // create game logic
        _lm = new LogicManager(c, _sp, _jq, sim, userOffsetUsec);
        AddChild(_lm);

        // ======== visual ========
        // note pool
        _nm = new NoteManager(_judgementLine, VisualVariationSelector.Select4K);
        _nm.AddNoteType(Constants.BarlineVisualType, "res://visual/BarlineObject.tscn", 8);
        _nm.AddNoteType(0, "res://visual/note/TapNoteObject.tscn", 16);
        _nm.UserSpeedPxPerSec = 500;

        _nm.Listen(_jq, _lm);

        // effect pool
        _em = new EffectManager(_judgementLine);
        _em.AddEffectType(TapNoteJudgementStrategy.CONTEXT_HIT, "res://visual/effect/HitEffectObject.tscn", 4);

        _em.Listen(_jq);

        // ======== misc ========
        // combo counter
        _comboCounter.ComboBreakPartitionIndex = tw.GetLastPartitionIndex();
        _jq.NoteJudged += _comboCounter.OnNoteJudged;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if(@event is InputEventKey ek && ek.Pressed && !ek.Echo)
        {
            // resume and pause
            if(ek.Keycode == Key.Space)
            {
                if(_sp.Playing) { _sp.Pause(); }
                else { _sp.Resume(); }
            }

            // reset gameplay
            if(ek.Keycode == Key.Escape)
            {
                _sp.Stop();

                _jq.Clear();
                _lm.Reset();
                _nm.DespawnAllNotes();
                _comboCounter.Reset();
            }
        }
    }
}
