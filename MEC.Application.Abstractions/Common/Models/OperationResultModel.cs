namespace MEC.Application.Abstractions.Common.Models
{
    public class OperationResultModel
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();

        public static OperationResultModel Success(string message = "", string level = "success")
        {
            return new OperationResultModel
            {
                IsSuccess = true,
                Message = message,
                Level = level
            };
        }

        public static OperationResultModel Fail(string message, string level = "danger")
        {
            return new OperationResultModel
            {
                IsSuccess = false,
                Message = message,
                Level = level,
                Errors = string.IsNullOrWhiteSpace(message) ? new List<string>() : new List<string> { message }
            };
        }
    }

    public class OperationResultModel<T> : OperationResultModel
    {
        public T? Data { get; set; }

        public static OperationResultModel<T> Success(T data, string message = "", string level = "success")
        {
            return new OperationResultModel<T>
            {
                IsSuccess = true,
                Message = message,
                Level = level,
                Data = data
            };
        }

        public new static OperationResultModel<T> Fail(string message, string level = "danger")
        {
            return new OperationResultModel<T>
            {
                IsSuccess = false,
                Message = message,
                Level = level,
                Errors = string.IsNullOrWhiteSpace(message) ? new List<string>() : new List<string> { message }
            };
        }
    }
}
