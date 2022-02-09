using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PCSC;

using PCSC.Iso7816;

namespace SmartCard_Host_App
{

    internal class CardReader
    {
        


        ISCardContext LocalContext;

        string[] ConntectedReaders = Array.Empty<string>();
        int SelectedCardReader = 0;

        /// <summary>
        /// Time in ms
        /// </summary>
        double lastExecutionTime = 0;

        public CardReader(ISCardContext context)
        {

            LocalContext = context;

            Console.WriteLine("Folgende Kartenleser sind verbunden: ");
            ConntectedReaders = context.GetReaders();

            for (int i = 0; i < ConntectedReaders.Length; i++)
            {
                Console.WriteLine("\t " + (i+1) + ": " + ConntectedReaders[i]);
            }


            Console.Write("Bitte Kartenleser auswählen: ");

            var readkey = Console.ReadKey();

            if (char.IsDigit(readkey.KeyChar))
            {
                SelectedCardReader = int.Parse(readkey.KeyChar.ToString())-1;
            }
            else
            {
                SelectedCardReader = -1;
            }
            

            Console.WriteLine("\n");

        }

        public Response SendAPDU(CommandApdu apdu)
        {
            // check context
            if (!LocalContext.IsValid())
                throw new Exception("Der Ressourcen Kontext ist nicht mehr gültig!");


            // check if SelectedCardReader is not -1
            if (SelectedCardReader == -1 || SelectedCardReader > ConntectedReaders.Length)
                throw new Exception("Es wurde ein Falscher Kartenleser ausgewählt!");


            var sw = new System.Diagnostics.Stopwatch();

            using (var isoReader = new IsoReader(LocalContext, ConntectedReaders[SelectedCardReader], SCardShareMode.Shared, SCardProtocol.T1, false))
            {
                sw.Start();
                var rAPDU = isoReader.Transmit(apdu);
                sw.Stop();

                lastExecutionTime = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

                return rAPDU;
            }

        }

        /// <summary>
        /// Function to Select a JavaCard Applet on the Card
        /// </summary>
        /// <param name="AID">Byte array constisting of full AID Byte</param>
        /// <returns></returns>
        public bool SelectApplet(byte[] AID)
        {

            var apdu = new CommandApdu(IsoCase.Case3Short, SCardProtocol.Unset)
            {
                CLA = 0x00, // Class
                Instruction = InstructionCode.SelectFile,
                P1 = 0x4, // Parameter 1
                //P1 = 0x00,
                P2 = 0x00, // Parameter 2
                Data = AID
            };

            var resp = SendAPDU(apdu);
            Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", resp.SW1, resp.SW2);

            // check SW1 and SW2
            return CheckResponse(resp);
        }

        public bool CheckResponse(Response resp)
        {
            if (resp.SW1 == 0x90 && resp.SW2 == 0x00)
                return true;
            else
                return false;

        }

        public double GetLastExecutionTime()
        {
            return lastExecutionTime;
        }

        public string GetActiveReaderName()
        {
            return ConntectedReaders[SelectedCardReader];
        }

    }
}
