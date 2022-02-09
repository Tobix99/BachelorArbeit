using PCSC;
using PCSC.Iso7816;

using System.Security.Cryptography;

namespace SmartCard_Host_App
{
    internal class ProofOfConcept
    {

        private bool isHostAuthenticatedByCard = false;
        private bool isCardAuthenticated = false;


        // only for testing Purposes --> in real world don't store key in App!
        // 128Bit Keys --> 16 Bytes
        private byte[] internalAuthKey = new byte[] { 0x12, 0x34, 0x56, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x17 };
        private byte[] externalAuthKey = new byte[] { 0x12, 0x34, 0x56, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18 };

        // 128Bit IV --> 16 Bytes
        private byte[] initialVector = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x16, 0x00, 0x00 };

        public ProofOfConcept()
        {



            var contextFactory = ContextFactory.Instance;
            using (var context = contextFactory.Establish(SCardScope.System))
            {

                try
                {
                    // create CardReader instance (handels selection)
                    var cardReader = new CardReader(context);

                    // select Applet
                    if (cardReader.SelectApplet(MainApp.ProofOfConceptAID))
                        Console.WriteLine("Applet wurde selektiert");
                    else
                    {
                        Console.WriteLine("Bei der Selektierung des Applets ist ein Fehler aufgetreten");
                        throw new Exception("Applet selection failed");
                    }

                    while (true)
                    {
                        // Funktion auswählen
                        Console.WriteLine("Bitte aus folgenden Funktionen auswählen:");
                        Console.WriteLine();

                        Console.WriteLine("1.) Genegüber der Karte authentifizieren [Aktuell: \"" + isHostAuthenticatedByCard + "\"]");
                        Console.WriteLine("2.) Karten authentifizieren [Aktuell: \"" + isCardAuthenticated + "\"]");
                        Console.WriteLine("3.) AES Schlüssel auf Karte erstellen");
                        Console.WriteLine("4.) AES Schlüssel auf Karte löschen");
                        Console.WriteLine("5.) String mit Karte verschlüsseln");
                        Console.WriteLine("6.) String mit Karte entschlüsseln");
                        Console.WriteLine("7.) Beenden");

                        Console.WriteLine();
                        Console.Write("Ihre Auswahl: ");

                        var key = Console.ReadKey();

                        Console.WriteLine("\n");




                        // encrypt string [32 Char]

                        // decrypt string [32 Char]

                        try
                        {



                            switch (key.KeyChar)
                            {
                                case '1':
                                    // External Auth
                                    Console.WriteLine("Host wird verifiziert...");
                                    if (ExternalAuth(cardReader))
                                        Console.WriteLine("Authentifizierung erfolgreich");
                                    else
                                        Console.WriteLine("Authentifizierung fehlgeschlagen");
                                    break;

                                case '2':
                                    // internal Auth
                                    Console.WriteLine("Karte wird überprüft...");
                                    if (InternalAuth(cardReader))
                                        Console.WriteLine("Authentifizierung erfolgreich");
                                    else
                                        Console.WriteLine("Authentifizierung fehlgeschlagen");

                                    break;

                                case '3':
                                    // create AES Key
                                    Console.WriteLine("Schlüssel wird auf Karte erstellt...");
                                    if (CreateAESKey(cardReader))
                                        Console.WriteLine("Schlüssel erfolgreich erstellt");
                                    else
                                        Console.WriteLine("Schlüsselerstellung fehlerhaft");

                                    break;

                                case '4':
                                    // delete AES Key
                                    Console.WriteLine("Schlüssel wird auf Karte gelöscht...");
                                    if (DeleteAESKey(cardReader))
                                        Console.WriteLine("Schlüssel erfolgreich gelöscht");
                                    else
                                        Console.WriteLine("Bei dem Löschen des Schlüssels ist ein Fehler aufgetreten");

                                    break;

                                case '5':
                                    // encrypt
                                    EncryptData(cardReader, GetStringFromConsole());
                                    break;

                                case '6':
                                    // decrypt
                                    DecryptData(cardReader, GetStringFromConsole());
                                    break;

                                case '7':
                                    // end

                                    return;

                                default:
                                    Console.WriteLine("Falsche Auswahl");
                                    break;
                            }

                        }
                        catch (Exception ex)
                        {

                            Console.WriteLine("Fehler: " + ex.Message); ;
                        }

                        Console.WriteLine("\n");
                        Console.WriteLine("Zum fortfahren [ENTER] drücken");
                        Console.ReadLine();

                        Console.Clear();


                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler: " + ex.Message);
                }


            }

        }


        private string GetStringFromConsole()
        {

            Console.Clear();

            Console.WriteLine("Bitte Daten eingeben:");
            Console.WriteLine();

            var data = Console.ReadLine();

            //check len of data
            return data;
        }

        private bool ExternalAuth(CardReader reader)
        {
            // GetRandomFromCard
            var response = reader.SendAPDU(GetGenerateRandomNumberAPDU());
            if (!(reader.CheckResponse(response)))
                throw new Exception("Fehler: Statuswörter ungültig");

            if (!(response.HasData))
                throw new Exception("Fehler: Keine Daten empfangen");

            var rndNumberFromCard = response.GetData();

            // Encrypt
            var encrypted = Encrypt_ByteArrayWithAES(rndNumberFromCard, externalAuthKey, initialVector);
            // send to Card
            response = reader.SendAPDU(GetExternalAuthAPDU(encrypted));
            // check SW1 and SW2
            if (!(reader.CheckResponse(response)))
            {
                isHostAuthenticatedByCard = false;
                //throw new Exception("Fehler: Statuswörter ungültig");
                return false;
            }
            else
            {
                isHostAuthenticatedByCard = true;
                return true;
            }
        }

        private bool InternalAuth(CardReader reader)
        {
            // generate PseudoRandom Number
            Random rnd = new Random();
            int rndNumber = rnd.Next(1, int.MaxValue);
            Console.WriteLine("Zu sendene Nummer:  " + rndNumber); // for padding reasons in Terminal 2 whitespaces

            // Send RND Number to Card
            var response = reader.SendAPDU(GetInternalAuthAPDU(rndNumber));
            if (!(reader.CheckResponse(response)))
                throw new Exception("Fehler: Statuswörter ungültig");

            if (!(response.HasData))
                throw new Exception("Fehler: Keine Daten empfangen");


            // Verify Response
            var cardEncrypted = response.GetData();

            var cardDecrypted = Decrypt_ByteArrayWithAES(cardEncrypted, internalAuthKey, initialVector);

            int cardDecryptInt = BitConverter.ToInt32(cardDecrypted);

            Console.WriteLine("Empfangende Nummer: " + cardDecryptInt);

            if (cardDecryptInt == rndNumber)
            {
                isCardAuthenticated = true;
                return true; // Auth successfull
            }
            else
            {
                isCardAuthenticated = false;
                return false; // Auth failed
            }

        }


        private bool CreateAESKey(CardReader reader)
        {

            // send command APDU
            var response = reader.SendAPDU(GetCreateAESKeyAPDU());
            if (reader.CheckResponse(response))
            {
                // Succesfully created Key on Card
                return true;
            }
            else
            {
                // error while creatin key on card
                return false;
            }

        }

        private bool DeleteAESKey(CardReader reader)
        {

            // send command APDU
            var response = reader.SendAPDU(GetDeleteAESKeyAPDU());
            if (reader.CheckResponse(response))
            {
                // Succesfully created Key on Card
                return true;
            }
            else
            {
                // error while creatin key on card
                return false;
            }

        }

        private bool EncryptData(CardReader reader, string Data)
        {

            byte[] dataToTransmit = System.Text.Encoding.UTF8.GetBytes(Data);

            var response = reader.SendAPDU(GetEncryptdataAPDU(dataToTransmit));

            if (!(reader.CheckResponse(response)))
                throw new Exception("Fehler: Statuswörter ungültig, wahrscheinlich wurde kein AES-Schlüssel auf der Karte erzeugt");


            if (!(response.HasData))
                throw new Exception("Fehler: Keine Daten empfangen");

            var data = response.GetData();

            Console.WriteLine();
            Console.WriteLine("Daten bitte zum Entschlüsseln notieren");
            Console.WriteLine();
            Console.WriteLine("---------- Verschlüsselte Daten ----------");

            Console.WriteLine(Convert.ToHexString(data));

            Console.WriteLine("---------- Verschlüsselte Daten ----------");
            return true;


        }


        private bool DecryptData(CardReader reader, string Data)
        {
            var lol = Convert.FromHexString(Data);
            var response = reader.SendAPDU(GetDecryptdataAPDU(lol));

            if (!(reader.CheckResponse(response)))
                throw new Exception("Fehler: Statuswörter ungültig, wahrscheinlich wurde kein AES-Schlüssel auf der Karte erzeugt");

            if (!(response.HasData))
                throw new Exception("Fehler: Keine Daten empfangen");

            var data = response.GetData();

            Console.WriteLine("---------- Entschlüsselte Daten ----------");

            Console.WriteLine(System.Text.Encoding.UTF8.GetString(data));

            Console.WriteLine("---------- Entschlüsselte Daten ----------");

            return true;

        }

        private byte[] Encrypt_ByteArrayWithAES(byte[] BytesToEncrypt, byte[] Key, byte[] IV)
        {
            byte[] encrypted;

            using (Aes Aes = Aes.Create())
            {
                Aes.Key = Key;
                Aes.IV = IV;
                Aes.Mode = CipherMode.CBC;
                Aes.Padding = PaddingMode.Zeros;


                ICryptoTransform encryptor = Aes.CreateEncryptor(Aes.Key, Aes.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(BytesToEncrypt);
                        csEncrypt.FlushFinalBlock();

                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            return encrypted;
        }


        private byte[] Decrypt_ByteArrayWithAES(byte[] BytesToDecrypt, byte[] Key, byte[] IV)
        {
            List<byte> decrypted = new();

            using (Aes Aes = Aes.Create())
            {
                Aes.Key = Key;
                Aes.IV = IV;
                Aes.Mode = CipherMode.CBC;
                Aes.Padding = PaddingMode.Zeros;

                ICryptoTransform decryptor = Aes.CreateDecryptor(Aes.Key, Aes.IV);

                using (MemoryStream msDecrypt = new MemoryStream(BytesToDecrypt))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        // create 1 Byte buffer to read into List
                        var buffer = new byte[1];

                        var read = csDecrypt.Read(buffer, 0, buffer.Length);
                        while (read > 0)
                        {
                            decrypted.Add(buffer[0]);
                            read = csDecrypt.Read(buffer, 0, buffer.Length);
                        }
                        csDecrypt.Flush();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return decrypted.ToArray();
        }

        #region "APDUs"


        private CommandApdu GetGenerateRandomNumberAPDU()
        {
            return new CommandApdu(IsoCase.Case2Short, SCardProtocol.Unset)
            {
                CLA = 0x00, // 0xB1
                INS = 0x01,
                P1 = 0x00,
                P2 = 0x00,
                Le = 0x04, // TODO: CHANGE!
            };
        }

        private CommandApdu GetInternalAuthAPDU(int RandomNumber)
        {

            var data = BitConverter.GetBytes(RandomNumber);

            //test pad
            byte[] testPad = new byte[16];

            Buffer.BlockCopy(data, 0, testPad, 0, data.Length);
            Buffer.BlockCopy(new byte[] { 0x80 }, 0, testPad, data.Length, 1);

            return new CommandApdu(IsoCase.Case4Short, SCardProtocol.Unset)
            {
                CLA = 0x00, // 0xB1
                INS = 0x02,
                P1 = 0x00,
                P2 = 0x00,
                Le = 0x10, // TODO: Change
                Data = testPad,
            };
        }

        private CommandApdu GetExternalAuthAPDU(byte[] data)
        {
            return new CommandApdu(IsoCase.Case3Short, SCardProtocol.Unset)
            {
                CLA = 0x00, // 0xB1
                INS = 0x03,
                P1 = 0x00,
                P2 = 0x00,
                Data = data,
            };
        }

        private CommandApdu GetCreateAESKeyAPDU()
        {
            return new CommandApdu(IsoCase.Case1, SCardProtocol.Unset)
            {
                CLA = 0x00, // 0xB1
                INS = 0x04,
                P1 = 0x01, // create
                P2 = 0x00,
            };
        }

        private CommandApdu GetDeleteAESKeyAPDU()
        {
            return new CommandApdu(IsoCase.Case1, SCardProtocol.Unset)
            {
                CLA = 0x00, // 0xB1
                INS = 0x04,
                P1 = 0x02, // delete
                P2 = 0x00,
            };
        }

        private CommandApdu GetEncryptdataAPDU(byte[] data)
        {
            //calc blocklen
            int len = Convert.ToInt32(Math.Ceiling(data.Length / 16m)) * 16;

            // add zero padding
            byte[] temp = new byte[len];
            Buffer.BlockCopy(data, 0, temp, 0, data.Length);


            return new CommandApdu(IsoCase.Case4Extended, SCardProtocol.Unset)
            {
                CLA = 0x00, // 0xB1
                INS = 0x06,
                P1 = 0x01, // encrypt
                P2 = 0x00,
                Le = len,
                Data = temp
            };

        }

        private CommandApdu GetDecryptdataAPDU(byte[] data)
        {

            return new CommandApdu(IsoCase.Case4Extended, SCardProtocol.Unset)
            {
                CLA = 0x00, // 0xB1
                INS = 0x06,
                P1 = 0x02, // decrypt
                P2 = 0x00,
                Le = data.Length,
                Data = data
            };

        }

        #endregion "APDUs"


    }
}
