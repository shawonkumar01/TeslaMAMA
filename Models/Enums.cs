
namespace TeslaMAMA.Models;

public enum UserRole
{
    Passenger,
    Driver,
    Admin
}

public enum PoolStatus
{
    Open,
    Full,
    InProgress,
    Completed,
    Cancelled
}

public enum RideStatus
{
    Requested,
    Matched,
    DriverArrived,
    Started,
    WaitingForPayment,
    Completed,
    Cancelled
}

public enum PaymentMethod
{
    Cash,
    Card,
    MobileBanking
}

public enum DriverOfferStatus
{
    Pending,
    Accepted,
    Rejected,
    Expired
}
