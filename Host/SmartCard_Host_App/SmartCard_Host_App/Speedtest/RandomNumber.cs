using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using PCSC;
using PCSC.Iso7816;

namespace SmartCard_Host_App
{
    internal class RandomNumber
    {

        CardReader _Reader;

        List<int> bytesToTest = new();


        int NumberOfRandomNumberGenerationRepeats = MainApp.rounds;

        string FilenameAES = "Test_Random_Number_Generation.csv";

        private class CSVFormatRNDNumberGen : MainApp.CSVBase
        {
            public int Size { get; set; }
            public string Size_Unit { get; set; } = "byte";
        }

        private class CSVFormaRNDNumberGenMap : CsvHelper.Configuration.ClassMap<CSVFormatRNDNumberGen>
        {
            public CSVFormaRNDNumberGenMap()
            {
                Map(m => m.functionName).Index(0).Name("Funktionsname");
                Map(m => m.terminalName).Index(1).Name("Terminal Name");
                Map(m => m.cardName).Index(2).Name("Karten Name");
                Map(m => m.functionRepeats).Index(3).Name("Wiederholungen");
                Map(m => m.Size_Unit).Index(4).Name("Einheit Groesse");
                Map(m => m.durationUnit).Index(5).Name("Einheit Laufzeit");
                Map(m => m.Size).Index(6).Name("Groesse");
                Map(m => m.duration).Index(7).Name("Laufzeit");
            }
        }

        List<CSVFormatRNDNumberGen> RecordTimes = new List<CSVFormatRNDNumberGen>();




        internal RandomNumber(CardReader reader)
        {
            _Reader = reader;

            // TODO: ??
            bytesToTest.Add(16);
            bytesToTest.Add(32);
            bytesToTest.Add(64);
            bytesToTest.Add(128);
            bytesToTest.Add(256);
            bytesToTest.Add(512);
            bytesToTest.Add(1024);
        }


        // TODO: run Tests multiple Times and average/standart deviation
        internal void Start()
        {
            Console.WriteLine("\n");
            foreach (var size in bytesToTest)
            {
                Console.WriteLine("Testing size: " + size);

                List<double> responseTime = new();

                for (int i = 0; i < NumberOfRandomNumberGenerationRepeats; i++)
                {

                

                var response = _Reader.SendAPDU(GetCreateRandomDataAPDU(size));
                

                if (_Reader.CheckResponse(response))
                {


                        responseTime.Add(_Reader.GetLastExecutionTime());

                    }
                else
                {
                    Console.WriteLine("------------------------\n");
                    Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                    Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                    throw new Exception("Error while generation Rnadiom Numbers on Card");
                }
                }

                Console.WriteLine("Time: {0}ms", responseTime.Average());
                RecordTimes.Add(new CSVFormatRNDNumberGen
                {
                    functionName = "Random Number Generation",
                    terminalName = _Reader.GetActiveReaderName(),
                    cardName = MainApp.CardName,
                    functionRepeats = NumberOfRandomNumberGenerationRepeats,
                    Size = size,
                    duration = responseTime.Average(),

                    //StandardAbweichung = Math.Sqrt(responseTime.Average(v => Math.Pow(v - responseTime.Average(), 2)))

                });


            }

            Console.WriteLine("\n");
            Console.WriteLine("\n");

            WriteCSVFile();
        }

        private void WriteCSVFile()
        {
            var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture)
            {
                Delimiter = ";",

            };


            using (var writer = new StreamWriter(MainApp.PathToCSV + FilenameAES))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.Context.RegisterClassMap<CSVFormaRNDNumberGenMap>();
                csv.WriteRecords(RecordTimes);

            }

        }


        #region "Random Data APDUs"

        /// <summary>
        /// Create bytes with the true random number generator on the card.
        /// The short created by P1 and P2 defines the number of bytes that should be created.(Example: P1 = 0x01, P2 = 0x23 to create 291 bytes)
        /// </summary>
        /// <param name="NumberOfRNDBytes">Set the Number of Random Bytes to create</param>
        /// <returns></returns>
        private CommandApdu GetCreateRandomDataAPDU(int NumberOfRNDBytes)
        {
            if (NumberOfRNDBytes < 0 || NumberOfRNDBytes > 65535)
                throw new Exception("Wrong Testsize for Random Number Test");

            // Converting INT into 2 Bytes
            byte P1_ = (byte)(NumberOfRNDBytes >> 8);
            byte P2_ = (byte)NumberOfRNDBytes;

            return new CommandApdu(IsoCase.Case1, SCardProtocol.Unset)
            {
                CLA = SpeedTest.CLA_RANDOM,
                INS = 0x30,
                P1 = P1_,
                P2 = P2_,
            };
        }

        #endregion "Random Data APDUs"

    }
}
