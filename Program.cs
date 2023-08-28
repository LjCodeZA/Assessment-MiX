using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.VisualBasic;

namespace MiXAssessment
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start...");

            Int32 vehicleId;
            string vehicleRegistration;
            float latitude;
            float longitude;
            UInt32 recordedTimeUTC;
            const int GEOCODE_PRECISION = 9;

            var coordinates = new List<Coordinate>();
            var helper = new Helpers();

            //Binary data file to be placed in the same directory
            using (var stream = File.Open("VehiclePositions.dat", FileMode.Open))
            {
                using (var binaryFileReader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    while (binaryFileReader.BaseStream.Position < binaryFileReader.BaseStream.Length)
                    {
                        vehicleId = binaryFileReader.ReadInt32();
                        vehicleRegistration = binaryFileReader.ReadNullTerminatedString();
                        latitude = binaryFileReader.ReadSingle();
                        longitude = binaryFileReader.ReadSingle();
                        recordedTimeUTC = binaryFileReader.ReadUInt32();

                        //Advance 4 bytes
                        binaryFileReader.ReadBytes(4);

                        coordinates.Add(new Coordinate()
                        {
                            Lat = latitude,
                            Long = longitude,
                            VehicleRegistration = vehicleRegistration,
                            Geohash = helper.Encode(latitude, longitude, GEOCODE_PRECISION)
                        });
                    }
                }
            }

            var targetCoordinateList = new List<Coordinate> {
                new Coordinate() { Lat = 34.544909F, Long = -102.100843F },
                new Coordinate() { Lat = 32.345544F, Long = -99.123124F },
                new Coordinate() { Lat = 33.234235F, Long = -100.214124F },
                new Coordinate() { Lat = 35.195739F, Long = -95.348899F },
                new Coordinate() { Lat = 31.895839F, Long = -97.789573F },
                new Coordinate() { Lat = 32.895839F, Long = -101.789573F },
                new Coordinate() { Lat = 34.115839F, Long = -100.225732F },
                new Coordinate() { Lat = 32.335839F, Long = -99.992232F },
                new Coordinate() { Lat = 33.535339F, Long = -94.792232F },
                new Coordinate() { Lat = 32.234235F, Long = -100.222222F }
            };

            helper.SortedGeohashList = coordinates.OrderBy(i => i.Geohash).ToList();

            foreach (var targetToFind in targetCoordinateList)
            {
                targetToFind.Geohash = helper.Encode(targetToFind.Lat, targetToFind.Long, GEOCODE_PRECISION);
                var closestCoordinate = helper.ClosestCoordinate(targetToFind.Lat, targetToFind.Long, targetToFind.Geohash);

                Console.WriteLine($"Target: ({targetToFind.Lat}, {targetToFind.Long})");
                Console.WriteLine($"Nearest: Vehicle Registration - {closestCoordinate.VehicleRegistration} ({closestCoordinate.Lat}, {closestCoordinate.Long})");
                Console.WriteLine($"Distance: {closestCoordinate.Distance.ToString()}");
            }

            Console.WriteLine("Done...");
        }
    }
}