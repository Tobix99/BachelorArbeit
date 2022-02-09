using System.Text;

using PCSC;
using PCSC.Iso7816;


namespace SmartCard_Host_App
{
    internal class HelloWorld
    {

        
        readonly byte InstructionNormal = 0x00;
        readonly byte InstructionWithEcho = 0x01;

        string defaultMessage = "Hello World";

        public HelloWorld()
        {

            // establish Ressource Context
            var contextFactory = ContextFactory.Instance;
            using (var context = contextFactory.Establish(SCardScope.System))
            {

                try
                {
                    // create CardReader instance (handels selection of reader)
                    var cardReader = new CardReader(context);

                    // select Applet
                    if (cardReader.SelectApplet(MainApp.HelloWorldAID))
                        Console.WriteLine("Applet wurde selektiert");
                    else
                    {
                        Console.WriteLine("Bei der Selektierung des Applets ist ein Fehler aufgetreten");
                        throw new Exception("Applet selection failed");
                    }


                    // send APDU via CardReader
                    Console.WriteLine("Es kann die auszuführende Funktion gewählt werden.");
                    Console.WriteLine("Auswahl zwischen\n");

                    Console.WriteLine("1. Einfacher \"" + defaultMessage + "\" Nachricht");
                    Console.WriteLine("2. Eigene Nachricht");

                    Console.WriteLine();

                    Console.Write("Ihre Auswahl: ");

                    var key = Console.ReadKey();

                    Console.WriteLine("\n");

                    Response response = null;

                    if(key.KeyChar == '1')
                    {
                        response = cardReader.SendAPDU(GetHelloWorldNormalAPDU());
                    }
                        
                    else if(key.KeyChar == '2')
                    {
                        response = cardReader.SendAPDU(GetHelloWorldWithReturnAPDU());
                    }
                    else
                    {
                        Console.WriteLine("Auswahl wurde nicht erkannt, es wird mit 1 fortgefahren.");
                        response = cardReader.SendAPDU(GetHelloWorldNormalAPDU());
                    }
                        

                    if (cardReader.CheckResponse(response))
                    {
                        if (response.HasData)
                        {
                            Console.WriteLine("\n");
                            Console.WriteLine("Antwort: " + Encoding.ASCII.GetString(response.GetData()));
                        }
                    }
                    else
                    {
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


        // APDU für HelloWorld zurückgabe
        private CommandApdu GetHelloWorldNormalAPDU()
        {

            return new CommandApdu(IsoCase.Case2Short, SCardProtocol.Unset)
            {
                CLA = 0x00,
                INS = InstructionNormal,
                P1 = 0x00,
                P2 = 0x00,
                Le = defaultMessage.ToCharArray().Length
            };

        }

        // APDU für Echo nachricht mit übergabeparameter
        private CommandApdu GetHelloWorldWithReturnAPDU()
        {
            Console.Write("Bitte Nachricht eingeben /nicht mehr als 100 Zeichen): ");

            var line = Console.ReadLine();
            Console.WriteLine("");

            if (line == null)
                line = defaultMessage;

            if (line.Length > 100)
            {
                Console.WriteLine("Nachricht zu lang! Es wird \"" + defaultMessage + "\" genutzt.");
                line = defaultMessage;
            }

            return new CommandApdu(IsoCase.Case4Short, SCardProtocol.Unset)
            {
                CLA = 0x00,
                INS = InstructionWithEcho,
                P1 = 0x00,
                P2 = 0x00,
                Data = Encoding.ASCII.GetBytes(line.ToCharArray()),
                Le = line.ToCharArray().Length
            };
        }

    }
}
