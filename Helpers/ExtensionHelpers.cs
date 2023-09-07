using System.Text;

namespace MiXAssessment
{
    public static class ExtensionHelpers
    {
        public static long FindLastPaddingPosition(this BinaryReader binaryFileReader, long startPosition)
        {
            binaryFileReader.BaseStream.Position = startPosition;
            int paddingReachedCount = 4;
            int paddingCheckCount = 0;

            while (true)
            {
                byte b = binaryFileReader.ReadByte();
                if (0 == b)
                {
                    paddingCheckCount++;

                    if (paddingCheckCount == paddingReachedCount)
                    {
                        break;
                    }
                }
                else
                    paddingCheckCount = 0;
            }

            var lastPaddingPosition = binaryFileReader.BaseStream.Position;
            binaryFileReader.BaseStream.Position = startPosition;

            return lastPaddingPosition;
        }
    }
}