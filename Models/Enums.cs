
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
    Completed,
    Cancelled
}

public enum PaymentMethod
{
    Cash,
    Card,
    MobileBanking
}
 