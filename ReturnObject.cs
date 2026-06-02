namespace unicli;

public enum ReturnCode
{
    Empty,
    Success,
    Error,
    SuccessWithMessage,
    SuccessWithWarning
}

public struct ReturnObject
{
    public ReturnCode Code;
    public Type? ReturnType;
    public object? ReturnData;

    public bool IsSuccess => Code != ReturnCode.Error;

    public ReturnObject()
    {
        Code = ReturnCode.Empty;
        ReturnType = null;
        ReturnData = null;
    }

    public ReturnObject(ReturnCode code, Type returnType, object returnData)
    {
        Code = code;
        ReturnType = returnType;
        ReturnData = returnData;
    }
}