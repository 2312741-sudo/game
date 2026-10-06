using System;

namespace TramChanh.Core
{
    public readonly struct Result
    {
        private readonly string _reasonKey;
        public bool IsSuccess { get; }
        public string ReasonKey => IsSuccess ? null : _reasonKey ?? "core.result.uninitialized";

        private Result(bool isSuccess, string reasonKey)
        {
            IsSuccess = isSuccess;
            _reasonKey = reasonKey;
        }

        public static Result Success()
        {
            return new Result(true, null);
        }

        public static Result Fail(string reasonKey)
        {
            if (string.IsNullOrWhiteSpace(reasonKey))
            {
                throw new ArgumentException("A failure requires a reason key.", nameof(reasonKey));
            }
            return new Result(false, reasonKey);
        }
    }
}
