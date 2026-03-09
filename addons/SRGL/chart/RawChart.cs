namespace SRGL;

using System.Text.Json.Serialization;
using SRGL.Common;

// json file format
public class RawChart
{
    // ======== metadata ========

    // ======== chart data ========
    public string ImagePath { get; init; }
    public string AudioPath { get; init; }
    
    public long PPQN { get; init; } // pulses per quarter note
    public long EndOfTrack { get; init; } // length of this chart (PPQN value)
    public int LaneCount { get; init; } // the number of logical lanes
    public long OffsetUsec { get; init; } // audio offset [us]

    public RawTempo[] Tempos { get; init; }
    public RawTimeSignature[] TimeSignatures { get; init; }
    public RawSvChange[] SvChanges { get; init; }
    public RawNote[] Notes { get; init; }
    // ======== end ========

    public readonly record struct RawTempo
    {
        [JsonPropertyName("S")]
        public long StartTick { get; init; }

        [JsonPropertyName("B")]
        public double Bpm { get; init; }
    }
    
    public readonly record struct RawTimeSignature
    {
        [JsonPropertyName("S")]
        public long StartTick { get; init; }

        [JsonPropertyName("N")]
        public int Numerator { get; init; }

        [JsonPropertyName("D")]
        public int Denominator { get; init; }
    }

    public readonly record struct RawSvChange // Scroll Velocity
    {
        [JsonPropertyName("S")]
        public long StartTick { get; init; }

        [JsonPropertyName("M")]
        public double Multiplier { get; init; }

        [JsonPropertyName("I")]
        public InterpolationType Interpolation { get; init; }
    }
    
    public readonly record struct RawNote
    {
        [JsonPropertyName("S")]
        public long StartTick { get; init; }

        [JsonPropertyName("E")]
        public long EndTick { get; init; }
        
        [JsonPropertyName("L")]
        public int Lane { get; init; }

        [JsonPropertyName("J")] // Judgement strategy
        public int LogicType { get; init; }

        [JsonPropertyName("V")]
        public int VisualType { get; init; }

        [JsonPropertyName("T")]
        public int TickRate { get; init; } // how many times Denominator-th note is divided
        
        [JsonPropertyName("O")]
        public NoteOptions Options { get; init; }
    }
}