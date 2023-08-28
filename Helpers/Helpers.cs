using System.Net;
using System.Text;
using MiXAssessment;

namespace MiXAssessment
{
    public class Helpers
    {
        public List<Coordinate> SortedGeohashList { get; set; }
        private readonly string Base32Characters = "0123456789bcdefghjkmnpqrstuvwxyz";
        private const float EarthRadiusKm = 6371.0f; // Earth's radius in kilometers
        private float ToRadians(float degrees)
        {
            return degrees * MathF.PI / 180.0f;
        }

        public float CalculateDistance(float lat1, float lon1, float lat2, float lon2)
        {
            lat1 = ToRadians(lat1);
            lon1 = ToRadians(lon1);
            lat2 = ToRadians(lat2);
            lon2 = ToRadians(lon2);

            float dlat = lat2 - lat1;
            float dlon = lon2 - lon1;
            float a = MathF.Sin(dlat / 2) * MathF.Sin(dlat / 2) +
                    MathF.Cos(lat1) * MathF.Cos(lat2) *
                    MathF.Sin(dlon / 2) * MathF.Sin(dlon / 2);
            float c = 2 * MathF.Atan2(MathF.Sqrt(a), MathF.Sqrt(1 - a));

            float distance = EarthRadiusKm * c;
            return distance;
        }

        public string Encode(float lat, float lon, int precision)
        {
            if (precision == 0)
            {
                return "";
            }

            bool evenBit = true;
            int idx = 0;
            int bit = 0;
            string geohash = "";

            float latMin = -90, latMax = 90;
            float lonMin = -180, lonMax = 180;

            while (geohash.Length < precision)
            {
                if (evenBit)
                {
                    float lonMid = (lonMin + lonMax) / 2;
                    if (lon >= lonMid)
                    {
                        idx = idx * 2 + 1;
                        lonMin = lonMid;
                    }
                    else
                    {
                        idx = idx * 2;
                        lonMax = lonMid;
                    }
                }
                else
                {
                    float latMid = (latMin + latMax) / 2;
                    if (lat >= latMid)
                    {
                        idx = idx * 2 + 1;
                        latMin = latMid;
                    }
                    else
                    {
                        idx = idx * 2;
                        latMax = latMid;
                    }
                }
                evenBit = !evenBit;

                if (++bit == 5)
                {
                    geohash += Base32Characters[idx];
                    bit = 0;
                    idx = 0;
                }
            }

            return geohash;
        }

        public Coordinate ClosestCoordinate(float targetLat, float targetLong, string geohash)
        {
            //Can be further improved with incremental precision decrease to search a larger area

            int widenPrecision = 4;
            Coordinate coordinateClosestToTarget = new Coordinate();

            var geohashWidenSearch = geohash.Substring(0, geohash.Length - widenPrecision);
            var closestCoordinates = BinarySearchForGeohashItems(geohashWidenSearch, widenPrecision);

            float minDistance = float.MaxValue;
            foreach (var itemDistanceToCalculate in closestCoordinates)
            {
                float distance = CalculateDistance(itemDistanceToCalculate.Lat, itemDistanceToCalculate.Long, targetLat, targetLong);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    itemDistanceToCalculate.Distance = distance;
                    coordinateClosestToTarget = itemDistanceToCalculate;
                }
            }

            return coordinateClosestToTarget;

        }

        public List<Coordinate> BinarySearchForGeohashItems(string geohashTarget, int widenPrecision)
        {
            var closestItems = new List<Coordinate>();
            int left = 0;
            int right = SortedGeohashList.Count - 1;

            while (left <= right)
            {
                int middle = left + (right - left) / 2;

                var itemAtIndex = SortedGeohashList[middle];
                var geohashToCompare = itemAtIndex.Geohash.Substring(0, itemAtIndex.Geohash.Length - widenPrecision);

                int comparison = geohashTarget.CompareTo(geohashToCompare);

                if (comparison == 0)
                {
                    //Search up and down sorted list on geohashTarget from middle index to find matching items
                    for (int i = middle; i < SortedGeohashList.Count; i++)
                    {
                        var item = SortedGeohashList[i];

                        if (item.Geohash.Substring(0, itemAtIndex.Geohash.Length - widenPrecision) == geohashTarget)
                            closestItems.Add(item);
                        else
                            break;
                    }

                    for (int i = middle; i > 0; i--)
                    {
                        var item = SortedGeohashList[i];

                        if (item.Geohash.Substring(0, itemAtIndex.Geohash.Length - widenPrecision) == geohashTarget)
                            closestItems.Add(item);
                        else
                            break;
                    }

                    return closestItems;

                }
                else if (comparison < 0)
                    right = middle - 1;
                else
                    left = middle + 1;
            }

            return new List<Coordinate>();
        }
    }
}