using IrisBot.NexonAPI.Responses;

namespace IrisBot.NexonAPI
{
    public enum NexonAPIErrorCode
    {
        OPENAPI00001,
        OPENAPI00002,
        OPENAPI00003,
        OPENAPI00004,
        OPENAPI00005,
        OPENAPI00006,
        OPENAPI00007,
        OPENAPIERROR,
    }

    public class NexonAPIExceptions : Exception
    {
        internal NexonAPIExceptions(NexonAPIErrorCode errorCode, string message) : base(message)
        {
            Message = message;
            ErrorCode = errorCode;
        }

        internal NexonAPIExceptions(ErrorBody errorBody) : base(errorBody.Error.Message)
        {
            Message = errorBody.Error.Message;

            if (string.Equals(errorBody.Error.Name, "OPENAPI00001"))
                ErrorCode = NexonAPIErrorCode.OPENAPI00001;
            else if (string.Equals(errorBody.Error.Name, "OPENAPI00002"))
                ErrorCode = NexonAPIErrorCode.OPENAPI00002;
            else if (string.Equals(errorBody.Error.Name, "OPENAPI00003"))
                ErrorCode = NexonAPIErrorCode.OPENAPI00003;
            else if (string.Equals(errorBody.Error.Name, "OPENAPI00004"))
                ErrorCode = NexonAPIErrorCode.OPENAPI00004;
            else if (string.Equals(errorBody.Error.Name, "OPENAPI00006"))
                ErrorCode = NexonAPIErrorCode.OPENAPI00005;
            else if (string.Equals(errorBody.Error.Name, "OPENAPI00006"))
                ErrorCode = NexonAPIErrorCode.OPENAPI00006;
            else if (string.Equals(errorBody.Error.Name, "OPENAPI00007"))
                ErrorCode = NexonAPIErrorCode.OPENAPI00007;
            else if (string.Equals(errorBody.Error.Name, "OPENAPIERROR"))
                ErrorCode = NexonAPIErrorCode.OPENAPIERROR;
        }

        public new string Message { get; }
        public NexonAPIErrorCode ErrorCode { get; }
    }
}
