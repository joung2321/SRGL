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

    public Verifier Ensure(bool condition, string errorMessage)
    {
        if(!condition) { _errorMessages.Add(errorMessage); }
        return this;
    }

    public Verifier Ensure(bool condition, Func<string> errorMessageProvider)
    {
        if(!condition) { _errorMessages.Add(errorMessageProvider()); }
        return this;
    }

    public void ThrowIfInvalid()
    {
        if(_errorMessages.Count > 0)
        {
            _errorMessages.Insert(0, string.Empty);
            throw new SrglException(string.Join('\n', _errorMessages));
        }
    }
}
