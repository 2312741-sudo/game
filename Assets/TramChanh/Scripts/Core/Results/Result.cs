using System;

namespace TramChanh.Core
{
    public readonly struct Result<T>
    {
        private readonly Result _result;
        private readonly T _value;
        public bool IsSuccess => _result.IsSuccess;
        public string ReasonKey => _result.ReasonKey;
        public T Value
        {
            get
            {
                if (!IsSuccess)
                {
                    throw new InvalidOperationException($"Failed result has no value: {ReasonKey}.");
                }
                return _value;
            }
        }

        private Result(Result result, T value)
        {
            _result = result;
            _value = value;
        }

        public static Result<T> Success(T value)
        {
            return new Result<T>(Result.Success(), value);
        }

        public static Result<T> Fail(string reasonKey)
        {
            return new Result<T>(Result.Fail(reasonKey), default);
        }
    }
}
