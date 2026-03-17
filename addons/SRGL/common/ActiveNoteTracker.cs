namespace SRGL.Common;

using System.Collections.Immutable;

public class ActiveNoteTracker
{
    public NoteState State;
    private int _nextTimeIndex;

    public ActiveNoteTracker() { Init(); }
    public void Init() { State = NoteState.Idle; _nextTimeIndex = 0; }

    public int ConsumeMiddleTimes(long currentTime_us, ImmutableArray<long> middleTimes_us)
    {
        if(middleTimes_us == null || middleTimes_us.Length <= 0) { return 0; }
        
        int rawCombo = 0;

        for(int len=middleTimes_us.Length; _nextTimeIndex < len; _nextTimeIndex++)
        {
            if(currentTime_us >= middleTimes_us[_nextTimeIndex]) { rawCombo++; }
            else { break; }
        }

        return rawCombo;
    }
}