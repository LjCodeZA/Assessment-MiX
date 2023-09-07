using System.Text;
using System.Diagnostics;
using System.Data;
using System.Collections.Concurrent;

namespace MiXAssessment
{
    public class Program
    {
        static ConcurrentBag<Coordinate> concurrentCoordinates = new ConcurrentBag<Coordinate>();
        static Helpers helper = new Helpers();
        static int GEOCODE_PRECISION = 9;
        static int logicalProcessorCountForConcurrency = Environment.ProcessorCount;

        static void Main(string[] args)
        {
            Console.WriteLine("Start...");
            Stopwatch stopWatchTotal = new Stopwatch();
            stopWatchTotal.Start();

            Stopwatch stopWatchLoad = new Stopwatch();
            stopWatchLoad.Start();

            //Binary data file to be placed in the same directory
            using (var stream = File.Open("VehiclePositions.dat", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var binaryFileReader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    long startPosition = 0;
                    long paddingEndPosition = 0;
                    var bytesToRead = new List<PartitionItem>();
                    long split = binaryFileReader.BaseStream.Length / logicalProcessorCountForConcurrency;

                    for (int i = 0; i < logicalProcessorCountForConcurrency; i++)
                    {
                        var endPositionToFind = startPosition + split;

                        if (endPositionToFind > binaryFileReader.BaseStream.Length)
                            paddingEndPosition = binaryFileReader.BaseStream.Length;
                        else
                            paddingEndPosition = binaryFileReader.FindLastPaddingPosition(endPositionToFind);

                        if (i == 0)
                        {
                            bytesToRead.Add(new PartitionItem
                            {
                                Start = 0,
                                End = paddingEndPosition
                            });
                        }
                        else
                        {
                            bytesToRead.Add(new PartitionItem
                            {
                                Start = startPosition,
                                End = paddingEndPosition
                            });
                        }

                        startPosition = paddingEndPosition + 1;

                        if (paddingEndPosition == binaryFileReader.BaseStream.Length)
                            break;
                    }

                    Parallel.For(0, logicalProcessorCountForConcurrency, threadIndex =>
                    {
                        long startOffset = bytesToRead[threadIndex].Start;
                        long endOffset = bytesToRead[threadIndex].End;
                        int bufferSize = (int)(endOffset - startOffset);

                        byte[] buffer = new byte[bufferSize];
                        ProcessBuffer(startOffset, endOffset, bufferSize, buffer, threadIndex, binaryFileReader);
                    });
                }
            }

            stopWatchLoad.Stop();

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

            Stopwatch stopWatchSort = new Stopwatch();
            stopWatchSort.Start();

            helper.SortedGeohashList = concurrentCoordinates.AsParallel().WithDegreeOfParallelism(logicalProcessorCountForConcurrency).OrderBy(p => p.Geohash).ToList();

            stopWatchSort.Stop();

            Stopwatch stopWatchSearch = new Stopwatch();
            stopWatchSearch.Start();

            foreach (var targetToFind in targetCoordinateList)
            {
                targetToFind.Geohash = helper.Encode(targetToFind.Lat, targetToFind.Long, GEOCODE_PRECISION);
                var closestCoordinate = helper.ClosestCoordinate(targetToFind.Lat, targetToFind.Long, targetToFind.Geohash);

                Console.WriteLine($"Target: ({targetToFind.Lat}, {targetToFind.Long})");
                Console.WriteLine($"Nearest: Vehicle Registration - {closestCoordinate.VehicleRegistration} ({closestCoordinate.Lat}, {closestCoordinate.Long}) | Geohash: {closestCoordinate.Geohash}");
                Console.WriteLine($"Distance: {closestCoordinate.Distance.ToString()}");
            }

            stopWatchSearch.Stop();
            stopWatchTotal.Stop();

            TimeSpan tsLoad = stopWatchLoad.Elapsed;
            string elapsedTimeLoad = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", tsLoad.Hours, tsLoad.Minutes, tsLoad.Seconds, tsLoad.Milliseconds / 10);
            Console.WriteLine("Load time " + elapsedTimeLoad);

            TimeSpan tsSort = stopWatchSort.Elapsed;
            string elapsedTimeSort = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", tsSort.Hours, tsSort.Minutes, tsSort.Seconds, tsSort.Milliseconds / 10);
            Console.WriteLine("sort time " + elapsedTimeSort);

            TimeSpan tsSearch = stopWatchSearch.Elapsed;
            string elapsedTimeSearch = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", tsSearch.Hours, tsSearch.Minutes, tsSearch.Seconds, tsSearch.Milliseconds / 10);
            Console.WriteLine("search time " + elapsedTimeSearch);

            TimeSpan tsDone = stopWatchTotal.Elapsed;
            string elapsedTimeTotal = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", tsDone.Hours, tsDone.Minutes, tsDone.Seconds, tsDone.Milliseconds / 10);
            Console.WriteLine("total time " + elapsedTimeTotal);

            Console.WriteLine("Done...");
        }

        static void ProcessBuffer(long startOffset, long endOffset, int bufferSize, byte[] buffer, int threadIndex, BinaryReader binaryFileReader)
        {
            int bytesRead;
            lock (binaryFileReader.BaseStream)
            {
                binaryFileReader.BaseStream.Seek(startOffset, SeekOrigin.Begin);
                bytesRead = binaryFileReader.Read(buffer, 0, bufferSize);
            }

            for (int i = 0; i < bytesRead; i++)
            {
                Int32 vehicleIdFromBuffer = BitConverter.ToInt32(buffer, i);
                i += sizeof(Int32);

                string vehicleRegistrationFromBuffer = helper.ReadNullTerminatedString(buffer, i);
                i += System.Text.ASCIIEncoding.UTF8.GetByteCount(vehicleRegistrationFromBuffer) + 1;

                float latitudeFromBuffer = BitConverter.ToSingle(buffer, i);
                i += sizeof(float);

                float longitudeFromBuffer = BitConverter.ToSingle(buffer, i);
                i += sizeof(float);

                UInt32 recordedTimeUTCFromBuffer = BitConverter.ToUInt32(buffer, i);
                i += sizeof(UInt32);

                float paddingFromBuffer = BitConverter.ToSingle(buffer, i);
                i += sizeof(float) - 1;

                var newCoordinate = new Coordinate()
                {
                    Lat = latitudeFromBuffer,
                    Long = longitudeFromBuffer,
                    VehicleRegistration = vehicleRegistrationFromBuffer,
                    Geohash = helper.Encode(latitudeFromBuffer, longitudeFromBuffer, GEOCODE_PRECISION)
                };

                concurrentCoordinates.Add(newCoordinate);
            }
        }
    }
}