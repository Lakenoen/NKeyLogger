// See https://aka.ms/new-console-template for more information
using ReportMaker;

ReportGenerator generator = new ReportGenerator("D:\\code\\projects\\NKeyLogger\\NKeyLogger\\NKeyLoggerServer\\bin\\Debug\\net9.0\\storage\\127.0.0.1.csv");
generator.generateRawReport("./RawReport.txt");
