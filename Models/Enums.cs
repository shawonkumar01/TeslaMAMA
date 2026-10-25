
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
    Cancelled,
    NoDriverAvailable
}

public enum PaymentMethod
{
    Cash,
    Card,
    MobileBanking
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    RefundRequested,
    Refunded,
    RefundRejected
}

public enum DriverOfferStatus
{
    Pending,
    Accepted,
    Rejected,
    Expired
}

public enum RideType
{
    SharedPool,
    Private
}
