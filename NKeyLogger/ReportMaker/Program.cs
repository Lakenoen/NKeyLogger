using System.Linq;
using ReportMaker;

class Program
{

    private static void PrintHelp()
    {
        Console.WriteLine("make <path to csv file> <target path> - for make report\r\n[help, -h] -  for print clue\r\n");
    }
    public static void Main(string[] args)
    {
        try
        {
            if(args.Length < 2 || args[0].ToLower() == "help" || args[0].ToLower() == "-h")
            {
                PrintHelp();
                return;
            }
            else if (args[0].ToLower() == "make")
            {
                ReportGenerator generator = new ReportGenerator(args[1]);
                string reportPath = args[2];
                if (Path.GetExtension(reportPath) == string.Empty)
                    reportPath = reportPath + ".txt";
                generator.generateReport(reportPath);
                Console.WriteLine("Success");
            }
            else
            {
                Console.WriteLine("Unknown command\r\n");
                PrintHelp();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
