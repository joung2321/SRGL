namespace SRGL;

using System;
using System.Text.Json.Serialization;
using SRGL.Common;

// json file format
public class RawChart
{
    // ======== metadata ========
    /// <summary>
    /// semantic versioning e.g.) 1.2.3
    /// </summary>
    public string FormatVersion { get; init; }
    
    public string Title { get; init; } = "";
    public string Composer { get; init; } = "";
    public string Illustrator { get; init; } = "";
    public string Charter { get; init; } = "";

    public int DifficultyCategory { get; init; }
    public float DifficultyLevel { get; init; }

    public string Description { get; init; } = "";

    // ======== chart data ========
    public string ImagePath { get; init; }
    public string AudioPath { get; init; }
    
    public long PPQN { get; init; } // pulses per quarter note
    public long EndOfTrack { get; init; } // length of this chart (PPQN value)
    public int LaneCount { get; init; } // the number of logical lanes
    public long OffsetUsec { get; init; } // audio offset [us]

    public RawTempo[] Tempos { get; init; } = Array.Empty<RawTempo>();
    public RawTimeSignature[] TimeSignatures { get; init; } = Array.Empty<RawTimeSignature>();
    public RawSvChange[] SvChanges { get; init; } = Array.Empty<RawSvChange>();
    public RawNote[] Notes { get; init; } = Array.Empty<RawNote>();
    // ======== end ========

    public readonly record struct RawTempo : IComparable<RawTempo>
    {
        [JsonPropertyName("S")]
        public long StartTick { get; init; }

        [JsonPropertyName("B")]
        public double Bpm { get; init; }

        // sort by StartTick
        public int CompareTo(RawTempo other) { return StartTick.CompareTo(other.StartTick); }

        // validity
        public bool IsValid() { return StartTick >= 0 && Bpm > 0; }
    }
    
    public readonly record struct RawTimeSignature : IComparable<RawTimeSignature>
    {
        [JsonPropertyName("S")]
        public long StartTick { get; init; }

        [JsonPropertyName("N")]
        public int Numerator { get; init; }

        [JsonPropertyName("D")]
        public int Denominator { get; init; }

        // sort by StartTick
        public int CompareTo(RawTimeSignature other) { return StartTick.CompareTo(other.StartTick); }

        // validity
        public bool IsValid() { return StartTick >= 0 && Numerator > 0 && Denominator > 0; }
    }

    public readonly record struct RawSvChange : IComparable<RawSvChange> // Scroll Velocity
    {
        [JsonPropertyName("S")]
        public long StartTick { get; init; }

        [JsonPropertyName("M")]
        public double Multiplier { get; init; }

        [JsonPropertyName("I")]
        public InterpolationType Interpolation { get; init; }

        // sort by StartTick
        public int CompareTo(RawSvChange other) { return StartTick.CompareTo(other.StartTick); }

        // validity
        public bool IsValid() { return StartTick >= 0; }
    }
    
    public readonly record struct RawNote : IComparable<RawNote>
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

        /// <summary>
        /// how many times Denominator-th note is divided
        /// </summary>
        [JsonPropertyName("T")]
        public int TickRate { get; init; }
        
        [JsonPropertyName("O")]
        public NoteOptions Options { get; init; }

        // sort by StartTick
        public int CompareTo(RawNote other) { return StartTick.CompareTo(other.StartTick); }

        // validity
        public bool IsValid(int laneCount) { return StartTick >= 0 && 0 <= Lane && Lane < laneCount && TickRate >= 0; }
    }
}