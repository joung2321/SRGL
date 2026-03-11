namespace SRGL.Common;

using System;

public class SrglException : Exception
{
    public SrglException() {}
    public SrglException(string message) : base(message) {}
    public SrglException(string message, Exception inner) : base(message, inner) {}
}
