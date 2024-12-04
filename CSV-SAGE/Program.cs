using Microsoft.Extensions.Configuration;
using Objets100cLib;
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
        try
        {
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
            string compteGColumn = configuration["CompteG"];
            string compteTiersColumn = configuration["CompteTiers"];
            string libelleColumn = configuration["Libelle"];
            string referenceColumn = configuration["Reference"];
            string montantColumn = configuration["Montant"];
 
            string NomCompteColumn = configuration["Nom_Compte"];
            string typeEcriture = configuration["TypeEcriture"];
            int erreurline;
            string erreur = "";
            bool success = true;
            Dictionary<String,String> lstCompteG = new Dictionary<String,String>();
            //List<String> lstJournal = new List<String>();
            Dictionary<String,String> lstFournisseurs = new Dictionary<String, String>();
            Dictionary<String, String> lstClients = new Dictionary<String, String>();
            List<string> Journals = new List<string>();
            List<DateTime> uniqueDates = new List<DateTime>();
            // Read all lines from the CSV file
   
            string[] lines = File.ReadAllLines(fileText);
 
             foreach (string line in lines.Skip(1))
            {
                // Split each line into columns by comma
                string[] columns = line.Split('	');
 
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
                    string[] columns = line.Split('	'); 

                    if (!string.IsNullOrEmpty(columns[int.Parse(journalColumn)]) && !Journals.Contains(columns[int.Parse(journalColumn)]))
                     {
                         Journals.Add(columns[int.Parse(journalColumn)]);
                     }

                     
                    if (!uniqueDates.Contains(DateTime.Parse(FormatDate(columns[int.Parse(dateColumn)])))
                        && DateTime.Parse(FormatDate(columns[int.Parse(dateColumn)])).Year >= 2020) { 
                             uniqueDates.Add(DateTime.Parse(FormatDate(columns[int.Parse(dateColumn)])));
                     }

                 }

           
            Console.WriteLine("Entrain d'ouvrir base sage ....");

            if (OpenBase(ref bCpta, @bCptaSetting, @Usernamesetting, @passwordsetting)){
            
              Console.WriteLine("base de données ouvert");


                        //Console.WriteLine("----------------Création des comptes généraux-------------------");

                        foreach (var (key, value) in lstCompteG)
                        {
                               if (key!="" && !bCpta.FactoryCompteG.ExistNumero(key)) {
                                    IBOCompteG3 compteG;
                                    compteG = (IBOCompteG3)bCpta.FactoryCompteG.Create();
                                    compteG.SetDefault();
                                    compteG.CG_Num = key.Length > 30 ? key.Substring(0, 30) : key;
                                    compteG.CG_Intitule = value.Trim().Length > 30 ? value.Trim().Substring(0, 30) : value.Trim();
                                    compteG.Write(); 
                                   // Console.WriteLine("compte G " + key + " non trouvé");
                               }
                        }
 

                //Console.WriteLine("--------------Création de tiers----------------------------");
              
                foreach (var (key, value) in lstClients)
                {
                    if (!bCpta.FactoryTiers.ExistNumero(key))
                    {
                        IBOTiers3 tiers;

                        tiers = (IBOTiers3)bCpta.FactoryClient.Create();
                        tiers.SetDefault();
                        tiers.CT_Num = key.Length > 30 ? key.Substring(0, 30) : key;
                        tiers.CT_Intitule = value.Length > 30 ? value.Substring(0, 30) : value;
                        tiers.Write();
                        //Console.WriteLine("Client " + key + " " + value + " crée");
                    }
                }
            
                foreach(var (key, value) in lstFournisseurs)
                {

                    if (!bCpta.FactoryTiers.ExistNumero(key))
                    {
                        IBOTiers3 tiers;

                        tiers = (IBOTiers3)bCpta.FactoryFournisseur.Create();
                        tiers.SetDefault();
                        tiers.CT_Num = key.Length > 30 ? key.Substring(0, 30) : key;
                        tiers.CT_Intitule = value.Length > 30 ? value.Substring(0, 30) : value;
                        tiers.Write();
                        Console.WriteLine("Fournisseur " + key + " " + value + " crée");
                    }
                }   

              
                //Console.WriteLine("--------------------Traitement des écritures-------------------");

            

                double credit=0, debit=0;

                foreach (string journalItem in Journals)
                 {
                     foreach (DateTime date in uniqueDates)
                     {
                         IPMEncoder mProcess = bCpta.CreateProcess_Encoder();

                         bool check = false;
                         foreach (string line in lines.Skip(1))
                         {
                            string[] columns = line.Split('	');
                            
                             double montant = 0;
                              IBOTiers3 tiers = null;
                             IBOCompteG3 compteg = null;

                             string piece = "";
                             string intitlule = "", reference = "";
                           

                             if (DateTime.Parse(FormatDate(columns[int.Parse(dateColumn)].Trim())) == date && journalItem.Trim().Equals(columns[int.Parse(journalColumn)].Trim()))
                             {
                                  check = true;
                                  intitlule = columns[int.Parse(libelleColumn)].Trim();
                                  compteg = bCpta.FactoryCompteG.ReadNumero(columns[int.Parse(compteGColumn)].Trim());
                                 if (columns[int.Parse(compteGColumn)].Contains("4411") || columns[int.Parse(compteGColumn)].Contains("3421"))
                                 {
                                       tiers = bCpta.FactoryTiers.ReadNumero(columns[int.Parse(compteTiersColumn)].Trim());
                                 }
                                montant = double.Parse(
                                          columns[int.Parse(montantColumn)].TrimStart('-').Replace('.', ',')
                                          );
                                 mProcess.Journal = bCpta.FactoryJournal.ReadNumero(journalItem.Trim());
                                 mProcess.Date = date;
                                 //mProcess.EC_Piece = piece;
                                 mProcess.EC_Intitule = intitlule;
 
                                 IBOEcriture3 ecriture = (IBOEcriture3)mProcess.FactoryEcritureIn.Create();
                                 if (compteg != null) ecriture.CompteG = compteg;
                                 if (tiers != null)
                                 {
                                     ecriture.Tiers = tiers;
                                     ecriture.EC_Echeance = DateTime.Now;
                                 }
                                 if (montant > 0 && columns[int.Parse(typeEcriture)].Equals("C"))
                                 {
                                     ecriture.EC_Sens = EcritureSensType.EcritureSensTypeCredit;
                                     ecriture.EC_Montant = montant;
                                    if (date.Day == 31 && date.Year == 2021 & date.Month == 08 && journalItem == "OD2")
                                    {
                                        //Console.WriteLine(ecriture.EC_Montant);
                                        credit += montant;
                                    }
                                }
                                 else if (montant > 0 && columns[int.Parse(typeEcriture)].Equals("D"))
                                 {
                                     ecriture.EC_Sens = EcritureSensType.EcritureSensTypeDebit;
                                     ecriture.EC_Montant = montant;
                                    if (date.Day == 31 && date.Year == 2021 & date.Month == 08 && journalItem == "OD2")
                                    {
                                        debit += montant;
                                    }
                                }
                                
                                ecriture.WriteDefault();
                             }


                         }

                         if (check == true)
                         {
                             if (!mProcess.CanProcess)
                             {
                                 success = false;
                                 int day = date.Day;
                                 int month = date.Month;
                                int year = date.Year;
                                 string formattedDay = day.ToString("00");
                                 string formattedMonth = month.ToString("00");
                                 string formattedYear = year.ToString("0000");
                                for (int d = 1; d <= mProcess.Errors.Count; d++)
                                 {
                                     IFailInfo iFail = mProcess.Errors[d];

                                     erreur += iFail.Text;

                                     if (iFail.ErrorCode == 28201)
                                     {
                                        // Console.WriteLine("Les écritures générales ne sont pas équilibrées pour le journal " + journalItem + " " + formattedYear+ "/" + formattedDay + "/" + formattedMonth);
                                        //Console.WriteLine(debit + "/" + credit);
                                    }

                                     else
                                     {
                                         //Console.WriteLine(iFail.Text + " , au journal " + journalItem + ", Date " + " " + formattedDay  + "/" + formattedMonth + "/" + formattedYear );
                                     }

                                 }
                                 bCpta.Close();

                             }

                         }


                     }
                 }

                 if (success == true)
                 {
                     foreach (string journalItem in Journals)
                     {
                         foreach (DateTime date in uniqueDates)
                         {
                             IPMEncoder mProcess = bCpta.CreateProcess_Encoder();

                             bool check = false;
                             int i = 0;
                             foreach (string line in lines.Skip(1))
                             {
                                string[] columns = line.Split('	');
                                erreurline = i;
                                 double montant = 0;
                                  IBOTiers3 tiers = null;
                                 IBOCompteG3 compteg = null;

                                 string piece = "";
                                 string intitlule = "", reference = "";
                                 DateTime rowDate;

                                if (DateTime.Parse(FormatDate(columns[int.Parse(dateColumn)].Trim())) == date && journalItem.Trim().Equals(columns[int.Parse(journalColumn)].Trim()))
                                {
                                    check = true;
                                     intitlule = columns[int.Parse(libelleColumn)].Trim();
                                     reference = columns[int.Parse(referenceColumn)].Trim();
                                     compteg = bCpta.FactoryCompteG.ReadNumero(columns[int.Parse(compteGColumn)].Trim());
                                    if (columns[int.Parse(compteGColumn)].Contains("4411") || columns[int.Parse(compteGColumn)].Contains("3421"))
                                    {
                                        tiers = bCpta.FactoryTiers.ReadNumero(columns[int.Parse(compteTiersColumn)].Trim());
                                     }
                                    montant = double.Parse(
                                                columns[int.Parse(montantColumn)].TrimStart('-').Replace('.', ',')
                                               );
                                     mProcess.Journal = bCpta.FactoryJournal.ReadNumero(journalItem.Trim());
                                     mProcess.Date = date;
                                     //mProcess.EC_Piece = piece;
                                     mProcess.EC_Intitule = intitlule;
                                      IBOEcriture3 ecriture = (IBOEcriture3)mProcess.FactoryEcritureIn.Create();
                                     if (compteg != null) ecriture.CompteG = compteg;
                                     if (tiers != null)
                                     {
                                         ecriture.Tiers = tiers;
                                         ecriture.EC_Echeance = DateTime.Now;
                                     }
                                     if (montant > 0 && columns[int.Parse(typeEcriture)].Equals("C"))
                                     {
                                         ecriture.EC_Sens = EcritureSensType.EcritureSensTypeCredit;
                                         ecriture.EC_Montant =  montant;
                                     }
                                     else if (montant > 0 && columns[int.Parse(typeEcriture)].Equals("D"))
                                     {
                                         ecriture.EC_Sens = EcritureSensType.EcritureSensTypeDebit;
                                         ecriture.EC_Montant = montant;
                                     }
                                    
                                    ecriture.WriteDefault();
                                 }
                                 i++;
                                 if (mProcess.CanProcess)
                                 {
                                     mProcess.Process();
                                 }
                                 else
                                 {
                                     for (int d = 1; d <= mProcess.Errors.Count; d++)
                                     {
                                         IFailInfo iFail = mProcess.Errors[d];
                                        // Console.WriteLine(iFail.Text);
                                     }
                                 }

                             }
                         }
                     }

                 }
              
                

            }
           
            Console.WriteLine("\nTraitement terminée");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Console.ReadLine(); // Keeps the console open
            
        }
        finally
        {
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

}




