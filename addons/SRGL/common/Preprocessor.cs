namespace SRGL.Common;

using System;
using System.Collections.Generic;

public static class Preprocessor
{
    public static Tempo[] PreprocessTempos(RawChart.RawTempo[] rawArr, long ppqn)
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

        return arr;
    }

    public static TimeSignature[] PreprocessTimeSignatures(RawChart.RawTimeSignature[] rawArr, long ppqn)
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

        return arr;
    }

    public static SvChange[] PreprocessSvChanges(RawChart.RawSvChange[] rawArr, long ppqn, Tempo[] tempos)
    {
        // construct array
        int len = rawArr.Length;
        SvChange[] arr = new SvChange[len];

        // fill array
        arr[0] = new SvChange
        {
            StartTimeSec = 0,
            Multiplier = rawArr[0].Multiplier,
            Position = 0,
            Interpolation = rawArr[0].Interpolation
        };

        int cachedIndex = 0;
        for(int i=1; i<len; i++)
        {
            double st = Converter.TickToTime(rawArr[i].StartTick, ppqn, tempos, ref cachedIndex); // start time
            double dt = st - arr[i-1].StartTimeSec;
            double p = arr[i-1].Position + arr[i-1].Multiplier * dt; // position
            
            arr[i] = new SvChange
            {
                StartTimeSec = st,
                Multiplier = rawArr[i].Multiplier,
                Position = p,
                Interpolation = rawArr[i].Interpolation
            };
        }

        return arr;
    }

    public static NoteData[] PreprocessNotes(RawChart.RawNote[] rawArr, long ppqn, Tempo[] tempos, TimeSignature[] timeSignatures, SvChange[] svChanges)
    {
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
            long[] mt; // Middle Times

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
                mt = Array.Empty<long>();
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

        return arr;
    }

    // middle times [us]
    private static readonly List<long> _mt = new List<long>(128);

    private static long[] GenerateMiddleTimesUsec(long headPulse, long tailPulse, int tickRate, long ppqn, TimeSignature[] timeSignatures, Tempo[] tempos)
    {
        if(tickRate <= 0) { return Array.Empty<long>(); }

        // local variables
        int i = 0; // index for timeSignatures
        int cachedIndex = 0; // cached index for Converter.TickToTime()

        // return value: middle times [us]
        _mt.Clear(); // reuse _mt to avoid GC

        // find starting point
        while(i < timeSignatures.Length && timeSignatures[i].EndTick <= headPulse) { i++; }

        // for each time signature
        while(i < timeSignatures.Length && timeSignatures[i].StartTick <= tailPulse)
        {
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

                    _mt.Add(t_us);
                }
                else if(tailPulse <= currTick) { break; }

                currTick += dTick;
            }

            i++;
        }

        if(_mt.Count > 0) { return _mt.ToArray(); }
        else { return Array.Empty<long>(); }
    }

    // barline times [s]
    private static readonly List<double> _bt = new List<double>(128);

    /// <summary>
    /// [CAUTION] For barlines, Lane = LogicType = VisualType = -1.
    /// </summary>
    public static NoteData[] GenerateBarlines(long endOfTrack, long ppqn, TimeSignature[] timeSignatures, Tempo[] tempos, SvChange[] svChanges)
    {
        // return value: barline positions [s]
        _bt.Clear(); // reuse _bp to avoid GC

        int i = 0; // index for timeSignatures, _bt, arr
        int cachedIndex = 0; // cached index for Converter.TickToTime(), Converter.TimeToPosition()

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
                    _bt.Add(Converter.TickToTime(currTick, ppqn, tempos, ref cachedIndex));
                }
                else { break; }

                currTick += dTick;
            }

            i++;
        }

        // ======== generate note data of barlines ========
        cachedIndex = 0;

        if(_bt.Count <= 0) { return Array.Empty<NoteData>(); }
        else
        {
            // construct array
            int len = _bt.Count;
            NoteData[] arr = new NoteData[len];

            for(i=0; i<len; i++)
            {
                long t = (long)Math.Round(_bt[i] * 1_000_000);
                NoteLogicData nld = new NoteLogicData
                {
                    Lane = Constants.BarlineLane,
                    StartTimeUsec = t,
                    LogicType = Constants.BarlineLogicType,

                    EndTimeUsec = t,
                    MiddleTimesUsec = Array.Empty<long>(),

                    Options = NoteOptions.Dummy
                };

                NoteVisualData nvd = new NoteVisualData
                {
                    Lane = Constants.BarlineLane,
                    Position = _bt[i],
                    Length = 0,
                    VisualType = Constants.BarlineVisualType
                };

                arr[i] = new NoteData{ LogicData = nld, VisualData = nvd };
            }

            return arr;
        }
    }
}