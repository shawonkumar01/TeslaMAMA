namespace TeslaMAMA.Models;

public record ZonePoint(double Lat, double Lng);

public static class ZoneCoordinates
{
    public static Dictionary<string, ZonePoint> GetCoordinates()
    {
        return new Dictionary<string, ZonePoint>(StringComparer.OrdinalIgnoreCase)
        {
            // Dhaka major zones (approximate coordinates)
            { "Gulshan", new ZonePoint(23.7825, 90.4150) },
            { "Gulshan 1", new ZonePoint(23.7850, 90.4100) },
            { "Gulshan 2", new ZonePoint(23.7800, 90.4200) },
            { "Banani", new ZonePoint(23.7925, 90.4050) },
            { "Baridhara", new ZonePoint(23.7850, 90.4250) },
            { "Uttara", new ZonePoint(23.8750, 90.4000) },
            { "Dhanmondi", new ZonePoint(23.7525, 90.3750) },
            { "Mirpur", new ZonePoint(23.8225, 90.3500) },
            { "Mohammadpur", new ZonePoint(23.7825, 90.3500) },
            { "Mohakhali", new ZonePoint(23.7776, 90.4050) },
            { "Pallabi", new ZonePoint(23.8300, 90.3650) },
            { "Adabor", new ZonePoint(23.8200, 90.3700) },
            { "Kafrul", new ZonePoint(23.8150, 90.3800) },
            { "Sheraton", new ZonePoint(23.7700, 90.3900) },
            { "Paltan", new ZonePoint(23.7350, 90.3950) },
            { "Motijheel", new ZonePoint(23.7400, 90.4000) },
            { "Ramna", new ZonePoint(23.7350, 90.3800) },
            { "Dilkusha", new ZonePoint(23.7300, 90.4050) },
            { "Lalbagh", new ZonePoint(23.7150, 90.3850) },
            { "Hazrat Shahjalal", new ZonePoint(23.8433, 90.3970) },
            { "Kuril", new ZonePoint(23.8250, 90.4200) },
            { "Bashundhara", new ZonePoint(23.8150, 90.4300) },
            { "Nikunja", new ZonePoint(23.8350, 90.4350) },
            { "Khilgaon", new ZonePoint(23.7600, 90.4250) },
            { "Sabujbagh", new ZonePoint(23.7650, 90.4300) },
            { "Demra", new ZonePoint(23.7200, 90.4350) },
            { "Matuail", new ZonePoint(23.7050, 90.4400) },
            { "Shyampur", new ZonePoint(23.7150, 90.4450) },
            { "Keraniganj", new ZonePoint(23.6900, 90.4300) },
            { "Kamrangirchar", new ZonePoint(23.7000, 90.4200) },
            { "Hazrat Shahjalal Airport", new ZonePoint(23.8433, 90.3970) },
            { "Tejgaon", new ZonePoint(23.7550, 90.3850) },
            { "Farmgate", new ZonePoint(23.7600, 90.3750) },
            { "Elephant Road", new ZonePoint(23.7500, 90.3800) },
            { "New Market", new ZonePoint(23.7450, 90.3750) },
            { "Azimpur", new ZonePoint(23.7300, 90.3800) },
            { "Lalbagh Fort", new ZonePoint(23.7180, 90.3880) },
            { "Ahsan Manzil", new ZonePoint(23.7230, 90.3930) },
            { "Sadarghat", new ZonePoint(23.7250, 90.4000) },
            { "Bangabhaban", new ZonePoint(23.7400, 90.3920) },
            { "Secretariat", new ZonePoint(23.7380, 90.3950) },
            { "Dhaka University", new ZonePoint(23.7250, 90.3950) },
            { "BUET", new ZonePoint(23.7300, 90.3900) },
            { "Dhaka Medical College", new ZonePoint(23.7350, 90.3950) },
            { "Shahbagh", new ZonePoint(23.7450, 90.3850) },
            { "Ramna Park", new ZonePoint(23.7350, 90.3800) },
            { "Suhrawardy Uddyan", new ZonePoint(23.7250, 90.3950) },
            { "Manik Mia Avenue", new ZonePoint(23.7350, 90.3900) },
            { "Bijoy Nagar", new ZonePoint(23.7400, 90.4100) },
            { "Kakrail", new ZonePoint(23.7450, 90.4000) },
            { "Wari", new ZonePoint(23.7300, 90.4100) },
            { "Narinda", new ZonePoint(23.7250, 90.4050) },
            { "Islampur", new ZonePoint(23.7200, 90.4150) },
            { "Bangshal", new ZonePoint(23.7300, 90.4000) },
            { "Nayabazar", new ZonePoint(23.7350, 90.3950) },
            { "Chawk Bazar", new ZonePoint(23.7230, 90.3950) },
            { "Babubazar", new ZonePoint(23.7250, 90.4000) },
            { "Sutrapur", new ZonePoint(23.7280, 90.4080) },
            { "Gendaria", new ZonePoint(23.7350, 90.4150) },
            { "Shyambazar", new ZonePoint(23.7650, 90.3900) },
            { "Uttara Sector 1", new ZonePoint(23.8800, 90.3950) },
            { "Uttara Sector 2", new ZonePoint(23.8700, 90.4000) },
            { "Uttara Sector 3", new ZonePoint(23.8600, 90.4050) },
            { "Uttara Sector 4", new ZonePoint(23.8500, 90.4100) },
            { "Uttara Sector 5", new ZonePoint(23.8400, 90.4150) },
            { "Uttara Sector 6", new ZonePoint(23.8300, 90.4200) },
            { "Purbachal", new ZonePoint(23.8900, 90.4500) },
            { "Narsingdi", new ZonePoint(23.9300, 90.7200) },
            { "Savar", new ZonePoint(23.8600, 90.2700) },
            { "Manikganj", new ZonePoint(23.8600, 90.0000) },
            { "Gazipur", new ZonePoint(23.9200, 90.3800) },
            { "Tongi", new ZonePoint(23.9100, 90.4000) },
            { "Narayanganj", new ZonePoint(23.6300, 90.5000) },
            { "Siddhirganj", new ZonePoint(23.6500, 90.4900) },
            { "Rupganj", new ZonePoint(23.6800, 90.5200) },
            { "Sonargaon", new ZonePoint(23.6600, 90.5300) },
            { "Bhairab", new ZonePoint(23.9500, 90.8000) },
            { "Kishoreganj", new ZonePoint(24.4500, 90.7800) },
            { "Narsingdi Sadar", new ZonePoint(23.9300, 90.7200) },
            { "Belabo", new ZonePoint(23.9500, 90.6800) },
            { "Monohardi", new ZonePoint(23.9800, 90.7500) },
            { "Raipura", new ZonePoint(23.9100, 90.7300) },
            { "Palash", new ZonePoint(23.8900, 90.7600) },
            // Additional zones from fare service
            { "Badda", new ZonePoint(23.7850, 90.4350) },
            { "Notun Bazar", new ZonePoint(23.7900, 90.4250) },
            { "Hatirjheel", new ZonePoint(23.7500, 90.4000) },
            { "Mohanagar", new ZonePoint(23.7300, 90.4100) },
            { "Rampura", new ZonePoint(23.7600, 90.4200) }
        };
    }

    public static ZonePoint? GetCoordinate(string zoneName)
    {
        var coordinates = GetCoordinates();
        return coordinates.TryGetValue(zoneName, out var coord) ? coord : null;
    }

    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula to calculate distance between two points in km
        const double R = 6371; // Earth's radius in km

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Asin(Math.Sqrt(a));

        return R * c;
    }

    private static double ToRadians(double angle)
    {
        return angle * Math.PI / 180.0;
    }
}
