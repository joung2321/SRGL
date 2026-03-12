namespace SRGL.Common;

using System;
using System.Collections.Immutable;

public static class Converter
{
    /// <summary>
    /// Caches max(index) s.t. tempos[index].Beat &lt;= targetTick<br/>
    /// Converts targetTick to time [s]
    /// </summary>
    /// <param name="tempos">sorted by StartTick in ascending order</param>
    public static double TickToTime(long targetTick, long ppqn, ImmutableArray<Tempo> tempos, ref int cachedIndex)
    {
        // check errors
        if(tempos == null || tempos.Length <= 0) { throw new ArgumentException("tempos is empty."); }

        // correct invalid index
        if(cachedIndex >= tempos.Length) { cachedIndex = tempos.Length - 1; }
        if(cachedIndex < 0) { cachedIndex = 0; }
        
        // finds max(index) s.t. tempos[index].StartTick <= targetTick
        if(tempos[cachedIndex].StartTick < targetTick)
        {
            while(cachedIndex + 1 < tempos.Length && tempos[cachedIndex + 1].StartTick <= targetTick)
            {
                cachedIndex++;
            }
        }
        else if(tempos[cachedIndex].StartTick > targetTick)
        {
            while(cachedIndex - 1 >= 0 && tempos[cachedIndex].StartTick > targetTick)
            {
                cachedIndex--;
            }
        }
        else
        {
            return tempos[cachedIndex].StartTimeSec;
        }

        // calculate time
        long dTick = targetTick - tempos[cachedIndex].StartTick;
        return tempos[cachedIndex].StartTimeSec + dTick * 60 / (tempos[cachedIndex].Bpm * ppqn);
    }

    /// <summary>
    /// Caches max(index) s.t. svChanges[index].StartTime &lt;= targetTimeSec<br/>
    /// Converts targetTimeSec to position
    /// </summary>
    /// <param name="svChanges">sorted by StartTime in ascending order</param>
    public static double TimeToPosition(double targetTimeSec, ImmutableArray<SvChange> svChanges, ref int cachedIndex)
    {
        // check errors
        if(svChanges == null || svChanges.Length <= 0) { return targetTimeSec; } // assume all multipliers are 1.0

        // correct invalid index
        if(cachedIndex >= svChanges.Length) { cachedIndex = svChanges.Length - 1; }
        if(cachedIndex < 0) { cachedIndex = 0; }
                
        // finds max(index) s.t. svChanges[index].StartTime <= targetTimeSec
        if(svChanges[cachedIndex].StartTimeSec < targetTimeSec)
        {
            while(cachedIndex + 1 < svChanges.Length && svChanges[cachedIndex + 1].StartTimeSec <= targetTimeSec)
            {
                cachedIndex++;
            }
        }
        else if(svChanges[cachedIndex].StartTimeSec > targetTimeSec)
        {
            while(cachedIndex - 1 >= 0 && svChanges[cachedIndex].StartTimeSec > targetTimeSec)
            {
                cachedIndex--;
            }
        }
        else { return svChanges[cachedIndex].Position; }
                
        // calculate position
        switch(svChanges[cachedIndex].Interpolation)
        {
            default:
            case InterpolationType.Linear:
            double dt = targetTimeSec - svChanges[cachedIndex].StartTimeSec;
            return svChanges[cachedIndex].Position + svChanges[cachedIndex].Multiplier * dt;

            case InterpolationType.Discrete:
            return svChanges[cachedIndex].Position;
        }
    }
}