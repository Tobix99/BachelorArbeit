using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using PCSC;
using PCSC.Iso7816;


namespace SmartCard_Host_App
{
    internal class Transport
    {
        CardReader _Reader;

        List<int> bytesToTest = new() { 0, 16, 20, 32, 40, 64, 80, 128, 160, 192, 256, 320, 512, 640, 1024 };

        int NumberOfTransportInRepeats = MainApp.rounds;
        int NumberOfTransportOutRepeats = MainApp.rounds;

        string FilenameTransport = "Test_Transport.csv";

        private class CSVFormatTransport : MainApp.CSVBase
        {
            public string Einheit_Groesse { get; set; } = "byte";
            public int Groesse { get; set; }
            //public double StandardAbweichung { get; set; }
        }

        private class CSVFormatTransportMap : ClassMap<CSVFormatTransport>
        {
            public CSVFormatTransportMap()
            {
                Map(m => m.functionName).Index(0).Name("Funktionsname");
                Map(m => m.terminalName).Index(1).Name("Terminal Name");
                Map(m => m.cardName).Index(2).Name("Karten Name");
                Map(m => m.functionRepeats).Index(3).Name("Wiederholungen");
                Map(m => m.Einheit_Groesse).Index(4).Name("Einheit Groesse");
                Map(m => m.durationUnit).Index(5).Name("Einheit Laufzeit");
                Map(m => m.Groesse).Index(6).Name("Groesse");
                Map(m => m.duration).Index(7).Name("Laufzeit");
                //Map(m => m.StandardAbweichung).Index(8).Name("Standard Abweichung");
                // TODO: Varianz?
            }

        }

        List<CSVFormatTransport> RecordTimes = new List<CSVFormatTransport>();

        internal Transport(CardReader reader)
        {
            _Reader = reader;

        }


        internal void Start()
        {

            // transport in (to Card)
            TestTransportIn();

            // transport out (get from Card)
            TestTransportOut();

            // write CSV Data to File
            WriteCSVFile();

        }



        // Send bytes to Card
        private void TestTransportIn()
        {

            Console.WriteLine("\n Transport IN / Send to Card \n");

            foreach (var size in bytesToTest)
            {
                Console.WriteLine("Testing size: " + size);

                List<double> responseTime = new();


                for (int i = 0; i <= NumberOfTransportInRepeats; i++)
                {
                    // send APDU via CardReader
                    var response = _Reader.SendAPDU(GetSendDataToCardExtendedAPDU(size));

                    if (_Reader.CheckResponse(response))
                    {
                        responseTime.Add(_Reader.GetLastExecutionTime());
                    }
                    else
                    {
                        Console.WriteLine("------------------------\n");
                        Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                        Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                    }
                }

                Console.WriteLine("Time: {0}ms", responseTime.Average());
                RecordTimes.Add(new CSVFormatTransport
                {
                    functionName = "Transport - Send",
                    terminalName = _Reader.GetActiveReaderName(),
                    cardName = MainApp.CardName,
                    functionRepeats = NumberOfTransportInRepeats,
                    Groesse = size,
                    duration = responseTime.Average(),
                    //StandardAbweichung = Math.Sqrt(responseTime.Average(v => Math.Pow(v - responseTime.Average(), 2)))

                });

                Console.WriteLine("\n");
                Console.WriteLine("\n");
            }
  
        }


        // get From Card
        private void TestTransportOut()
        {
            Console.WriteLine("\n Transport OUT / Data from Card \n");

            foreach (var size in bytesToTest)
            {
                Console.WriteLine("Testing size: " + size);

                List<double> responseTime = new();


                for (int i = 0; i <= NumberOfTransportOutRepeats; i++)
                {

                    // send APDU via CardReader
                    var response = _Reader.SendAPDU(GetReceiveDataFromCardExtendedAPDU(size));

                    if (_Reader.CheckResponse(response))
                    {
                        // getLastExecutionTime and Print
                        if ((!response.HasData) && (size != 0))
                            throw new Exception("Not Data in response");
                        responseTime.Add(_Reader.GetLastExecutionTime());
                    }
                    else
                    {
                        Console.WriteLine("------------------------\n");
                        Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                        Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                    }
                }

                Console.WriteLine("Time: {0}ms", responseTime.Average());
                RecordTimes.Add(new CSVFormatTransport
                {
                    functionName = "Transport - Receive",
                    terminalName = _Reader.GetActiveReaderName(),
                    cardName = MainApp.CardName,
                    functionRepeats = NumberOfTransportOutRepeats,
                    Groesse = size,
                    duration = responseTime.Average(),
                    //StandardAbweichung = Math.Sqrt(responseTime.Average(v => Math.Pow(v - responseTime.Average(), 2)))

                });

                Console.WriteLine("\n");
                Console.WriteLine("\n");
            }
        }

        private void WriteCSVFile()
        {
            var config = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                Delimiter = ";",
                
            };


            using (var writer = new StreamWriter(MainApp.PathToCSV + FilenameTransport))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.Context.RegisterClassMap<CSVFormatTransportMap>();
                csv.WriteRecords(RecordTimes);
               
            }


        }

        #region "Transport APDUs"

        /// <summary>
        /// Get an acknowledgement that the card received the data.
        /// </summary>
        /// <returns></returns>
        private CommandApdu GetSendDataToCardExtendedAPDU(int NumberOfBytes)
        {

            // generate 
            Random rnd = new Random();
            byte[] data = new byte[NumberOfBytes];
            rnd.NextBytes(data);

            return new CommandApdu(IsoCase.Case3Extended, SCardProtocol.Unset)
            {
                CLA = SpeedTest.CLA_TRANSPORT,
                INS = 0x80,
                P1 = 0x00,
                P2 = 0x00,
                Data = data,
            };
        }


        /// <summary>
        /// Returns up to 1024 bytes from card. P1 and P2 define the number of bytes the card should send. Example: P1 = 0x04 and P2 = 0x00 to receive 1024 bytes from card.
        /// </summary>
        /// <returns></returns>
        private CommandApdu GetReceiveDataFromCardExtendedAPDU(int NumberOfBytes)
        {

            byte P1_ = (byte)(NumberOfBytes >> 8);
            byte P2_ = (byte)NumberOfBytes;

            return new CommandApdu(IsoCase.Case2Extended, SCardProtocol.Unset)
            {
                CLA = SpeedTest.CLA_TRANSPORT,
                INS = 0x81,
                P1 = P1_,
                P2 = P2_
            };
        }

        #endregion "Transport APDUs"



    }
}
