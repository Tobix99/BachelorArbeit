using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PCSC;
using PCSC.Iso7816;

namespace SmartCard_Host_App
{
    public class SpeedTest
    {

        

        public enum KeyLengths
        {
            Bit128 = 128,
            Bit192 = 192,
            Bit256 = 256
        }

        public static byte CLA_PING = 0xB1;
        public static byte CLA_ECHO = 0xB2;
        public static byte CLA_RANDOM = 0xB3;
        public static byte CLA_SIGANTURE_AVAILABILITY = 0xB4;
        public static byte CLA_AES = 0xB5;
        public static byte CLA_EC = 0xB6;
        public static byte CLA_TRANSPORT = 0xB7;

        public SpeedTest()
        {

            var contextFactory = ContextFactory.Instance;
            using (var context = contextFactory.Establish(SCardScope.System))
            {

                try
                {
                    // create CardReader instance (handels selection)
                    var cardReader = new CardReader(context);

                    // select Applet
                    if (cardReader.SelectApplet(MainApp.SpeedTestAID))
                        Console.WriteLine("Applet wurde selektiert");
                    else
                    {
                        Console.WriteLine("Bei der Selektierung des Applets ist ein Fehler aufgetreten");
                        throw new Exception("Applet selection failed");
                    }


                    // BASE TIME
                    Console.WriteLine("STARTING Basetime/Ping Test");
                    var baseTimeTest = new BaseTime(cardReader);
                    baseTimeTest.Start();
                    Console.WriteLine("Basetime/Ping Test DONE");
                    Console.WriteLine("\n");

                    // RANDOM Number
                    Console.WriteLine("STARTING Random Number Test");
                    var rndTest = new RandomNumber(cardReader);
                    rndTest.Start();
                    Console.WriteLine("Random Number Test DONE");
                    Console.WriteLine("\n");


                    // AES 
                    Console.WriteLine("STARTING AES Test");
                    var AESTest = new AES(cardReader);
                    AESTest.Start();
                    Console.WriteLine("AES Test DONE");
                    Console.WriteLine("\n");



                    // Ellipctic Curves
                    Console.WriteLine("STARTING EC Test");
                    var ECTest = new EllipticCurves(cardReader);
                    ECTest.Start();
                    Console.WriteLine("EC Test DONE");
                    Console.WriteLine("\n");



                    // Transport
                    Console.WriteLine("STARTING Transport Test");
                    var TPTest = new Transport(cardReader);
                    TPTest.Start();
                    Console.WriteLine("Transport Test DONE");


                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler: " + ex.Message);
                }

            }

        }

    }
}
