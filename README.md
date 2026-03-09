# 🎵 Simple Rhythm Game Library (SRGL)
SRGL은 **Godot 4 (.NET)** 기반의 키보드 리듬게임 라이브러리입니다.  
캐주얼한 건반형 리듬게임(VSRG) 제작에 특화되어있습니다.  
현재 2D만 지원됩니다.
## ✨ 특징
- 게임 로직과 비주얼이 완전히 분리되어 있습니다.
- JSON 형식의 채보 파일을 사용합니다.
- 박자표에 맞춰 롱노트 틱과 마디선을 자동으로 생성합니다.
- 기본적인 스크롤 변속, 정지 연출을 지원합니다.
- 타이밍 윈도우, 입력 시스템, 사용자 오프셋, 노트 속도 등을 커스텀할 수 있습니다.
- IJudgementStrategy 인터페이스를 사용하여 노트 종류별 판정 로직을 쉽게 추가할 수 있습니다.
- ObjectPool 클래스로 노트와 이펙트를 관리하는 기본적인 최적화를 수행했습니다.
## 🛠️ 설치
SRGL은 C# 스크립트 기반 라이브러리입니다. **Godot 4 (.NET)** 프로젝트의 하위 경로에 클론하여 사용하세요.
```bash
git clone https://github.com/joung2321/SRGL.git
```
## 🚀 퀵 스타트
본 절에서는 간단한 4키 리듬게임을 구현합니다.
### 1. 음원 파일 준비
아래 악보에 대한 음원 파일을 준비합니다. 확장자는 OGG를 권장합니다.  
본 예제에서는 **quickStart.ogg**를 사용합니다.
![quickStart_score](./assets/quickStart.png)
**⚠️주의:** MuseScore를 사용할 경우, MuseScore가 생성한 음원을 Audacity에서 OGG로 다시 저장해야 Godot 4가 정상적으로 로드할 수 있는 OGG 파일이 됩니다.
### 2. 채보 파일 작성
채보 파일로 사용할 **chart_quickStart.json**을 작성합니다.
```json
{
	"AudioPath": "res://quickStart.ogg",

	"PPQN": 10,
	"EndOfTrack": 200,
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
채보 파일 설명: 작성예정 (일단 지금은 RawChart.cs의 주석을 참고해주세요.)
### 3. 게임 로직 구현
```csharp
using Godot;

// SRGL 네임스페이스 사용
using SRGL;
using SRGL.Common;

public partial class Main : Node
{
    public override void _Ready()
    {
        // 채보 로드
        RawChart rc = RawChartLoader.Load("res://chart_quickStart.json");
        Chart c = new Chart(rc);

        // 음원 로드
        SongPlayer sp = new SongPlayer(this);
        sp.LoadSong(c.AudioPath);

        // 타이밍 윈도우 설정 (Arcaea 스타일)
        TimingWindow tw = new TimingWindow();
        tw.Partition(25 * 1_000); // pure (세부)
        tw.Partition(50 * 1_000); // pure
        tw.Partition(100 * 1_000); // far
        tw.RadiusUsec = 120 * 1_000; // lost

        // 판정 로직 정의
        JudgementQueue jq = new JudgementQueue(c.LaneCount, tw);
        jq.AddStrategy(0, new TapNoteJS()); // 채보 파일에서, LogicType의 기본값은 0

        // 입력 시스템 설정
        StandardBSM bsm = new StandardBSM();
        bsm.AssignKey(Key.D, 0);
        bsm.AssignKey(Key.F, 1);
        bsm.AssignKey(Key.J, 2);
        bsm.AssignKey(Key.K, 3);

        // 사용자 오프셋 설정 [us]
        long userOffsetUsec = 0;

        // 게임 로직 생성
        LogicManager lm = new LogicManager(c, sp, jq, bsm, userOffsetUsec);
        jq.NoteJudged += OnNoteJudged; // 판정 이벤트 구독
        AddChild(lm); // 게임 로직을 SceneTree에 추가

        // 게임플레이 시작
        sp.Resume();
    }

    // 판정 이름 (Arcaea 스타일)
    private string[] judgementNames = { "PURE", "Pure", "FAR ", "LOST" };

    // 판정 이벤트 핸들러
    private void OnNoteJudged(Judgement j)
    {
        // 판정 정보 출력
        GD.Print($"[Lane{j.Lane}] {judgementNames[j.PartitionIndex]}, Error: {j.ErrorUsec / 1000} [ms]");
    }
}
```
### 4. 비주얼 구현
작성예정
## 📄 라이선스
SRGL은 MIT 라이선스를 따릅니다.
