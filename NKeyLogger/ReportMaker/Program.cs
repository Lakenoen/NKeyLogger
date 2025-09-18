// See https://aka.ms/new-console-template for more information
using ReportMaker;

ReportGenerator generator = new ReportGenerator(AppDomain.CurrentDomain.BaseDirectory + "Report.txt");
generator.generate("D:\\code\\projects\\NKeyLogger\\NKeyLogger\\NKeyLoggerServer\\bin\\Debug\\net9.0\\storage\\127.0.0.1.csv");
