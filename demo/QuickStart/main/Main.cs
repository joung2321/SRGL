using Godot;

using SRGL;
using SRGL.Common;
using SRGL.Standard;

public partial class Main : Node
{
    [Export] private JudgementLine _judgementLine;

    // logic
    private SongPlayer _sp;
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
        JudgementQueue jq = new JudgementQueue(c.LaneCount, tw);
        jq.AddStrategy(0, new TapNoteJudgementStrategy());

        // define key map
        StandardInputMapper sim = new StandardInputMapper();
        sim.AssignKey(Key.D, 0);
        sim.AssignKey(Key.F, 1);
        sim.AssignKey(Key.J, 2);
        sim.AssignKey(Key.K, 3);

        // user offset [us]
        long userOffsetUsec = 0;

        // create game logic
        _lm = new LogicManager(c, _sp, jq, sim, userOffsetUsec);
        AddChild(_lm);

        // ======== visual ========
        // note pool
        _nm = new NoteManager(_judgementLine, VisualVariationSelector.Select4K);

        _nm.AddNoteType(Constants.BarlineVisualType, "res://visual/BarlineObject.tscn", 4);
        _nm.AddNoteType(0, "res://visual/note/TapNoteObject.tscn", 12);

        _nm.UserSpeedPxPerSec = 500;
        _nm.Listen(jq, _lm);

        // effect pool
        _em = new EffectManager(_judgementLine);
        _em.AddEffectType(TapNoteJudgementStrategy.CONTEXT_HIT, "res://visual/effect/HitEffectObject.tscn", 8);
        _em.Listen(jq);

        // run
        _sp.Resume();
    }
}
