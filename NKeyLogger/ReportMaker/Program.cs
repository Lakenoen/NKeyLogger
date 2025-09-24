using System.Linq;
using ReportMaker;

class Program
{
    public static void Main(string[] args)
    {
        try
        {
            ReportGenerator generator = new ReportGenerator(args[0]);
            string reportPath = args[1];
            if (Path.GetExtension(reportPath) == string.Empty)
                reportPath = reportPath + ".txt";
            generator.generateReport(reportPath);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
