using System;
using SRGL;
using SRGL.Common;

public static class RawChartVerifier
{
    public static void Verify(RawChart rawChart)
    {
        bool isEmpty;
        Verifier v = new Verifier();

        v.Ensure(rawChart.AudioPath != null, "null AudioPath");
        v.Ensure(rawChart.PPQN > 0, "non-positive PPQN");
        v.Ensure(rawChart.LaneCount >= 0, "negative LaneCount");

        // ======== Tempos ========
        RawChart.RawTempo[] tArr = rawChart.Tempos;
        isEmpty = (tArr == null) || (tArr.Length <= 0);

        v.Ensure(!isEmpty, "empty Tempos");
        if(!isEmpty)
        {
            // check first element
            v.Ensure(tArr[0].StartTick == 0, "first StartTick should be 0");

            for(int i=0; i<tArr.Length; i++)
            {
                // check each element
                v.Ensure(tArr[i].Bpm > 0, () => $"non-positive bpm: {tArr[i].Bpm}");

                // check previous element
                if(i > 0)
                {
                    v.Ensure(tArr[i-1].StartTick < tArr[i].StartTick, "StartTick should be strictly increasing");
                }
            }
        }

        // ======== TimeSignatures ========
        RawChart.RawTimeSignature[] tsArr = rawChart.TimeSignatures;
        isEmpty = (tsArr == null) || (tsArr.Length <= 0);

        v.Ensure(!isEmpty, "empty TimeSignatures");
        if(!isEmpty)
        {
            // check first element
            v.Ensure(tsArr[0].StartTick == 0, "first StartTick should be 0");

            for(int i=0; i<tsArr.Length; i++)
            {
                // check each element
                v.Ensure(tsArr[i].Numerator > 0, () => $"non-positive Numerator: {tsArr[i].Numerator}");
                v.Ensure(tsArr[i].Denominator > 0, () => $"non-positive Denominator: {tsArr[i].Denominator}");
                v.Ensure(4 * rawChart.PPQN % tsArr[i].Denominator == 0, () => $"inappropriate PPQN for Denominator = {tsArr[i].Denominator}");

                // check previous element
                if(i > 0)
                {
                    v.Ensure(tsArr[i-1].StartTick < tsArr[i].StartTick, "StartTick should be strictly increasing");
                }
            }
        }

        // ======== SvChanges ========
        RawChart.RawSvChange[] svArr = rawChart.SvChanges;
        isEmpty = (svArr == null) || (svArr.Length <= 0);

        v.Ensure(!isEmpty, "empty SvChanges");
        if(!isEmpty)
        {
            // check first element
            v.Ensure(svArr[0].StartTick == 0, "first StartTick should be 0");

            for(int i=0; i<svArr.Length; i++)
            {
                // check previous element
                if(i > 0)
                {
                    v.Ensure(svArr[i-1].StartTick < svArr[i].StartTick, "StartTick should be strictly increasing");
                }
            }
        }

        // ======== Notes ========
        RawChart.RawNote[] nArr = rawChart.Notes;
        isEmpty = (nArr == null) || (nArr.Length <= 0);

        if(!isEmpty)
        {
            bool isSorted = true;

            for(int i=0; i<nArr.Length; i++)
            {
                // check each element
                v.Ensure(nArr[i].StartTick >= 0, () => "negative StartTick");
                v.Ensure(0 <= nArr[i].Lane && nArr[i].Lane < rawChart.LaneCount, () => "invalid Lane");
                v.Ensure(nArr[i].TickRate >= 0, () => "negative TickRate");

                // check previous element
                if(i > 0)
                {
                    bool isMonotonicIncreasing = nArr[i-1].StartTick <= nArr[i].StartTick;

                    v.Ensure(isMonotonicIncreasing, "StartTick should be monotonically increasing");
                    if(!isMonotonicIncreasing) { isSorted = false; }
                }
            }
            
            // verify that there are no overlapping notes in a single lane
            if(isSorted && rawChart.LaneCount > 0)
            {
                long[] lastTicks = new long[rawChart.LaneCount];
                for(int i=0; i<lastTicks.Length; i++) { lastTicks[i] = -1; }

                for(int i=0; i<nArr.Length; i++)
                {
                    // current note
                    RawChart.RawNote note = nArr[i];

                    // check overlap
                    bool noOverlap = lastTicks[note.Lane] < note.StartTick;
                    v.Ensure(noOverlap, "overlapping notes");

                    // update lastTicks
                    lastTicks[note.Lane] = Math.Max(note.StartTick, note.EndTick);
                }
            }
        }

        // throw exception
        v.ThrowIfInvalid();
    }
}
