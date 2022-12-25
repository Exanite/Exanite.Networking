namespace Exanite.Networking.Client
{
    public class ClientConnectResult
    {
        public ClientConnectResult(bool isSuccess, string failReason = Constants.DefaultReason)
        {
            IsSuccess = isSuccess;
            FailReason = failReason;
        }

        public bool IsSuccess { get; }
        public string FailReason { get; }
    }
}
