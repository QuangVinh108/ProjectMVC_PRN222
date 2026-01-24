using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Helpers
{
    public class GenericResult<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Data { get; private set; }
        public string? Message { get; private set; }
        public List<string> Errors { get; private set; } = new();

        public static GenericResult<T> Success(T data, string? message = null)
            => new() { IsSuccess = true, Data = data, Message = message };

        public static GenericResult<T> Failure(params string[] errors)
            => new() { IsSuccess = false, Errors = errors.ToList() };

    }
}