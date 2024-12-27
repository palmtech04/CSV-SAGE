using Microsoft.Extensions.Configuration;
using Objets100cLib;
using System;
using System.Diagnostics;
using System.Data.SqlClient; // Use Microsoft.Data.SqlClient if preferred.
using System.Globalization;
using System.Reflection.Metadata;
 class Program
{
    // Declaration of global objects
    private static BSCPTAApplication100c bCpta = new();
    private static BSCIALApplication100c bCial = new();
     private static int rowCount;
    static async Task Main(string[] args)
    {
        int currentline = 0;
        // Get the directory where the executable is running
        string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Set the log file path to the same directory as the executable
        string logFilePath = Path.Combine(exeDirectory, "logfile.txt");

        try
        {
            Console.WriteLine("Execution a commencé");
            WriteToLog(logFilePath, "--------------------------------------------------");
            WriteToLog(logFilePath, "L'execution a commencé");

            Stopwatch stopwatch = new Stopwatch();

            IConfiguration configuration = new ConfigurationBuilder()
           .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "settings.json"), optional: true, reloadOnChange: true)
           .Build();

            string fileText = configuration["fichier"];
            string bCptaSetting = configuration["bCpta"];
            string Usernamesetting = configuration["username"];
            string passwordsetting = configuration["password"];
            string journalColumn = configuration["Journal"];
            string numCompteColumn = configuration["numcompte"];
            string dateColumn = configuration["Date"];
            string dateSaisieColumn = configuration["DateSaisie"];
            string compteGColumn = configuration["CompteG"];
            string referenceColumn  = configuration["Reference"];
             string compteTiersColumn = configuration["CompteTiers"];
            string libelleColumn = configuration["Libelle"];
             string montantColumn = configuration["Montant"];
            string NomCompteColumn = configuration["Nom_Compte"];
            string typeEcriture = configuration["TypeEcriture"];
            string jrnalLineColumn = configuration["jrnal_Line"];
            string nFactureColumn = configuration["Nfacture"];
            string scriptLink = configuration["scriptLink"];
            string Dbuser = configuration["Dbuser"];
            string userPassword = configuration["userPassword"];
            string db = configuration["Db"];
            string server = configuration["server"];
            string erreur = "";
            bool success = true;
            string nPieceColumn = configuration["NPiece"];

            string connectionString = "Server="+server
                +";Database="+db+";User Id="+ Dbuser + ";Password="+ userPassword+"; ";
            string sqlScript = File.ReadAllText(scriptLink); // Read the SQL script from the file.
           
            ExecuteSqlScript(connectionString, sqlScript);
          
            Console.WriteLine("Le script SQL a été exécuté avec succès.");

            Dictionary<String,String> lstCompteG = new Dictionary<String,String>();
            //List<String> lstJournal = new List<String>();
            Dictionary<String,String> lstFournisseurs = new Dictionary<String, String>();
            Dictionary<String, String> lstClients = new Dictionary<String, String>();
            List<string> Journals = new List<string>();
            // Read all lines from the CSV file
   
            string[] lines = File.ReadAllLines(fileText);
    
             foreach (string line in lines.Skip(1))
             {
                 // Split each line into columns by comma
                string[] columns = line.Split('|');
 
                if (!lstCompteG.ContainsKey(columns[int.Parse(compteGColumn)]))
                {
                    lstCompteG.Add(columns[int.Parse(compteGColumn)], columns[int.Parse(NomCompteColumn)]);
                }

                if (columns[int.Parse(compteGColumn)].Contains("3421")){

                    if (!lstClients.ContainsKey(columns[int.Parse(numCompteColumn)]))
                    {
                        lstClients.Add(columns[int.Parse(numCompteColumn)], columns[int.Parse(NomCompteColumn)]);
                    }
                }

                if(columns[int.Parse(compteGColumn)].Contains("4411"))
                {
                    if (!lstFournisseurs.ContainsKey(columns[int.Parse(numCompteColumn)]))
                    {
                        lstFournisseurs.Add(columns[int.Parse(numCompteColumn)], columns[int.Parse(NomCompteColumn)]);
                    }
                }   
             
             }
                  foreach (string line in lines.Skip(1))
                 {
                    // Split each line into columns by comma
                    string[] columns = line.Split('|'); 

                    if (!string.IsNullOrEmpty(columns[int.Parse(journalColumn)]) && !Journals.Contains(columns[int.Parse(journalColumn)]))
                     {
                         Journals.Add(columns[int.Parse(journalColumn)]);
                     }
 

                 }

            stopwatch.Start();

            Console.WriteLine("Entrain d'ouvrir base sage ....");

            if (OpenBase(ref bCpta, @bCptaSetting, @Usernamesetting, @passwordsetting)){

                Console.WriteLine("base de données ouvert");

                Console.WriteLine("traitement a commencé");

                //Console.WriteLine("----------------Création des comptes généraux-------------------");

                foreach (var (key, value) in lstCompteG)
                        {
                               if (key!="" && !bCpta.FactoryCompteG.ExistNumero(key)) {
                                    IBOCompteG3 compteG;
                                    compteG = (IBOCompteG3)bCpta.FactoryCompteG.Create();
                                    compteG.CG_Num = key.Length > 30 ? key.Substring(0, 30) : key;
                                    compteG.CG_Intitule = value.Trim().Length > 30 ? value.Trim().Substring(0, 30) : value.Trim();
                                    compteG.SetDefault();
                                    compteG.Write(); 
                                   // Console.WriteLine("compte G " + key + " non trouvé");
                               }
                        }

                WriteToLog(logFilePath,"les comptesG a jour");

                //Console.WriteLine("--------------Création de tiers----------------------------");

                foreach (var (key, value) in lstClients)
                {
                    if (!bCpta.FactoryTiers.ExistNumero(key))
                    {
                        IBOTiers3 tiers;

                        tiers = (IBOTiers3)bCpta.FactoryClient.Create();
                        tiers.CT_Num = key.Length > 30 ? key.Substring(0, 30) : key;
                        tiers.CT_Intitule = value.Length > 30 ? value.Substring(0, 30) : value;
                        tiers.SetDefault();
                        tiers.Write();
                        //Console.WriteLine("Client " + key + " " + value + " crée");
                    }
                }
                WriteToLog(logFilePath,"les clients a jour");
                foreach(var (key, value) in lstFournisseurs)
                {

                    if (!bCpta.FactoryTiers.ExistNumero(key))
                    {
                        IBOTiers3 tiers;

                        tiers = (IBOTiers3)bCpta.FactoryFournisseur.Create();
                        tiers.CT_Num = key.Length > 30 ? key.Substring(0, 30) : key;
                        tiers.CT_Intitule = value.Length > 30 ? value.Substring(0, 30) : value;
                        tiers.SetDefault();
                        tiers.Write();
                       //Console.WriteLine("Fournisseur " + key + " " + value + " crée");
                    }
                }
                WriteToLog(logFilePath, "les fournisseurs a jour");




                //Console.WriteLine("--------------------Traitement des écritures-------------------");


                /*
 

                foreach (string journalItem in Journals)
                 {
                    Dictionary<int, HashSet<int>> uniqueDates = new Dictionary<int, HashSet<int>>();
                    WriteToLog(logFilePath, "Vérification du Journal " + journalItem + " ....");

                    foreach (string line in lines.Skip(1))
                    {
                        // Split each line into columns by '|'
                        string[] columns = line.Split('|');
                         if (journalItem.Trim() == columns[int.Parse(journalColumn)].Trim())
                        {


                             // Extract the year and month
                            // Extract the month (first 2 digits)
                            int month = int.Parse(columns[int.Parse(dateColumn)].Trim().Substring(0, 3));

                            // Extract the year (remaining digits)
                            int year = int.Parse(columns[int.Parse(dateColumn)].Trim().Substring(3));

                            // Check if the year already exists in the dictionary
                            // Assuming uniqueDates is a Dictionary<int, int>
                            // Assuming uniqueDates is a Dictionary<int, HashSet<int>>
                            if (month == 13) month = 12;

                            if (!uniqueDates.ContainsKey(year) && year >= 2020)
                            {
                                // If the year doesn't exist, add it to the dictionary with an empty HashSet for months
                                uniqueDates[year] = new HashSet<int>();
                            }

                            // Now, check if the month is already in the HashSet for that year
                            if (uniqueDates.ContainsKey(year) && !uniqueDates[year].Contains(month))
                            {
                                // Add the month to the HashSet for the specified year
                                uniqueDates[year].Add(month);
                            }



                        }
                    }
                    //.WriteLine("in list" + journalItem);
                    foreach (var date in uniqueDates)
                    {
                        foreach (var dt in date.Value)
                        {
                          //  Console.WriteLine("Anné " + date.Key +  " Mois "+ dt.ToString());
                        }
                    }

                    foreach (var date in uniqueDates)
                    {
                     foreach (var dt in date.Value) { 

                       IPMEncoder mProcess = bCpta.CreateProcess_Encoder();
                        currentline = 0;
                        bool check = false;
                        foreach (string line in lines.Skip(1))
                        {
                            string[] columns = line.Split('|');
                            currentline++;

                            if (columns[int.Parse(compteGColumn)].Trim() != "999999")
                            {
                                IBOTiers3 tiers = null;
                                IBOCompteG3 compteg = null;

                                string piece = "";
                                string intitlule = "", reference = "", nPiece = "", jrnalLine = "", nFacture = "";
                                    DateTime dateSaisie = DateTime.Parse(FormatDate(columns[int.Parse(dateSaisieColumn)].Trim()));
                                    int lineDatemonth = int.Parse(columns[int.Parse(dateColumn)].Trim().Substring(0, 3));

                                    // Extract the year (remaining digits)
                                    int lineDateyear = int.Parse(columns[int.Parse(dateColumn)].Trim().Substring(3));
                                    // Check if the year already exists in the dictionary
                                    // Assuming uniqueDates is a Dictionary<int, int>
                                    if (lineDatemonth == 13) lineDatemonth = 12;
                                    double montant = double.Parse(
                                              columns[int.Parse(montantColumn)].Trim().TrimStart('-').Replace('.', ',')
                                              );
                                if (journalItem.Trim().Equals(columns[int.Parse(journalColumn)].Trim()) && montant > 0)
                                {
                                    if (lineDatemonth == dt && lineDateyear == date.Key)
                                    {
                                        check = true;
                                        intitlule = columns[int.Parse(libelleColumn)].Trim();
                                        compteg = bCpta.FactoryCompteG.ReadNumero(columns[int.Parse(compteGColumn)].Trim());
                                        reference = columns[int.Parse(referenceColumn)].Trim();
                                        jrnalLine = columns[int.Parse(jrnalLineColumn)].Trim();
                                        nPiece = columns[int.Parse(nPieceColumn)].Trim();
                                        nFacture = columns[int.Parse(nFactureColumn)].Trim();
                                        if (columns[int.Parse(compteGColumn)].Contains("4411") || columns[int.Parse(compteGColumn)].Contains("3421"))
                                        {
                                            tiers = bCpta.FactoryTiers.ReadNumero(columns[int.Parse(compteTiersColumn)].Trim());
                                        }


                                        mProcess.Journal = bCpta.FactoryJournal.ReadNumero(journalItem.Trim());
                                        DateTime parsedDate = new DateTime(lineDateyear, lineDatemonth,1);

                                        mProcess.Date = parsedDate;
                                        mProcess.EC_Piece = nPiece;
                                        mProcess.EC_Reference = reference;
                                        mProcess.EC_Intitule = intitlule;
                                        IBOEcriture3 ecriture = (IBOEcriture3)mProcess.FactoryEcritureIn.Create();
                                        ecriture.DateSaisie = dateSaisie;

                                         
                                        ecriture.EC_RefPiece = nFacture;
                                        if (compteg != null) ecriture.CompteG = compteg;
                                        if (tiers != null)
                                        {
                                            ecriture.Tiers = tiers;
                                            ecriture.EC_Echeance = DateTime.Now;
                                        }
                                        if (columns[int.Parse(typeEcriture)].Trim().Equals("C"))
                                        {
                                            ecriture.EC_Sens = EcritureSensType.EcritureSensTypeCredit;
                                            ecriture.EC_Montant = montant;
                                        }
                                        else if (columns[int.Parse(typeEcriture)].Trim().Equals("D"))
                                        {
                                            ecriture.EC_Sens = EcritureSensType.EcritureSensTypeDebit;
                                            ecriture.EC_Montant = montant;

                                        }
                                        ecriture.WriteDefault();
                                    }
                                }
                            }
                        }

                        if (check == true)
                        {
                            if (!mProcess.CanProcess)
                            {
                                success = false;
                                int month = dt;
                                int year = date.Key;

                                for (int d = 1; d <= mProcess.Errors.Count; d++)
                                {
                                    IFailInfo iFail = mProcess.Errors[d];

                                    erreur += iFail.Text;

                                    if (iFail.ErrorCode == 28201)
                                    {
                                        WriteToLog(logFilePath, "Les écritures générales ne sont pas équilibrées pour le journal " + journalItem + " " + month + "/" + year);
                                        //Console.WriteLine(debit + "/" + credit);
                                        WriteToLog(logFilePath, "debit " + mProcess.Debit + "/ CREDIT :" + mProcess.Credit);
                                    }

                                    else
                                    {
                                        WriteToLog(logFilePath, iFail.Text + " , au journal " + journalItem + ", Date " + " " + "/" + month + "/" + year);
                                        WriteToLog(logFilePath, iFail.ErrorCode);

                                    }

                                }
                                bCpta.Close();

                            }

                        }

                     }
                     }
                    WriteToLog(logFilePath,"Journal " + journalItem + " equilibré");
                            
                }
                
    */

             



                success = true;
                 if (success == true)
                 {
                     foreach (string journalItem in Journals)
                     {
                        Dictionary<int, HashSet<int>> uniqueDates = new Dictionary<int, HashSet<int>>();

                        foreach (string line in lines.Skip(1))
                        {
                            // Split each line into columns by '|'
                            string[] columns = line.Split('|');

                            if (journalItem.Trim() == columns[int.Parse(journalColumn)].Trim())
                            {


                                // Extract the year and month
                                // Extract the month (first 2 digits)
                                int month = int.Parse(columns[int.Parse(dateColumn)].Trim().Substring(0, 3));

                                // Extract the year (remaining digits)
                                int year = int.Parse(columns[int.Parse(dateColumn)].Trim().Substring(3));
                                if (month == 13) month = 12;

                                // Assuming uniqueDates is a Dictionary<int, HashSet<int>>
                                if (!uniqueDates.ContainsKey(year) && year >= 2020)
                                {
                                    // If the year doesn't exist, add it to the dictionary with an empty HashSet for months
                                    uniqueDates[year] = new HashSet<int>();
                                }

                                // Now, check if the month is already in the HashSet for that year
                                if (uniqueDates.ContainsKey(year) && !uniqueDates[year].Contains(month))
                                {
                                    // Add the month to the HashSet for the specified year
                                    uniqueDates[year].Add(month);
                                }



                            }
                        }

                        WriteToLog(logFilePath, "Journal " + journalItem + " commencé l'écriture");
                 
                        foreach (var date in uniqueDates)
                        {
                         foreach (var dt in date.Value)
                            {
                             IPMEncoder mProcess = bCpta.CreateProcess_Encoder();
                            currentline = 0;
                            bool check = false;
                            foreach (string line in lines.Skip(1))
                            {
                                string[] columns = line.Split('|');
                                currentline++;

                                if (columns[int.Parse(compteGColumn)].Trim() != "999999")
                                {
                                    IBOTiers3 tiers = null;
                                    IBOCompteG3 compteg = null;

                                    string piece = "";
                                    string intitlule = "", reference = "", nPiece = "", jrnalLine = "", nFacture = "";
                                    DateTime dateSaisie = DateTime.Parse(FormatDate(columns[int.Parse(dateSaisieColumn)].Trim()));
                                        int lineDatemonth = int.Parse(columns[int.Parse(dateColumn)].Trim().Substring(0, 3));

                                        // Extract the year (remaining digits)
                                        int lineDateyear = int.Parse(columns[int.Parse(dateColumn)].Trim().Substring(3));
                                        // Check if the year already exists in the dictionary
                                        // Assuming uniqueDates is a Dictionary<int, int>
                                        if (lineDatemonth == 13) lineDatemonth = 12;

                                        double montant = double.Parse(
                                                  columns[int.Parse(montantColumn)].Trim().TrimStart('-').Replace('.', ',')
                                                  );
                                    if (journalItem.Trim().Equals(columns[int.Parse(journalColumn)].Trim()) && montant > 0)
                                    {
                                            if (lineDatemonth == dt && lineDateyear == date.Key)
                                            {
                                                check = true;
                                                intitlule = columns[int.Parse(libelleColumn)].Trim();
                                                compteg = bCpta.FactoryCompteG.ReadNumero(columns[int.Parse(compteGColumn)].Trim());
                                                reference = columns[int.Parse(referenceColumn)].Trim();
                                                jrnalLine = columns[int.Parse(jrnalLineColumn)].Trim();
                                                nPiece = columns[int.Parse(nPieceColumn)].Trim();
                                                nFacture = columns[int.Parse(nFactureColumn)].Trim();
                                                if (columns[int.Parse(compteGColumn)].Contains("4411") || columns[int.Parse(compteGColumn)].Contains("3421"))
                                                {
                                                    tiers = bCpta.FactoryTiers.ReadNumero(columns[int.Parse(compteTiersColumn)].Trim());
                                                }


                                                mProcess.Journal = bCpta.FactoryJournal.ReadNumero(journalItem.Trim());
                                                DateTime parsedDate = new DateTime(lineDateyear, lineDatemonth, 1);
                                                mProcess.Date = parsedDate;
                                                mProcess.EC_Piece = nPiece;
                                                mProcess.EC_Reference = reference;
                                                mProcess.EC_Intitule = intitlule;
                                                IBOEcriture3 ecriture = (IBOEcriture3)mProcess.FactoryEcritureIn.Create();
                                                 ecriture.DateSaisie = dateSaisie;
                                                 ecriture.EC_RefPiece = nFacture;
 
                                                if (compteg != null) ecriture.CompteG = compteg;
                                                if (tiers != null)
                                                {
                                                    ecriture.Tiers = tiers;
                                                    ecriture.EC_Echeance = DateTime.Now;
                                                }
                                                if (columns[int.Parse(typeEcriture)].Trim().Equals("C"))
                                                {
                                                    ecriture.EC_Sens = EcritureSensType.EcritureSensTypeCredit;
                                                    ecriture.EC_Montant = montant;
                                                }
                                                else if (columns[int.Parse(typeEcriture)].Trim().Equals("D"))
                                                {
                                                    ecriture.EC_Sens = EcritureSensType.EcritureSensTypeDebit;
                                                    ecriture.EC_Montant = montant;

                                                }
                                                ecriture.Write();
                                             
                                                //ecriture.InfoLibre["JRNAL_NO"] = "test";
                                                //ecriture.Write();
                                                // ecritureModel.WriteDefault();




                                                //Console.WriteLine("ecriture : " + ecriture.EC_No);
                                            }
                                        }
                                }
                            }

                            if (mProcess.CanProcess)
                            {
                                    mProcess.Process();


                                    /*
                                    foreach(IBOEcriture3 ecr in mProcess.ListEcrituresOut) {

                                        foreach (string line in lines.Skip(1))
                                        {
                                            string[] columns = line.Split('|');
                                            if(ecr.EC_Intitule == columns[int.Parse(libelleColumn)].Trim() && ecr..InfoLibre["JRNAL_NO"]=="") { 
                                            ecr.InfoLibre["JRNAL_NO"] = columns[int.Parse(jrnalLineColumn)].Trim();
                                            ecr.Write();
                                            }
                                        }
                                    }*/
                                }
                                else
                            {
                                for (int d = 1; d <= mProcess.Errors.Count; d++)
                                {
                                    IFailInfo iFail = mProcess.Errors[d];
                                    WriteToLog(logFilePath, iFail.Text + iFail.ToString());
                               }
                            }

                        }
                        }

                        WriteToLog(logFilePath, "Journal " + journalItem + " a fini l'insertion des ecritures");

                    }

                }
                           
            
            }

            stopwatch.Stop();
            WriteToLog(logFilePath, "temps pour tout importer" + stopwatch.Elapsed.TotalMinutes);

            WriteToLog(logFilePath, "\nTraitement terminée");
        }
        catch (Exception ex)
        {
            WriteToLog(logFilePath, $"An unexpected error occurred: {ex.Message}");
            
        }
        finally
        {
        }

    }







    static void WriteToLog(string fileName, string message)
    {
        // Check if fileName is empty or null
        if (string.IsNullOrEmpty(fileName))
        {
            Console.WriteLine("Invalid log file name.");
            return;
        }

        // Get the current timestamp
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        try
        {
            // Combine the current directory with the fileName to get the full path
            string filePath = Path.Combine(Environment.CurrentDirectory, fileName);

            // Open the log file in append mode or create if it doesn't exist
            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                // Write the timestamp and message to the log file
                sw.WriteLine($"{timestamp} - {message}{Environment.NewLine}");
                Console.WriteLine($"{timestamp} - {message}{Environment.NewLine}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to log file: {ex}");
        }
    }


    static string FormatDate(string input)
    {
        // Pad the input with leading zeros if necessary
        input = input.PadLeft(8, '0');

        // Extract day, month, and year
        string day = input.Substring(0, 2);
        string month = input.Substring(2, 2);
        string year = input.Substring(4, 4);

        // Return the date in dd/mm/yyyy format
        return $"{day}/{month}/{year}";
    }
    public static bool OpenBase(ref BSCPTAApplication100c BaseCpta, string sMae, string sUid, string sPwd)
    {
        try
        {
            BaseCpta.Name = sMae;
            BaseCpta.Loggable.UserName = sUid;
            BaseCpta.Loggable.UserPwd = sPwd;
            BaseCpta.Open();
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public static bool CloseBase(ref BSCIALApplication100c bCial)
    {
        try
        {
            if (bCial.IsOpen)
                bCial.Close();
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    static void ExecuteSqlScript(string connectionString, string script)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                 string[] commands = script.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string command in commands)
                {
                     using (SqlCommand sqlCommand = new SqlCommand(command, connection))
                    {

                        sqlCommand.CommandTimeout = 0;
                        sqlCommand.ExecuteNonQuery();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + "/" + ex.StackTrace);
            return;
        }
    }

}




