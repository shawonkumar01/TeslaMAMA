# TeslaMAMA

A modern ride-sharing application built with ASP.NET Core, featuring real-time driver matching, shared pool rides, and comprehensive payment management.

## Features

### For Passengers
- **Ride Booking**: Book rides with pickup and destination zones
- **Shared Pool**: Cost-effective rides with other passengers
- **Real-time Tracking**: Track your ride with live map integration
- **Activity Dashboard**: View ride history with detailed timeline
- **Payment History**: Complete payment transaction management
- **Refund Requests**: Request refunds for completed payments

### For Drivers
- **Live Offers**: Receive real-time ride requests
- **Countdown Timers**: Time-sensitive offer acceptance
- **Ride Management**: Complete rides and update status
- **Earnings Tracking**: View completed rides and earnings

### System Features
- **Smart Pool Matching**: Route-based matching with detour limits
- **Road Distance Calculation**: OSRM integration for accurate distances
- **Fare Calculation**: Fixed fare system with pool discounts
- **Multi-zone Support**: Zone-based pickup and destination system
- **Responsive Design**: Mobile-first UI with desktop support

## Tech Stack

- **Backend**: ASP.NET Core 8.0
- **Frontend**: Razor Pages with CSS
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Maps**: Leaflet.js with OSRM routing
- **Charts**: Chart.js for data visualization

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server (or use Docker)
- Node.js (for frontend tooling, if needed)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/shawonkumar01/TeslaMAMA.git
cd TeslaMAMA
```

2. Configure the database connection in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TeslaMAMADb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

3. Run database migrations:
```bash
dotnet ef database update
```

4. Run the application:
```bash
dotnet run
```

5. Access the application at `https://localhost:5001`

### Using Docker

1. Build and run with Docker Compose:
```bash
docker-compose up --build
```

## Project Structure

```
TeslaMAMA/
├── Controllers/          # API controllers
│   ├── AccountController.cs
│   ├── DriverController.cs
│   └── PassengerController.cs
├── Data/                # Database context and entities
│   ├── AppDbContext.cs
│   └── Entities/
├── Models/              # View models and DTOs
├── Services/            # Business logic services
│   ├── DriverAssignmentService.cs
│   ├── FareCalculationService.cs
│   └── PassengerOverviewService.cs
├── Views/               # Razor views
│   ├── Account/
│   ├── Driver/
│   └── Passenger/
├── wwwroot/             # Static assets
│   ├── css/
│   ├── js/
│   └── images/
└── Migrations/          # Database migrations
```

## Key Features Explained

### Ride Matching System
- Drivers receive offers based on their current zone
- Offers have countdown timers (default: 60 seconds)
- Multiple drivers can be offered the same ride
- First driver to accept gets the ride

### Pool Matching
- Routes are calculated using OSRM for accurate distances
- Pool rides match passengers with similar routes
- Detour limits ensure reasonable detours for shared rides
- 20% discount on pool rides

### Payment System
- Payments are processed after ride completion
- Multiple payment methods: Cash, Card, Mobile Banking
- Refund workflow with admin approval
- Complete payment history tracking

## Configuration

### Fare Calculation
- Base fare + distance-based fare calculation
- Pool discount: 20%
- Additional seats multiplier for larger groups

### Zone System
- Predefined pickup and destination zones
- Zone coordinates for map rendering
- Road distance calculation between zones

## Development

### Running Migrations
```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

### Building the Project
```bash
dotnet build
```

### Running Tests
```bash
dotnet test
```

## Deployment

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Development/Staging/Production
- `ConnectionStrings__DefaultConnection`: Database connection string
- `OSRM_BASE_URL`: OSRM routing service URL (optional)

### Production Considerations
- Use HTTPS in production
- Configure proper CORS settings
- Set up SSL certificates
- Configure logging and monitoring

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License.

## Support

For support, please open an issue in the GitHub repository or contact the maintainers.

## Acknowledgments

- Built with ASP.NET Core
- Maps powered by Leaflet.js and OSRM
- Charts powered by Chart.js
