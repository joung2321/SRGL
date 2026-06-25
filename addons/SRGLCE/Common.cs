namespace SRGLCE.Common;

public enum FileMenu { New, Open, Close, Save, SaveAs };
public enum TypeMenu { Metadata, Tempo, TimeSignature, SvChange, Note };
public enum ModeMenu { Input, Edit };

public enum DivisionMenu
{
    ByBeat = 0,

    ByWholeNote = 1,
    ByHalfNote = 2,
    ByQuarterNote = 4,
    
    By8th = 8,
    By16th = 16,
    By32th = 32,
    By64th = 64,
    By128th = 128,

    Custom = -1
};