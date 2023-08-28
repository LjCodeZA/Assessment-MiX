using System.Text;

namespace MiXAssessment
{
    public static class ExtensionHelpers
    {
        public static string ReadNullTerminatedString(this BinaryReader binaryFileReader)
        {
            var result = new StringBuilder();
            while (true)
            {
                byte b = binaryFileReader.ReadByte();
                if (0 == b)
                    break;
                result.Append((char)b);
            }
            return result.ToString();
        }
    }
}