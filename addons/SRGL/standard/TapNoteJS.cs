namespace SRGL.Standard;

using System;
using SRGL.Common;

// judgement strategy for tap note
public class TapNoteJS : IJudgementStrategy
{
    public const int CONTEXT_HIT  = 0;
    public const int CONTEXT_MISS = 1;

    public bool OnPress(long timeUsec, NoteLogicInstance nli, ActiveNoteTracker tracker, TimingWindow tw, Action<Judgement> invokeNoteJudged)
    {
        long errorUsec = TimingWindow.CalculateErrorUsec(nli.Data.StartTimeUsec, timeUsec);

        if(tw.CanJudge(errorUsec))
        {
            Judgement j = new Judgement
            {
                ErrorUsec = errorUsec,
                PartitionIndex = tw.GetPartitionIndex(errorUsec),
                Count = 1,
                Lane = nli.Data.Lane,
                Context = CONTEXT_HIT
            };
            invokeNoteJudged(j);

            return true;
        }
        else { return false; }
    }

    public bool OnRelease(long timeUsec, NoteLogicInstance nli, ActiveNoteTracker tracker, TimingWindow tw, Action<Judgement> invokeNoteJudged)
    {
        return false;
    }

    public bool OnUpdate(long timeUsec, NoteLogicInstance nli, ActiveNoteTracker tracker, TimingWindow tw, Action<Judgement> invokeNoteJudged)
    {
        if(tw.IsTooLate(nli.Data.StartTimeUsec, timeUsec))
        {
            Judgement j = new Judgement
            {
                ErrorUsec = tw.RadiusUsec,
                PartitionIndex = tw.GetLastPartitionIndex(),
                Count = 1,
                Lane = nli.Data.Lane,
                Context = CONTEXT_MISS
            };
            invokeNoteJudged(j);

            return true;
        }
        else { return false; }
    }
}