using System;
using System.Linq;
using CommandLine;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace VPX_VBS_Extractor
{
    class Program
    {
        static void Main(string[] args)
        {
            ParserResult<Options> options = Parser.Default.ParseArguments<Options>(args);

            if (options.Errors.Any()) return;

            ManualResetEvent resetEvent = new ManualResetEvent(false);

            string[] tablePaths = Directory.GetFiles(options.Value.PathToTables, "*.vpx");

            foreach (string tablePath in tablePaths)
            {
                string tableFile = (new FileInfo(tablePath)).Name;
                string vbsFile = tableFile.Replace(".vpx", ".vbs");
                string povFile = tableFile.Replace(".vpx", ".pov");
                string vbsPath = Path.Combine(options.Value.PathToTables, vbsFile);
                string povPath = Path.Combine(options.Value.PathToTables, povFile);
                string commandString;

                if (!options.Value.Overwrite && File.Exists(vbsPath))
                {
                    Console.WriteLine($"SKIP: Script already exists for {tableFile}");
                }
                else
                {
                    commandString = $"-minimized -extractvbs \"{tablePath}\"";

                    StartProcess(options, resetEvent, vbsFile, commandString);
                }

                if (!options.Value.Overwrite && File.Exists(povPath))
                {
                    Console.WriteLine($"SKIP: POV already exists for {tableFile}");
                }
                else
                {
                    commandString = $"-minimized -pov \"{tablePath}\"";

                    StartProcess(options, resetEvent, povFile, commandString);
                }

                if (options.Value.TestMode) return;
            }
        }

        private static void StartProcess(ParserResult<Options> options, ManualResetEvent resetEvent, string fileName, string commandString)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(options.Value.PathToVPinballXExecutable, commandString);
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = true;

            Process process = new Process();
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => { resetEvent.Set(); };

            process.Start();

            if (resetEvent.WaitOne(options.Value.TimeoutSeconds * 1000))
            {
                Console.WriteLine($"CREATE: {fileName}");
            }
            else
            {
                Console.WriteLine($"TIMEOUT: {fileName} - Unsure if script was extracted correctly.");
            }

            resetEvent.Reset();
        }
    }

    public class Options
    {
        [Option('o', "overwrite", Default = false, HelpText = "Will overwrite existing .vbs files if true, will skip the table file if false.")]
        public bool Overwrite { get; set; }

        [Option('w', "timeout", Default = 60, HelpText = "Number of seconds to wait for VPX to exit before continuing to the next table.")]
        public int TimeoutSeconds { get; set; }

        [Option('t', "pathToTables", HelpText = "The path to the vpx tables, e.g. C:\\VisualPinball\\Tables", Required = true)]
        public string PathToTables { get; set; }

        [Option('p', "pathToVPinballX", HelpText = "The path to VPinballX.exe, e.g. C:\\VisualPinball\\VPinballX.exe", Required = true)]
        public string PathToVPinballXExecutable { get; set; }

        [Option('s', "testMode", Default = false, HelpText = "If true, stops after extracting the first script. Useful to tune timeout length.")]
        public bool TestMode { get; set; }
    }
}
