namespace SRGL.Common;

public static class VisualVariationSelector
{
    /// <summary>
    /// <code>
    /// input : ..., -1; 0, 1, 2, 3; 4, 5, 6, 7; ...<br/>
    /// output:  -1, -1; 0, 1, 1, 0; 0, 1, 1, 0; ...
    /// </code>
    /// </summary>
    public static int Select4K(int laneIndex)
    {
        if(laneIndex < 0) { return -1; }
        else
        {
            laneIndex = laneIndex % 4;
            return (laneIndex < 2)? laneIndex: 3 - laneIndex;
        }
    }

    /// <summary>
    /// <code>
    /// input : ..., -1; 0, 1, 2, 3, 4; 5, 6, 7, 8, 9; ...<br/>
    /// output:  -1, -1; 0, 1, 0, 1, 0; 0, 1, 0, 1, 0; ...
    /// </code>
    /// </summary>
    public static int Select5K(int laneIndex)
    {
        if(laneIndex < 0) { return -1; }
        else
        {
            laneIndex = laneIndex % 5;
            return laneIndex % 2;
        }
    }

    /// <summary>
    /// <code>
    /// input : ..., -1; 0, 1, 2, 3, 4, 5; 6, 7, 8, 9, 10, 11; ...<br/>
    /// output:  -1, -1; 0, 1, 0, 0, 1, 0; 0, 1, 0, 0,  1,  0; ...
    /// </code>
    /// </summary>
    public static int Select6K(int laneIndex)
    {
        if(laneIndex < 0) { return -1; }
        else
        {
            laneIndex = laneIndex % 3;
            return laneIndex % 2;
        }
    }
}