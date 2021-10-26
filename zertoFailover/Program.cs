using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using CsvHelper;
using IniParser;
using IniParser.Model;
using zertoFailover;

namespace ZertoFailover
{
    class Program
    {
        public class Options
        {
            [Option('c', "csv", Required = true, HelpText = "point to the csv file containing vm/vpg build details")]
            public string Csv { get; set; }

            [Option('m', "mode", Required = true, HelpText = "specify if the script should start or stop the failover (start | stop)")]
            public string Mode { get; set; }

            [Option('f', "failoverType", Default = "test", Required = false, HelpText = "specify if the script should perform a test or live failover (test | live)")]
            public string FailoverType { get; set; }

            [Option('p', "commitPolicy", Default = "rollback", Required = false, HelpText = "the policy to use after the failover enters a 'Before Commit' state (rollback | commit | none)")]
            public string CommitPolicy { get; set; }

            [Option('t', "waitTime", Default = 3600, Required = false, HelpText = "the amount of time in seconds the failover waits in a 'Before Commit' state before processing the commitPolicy")]
            public int WaitTime { get; set; }
        }

        public class Common
        {
            public static string vc;
            public static string vc_username;
            public static string vc_password;
            public static string zvm;
            public static string zvm_username;
            public static string zvm_password;
        }

        public class CsvList
        {
            public int BuildGroup { get; set; }
            public string VpgName { get; set; }
            public int Delay { get; set; }
        }

        public class Output
        {
            private readonly string LogDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

            private static Output _outputSingleton;
            private static Output OutputSingleton
            {
                get
                {
                    if (_outputSingleton == null)
                    {
                        _outputSingleton = new Output();
                    }
                    return _outputSingleton;
                }
            }

            public StreamWriter SW { get; set; }

            public Output()
            {
                EnsureLogDirectoryExists();
                InstantiateStreamWriter();
            }

            ~Output()
            {
                if (SW != null)
                {
                    try
                    {
                        SW.Dispose();
                    }
                    catch (ObjectDisposedException) { } // object already disposed - ignore exception
                }
            }

            public static void WriteLine(string str)
            {
                Console.WriteLine(str);
                OutputSingleton.SW.WriteLine(str);
            }

            public static void Write(string str)
            {
                Console.Write(str);
                OutputSingleton.SW.Write(str);
            }

            private void InstantiateStreamWriter()
            {
                string filePath = Path.Combine(LogDirPath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")) + ".txt";
                try
                {
                    StreamWriter streamWriter = new StreamWriter(filePath);
                    SW = streamWriter;
                    SW.AutoFlush = true;
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new ApplicationException(string.Format("Access denied. Could not instantiate StreamWriter using path: {0}.", filePath), ex);
                }
            }

            private void EnsureLogDirectoryExists()
            {
                if (!Directory.Exists(LogDirPath))
                {
                    try
                    {
                        Directory.CreateDirectory(LogDirPath);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        throw new ApplicationException(string.Format("Access denied. Could not create log directory using path: {0}.", LogDirPath), ex);
                    }
                }
            }
        }



        private static void Initialise()
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile("zertoFailover.ini");
            //Common.vc = data["vCenter"]["uri"];
            //Common.vc_username = data["vCenter"]["username"];
            //Common.vc_password = data["vCenter"]["password"];
            Common.zvm = data["ZVM"]["uri"];
            Common.zvm_username = data["ZVM"]["username"];
            Common.zvm_password = data["ZVM"]["password"];
        }

        private static async Task<bool> Delay(int secs)
        {
            var count = 0;
            while (count < secs)
            {
                Output.Write(".");
                count++;
                await Task.Delay(1000);
            }

            return true;
        }

        private static async Task<bool> CheckTaskStatus(string taskId)
        {
            if (taskId.Contains("Message"))
            {
                return false;
            }
            var taskStatus = false;
            while (!taskStatus)
            {
                taskStatus = ZertoZvm.TaskComplete(taskId);
                Output.Write(".");
                await Task.Delay(5000);
            }
            return true;
        }

        private static int GetCommitPolicy(string commitPolicy)
        {
            switch (commitPolicy.ToUpper())
            {
                case "ROLLBACK":
                    return 0;
                case "COMMIT":
                    return 1;
                default:
                    return 2;
            }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                if (o.Mode == null || (o.Mode.ToUpper() != "START" && o.Mode.ToUpper() != "STOP")) { o.Mode = "START";  }
                if (o.FailoverType == null || (o.FailoverType.ToUpper() != "TEST" && o.FailoverType.ToUpper() != "LIVE")) { o.FailoverType = "LIVE";  }

                Initialise();
                Stopwatch watch = new Stopwatch();
                watch.Start();
                Console.Clear();
                Output.WriteLine("Zerto Failover Application v2.0");
                //Output.WriteLine("vCenter:       " + Common.vc);
                Output.WriteLine("ZVM:           " + Common.zvm);
                Output.WriteLine("Mode:          " + o.Mode.ToUpper());
                Output.WriteLine("Type:          " + o.FailoverType.ToUpper());
                if (o.FailoverType.ToUpper() =="LIVE" )
                {
                    Output.WriteLine("Commit Policy: " + o.CommitPolicy.ToUpper());
                    Output.WriteLine("Wait Time (s): " + o.WaitTime.ToString());
                }
                Output.WriteLine("CSV File:      " + o.Csv.ToUpper());
                Output.WriteLine("------------------------------------------------------------------------------");
                try
                {
                    var reader = new StreamReader(o.Csv);
                    CsvReader csv = new CsvReader(reader);
                    csv.Configuration.HasHeaderRecord = true;
                    csv.Configuration.MissingFieldFound = null;
                    var csvList = new CsvList();
                    var csvRecords = csv.EnumerateRecords(csvList);

                    if (ZertoZvm.GetSession(Common.zvm, Common.zvm_username, Common.zvm_password, "application/json"))
                    {
                        foreach (var row in csvRecords)
                        {
                            if (o.Mode.ToUpper() == "START" && o.FailoverType.ToUpper() == "TEST")
                            // START TEST FAILOVER
                            {
                                //START FAILOVER PROCESS
                                Output.WriteLine("");
                                Output.Write("Starting Failover Test for " + row.VpgName);
                                try
                                {
                                    Task<bool> failoverTest = CheckTaskStatus(ZertoZvm.FailoverTest(row.VpgName, "").ToString());
                                    failoverTest.Wait();
                                    Output.WriteLine("");
                                    if (failoverTest.Result == true)
                                    {
                                        Output.WriteLine("Built Failover Test for " + row.VpgName);
                                    }
                                    else
                                    {
                                        Output.WriteLine("Unable to Build Failover Test for " + row.VpgName);
                                    }
                                    failoverTest.Dispose();
                                    if (row.Delay > 0)
                                    {
                                        Output.WriteLine("Pausing for " + row.Delay + " seconds");
                                        Task<bool> delay = Delay(row.Delay);
                                        delay.Wait();
                                        delay.Dispose();
                                        Output.WriteLine("");
                                    }
                                }
                                catch (Exception e)
                                {
                                    Output.WriteLine("");
                                    Output.WriteLine("EXCEPTION " + e);
                                }
                            }

                            if (o.Mode.ToUpper() == "START" && o.FailoverType.ToUpper() == "LIVE")
                            // START LIVE FAILOVER
                            {
                                //START FAILOVER PROCESS
                                Output.WriteLine("");
                                Output.Write("Starting Failover for " + row.VpgName);
                                try
                                {
                                    Task<bool> failover = CheckTaskStatus(ZertoZvm.Failover(row.VpgName, "", GetCommitPolicy(o.CommitPolicy), o.WaitTime).ToString());
                                    failover.Wait();
                                    Output.WriteLine("");
                                    if (failover.Result == true)
                                    {
                                        Output.WriteLine("Built Failover for " + row.VpgName);
                                    }
                                    else
                                    {
                                        Output.WriteLine("Unable to Build Failover for " + row.VpgName);
                                    }
                                    failover.Dispose();
                                    if (row.Delay > 0)
                                    {
                                        Output.WriteLine("Pausing for " + row.Delay + " seconds");
                                        Task<bool> delay = Delay(row.Delay);
                                        delay.Wait();
                                        delay.Dispose();
                                        Output.WriteLine("");
                                    }
                                }
                                catch (Exception e)
                                {
                                    Output.WriteLine("");
                                    Output.WriteLine("EXCEPTION " + e);
                                }

                            }

                            if (o.Mode.ToUpper() == "STOP" && o.FailoverType.ToUpper() == "TEST")
                            // STOP TEST FAILOVER
                            {
                                //STOP FAILOVER PROCESS
                                Output.WriteLine("");
                                Output.Write("Stopping Failover Test for " + row.VpgName);
                                try
                                {
                                    Task<bool> stopFailover = CheckTaskStatus(ZertoZvm.FailoverTestStop(row.VpgName).ToString());
                                    stopFailover.Wait();
                                    Output.WriteLine("");
                                    if (stopFailover.Result == true)
                                    {
                                        Output.WriteLine("Destroyed Failover Test for " + row.VpgName);
                                    }
                                    else
                                    {
                                        Output.WriteLine("Unable to Destroy Failover Test for " + row.VpgName);
                                    }
                                    stopFailover.Dispose();
                                }
                                catch (Exception e)
                                {
                                    Output.WriteLine("");
                                    Output.WriteLine("EXCEPTION " + e.Message);
                                }
                            }

                            if (o.Mode.ToUpper() == "STOP" && o.FailoverType.ToUpper() == "LIVE")
                            // NOT AN AVAILABLE OPTION
                            {
                                Output.WriteLine("");
                                Output.WriteLine("Stopping a LIVE failover process is not a valid option...");
                            }
                        }
                    }
                    else
                    {
                        Output.WriteLine("ERROR");
                        Output.WriteLine("Unable to authenticate with ZVM API");
                    }
                }
                catch (Exception e)
                {
                    Output.WriteLine("EXCEPTION");
                    Output.WriteLine(e.Message);
                    Output.WriteLine(e.Data.ToString());
                }
                watch.Stop();
                Output.WriteLine("");
                Output.WriteLine("Duration: " + TimeSpan.FromSeconds(watch.Elapsed.TotalSeconds).ToString(@"hh\:mm\:ss"));
                Console.WriteLine();
                Console.WriteLine("Press any key to close...");
                Output.WriteLine("------------------------------------------------------------------------------");
                Console.ReadKey();
            });
        }
    }
}
