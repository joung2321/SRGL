namespace SRGL.Common;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

public static class Preprocessor
{
    public static ImmutableArray<Tempo> PreprocessTempos(RawChart.RawTempo[] rawArr, long ppqn)
    {
        // construct array
        int len = rawArr.Length;
        Tempo[] arr = new Tempo[len];

        // fill array
        arr[0] = new Tempo
        {
            StartTick = 0,
            Bpm = rawArr[0].Bpm,
            StartTimeSec = 0
        };

        for(int i=1; i<len; i++)
        {
            long dPulse = rawArr[i].StartTick - rawArr[i-1].StartTick;
            double st = arr[i-1].StartTimeSec + dPulse * 60 / (arr[i-1].Bpm * ppqn); // start time
            
            arr[i] = new Tempo
            {
                StartTick = rawArr[i].StartTick,
                Bpm = rawArr[i].Bpm,
                StartTimeSec = st
            };
        }

        return arr.ToImmutableArray();
    }

    public static ImmutableArray<TimeSignature> PreprocessTimeSignatures(RawChart.RawTimeSignature[] rawArr, long ppqn)
    {
        // construct array
        int len = rawArr.Length;
        TimeSignature[] arr = new TimeSignature[len];

        // fill array
        for(int i=0; i<len; i++)
        {
            long et = (i+1 < len)? rawArr[i+1].StartTick: long.MaxValue; // end tick
            long tpb = 4 * ppqn / rawArr[i].Denominator; // ticks per beat

            arr[i] = new TimeSignature
            {
                StartTick = rawArr[i].StartTick,
                EndTick = et,
                Numerator = rawArr[i].Numerator,
                TicksPerBeat = tpb
            };
        }

        return arr.ToImmutableArray();
    }

    public static ImmutableArray<SvChange> PreprocessSvChanges(RawChart.RawSvChange[] rawArr, long ppqn, ImmutableArray<Tempo> tempos)
    {
        // construct array
        int len = rawArr.Length;
        SvChange[] arr = new SvChange[len];

        // fill array
        int cachedIndex = 0;
        for(int i=0; i<len; i++)
        {
            double st, p, d, em; // start time, position, duration, end multiplier
            
            // start time, position
            if(i == 0)
            {
                st = 0;
                p = 0;
            }
            else
            {
                st = Converter.TickToTime(rawArr[i].StartTick, ppqn, tempos, ref cachedIndex);

                switch(rawArr[i-1].Interpolation)
                {
                    default:
                    case InterpolationType.Step:
                    case InterpolationType.Impulse:
                    p = arr[i-1].Position + arr[i-1].Multiplier * arr[i-1].DurationSec;
                    break;

                    case InterpolationType.Linear:
                    p = (arr[i-1].Multiplier + arr[i-1].EndMultiplier) * arr[i-1].DurationSec / 2;
                    break;
                }
            }

            // duration, end multiplier
            if(i+1 < len)
            {
                d = Converter.TickToTime(rawArr[i+1].StartTick, ppqn, tempos, ref cachedIndex) - st;
                em = rawArr[i+1].Multiplier;
            }
            else
            {
                d = double.PositiveInfinity;
                em = rawArr[i].Multiplier;
            }

            arr[i] = new SvChange
            {
                StartTimeSec = st,
                Multiplier = rawArr[i].Multiplier,
                Position = p,
                Interpolation = rawArr[i].Interpolation,

                DurationSec = d,
                EndMultiplier = em
            };
        }

        return arr.ToImmutableArray();
    }

    public static ImmutableArray<NoteData> PreprocessNotes(RawChart.RawNote[] rawArr, long ppqn, ImmutableArray<Tempo> tempos, ImmutableArray<TimeSignature> timeSignatures, ImmutableArray<SvChange> svChanges)
    {
        // check if rawArr is null or empty
        if(rawArr == null || rawArr.Length <= 0) { return ImmutableArray<NoteData>.Empty; }

        // construct array
        int len = rawArr.Length;
        NoteData[] arr = new NoteData[len];

        // fill array
        int cachedBpmIndex = 0;
        int cachedSvIndex = 0;
        for(int i=0; i<len; i++)
        {
            RawChart.RawNote rawNote = rawArr[i];

            double st, et, p, l; // Start Time, End Time, Position, Length
            ImmutableArray<long> mt; // Middle Times

            st = Converter.TickToTime(rawNote.StartTick, ppqn, tempos, ref cachedBpmIndex);
            p = Converter.TimeToPosition(st, svChanges, ref cachedSvIndex);

            if(rawNote.StartTick < rawNote.EndTick) // long note
            {
                et = Converter.TickToTime(rawNote.EndTick, ppqn, tempos, ref cachedBpmIndex);
                l = Converter.TimeToPosition(et, svChanges, ref cachedSvIndex) - p;
                mt = GenerateMiddleTimesUsec(rawNote.StartTick, rawNote.EndTick, rawNote.TickRate, ppqn, timeSignatures, tempos);
            }
            else // tap note
            {
                et = st;
                l = 0;
                mt = ImmutableArray<long>.Empty;
            }

            NoteLogicData nld = new NoteLogicData
            {
                Lane = rawNote.Lane,
                StartTimeUsec = (long)Math.Round(st * 1_000_000),
                LogicType = rawNote.LogicType,

                EndTimeUsec = (long)Math.Round(et * 1_000_000),
                MiddleTimesUsec = mt,

                Options = rawNote.Options
            };

            NoteVisualData nvd = new NoteVisualData
            {
                Lane = rawNote.Lane,
                Position = p,
                Length = l,
                VisualType = rawNote.VisualType
            };

            arr[i] = new NoteData { LogicData = nld, VisualData = nvd };
        }

        return arr.ToImmutableArray();
    }

    private static ImmutableArray<long> GenerateMiddleTimesUsec(long headPulse, long tailPulse, int tickRate, long ppqn, ImmutableArray<TimeSignature> timeSignatures, ImmutableArray<Tempo> tempos)
    {
        if(tickRate <= 0) { return ImmutableArray<long>.Empty; }

        // local variables
        int i = 0; // index for timeSignatures
        int cachedIndex = 0; // cached index for Converter.TickToTime()

        // return value: middle times [us]
        List<long> mt = new List<long>(128);

        // find starting point
        while(i < timeSignatures.Length && timeSignatures[i].EndTick <= headPulse) { i++; }

        // for each time signature
        while(i < timeSignatures.Length && timeSignatures[i].StartTick <= tailPulse)
        {
            // check truncating division
            if(timeSignatures[i].TicksPerBeat % tickRate != 0) { throw new SrglException("inappropriate tickRate"); }

            long currTick = timeSignatures[i].StartTick;
            long dTick = timeSignatures[i].TicksPerBeat / tickRate;

            // ignore beats before headPulse
            if(currTick < headPulse)
            {
                currTick += ((headPulse - currTick) / dTick + 1) * dTick;
            }

            // generate ticks for current time signature
            while(currTick < timeSignatures[i].EndTick)
            {
                if(headPulse < currTick && currTick < tailPulse) // consider middle ticks ONLY
                {
                    double t = Converter.TickToTime(currTick, ppqn, tempos, ref cachedIndex);
                    long t_us = (long)Math.Round(t * 1_000_000);

                    mt.Add(t_us);
                }
                else if(tailPulse <= currTick) { break; }

                currTick += dTick;
            }

            i++;
        }

        if(mt.Count > 0) { return mt.ToImmutableArray(); }
        else { return ImmutableArray<long>.Empty; }
    }

    /// <summary>
    /// [CAUTION] For barlines, Lane = LogicType = VisualType = -1.
    /// </summary>
    public static ImmutableArray<NoteData> GenerateBarlines(long endOfTrack, long ppqn, ImmutableArray<TimeSignature> timeSignatures, ImmutableArray<Tempo> tempos, ImmutableArray<SvChange> svChanges)
    {
        // barline times [s]
        List<double> bt = new List<double>(128);

        int i = 0; // index for timeSignatures, bt, arr
        int cachedIndex = 0; // cached index for Converter.TickToTime()

        // ======== calculate start times of barlines ========
        // for each time signature
        while(i < timeSignatures.Length && timeSignatures[i].StartTick <= endOfTrack)
        {
            long currTick = timeSignatures[i].StartTick;
            long dTick = timeSignatures[i].Numerator * timeSignatures[i].TicksPerBeat; // the number of ticks in a measure

            // generate ticks for current time signature
            while(currTick < timeSignatures[i].EndTick)
            {
                if(currTick <= endOfTrack)
                {
                    bt.Add(Converter.TickToTime(currTick, ppqn, tempos, ref cachedIndex));
                }
                else { break; }

                currTick += dTick;
            }

            i++;
        }

        // ======== generate note data of barlines ========
        cachedIndex = 0; // cached index for Converter.TimeToPosition()

        if(bt.Count <= 0) { return ImmutableArray<NoteData>.Empty; }
        else
        {
            // construct array
            int len = bt.Count;
            NoteData[] arr = new NoteData[len];

            for(i=0; i<len; i++)
            {
                long t = (long)Math.Round(bt[i] * 1_000_000);
                NoteLogicData nld = new NoteLogicData
                {
                    Lane = Constants.BarlineLane,
                    StartTimeUsec = t,
                    LogicType = Constants.BarlineLogicType,

                    EndTimeUsec = t,
                    MiddleTimesUsec = ImmutableArray<long>.Empty,

                    Options = NoteOptions.Dummy
                };

                NoteVisualData nvd = new NoteVisualData
                {
                    Lane = Constants.BarlineLane,
                    Position = Converter.TimeToPosition(bt[i], svChanges, ref cachedIndex),
                    Length = 0,
                    VisualType = Constants.BarlineVisualType
                };

                arr[i] = new NoteData{ LogicData = nld, VisualData = nvd };
            }

            return arr.ToImmutableArray();
        }
    }
}