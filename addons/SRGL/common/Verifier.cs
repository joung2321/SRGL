namespace SRGL.Common;

using System;
using System.Collections.Generic;

public class Verifier
{
    private List<string> _errorMessages;

    public Verifier()
    {
        _errorMessages = new List<string>(16); // reduce re-allocation costs
    }

    public Verifier Ensure(bool condition, Func<string> errorMessageProvider)
    {
        if(!condition)
        {
            _errorMessages.Add(errorMessageProvider());
        }

        return this;
    }

    public void ThrowIfInvalid()
    {
        if(_errorMessages.Count > 0)
        {
            throw new Exception(string.Join('\n', _errorMessages));
        }
    }

    public void Clear() { _errorMessages.Clear(); }
}
