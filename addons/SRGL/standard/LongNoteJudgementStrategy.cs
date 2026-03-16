namespace SRGL.Standard;

using System;
using SRGL.Common;

// judgement strategy for tap note
public class LongNoteJudgementStrategy : IJudgementStrategy
{
    public const int CONTEXT_HIT  = 0;
    public const int CONTEXT_MISS = 1;

    public bool OnPress(long timeUsec, NoteLogicInstance nli, ActiveNoteTracker tracker, TimingWindow tw, Action<Judgement> invokeNoteJudged)
    {
        throw new NotImplementedException();
    }

    public bool OnRelease(long timeUsec, NoteLogicInstance nli, ActiveNoteTracker tracker, TimingWindow tw, Action<Judgement> invokeNoteJudged)
    {
        throw new NotImplementedException();
    }

    public bool OnUpdate(long timeUsec, NoteLogicInstance nli, ActiveNoteTracker tracker, TimingWindow tw, Action<Judgement> invokeNoteJudged)
    {
        throw new NotImplementedException();
    }
}