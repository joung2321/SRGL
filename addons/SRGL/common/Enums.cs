namespace SRGL.Common;

using System;

public enum InterpolationType
{
    Step = 0, // default (no interpolation)
    Linear,
    Impulse
}

[Flags]
public enum NoteOptions
{
    Dummy = 1 << 0, // bar line, fake note, etc.
    AllowHoldAgain = 1 << 1,
    CheckRelease = 1 << 2
}

public enum NoteState { Idle, Holding, Miss, Released, Broken };