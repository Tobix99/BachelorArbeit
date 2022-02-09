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
    internal class BaseTime
    {

        CardReader _Reader;

        //int NumberOfPingRepeats = 50;
        int NumberOfPingRepeats = MainApp.rounds;

        string FilenamePing = "Test_base_time.csv";

        private class CSVFormatBaseTimeMap : ClassMap<MainApp.CSVBase>
        {
            public CSVFormatBaseTimeMap()
            {
                Map(m => m.functionName).Index(0).Name("Funktionsname");
                Map(m => m.terminalName).Index(1).Name("Terminal Name");
                Map(m => m.cardName).Index(2).Name("Karten Name");
                Map(m => m.functionRepeats).Index(3).Name("Wiederholungen");
                Map(m => m.durationUnit).Index(4).Name("Einheit Laufzeit");
                Map(m => m.duration).Index(5).Name("Laufzeit");

                // Map(m => m.StandardAbweichung).Index(7).Name("Standard Abweichung");
                // TODO: Varianz?
            }
        }


        List<MainApp.CSVBase> RecordTimes = new List<MainApp.CSVBase>();


        internal BaseTime(CardReader reader)
        {
            _Reader = reader;
        }


        internal void Start()
        {
            Console.WriteLine("\n");

            List<double> responseTime = new();

            for (int i = 0; i < NumberOfPingRepeats; i++)
            {



                // send APDU via CardReader
                var response = _Reader.SendAPDU(GetPingAPDU());


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


            Console.WriteLine("Time: {0}ms", _Reader.GetLastExecutionTime());
            RecordTimes.Add(new MainApp.CSVBase
            {
                functionName = "BaseTime",
                terminalName = _Reader.GetActiveReaderName(),
                cardName = MainApp.CardName,
                functionRepeats = NumberOfPingRepeats,
                duration = responseTime.Average(),
                //StandardAbweichung = Math.Sqrt(responseTime.Average(v => Math.Pow(v - responseTime.Average(), 2)))

            });



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

            using (var writer = new StreamWriter(MainApp.PathToCSV + FilenamePing))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.Context.RegisterClassMap<CSVFormatBaseTimeMap>();
                csv.WriteRecords(RecordTimes);

            }


        }


        #region "Ping/Base time APDUs"

        /// <summary>
        /// Get a 0x9000 reply from the card. No further operations are executed. This is used to measure the base connection time, the time it takes a card to reply to a four byte APDU.
        /// </summary>
        /// <returns></returns>
        /// 
        private CommandApdu GetPingAPDU()
        {
            return new CommandApdu(IsoCase.Case1, SCardProtocol.Unset)
            {
                CLA = SpeedTest.CLA_PING,
                INS = 0x20,
                P1 = 0x00,
                P2 = 0x00,

            };
        }
        #endregion


    }
}
