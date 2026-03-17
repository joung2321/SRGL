namespace SRGL.Standard;

using System;
using SRGL.Common;

// judgement strategy for long note (no re-holding, no release judgement)
public class LongNoteJudgementStrategy : IJudgementStrategy
{
    public const int CONTEXT_HIT  = 0;
    public const int CONTEXT_MISS = 1;

    public bool OnPress(long timeUsec, NoteLogicInstance nli, ActiveNoteTracker tracker, TimingWindow tw, Action<Judgement> invokeNoteJudged)
    {
        long errorUsec = TimingWindow.CalculateErrorUsec(nli.Data.StartTimeUsec, timeUsec);

        if(tracker.State == NoteState.Idle && tw.CanJudge(errorUsec))
        {
            tracker.State = NoteState.Holding;

            Judgement j = new Judgement
            {
                ErrorUsec = errorUsec,
                PartitionIndex = tw.GetPartitionIndex(errorUsec),
                Count = 1,
                Lane = nli.Data.Lane,
                Context = CONTEXT_HIT
            };
            invokeNoteJudged(j);
        }

        return false;
    }

    public bool OnRelease(long timeUsec, NoteLogicInstance nli, ActiveNoteTracker tracker, TimingWindow tw, Action<Judgement> invokeNoteJudged)
    {
        if(tracker.State == NoteState.Holding)
        {
            int remainingCombo = tracker.ConsumeMiddleTimes(long.MaxValue, nli.Data.MiddleTimesUsec);

            if(remainingCombo > 0)
            {
                tracker.State = NoteState.Broken;

                Judgement j = new Judgement
                {
                    ErrorUsec = tw.RadiusUsec,
                    PartitionIndex = tw.GetLastPartitionIndex(),
                    Count = 0,
                    Lane = nli.Data.Lane,
                    Context = CONTEXT_MISS
                };
                invokeNoteJudged(j);
            }
            else { tracker.State = NoteState.Released; }
        }

        return false;
    }

    public bool OnUpdate(long timeUsec, NoteLogicInstance nli, ActiveNoteTracker tracker, TimingWindow tw, Action<Judgement> invokeNoteJudged)
    {
        // ======== start time ========
        if(tracker.State == NoteState.Idle)
        {
            if(tw.IsTooLate(nli.Data.StartTimeUsec, timeUsec))
            {
                tracker.State = NoteState.Miss;

                Judgement j = new Judgement
                {
                    ErrorUsec = tw.RadiusUsec,
                    PartitionIndex = tw.GetLastPartitionIndex(),
                    Count = 0,
                    Lane = nli.Data.Lane,
                    Context = CONTEXT_MISS
                };
                invokeNoteJudged(j);
            }

            return false;
        }

        // ======== middle times ========
        int combo = tracker.ConsumeMiddleTimes(timeUsec, nli.Data.MiddleTimesUsec);

        if(tracker.State == NoteState.Holding && combo > 0)
        {
            Judgement j = new Judgement
            {
                ErrorUsec = 0,
                PartitionIndex = 0,
                Count = combo,
                Lane = nli.Data.Lane,
                Context = CONTEXT_HIT
            };
            invokeNoteJudged(j);
        }

        // ======== end time ========
        if(timeUsec >= nli.Data.EndTimeUsec) { return true; }
        else { return false; }
    }
}