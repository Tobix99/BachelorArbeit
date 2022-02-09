using PCSC;
using PCSC.Iso7816;

using CsvHelper.Configuration;
using CsvHelper;

using System.Security.Cryptography;



namespace SmartCard_Host_App
{
    internal class EllipticCurves
    {


        CardReader _Reader;

        List<byte> Algorithms = new List<byte> { 0x11, 0x21 }; // test for Algorithm SHA-1 and SHA-256 (0x11, 0x21)
        List<int> TestSizes = new List<int> { 16, 32, 64, 128, 256, 512, 768 };

        string FileNameEC = "Test_EC.csv";

        int NumberOfKeyPairGenRepeats = MainApp.rounds;
        int NumberOfSecretGenRepeats = MainApp.rounds;


        int NumberOfECDSASignRepeats = MainApp.rounds;
        int NumberOfECDSAVerfiyRepeats = MainApp.rounds;
        int NumberOfECDSAInitializeRepeats = MainApp.rounds;

        private class CSVFormatEC : MainApp.CSVBase
        {
            public string size_Unit { get; set; } = "byte";
            public int size { get; set; }
            public int EC_Key_Size { get; set; } = 256;
            public string EC_key_Size_Unit { get; set; } = "bit";
            public string Algorithm { get; set; } = "unbekannt";
            //public double StandardAbweichung { get; set; }
        }

        private class CSVFormatECMap : ClassMap<CSVFormatEC>
        {
            public CSVFormatECMap()
            {
                Map(m => m.functionName).Index(0).Name("Funktionsname");
                Map(m => m.terminalName).Index(1).Name("Terminal Name");
                Map(m => m.cardName).Index(2).Name("Karten Name");
                Map(m => m.functionRepeats).Index(3).Name("Wiederholungen");
                Map(m => m.EC_Key_Size).Index(4).Name("EC Schluesselgroesse");
                Map(m => m.EC_key_Size_Unit).Index(5).Name("EC Schluesselgroesseneinheit");
                Map(m => m.size_Unit).Index(6).Name("Einheit Groesse");
                Map(m => m.durationUnit).Index(7).Name("Einheit Laufzeit");
                Map(m => m.Algorithm).Index(8).Name("Hash-Algorithmus");
                Map(m => m.size).Index(9).Name("Groesse");
                Map(m => m.duration).Index(10).Name("Laufzeit");
                //Map(m => m.StandardAbweichung).Index(8).Name("Standard Abweichung");
                // TODO: Varianz?
            }

        }

        List<CSVFormatEC> RecordTimes = new List<CSVFormatEC>();

        internal EllipticCurves(CardReader reader)
        {
            _Reader = reader;
        }

        internal void Start()
        {

            KeyPairGen();
            SecretGen();

            ECDSA_Sign();
            ECDSA_Verfiy();
            ECDSA_Initialize();

            WriteCSVFile();
        }



        private void KeyPairGen()
        {


            Console.WriteLine("\nECKeyPairGeneration \n");


            List<double> responseTime = new();

            for (int i = 0; i < NumberOfKeyPairGenRepeats; i++)
            {
                // send APDU via CardReader
                var response = _Reader.SendAPDU(GetCreateEllipticCurveKeyPairAPDU());
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
            RecordTimes.Add(new CSVFormatEC
            {
                functionName = "ECDSA Key Pair Generation",
                terminalName = _Reader.GetActiveReaderName(),
                cardName = MainApp.CardName,
                functionRepeats = NumberOfKeyPairGenRepeats,
                size = 0,
                duration = responseTime.Average(),

                //StandardAbweichung = Math.Sqrt(responseTime.Average(v => Math.Pow(v - responseTime.Average(), 2)))

            });


            Console.WriteLine("\n");
            Console.WriteLine("\n");



        }


        // EC secret generation
        private void SecretGen()
        {

            Console.WriteLine("\nECSecretGeneration (ECDH) \n");


            // ----
            // Generate secret key pair on card
            // ----

            // send APDU via CardReader
            var response = _Reader.SendAPDU(GetCreateEphemeralKeyPairAPDU());
            Console.WriteLine("Time: {0}ms", _Reader.GetLastExecutionTime());

            //byte[] publicPartCard = new byte[0];

            if (_Reader.CheckResponse(response))
            {
                // getLastExecutionTime and Print
                if (response.HasData)
                {
                    Console.WriteLine("Got Data");
                    //publicPartCard = response.GetData();
                }
            }
            else
            {
                Console.WriteLine("------------------------\n");
                Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
            }


            ECDiffieHellmanCng HostECDH = new(ECCurve.NamedCurves.brainpoolP256r1);
            HostECDH.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            HostECDH.HashAlgorithm = CngAlgorithm.Sha1;


            var HostPlublicKey = HostECDH.ExportSubjectPublicKeyInfo().Skip(27).ToArray();

            // ----
            // DH exchange message
            // ----


            List<double> responseTime = new();

            for (int i = 0; i < NumberOfSecretGenRepeats; i++)
            {
                response = _Reader.SendAPDU(GetGenerateECDHSecretAPDU(HostPlublicKey));


                byte[] secretCard;

                if (_Reader.CheckResponse(response))
                {
                    // getLastExecutionTime and Print
                    if (response.HasData)
                    {
                        Console.WriteLine("Got Data");
                        secretCard = response.GetData(); // get 20 Byte Secret from Card

                        // print secret from card
                        Console.WriteLine("Card Secret: " + BitConverter.ToString(secretCard).Replace('-', ' '));

                        responseTime.Add(_Reader.GetLastExecutionTime());
                    }
                }
                else
                {
                    Console.WriteLine("------------------------\n");
                    Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                    Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                }

            }


            Console.WriteLine("Time: {0}ms", responseTime.Average());

            RecordTimes.Add(new CSVFormatEC
            {
                functionName = "ECDSA Generate Secret",
                terminalName = _Reader.GetActiveReaderName(),
                cardName = MainApp.CardName,
                functionRepeats = NumberOfSecretGenRepeats,
                size = 0,
                duration = responseTime.Average()

                //StandardAbweichung = Math.Sqrt(responseTime.Average(v => Math.Pow(v - responseTime.Average(), 2)))

            });

            Console.WriteLine("\n");
            Console.WriteLine("\n");

        }


        // ECDSA Sign
        private void ECDSA_Sign()
        {
            Console.WriteLine("\nECDSA Sign \n");


            foreach (var algo in Algorithms)
            {

                foreach (int size in TestSizes)
                {

                    // START Setup

                    Console.WriteLine("Generating Key");
                    var response = _Reader.SendAPDU(GetCreateEllipticCurveKeyPairAPDU());
                    if (_Reader.CheckResponse(response))
                    {
                        // Not Important
                        Console.WriteLine("KeyPair Gen Time: {0}ms", _Reader.GetLastExecutionTime());
                    }
                    else
                    {
                        Console.WriteLine("------------------------\n");
                        Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                        Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                    }

                    Console.WriteLine("Setting Hash Algorithm to: \"" + getAlgoName(algo) + "\"");
                    response = _Reader.SendAPDU(GetSetTheECDSAHashAlgorithmAPDU(algo));

                    if (_Reader.CheckResponse(response))
                    {
                        // Not Important
                        Console.WriteLine("Set Algorithm Time: {0}ms", _Reader.GetLastExecutionTime());
                    }
                    else
                    {
                        Console.WriteLine("------------------------\n");
                        Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                        Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                    }


                    Console.WriteLine("Inititilize ECDSA Signing Mode");
                    response = _Reader.SendAPDU(GetInitializeECDSASigningModeAPDU());

                    if (_Reader.CheckResponse(response))
                    {
                        // Not Important
                        Console.WriteLine("Inititilize ECDSA Signing Mode Time: {0}ms", _Reader.GetLastExecutionTime());
                    }
                    else
                    {
                        Console.WriteLine("------------------------\n");
                        Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                        Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                    }

                    // END Setup


                    Console.WriteLine("Create ECDSA Signature");

                    List<double> responseTime = new();

                    for (int i = 0; i < NumberOfECDSASignRepeats; i++)
                    {

                        response = _Reader.SendAPDU(GetCreateECDSASignatureAPDU(size));

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

                    Console.WriteLine("Create ECDSA Signature Time: {0}ms", responseTime.Average());

                    RecordTimes.Add(new CSVFormatEC
                    {
                        functionName = "ECDSA Sign",
                        terminalName = _Reader.GetActiveReaderName(),
                        cardName = MainApp.CardName,
                        functionRepeats = NumberOfECDSASignRepeats,
                        size = size,
                        duration = responseTime.Average(),
                        Algorithm = getAlgoName(algo),
                        //StandardAbweichung = Math.Sqrt(responseTime.Average(v => Math.Pow(v - responseTime.Average(), 2)))

                    });

                    Console.WriteLine("\n");

                }

                Console.WriteLine("\n"); Console.WriteLine("\n");
            }

        }


        // ECDSA Verify
        private void ECDSA_Verfiy()
        {

            Console.WriteLine("\nECDSA Verify \n");

            foreach (var algo in Algorithms)
            {

                foreach (int size in TestSizes)
                {

                    // START Setup

                    Console.WriteLine("Generating Key");
                    var response = _Reader.SendAPDU(GetCreateEllipticCurveKeyPairAPDU());
                    if (_Reader.CheckResponse(response))
                    {
                        // Not Important
                        Console.WriteLine("KeyPair Gen Time: {0}ms", _Reader.GetLastExecutionTime());
                    }
                    else
                    {
                        Console.WriteLine("------------------------\n");
                        Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                        Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                    }

                    // TODO: better output
                    Console.WriteLine("Setting Hash Algorithm to: \"" + getAlgoName(algo) + "\"");
                    response = _Reader.SendAPDU(GetSetTheECDSAHashAlgorithmAPDU(algo));

                    if (_Reader.CheckResponse(response))
                    {
                        // Not Important
                        Console.WriteLine("Set Algorithm Time: {0}ms", _Reader.GetLastExecutionTime());
                    }
                    else
                    {
                        Console.WriteLine("------------------------\n");
                        Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                        Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                    }


                    Console.WriteLine("Inititilize ECDSA Signing Mode");
                    response = _Reader.SendAPDU(GetInitializeECDSASigningModeAPDU());

                    if (_Reader.CheckResponse(response))
                    {
                        // Not Important
                        Console.WriteLine("Inititilize ECDSA Signing Mode Time: {0}ms", _Reader.GetLastExecutionTime());
                    }
                    else
                    {
                        Console.WriteLine("------------------------\n");
                        Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                        Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                    }




                    Console.WriteLine("Create ECDSA Signature");
                    response = _Reader.SendAPDU(GetCreateECDSASignatureAPDU(size));

                    if (_Reader.CheckResponse(response))
                    {
                        // Not Important
                        Console.WriteLine("Create ECDSA Signature Time: {0}ms", _Reader.GetLastExecutionTime());
                    }
                    else
                    {
                        Console.WriteLine("------------------------\n");
                        Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                        Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                    }





                    Console.WriteLine("Initialize ECDSA Verification Mode");
                    response = _Reader.SendAPDU(GetInitializeECDSAVerificationModeAPDU());

                    if (_Reader.CheckResponse(response))
                    {
                        // Not Important
                        Console.WriteLine("Initialize ECDSA Verification Mode Time: {0}ms", _Reader.GetLastExecutionTime());
                    }
                    else
                    {
                        Console.WriteLine("------------------------\n");
                        Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                        Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                    }



                    // END Setup





                    Console.WriteLine("Verify Signature");

                    List<double> responseTime = new();

                    for (int i = 0; i < NumberOfECDSAVerfiyRepeats; i++)
                    {


                        response = _Reader.SendAPDU(GetVerifyECDSASignatureAPDU());

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

                    Console.WriteLine("Verify Signature Time: {0}ms", responseTime.Average());

                    RecordTimes.Add(new CSVFormatEC
                    {
                        functionName = "ECDSA Verify",
                        terminalName = _Reader.GetActiveReaderName(),
                        cardName = MainApp.CardName,
                        functionRepeats = NumberOfECDSAVerfiyRepeats,
                        size = size,
                        duration = responseTime.Average(),
                        Algorithm = getAlgoName(algo),
                        //StandardAbweichung = Math.Sqrt(responseTime.Average(v => Math.Pow(v - responseTime.Average(), 2)))

                    });

                    Console.WriteLine("\n");

                }

                Console.WriteLine("\n"); Console.WriteLine("\n");
            }




        }

        // ECDSA Init
        private void ECDSA_Initialize()
        {

            Console.WriteLine("\nECDSA Sign \n");

            foreach (var algo in Algorithms)
            {



                // START Setup

                Console.WriteLine("Generating Key");
                var response = _Reader.SendAPDU(GetCreateEllipticCurveKeyPairAPDU());
                if (_Reader.CheckResponse(response))
                {
                    // Not Important
                    Console.WriteLine("KeyPair Gen Time: {0}ms", _Reader.GetLastExecutionTime());
                }
                else
                {
                    Console.WriteLine("------------------------\n");
                    Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                    Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                }

                Console.WriteLine("Setting Hash Algorithm to: \"" + getAlgoName(algo) + "\"");
                response = _Reader.SendAPDU(GetSetTheECDSAHashAlgorithmAPDU(algo));

                if (_Reader.CheckResponse(response))
                {
                    // Not Important
                    Console.WriteLine("Set Algorithm Time: {0}ms", _Reader.GetLastExecutionTime());
                }
                else
                {
                    Console.WriteLine("------------------------\n");
                    Console.WriteLine("Bei der Verarbeitung auf der Karte ist ein Fehler aufgetreten");
                    Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", response.SW1, response.SW2);
                }

                // END Setup

                Console.WriteLine("Inititilize ECDSA Signing Mode");


                List<double> responseTime = new();

                for (int i = 0; i < NumberOfECDSAInitializeRepeats; i++)
                {
                    response = _Reader.SendAPDU(GetInitializeECDSASigningModeAPDU());

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
                Console.WriteLine("Inititilize ECDSA Signing Mode Time: {0}ms", responseTime.Average());

                RecordTimes.Add(new CSVFormatEC
                {
                    functionName = "ECDSA Initialize",
                    terminalName = _Reader.GetActiveReaderName(),
                    cardName = MainApp.CardName,
                    functionRepeats = NumberOfECDSAInitializeRepeats,
                    size = 0,
                    duration = responseTime.Average(),
                    Algorithm = getAlgoName(algo),
                    //StandardAbweichung = Math.Sqrt(responseTime.Average(v => Math.Pow(v - responseTime.Average(), 2)))

                });

                Console.WriteLine("\n");

            }

            Console.WriteLine("\n"); Console.WriteLine("\n");

        }


        private string getAlgoName(byte Algo)
        {

            if (Algo == 0x11)
                return "SHA-1";
            else if (Algo == 0x21)
                return "SHA-256";

            return "Unknown Algo";
        }

        private void WriteCSVFile()
        {
            var config = new CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture)
            {
                Delimiter = ";",

            };


            using (var writer = new StreamWriter(MainApp.PathToCSV + FileNameEC))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.Context.RegisterClassMap<CSVFormatECMap>();
                csv.WriteRecords(RecordTimes);

            }


        }

        #region "Eliptic Curves"

        /// <summary>
        /// Set the ECDSA signing algorithm. The algorithm is defined by P1. P1 set with 0x11 set the algorithm to SHA-1, 0x21 sets it to SHA-256.
        /// </summary>
        /// <returns></returns>
        private CommandApdu GetSetTheECDSAHashAlgorithmAPDU(byte Algorithm)
        {
            return new CommandApdu(IsoCase.Case1, SCardProtocol.Unset)
            {
                CLA = SpeedTest.CLA_EC,
                INS = 0x6C,
                P1 = Algorithm,
                P2 = 0x00,
            };
        }

        /// <summary>
        /// Create a 256 bit elliptic curve key pair.
        /// </summary>
        /// <returns></returns>
        private CommandApdu GetCreateEllipticCurveKeyPairAPDU()
        {
            return new CommandApdu(IsoCase.Case1, SCardProtocol.Unset)
            {
                CLA = SpeedTest.CLA_EC,
                INS = 0x60,
                P1 = 0x00,
                P2 = 0x00,
            };
        }


        /// <summary>
        /// Initialize the signature object in signing mode
        /// Condition:
        /// - An elliptic curve key pair has to be generated in advance.
        /// </summary>
        /// <returns></returns>
        private CommandApdu GetInitializeECDSASigningModeAPDU()
        {
            return new CommandApdu(IsoCase.Case1, SCardProtocol.Unset)
            {
                CLA = SpeedTest.CLA_EC,
                INS = 0x68,
                P1 = 0x00,
                P2 = 0x00,
            };
        }


        /// <summary>
        /// Initialize the signature object in verification mode
        /// Conditions:
        /// - An elliptic curve key pair has to be generated in advance.
        /// </summary>
        /// <returns></returns>
        private CommandApdu GetInitializeECDSAVerificationModeAPDU()
        {
            return new CommandApdu(IsoCase.Case1, SCardProtocol.Unset)
            {
                CLA = SpeedTest.CLA_EC,
                INS = 0x69,
                P1 = 0x00,
                P2 = 0x00,
            };
        }


        /// <summary>
        /// Create an ECDSA signature in the transient memory of the card.
        /// The short created by P1 and P2 defines the amount of bytes over which the signature should be created.
        /// Example: To sign 564 bytes P1 = 0x02 and P2 = 0x34 (The limit is 768 bytes, to the the transient array size on the card).
        /// Conditions:
        /// - The hash algorithm needs to be set in advance
        /// - A key pair needs to be created in advance
        /// - The mode needs to be set to signature creation
        /// </summary>
        /// <returns></returns>
        private CommandApdu GetCreateECDSASignatureAPDU(int sizeOfSignature)
        {
            // check that siue is not above 768 Bytes
            if (sizeOfSignature > 768)
                throw new Exception("Size for Signature generation too large");

            byte P1_ = (byte)(sizeOfSignature >> 8);
            byte P2_ = (byte)sizeOfSignature;

            return new CommandApdu(IsoCase.Case1, SCardProtocol.Unset)
            {
                CLA = SpeedTest.CLA_EC,
                INS = 0x6A,
                P1 = P1_,
                P2 = P2_,
            };
        }


        /// <summary>
        /// Verify an ECDSA signature that has been previously created over a temporary array on the card Command APDU
        /// Conditions:
        /// - The signature creation command has to be executed in advance
        /// - The signature mode has to be switched to verification mode in advance.
        /// </summary>
        /// <returns></returns>
        private CommandApdu GetVerifyECDSASignatureAPDU()
        {
            return new CommandApdu(IsoCase.Case1, SCardProtocol.Unset)
            {
                CLA = SpeedTest.CLA_EC,
                INS = 0x6B,
                P1 = 0x00,
                P2 = 0x00,
            };
        }


        /// <summary>
        /// Generate an ephemeral 256 bit elliptic curve key pair and return the public key.
        /// </summary>
        /// <returns></returns>
        private CommandApdu GetCreateEphemeralKeyPairAPDU()
        {
            return new CommandApdu(IsoCase.Case2Short, SCardProtocol.Unset)
            {
                CLA = SpeedTest.CLA_EC,
                INS = 0x64,
                P1 = 0x00,
                P2 = 0x00,
                Le = 0xFF
            };
        }


        /// <summary>
        /// Generate an ECDH secret with an externally provided public key and an ephemeral key on card and return the 20 byte secret.
        /// Conditions:
        /// - An ephemeral key needs to be created on the card in advance
        /// </summary>
        /// <param name="publicKeyPoint">65 byte public key point</param>
        /// <returns></returns>
        private CommandApdu GetGenerateECDHSecretAPDU(byte[] publicKeyPoint)
        {
            return new CommandApdu(IsoCase.Case4Short, SCardProtocol.Unset)
            {
                CLA = SpeedTest.CLA_EC,
                INS = 0x65,
                P1 = 0x00,
                P2 = 0x00,
                Le = 0x65,// returns 20 byte secret
                Data = publicKeyPoint // 65 byte public key point
            };
        }


        #endregion "Eliptic Curves"

    }
}
