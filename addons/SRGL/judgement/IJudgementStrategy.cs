namespace SRGL;

using System;
using SRGL.Common;

public interface IJudgementStrategy
{
    /// <summary>
    /// Invokes multiple JudgementManager.Judged
    /// </summary>
    /// <returns>True if the current note should be despawned; otherwise false.</returns>
    bool OnPress(long timeUsec, NoteLogicInstance nli, ActiveNoteTracker tracker, TimingWindow tw, Action<Judgement> invokeNoteJudged);

    /// <summary>
    /// Invokes multiple JudgementManager.Judged
    /// </summary>
    /// <returns>True if the current note should be despawned; otherwise false.</returns>
    bool OnRelease(long timeUsec, NoteLogicInstance nli, ActiveNoteTracker tracker, TimingWindow tw, Action<Judgement> invokeNoteJudged);

    /// <summary>
    /// Invokes multiple JudgementManager.Judged
    /// </summary>
    /// <returns>True if the current note should be despawned; otherwise false.</returns>
    bool OnUpdate(long timeUsec, NoteLogicInstance nli, ActiveNoteTracker tracker, TimingWindow tw, Action<Judgement> invokeNoteJudged);
}