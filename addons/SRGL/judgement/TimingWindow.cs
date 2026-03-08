namespace SRGL;

using System.Collections.Generic;

// class defining timing window and judging a single timing
public class TimingWindow
{
    private long _radiusUsec; // radius of timing window [us] (all input whose absolute error is greater than _radius are ignored)
    private List<long> _partitionsUsec; // contains non-negative, ascending order, unique values ONLY; contains long.MaxValue by default [us]

    public long RadiusUsec
    {
        get { return _radiusUsec; }
        set { if(value >= 0) { _radiusUsec = value; } }
    }

    public TimingWindow()
    {
        _partitionsUsec = new List<long>();
        Init();
    }

    public void Init()
    {
        _radiusUsec = long.MaxValue;

        _partitionsUsec.Clear();
        Partition(long.MaxValue); // _partitionsUsec contains long.MaxValue by default.
    }
    
    public void Partition(long errorUsec)
    {
        if(errorUsec < 0) { return; } // ignore invalid value
        if(_partitionsUsec.Contains(errorUsec)) { return; } // ignore duplicates

        _partitionsUsec.Add(errorUsec);
        _partitionsUsec.Sort();
    }

    // error = noteTimeUsec - currentTimeUsec
    public static long CalculateErrorUsec(long noteTimeUsec, long currentTimeUsec)
    {
        return noteTimeUsec - currentTimeUsec;
    }

    /// <param name="errorUsec">a return value of TimingWindow.CalculateErrorUsec()</param>
    public int GetPartitionIndex(long errorUsec)
    {
        // errorUsec = abs(errorUsec)
        if(errorUsec < 0) { errorUsec = -errorUsec; }

        // find partition index
        int i = 0;
        for(int cnt = _partitionsUsec.Count; i < cnt; i++)
        {
            if(errorUsec <= _partitionsUsec[i]) { break; }
        }

        return i;
    }

    // partition index for missed note
    public int GetLastPartitionIndex() { return _partitionsUsec.Count - 1; }
    
    /// <summary>
    /// Checks whether a note is in this timing window
    /// </summary>
    /// <param name="errorUsec">a return value of TimingWindow.CalculateErrorUsec()</param>
    public bool CanJudge(long errorUsec)
    {
        return -_radiusUsec <= errorUsec && errorUsec <= _radiusUsec;
    }

    /// <param name="errorUsec">a return value of TimingWindow.CalculateErrorUsec()</param>
    public bool IsTooLate(long noteTimeUsec, long currentTimeUsec)
    {
        return noteTimeUsec + _radiusUsec < currentTimeUsec;
    }
}