# 🎵 Simple Rhythm Game Library (SRGL)
SRGL은 **Godot 4 (.NET)** 기반의 키보드 리듬게임 라이브러리입니다.  
캐주얼한 건반형 리듬게임(VSRG) 제작에 특화되어있습니다.  
현재 2D만 지원합니다.
## ✨ 특징
- 게임 로직과 비주얼이 완전히 분리되어 있습니다.
- JSON 형식의 채보 파일을 사용합니다.
- 박자표를 기준으로 롱노트 틱과 마디선을 자동으로 생성합니다.
- 기본적인 스크롤 변속과 정지 연출이 가능합니다.
- 매 프레임마다 오디오 드리프트를 자동으로 보정합니다.
- 타이밍 윈도우, 입력 시스템, 사용자 오프셋, 스크롤 속도 등을 커스텀할 수 있습니다.
- IJudgementStrategy 인터페이스로 노트 종류별 판정 로직을 쉽게 추가할 수 있습니다.
- JudgementLine, NoteObject, EffectObject 클래스로 비주얼 요소들을 쉽게 디자인 할 수 있습니다.
## 🛠️ 설치
SRGL은 C# 스크립트 기반 라이브러리입니다. **Godot 4 (.NET)** 프로젝트의 하위 경로에 클론하여 사용하세요.
```bash
git clone https://github.com/joung2321/SRGL.git
```
데모 프로젝트를 빌드하려면 심볼릭 링크를 생성해야 합니다.
```cmd
REM Windows
mklink /J SRGL\demo\QuickStart\addons SRGL\addons
```
```bash
# Linux
# [CAUTION] Untested command
ln -s SRGL/addons SRGL/demo/QuickStart/addons
```
## 🚀 퀵 스타트
본 절에서는 간단한 4키 리듬게임을 구현합니다.  
자세한 내용은 [demo/QuickStart](./demo/QuickStart/)를 참고하세요.
### 1. 음원 파일 준비
아래 악보에 대한 음원 파일을 준비합니다. 확장자는 OGG를 권장합니다.  
본 예제에서는 [quickStart.ogg](./demo/QuickStart/chart/quickStart.ogg)를 사용합니다.
![quickStart_score](./assets/quickStart_score.png)
**⚠️주의:** MuseScore를 사용할 경우, MuseScore가 생성한 음원을 Audacity에서 OGG로 다시 저장해야 Godot 4가 정상적으로 로드할 수 있는 OGG 파일이 됩니다.
### 2. 채보 파일 작성
채보 파일로 사용할 [quickStart.json](./demo/QuickStart/chart/quickStart.json)을 작성합니다.
```json
{
	"FormatVersion": "1.0.0",
	"Title": "",

	"AudioPath": "res://chart/quickStart.ogg",

	"PPQN": 10,
	"EndOfTrack": 320,
	"LaneCount": 4,
	"OffsetUsec": 2000,

	"Tempos":         [ {"s":0, "b":120} ],
	"TimeSignatures": [ {"s":0, "n":4, "d":4} ],
	"SvChanges":      [ {"s":0, "m":1} ],

	"Notes":
	[
		{"s":10, "l":0},
		{"s":20, "l":0},
		{"s":30, "l":1},

		{"s":50, "l":1},
		{"s":60, "l":1},
		{"s":70, "l":2},

		{"s":90,  "l":2},
		{"s":100, "l":2},
		{"s":110, "l":3},

		{"s":130, "l":3},
		{"s":140, "l":3},
		{"s":150, "l":0}
	]
}
```
채보 파일 구조: 작성예정
### 3. 비주얼 요소 디자인
#### a) 판정선, 마디선, 노트
SRGL이 기본적으로 제공하는
[JudgementLine.cs](./addons/SRGL/graphics/JudgementLine.cs),
[BarlineObject.cs](./addons/SRGL/standard/BarlineObject.cs),
[TapNoteObject.cs](./addons/SRGL/standard/TapNoteObject.cs)로  
[JudgementLine.tscn](./demo/QuickStart/visual/judgementLine/JudgementLine.tscn),
[BarlineObject.tscn](./demo/QuickStart/visual/BarlineObject.tscn),
[TapNoteObject.tscn](./demo/QuickStart/visual/note/TapNoteObject.tscn)을 각각 디자인합니다.
<p>
<img src="./assets/quickStart_judgementLine.png" width="98.5%">
<img src="./assets/quickStart_barline.png" width="49%">
<img src="./assets/quickStart_tapNote.png" width="49%">
</p>

#### b) 이펙트
SRGL은 EffectObject.cs의 파생 클래스를 제공하지 않습니다.  
[HitEffectObject.cs](./demo/QuickStart/visual/effect/HitEffectObject.cs)를 작성하고,
[HitEffectObject.tscn](./demo/QuickStart/visual/effect/HitEffectObject.tscn)을 디자인합니다.
```csharp
using Godot;

using SRGL;
using SRGL.Common;

public partial class HitEffectObject : EffectObject
{
    [Export] private AnimatedSprite2D _spriteAnimation;

    protected override void OnPlay(Judgement judgement)
    {
        _spriteAnimation.Stop();

        switch(judgement.PartitionIndex)
        {
            case 0:
            case 1:
            _spriteAnimation.Play("pure");
            break;

            case 2:
            _spriteAnimation.Play("far");
            break;

            default:
            _spriteAnimation.Play("default"); // empty animation
            break;
        }
    }
}
```
<p>
<img src="./assets/quickStart_hitEffect.png" width="49%">
<img src="./assets/quickStart_hitEffectAnim.png" width="49%">
</p>

### 4. 게임플레이 로직 작성
[Main.cs](./demo/QuickStart/main/Main.cs)를 작성합니다.
```csharp
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

        // load song
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
                _lm.Reset();
                _nm.DespawnAllNotes();
                _comboCounter.Reset();
            }
        }
    }
}
```
### 5. 게임플레이 씬 디자인
[Main.tscn](./demo/QuickStart/main/Main.tscn)의 루트 노드에 Main.cs를 부착하고, 앞서 만들었던 JudgementLine.tscn을 추가하여 게임플레이 씬을 완성합니다.
![quickStart_main](./assets/quickStart_main.png)
## 📄 라이선스
SRGL은 MIT 라이선스를 따릅니다.
