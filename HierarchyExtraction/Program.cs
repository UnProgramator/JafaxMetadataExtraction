using DRSTool.Extractor.DataExtraction;
using System.Text;

class Program
{
    private static void Main(string[] args)
    {
        string? inF, outF, etx;

        var fileStream = File.OpenRead("Config.txt");
        var streamReader = new StreamReader(fileStream, Encoding.UTF8, true);

        while (!streamReader.EndOfStream)
        {
            etx = null;
            do {
                inF = streamReader.ReadLine();
            }while(inF != null && inF.Equals(""));
            outF = streamReader.ReadLine();
            if (!streamReader.EndOfStream)
            {
                etx = streamReader.ReadLine();
                if (etx is not null && etx.Length == 0)
                    etx = null;
            }

            if (inF is null || outF is null)
                throw new Exception("Invalid config");

            Console.WriteLine($"Compute for input {inF}, writing results in {outF}, extension to remove {etx}");
            Console.ReadKey();

            new JafaxLayoutExtraction().extract(inF, outF, etx);
        }
    }
}