namespace BelengherAAA
{
    public class City
    {
        public String CountryCode { get; set; }
        public String County { get; set; }
        public String CityName { get; set; }
        public double Distance { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public City(String _CountryCode, String _County, String _CityName, double _Distance, double _Latitude, double _Longitude)
        {
            CountryCode = _CountryCode;
            County = _County;
            CityName = _CityName;
            Distance = _Distance;
            Latitude = _Latitude;
            Longitude = _Longitude;
        }
    }
}
