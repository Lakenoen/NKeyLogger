using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NKeyLoggerLib;
public class Result<T>
{
    public bool isSuccess { get; private set; }
    public bool isFailure
    {
        get
        {
            return !isSuccess;
        }
    }
    public T? Value { get; private set; }

    public string Error { get; private set; }
    protected Result(T? value, bool isSuccess, string error)
    {
        this.isSuccess = isSuccess;
        this.Error = error;
        this.Value = value;
    }

    public static Result<T> Success(T? value) => new Result<T> ( value, true, string.Empty);
    public static Result<T> Failure(string error) => new Result<T>(default, false, error);
    public static Result<T> Failure(T? value, string error) => new Result<T>(value, false, error);

}
