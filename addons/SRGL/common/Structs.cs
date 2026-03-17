using System.Collections.Immutable;

namespace SRGL.Common;

public record struct Tempo
{
    public long StartTick; // PPQN value
    public double Bpm;
    public double StartTimeSec; // [s]
}

public record struct TimeSignature
{
    public long StartTick; // PPQN value
    public long EndTick; // StartTick of the next time signature
    public int Numerator;
    public long TicksPerBeat; // (the number of ticks in a Denominator-th note) = 4 * ppqn / Denominator
}

// scroll velocity change
public record struct SvChange
{
    public double StartTimeSec; // [s]
    public double Multiplier;
    public double Position; // accumulated value of (Multiplier * StartTime) [s]
    public InterpolationType Interpolation;
}

// note data for judgement
public record struct NoteLogicData
{
    // general info
    // public int Id; // used to map NoteLogicData to Node2D
    public int Lane;
    public long StartTimeUsec; // [us]
    public int LogicType;
    
    // long note info
    public long EndTimeUsec; // [us]
    public ImmutableArray<long> MiddleTimesUsec; // [us]

    // other options
    public NoteOptions Options;
}

public record struct NoteLogicInstance
{
    public int Id;
    public NoteLogicData Data;
}

// note data for rendering
public record struct NoteVisualData
{
    public int Lane;
    public double Position;
    public double Length;
    public int VisualType;
}

// unified note data
public record struct NoteData
{
    public NoteLogicData LogicData { get; init; }
    public NoteVisualData VisualData { get; init; }
}

public record struct Judgement
{
    /// <summary>
    /// ErrorUsec = noteTimeUsec - currentTimeUsec. See TimingWindow.CalculateErrorUsec().
    /// </summary>
    public long ErrorUsec;
    public int PartitionIndex;
    public int Count;
    public int Lane;

    /// <summary>
    /// This value will be used as a effect type. See EffectManager.AddEffectType().
    /// </summary>
    public int Context;

    public bool IsEarly => ErrorUsec > 0;
    public bool IsLate  => ErrorUsec < 0;
}