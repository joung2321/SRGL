namespace SRGL.Misc;

using Godot;
using SRGL.Common;

public partial class ComboCounter : Label
{
    public int ComboBreakPartitionIndex { private get; set; }
    private int _combo;

    public ComboCounter()
    {
        Reset();
    }

    public void Reset()
    {
        _combo = 0;
        Text = string.Empty;
    }

    public void OnNoteJudged(Judgement judgement)
    {
        if(judgement.PartitionIndex < ComboBreakPartitionIndex)
        {
            _combo += judgement.Count;
            if(_combo >= 2) { Text = _combo.ToString(); }
        }
        else
        {
            _combo = 0;
            Text = string.Empty;
        }
    }
}