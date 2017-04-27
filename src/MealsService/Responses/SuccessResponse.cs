
namespace MealsService.Responses
{
    public class SuccessResponse<T>
    {
        public SuccessResponse(T data, bool isSuccess = true)
        {
            Data = data;
            Success = isSuccess;
        }

        public bool Success;

        public T Data;
    }

    public class SuccessResponse
    {
        public bool Success;

        public SuccessResponse(bool isSuccess = true)
        {
            Success = isSuccess;
        }
    }
}
