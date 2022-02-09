using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using PCSC;
using PCSC.Iso7816;

namespace SmartCard_Host_App
{
    internal class AES
    {


        CardReader _Reader;
        List<int> bytesToTest = new();

        //int NumberOfAESInternalRepeats = 100;
        int NumberOfAESRepeats = MainApp.rounds;

        string FilenameAES = "Test_AES_internal.csv";

        private class CSVFormatAES : MainApp.CSVBase
        {
            public int Key_Size { get; set; }
            public string Key_Size_Unit { get; set; } = "bit";
            public int Size { get; set; }
            public string Size_Unit { get; set; } = "byte";
        }

        private class CSVFormaAESMap : ClassMap<CSVFormatAES>
        {
            public CSVFormaAESMap()
            {
                Map(m => m.functionName).Index(0).Name("Funktionsname");
                Map(m => m.terminalName).Index(1).Name("Terminal Name");
                Map(m => m.cardName).Index(2).Name("Karten Name");
                Map(m => m.functionRepeats).Index(3).Name("Wiederholungen");
                Map(m => m.Key_Size_Unit).Index(4).Name("Einheit Schluesselgroesse");
                Map(m => m.Size_Unit).Index(5).Name("Einheit Groesse");
                Map(m => m.durationUnit).Index(6).Name("Einheit Laufzeit");
                Map(m => m.Key_Size).Index(7).Name("Schluesselgroesse");
                Map(m => m.Size).Index(8).Name("Groesse");
                Map(m => m.duration).Index(9).Name("Laufzeit");
            }
        }

        List<CSVFormatAES> RecordTimes = new List<CSVFormatAES>();

        internal AES(CardReader reader)
        {
            _Reader = reader;

            bytesToTest.Add(16);
            bytesToTest.Add(32);
            bytesToTest.Add(64);
            bytesToTest.Add(128);
            bytesToTest.Add(160);
            bytesToTest.Add(192);
            bytesToTest.Add(256);
            bytesToTest.Add(320);
            bytesToTest.Add(512);
            bytesToTest.Add(640);
            bytesToTest.Add(1024);
        }


        internal void Start()
        {


            Console.WriteLine("\n");

            foreach (int KeyLength in Enum.GetValues(typeof(SpeedTest.KeyLengths)))
            {
                foreach (var size in bytesToTest)
                {
                    Console.WriteLine("Testing Keylength: " + KeyLength);
                    Console.WriteLine("Testing Size: " + size);

                    List<double> responseTime = new();


                    for (int i = 0; i < NumberOfAESRepeats; i++)
                    {

                        // CreatingKey
                        var response = _Reader.SendAPDU(GetCreateAESKeyAPDU(KeyLength));
                        if (!_Reader.CheckResponse(response))
                            throw new Exception("Error while Creating AES Key on Card");


                        // Encrypt internally (in Card RAM)

                        response = _Reader.SendAPDU(GetAESEncryptInternalAPDU(size));


                        if (_Reader.CheckResponse(response))
                        {

                            responseTime.Add(_Reader.GetLastExecutionTime());

                        }
                        else
                        {
                            Console.WriteLine("------------------------\n");
                            Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                            Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                            throw new Exception("Error while Encrypting AES-Array on Card");
                        }

                    }
                    Console.WriteLine("Time: {0}ms", responseTime.Average());
                    RecordTimes.Add(new CSVFormatAES
                    {
                        functionName = "AES - Encrypt - Internal",
                        terminalName = _Reader.GetActiveReaderName(),
                        cardName = MainApp.CardName,
                        functionRepeats = NumberOfAESRepeats,
                        Key_Size = KeyLength,
                        Size = size,
                        duration = responseTime.Average(),

                        //StandardAbweichung = Math.Sqrt(responseTime.Average(v => Math.Pow(v - responseTime.Average(), 2)))

                    });


                    Console.WriteLine("\n");
                }
            }

            Console.WriteLine("\n");
            Console.WriteLine("\n");

            WriteCSVFile();
        
        
        }




        private void WriteCSVFile()
        {
            var config = new CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture)
            {
                Delimiter = ";",

            };


            using (var writer = new StreamWriter(MainApp.PathToCSV + FilenameAES))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.Context.RegisterClassMap<CSVFormaAESMap>();
                csv.WriteRecords(RecordTimes);

            }

        }


        #region "AES APDUs"

        /// <summary>
        /// Create an AES key from random data. The short created from P1 and P2 define the key length. Permitted key lengths are 128, 192, 256 bit. To create a 256 (0x0100) bit key P1 would be 0x01 and P2 = 0x00.
        /// </summary>
        /// <returns></returns>
        private CommandApdu GetCreateAESKeyAPDU(int keyLength)
        {
            // TODO: why use enum in the first place
            // check if KeyLenght is 128, 192 or 256
            if (keyLength != 128 && keyLength != 192 && keyLength != 256)
                throw new Exception("Wrong Keysize Specified");

            // convert Keysize int into P1/P2
            byte P1_ = (byte)(keyLength >> 8); 
            byte P2_ = (byte)keyLength;

            return new CommandApdu(IsoCase.Case1, SCardProtocol.Unset)
            {
                CLA = SpeedTest.CLA_AES,
                INS = 0x40,
                P1 = P1_,
                P2 = P2_,
            };
        }


        /// <summary>
        /// Encrypt an internal transient byte array. The amount of bytes to be encrypted is defined by the short created out of P1 and P2. (To encrypt 600 (0x0258) bytes P1 is set to 0x02 and P2 to 0x58).
        /// 
        /// Conditions:
        /// - The requested amount has to be a multiple of the AES block size 16 bytes
        /// - An AES key has to be created in advance
        /// </summary>
        /// <returns></returns>
        private CommandApdu GetAESEncryptInternalAPDU(int size)
        {

            byte P1_ = (byte)(size >> 8);
            byte P2_ = (byte)size;

            return new CommandApdu(IsoCase.Case1, SCardProtocol.Unset)
            {
                CLA = SpeedTest.CLA_AES,
                INS = 0x46,
                P1 = P1_,
                P2 = P2_,
            };
        }

        #endregion "AES APDUs"
    }
}
