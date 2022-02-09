using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCard_Host_App
{
    public class SetUpApp
    {

        public SetUpApp()
        {

            while (true)
            {

                Console.Clear();

                Console.WriteLine("Bitte aus folgenden Einstellungen auswählen:");
                Console.WriteLine();

                Console.WriteLine("1.) Kartenname setzen [Aktuell: \"" + MainApp.CardName + "\"]");
                Console.WriteLine("2.) CSV Basis-Pfad ändern [Aktuell: \"" + MainApp.PathToCSV + "\"]");
                Console.WriteLine("3.) Rundenanzahl pro Test ändern [Aktuell: \"" + MainApp.rounds + "\"]");
                Console.WriteLine("4.) Zurück");

                Console.WriteLine();
                Console.Write("Ihre Auswahl: ");

                var key = Console.ReadKey();

                Console.WriteLine("\n");

                switch (key.KeyChar)
                {
                    case '1':
                        // Kartenname setzen
                        Console.Write("Neuer Kartenname [Aktuell: \"" + MainApp.CardName + "\"]: ");
                        var newCardName = Console.ReadLine();

                        if (newCardName != "")
                            MainApp.CardName = newCardName;
                        else
                        {
                            Console.WriteLine("Kartenname darf nicht leer sein");
                            Console.WriteLine("");
                            Console.WriteLine("Zum fortfahren [ENTER] drücken");
                            Console.ReadLine();
                        }
                        break;

                    case '2':
                        //edit CSV Path
                        Console.Write("Neuer CSV Basis-Pfad [Aktuell: \"" + MainApp.PathToCSV + "\"]: ");
                        var newCSVPath = Console.ReadLine();

                        if (newCSVPath != "")
                            MainApp.PathToCSV = newCSVPath;
                        else
                        {
                            Console.WriteLine("Pfad darf nicht leer sein");
                            Console.WriteLine("");
                            Console.WriteLine("Zum fortfahren [ENTER] drücken");
                            Console.ReadLine();
                        }

                        break;


                    case '3':
                        // edit Round per test
                        Console.Write("Neue Rundenanzahl [Aktuell: \"" + MainApp.rounds + "\"]: ");
                        var newRoundCount = Console.ReadLine();

                        try
                        {
                            int newRoundInt = Convert.ToInt32(newRoundCount);
                            if (newRoundInt > 0)
                                MainApp.rounds = newRoundInt;
                            else
                            {
                                Console.WriteLine("Anzahl darf nicht kleiner als 1 sein");
                                Console.WriteLine("");
                                Console.WriteLine("Zum fortfahren [ENTER] drücken");
                                Console.ReadLine();
                            }
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Fehler beim Konvertieren der Zahl");
                            Console.WriteLine("");
                            Console.WriteLine("Zum fortfahren [ENTER] drücken");
                            Console.ReadLine();
                        }

                        break;

                    case '4':
                        // beenden
                        return;

                    default:
                        Console.WriteLine("Falsche Auswahl");
                        break;
                }

            }

        }

    }
}
