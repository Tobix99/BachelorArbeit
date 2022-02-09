using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PCSC;
using PCSC.Iso7816;

namespace SmartCard_Host_App
{
    public class SetUpSimulatorCard
    {



        public SetUpSimulatorCard()
        {
            // Select the Applet

            byte[] selectedAID = new byte[8];

            Console.WriteLine("Bitte das Applet auswählen, welches im Simulator eingestellt werden soll\n");

            Console.WriteLine("1. HelloWorld");
            Console.WriteLine("2. Speedtest");
            Console.WriteLine("3. ProofOfConcept");

            Console.WriteLine();

            Console.Write("Ihre Auswahl: ");
            var pressedKey = Console.ReadKey();

            switch (pressedKey.KeyChar)
            {

                case '1':
                    selectedAID = MainApp.HelloWorldAID;
                    break;

                case '2':
                    selectedAID = MainApp.SpeedTestAID;
                    break;

                case '3':
                    selectedAID = MainApp.ProofOfConceptAID;
                    break;

                default:
                    Console.Write("Falsche Auswahl!");
                    return;

            }

            Console.WriteLine();

            Console.WriteLine("Setting up Virtual Card");


            byte[] data = new byte[(selectedAID.Length * 2) + 2];
            byte[] len = new byte[1];

            len[0] = (byte)(selectedAID.Length); // what if to big?

            int offset = 0;
            Buffer.BlockCopy(len, 0, data, offset, len.Length);
            offset += len.Length;

            Buffer.BlockCopy(selectedAID, 0, data, offset, selectedAID.Length);
            offset += selectedAID.Length;

            Buffer.BlockCopy(len, 0, data, offset, len.Length);
            offset += len.Length;

            Buffer.BlockCopy(selectedAID, 0, data, offset, selectedAID.Length);



            // send JCardSim Install APDU
            // 80 b8 0000 0F 084141414141414101 084141414141414101 7f
            // 80 b8 0000 0F 084141414141414200 084141414141414200 7f
            var contextFactory = ContextFactory.Instance;
            using (var context = contextFactory.Establish(SCardScope.System))
            {

                try
                {
                    // connect to CARD
                    var cardReader = new CardReader(context);


                    var installAPDU = new CommandApdu(IsoCase.Case4Short, SCardProtocol.Unset)
                    {
                        CLA = 0x80,
                        INS = 0xb8,
                        P1 = 0x00,
                        P2 = 0x00,
                        Le = 0x7f,
                        Data = data
                    };

                    var response = cardReader.SendAPDU(installAPDU);
                    if (!(cardReader.CheckResponse(response)))
                    {
                        Console.WriteLine("------------------------\n");
                        Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                        Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler: " + ex.Message);
                }
            }

        }
    }
}
