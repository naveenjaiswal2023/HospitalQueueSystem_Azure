using HospitalQueueSystem.Domain.Interfaces;

namespace HospitalQueueSystem.Application.Common
{
    public class CommandResult : ICommandResult
    {
        public bool Succeeded { get; set; }
        public object Response { get; set; }
        public int StatusCode { get; set; }

        public static CommandResult Success(object response, int statusCode = 200)
        {
            return new CommandResult
            {
                Succeeded = true,
                Response = response,
                StatusCode = statusCode
            };
        }

        public static CommandResult Failure(object response, int statusCode = 400)
        {
            return new CommandResult
            {
                Succeeded = false,
                Response = response,
                StatusCode = statusCode
            };
        }
    }
}
