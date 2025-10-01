using System;
using System.Collections.Generic;

[Serializable]
public class ProblemDetails
{
    public string type;
    public string title;
    public int status;
    public string detail;
    public string instance;
    public List<Error> errors;
}

[Serializable]
public class Error
{
    public string code;
    public string description;
}