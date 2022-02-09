using System;

namespace SmartCard_Host_App
{
    public class MainApp
    {

        // Public AIDs

        public readonly static byte[] HelloWorldAID = new byte[] { 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x00 };
        public readonly static byte[] SpeedTestAID = new byte[] { 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x42, 0x00 };
        public readonly static byte[] ProofOfConceptAID = new byte[] { 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x43, 0x00 };


        // Basic Settings

        public static string CardName = "J3D081";
        public static string PathToCSV = ".\\";
        public static int rounds = 75;


        // Base Class for all CSV Objects
        public class CSVBase
        {
            public string cardName { get; set; } = CardName;
            public string terminalName { get; set; } = "TerminalName not found";
            public string functionName { get; set; } = "FunctionName not found";
            public string durationUnit { get; set; } = "ms";
            public double duration { get; set; }
            public int functionRepeats { get; set; }
        }




        // Main Method
        static public void Main(String[] args)
        {

            while (true)
            {
                Console.WriteLine("Bitte aus folgenden Anwendungen auswählen:");
                Console.WriteLine();

                Console.WriteLine("1.) HelloWorld");
                Console.WriteLine("2.) Speedtest");
                Console.WriteLine("3.) Proof of Concept");
                Console.WriteLine("4.) Setup Simulator Card");
                Console.WriteLine("5.) Setup Anwendung");
                Console.WriteLine("6.) Beenden");

                Console.WriteLine();
                Console.Write("Ihre Auswahl: ");

                // Read pressed key
                var key = Console.ReadKey();

                Console.WriteLine("\n");

                switch (key.KeyChar)
                {
                    case '1':
                        // Hello World

                         new HelloWorld();
                        break;

                    case '2':
                        // Speedtest

                        new SpeedTest();
                        break;

                    case '3':
                        // ProofOfConcept

                        new ProofOfConcept();
                        break;

                    case '4':
                        // Setup SIM Card (call install Method etc)

                        new SetUpSimulatorCard();
                        break;


                    case '5':
                        // Setup App --> CSV Path etc

                        new SetUpApp();
                        break;
                    case '6':
                        // exit 

                        Environment.Exit(0);
                        break;

                    default:

                        break;

                }


                Console.WriteLine("\n");
                Console.WriteLine("Zum fortfahren [ENTER] drücken");
                Console.ReadLine();

                Console.Clear();


            }
            
        }
    }
}

