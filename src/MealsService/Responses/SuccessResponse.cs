
namespace MealsService.Responses
{
    public class SuccessResponse<T>
    {
        public SuccessResponse(T data)
        {
            Data = data;
        }

        public bool Success { get; set; } = true;

        public T Data { get; set; }
    }

    public class SuccessResponse
    {
        public bool Success = true;
    }
}
