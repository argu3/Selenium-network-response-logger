using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.Events;
using System;
using System.Text.Json.Nodes;
using static System.Net.WebRequestMethods;

namespace SeleniumTesting
{
    class LogReader
    {
        private static Mutex logLock = new Mutex(false);
        //https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/implementing-the-event-based-asynchronous-pattern
        static ChromeDriver chromeDriver = InitializeDriver();
        static IReadOnlyCollection<OpenQA.Selenium.LogEntry> logEntries = new List<OpenQA.Selenium.LogEntry>();
        static Logs perfLogs = new Logs(chromeDriver);
        static List<IReadOnlyCollection<OpenQA.Selenium.LogEntry>> entryCollection = new List<IReadOnlyCollection<OpenQA.Selenium.LogEntry>>();
        static List<JToken> eventCollection = new List<JToken>();
        static bool oneTime = true;

        static String importantLogPath = "C:\\Users\\aguthr01\\OneDrive - AHS\\Desktop\\NotesAndTraining\\Scripts\\ag_script\\ReceivingScript\\DatabaseUpdates\\verizonOrdersLog.txt";
        static String otherLogPath = "C:\\Users\\aguthr01\\OneDrive - AHS\\Desktop\\NotesAndTraining\\Scripts\\ag_script\\ReceivingScript\\DatabaseUpdates\\otherLog.txt";
        static void Main(string[] args)
        {
            //chromeDriver.GetDevToolsSession().DevToolsEventReceived += GetNetworkResponse;

            NetworkManager networkManager = new NetworkManager(chromeDriver);
            NetworkResponseHandler networkResponseHandler = new NetworkResponseHandler();
            networkResponseHandler.ResponseMatcher = ResponseReader;
            networkResponseHandler.ResponseTransformer = ResponseTransformer;
            networkManager.AddResponseHandler(networkResponseHandler);
            networkManager.NetworkResponseReceived += ResponseReceivedAsync;
            networkManager.StartMonitoring();

            String url = "https://mb.verizonwireless.com/mbt/secure/index?appName=esm#/esm/dashboard";
            //String url = "https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.sorteddictionary-2?view=net-8.0&redirectedfrom=MSDN";
            chromeDriver.Navigate().GoToUrl(url);
            //https://codoid.com/selenium-testing/how-to-use-selenium-webdriver-event-listener/#:~:text=How%20to%20use%20Selenium%20WebDriver%20Event%20Listener%3F%201,afterFindBy%20...%205%20beforeClickOn%20...%206%20Conclusion%20
            while (true)
            {
                /*
                if (entryCollection.Count > 3 && oneTime)
                {
                    
                    NetEvent += WriteEvent;
                    oneTime = false;
                }
                */
            }
        }

        private static void ResponseReceivedAsync(object? sender, NetworkResponseReceivedEventArgs e)
        {
            //I think doing this directly in this method was causing a timeout of some sort?
            LogNetworkResponse(e);
        }

        static async Task LogNetworkResponse(NetworkResponseReceivedEventArgs e)
        {
            Console.WriteLine("NetworkresponseReceived, waiting");
            logLock.WaitOne();
            //Console.WriteLine("start of write");
            String path = otherLogPath;
            if (e.ResponseUrl != null)
            {
                if (e.ResponseUrl == "https://mb.verizonwireless.com/mbt/secure/pendingordersvc/mbt/orderdetails" || e.ResponseUrl == "https://mb.verizonwireless.com/mbt/secure/pendingordersvc/mbt/orders")
                {
                    path = importantLogPath;
                }
                System.IO.File.AppendAllText(path, e.ResponseUrl);
                System.IO.File.AppendAllText(path, "\n");
            }
            else
            {
                System.IO.File.AppendAllText(path, "no response url");
                System.IO.File.AppendAllText(path, "\n");
            }
            if (e.ResponseContent != null)
            {
                System.IO.File.AppendAllText(path, e.ResponseContent.ReadAsString());
                System.IO.File.AppendAllText(path, "\n");
            }
            else
            {
                System.IO.File.AppendAllText(path, "empty response body");
                System.IO.File.AppendAllText(path, "\n");
            }

            System.IO.File.AppendAllText(path, "GuthrieIterator\n");
            //System.IO.File.AppendAllText(path, DateTime.Now.ToString());
            //Console.WriteLine("end of write");
            logLock.ReleaseMutex();
        }
        static event Func<HttpResponseData, bool> ResponseReader = response =>
        {
            if(response.Url.Contains("atlantichealth") || response.Url.Contains("login.microsoft"))
            {
                Console.WriteLine($"     {response.Url}");
                Console.WriteLine("     not matched");
                return false;
            }
            Console.WriteLine($"     {response.Url}");
            Console.WriteLine("     matched");
            return true;
        };
        //hangs if I don't have it, just a passthrough
        static event Func<HttpResponseData, HttpResponseData> ResponseTransformer = response => response;

        static ChromeDriver InitializeDriver()
        {
            ChromeOptions options = new ChromeOptions();
            String appData = $"user-data-dir={Path.GetPathRoot(Environment.SystemDirectory)}users\\{Environment.UserName}\\AppData\\Local\\Google\\Chrome\\User Data";
            options.AddArgument(appData);
            ChromiumPerformanceLoggingPreferences pref = new ChromiumPerformanceLoggingPreferences();
            pref.IsCollectingNetworkEvents = true;
            options.PerformanceLoggingPreferences = pref;
            options.SetLoggingPreference(LogType.Performance, LogLevel.All);
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.LogPath = "C:\\Users\\aguthr01\\OneDrive - AHS\\Desktop\\log.txt";
            service.EnableVerboseLogging = true;
            service.HideCommandPromptWindow = true;
            ChromeDriver driver = new ChromeDriver(service, options);
            return driver;
        }

        static async void GetNetworkResponse(object? sender, DevToolsEventReceivedEventArgs e)
        {
            //https://stackoverflow.com/questions/27761852/how-do-i-await-events-in-c
            Func<object?, DevToolsEventReceivedEventArgs, Task> handler = NetEvent;
            if (handler != null)
            {
                Delegate[] invocationList = handler.GetInvocationList();
                Task[] handlerTasks = new Task[invocationList.Length];

                for (int i = 0; i < invocationList.Length; i++)
                {
                    handlerTasks[i] = ((Func<object, EventArgs, Task>)invocationList[i])(null, EventArgs.Empty);
                }
                Console.WriteLine($"task count {invocationList.Length}");
                //WriteEvent(sender, e);
                if (invocationList.Length > 1)
                {
                    await Task.WhenAll(handlerTasks);
                    Console.WriteLine("DoneWaiting");
                }
            }
            //Console.WriteLine("Response");
            JToken data = e.EventData;
            eventCollection.Add(data);
            //Console.WriteLine("start");
            foreach (String type in perfLogs.AvailableLogTypes)
            {
                //Console.WriteLine($"type: {type}");
                logEntries = perfLogs.GetLog(type);
                if (logEntries.Count > 0)
                {
                    //Console.WriteLine($"entry amounts: {logEntries.Count}");
                    entryCollection.Add(logEntries);
                    /*
                    foreach (OpenQA.Selenium.LogEntry logEntry in logEntries)
                    {
                        if(logEntry.ToString().Contains("Andrew") || logEntry.ToString().Contains("andrew") || logEntry.ToString().Contains("andrgu3"))
                        {
                            //Console.WriteLine(logEntry.ToString());
                        }
                        //String path = "C:\\Users\\aguthr01\\OneDrive - AHS\\Desktop\\NotesAndTraining\\Scripts\\ag_script\\ReceivingScript\\DatabaseUpdates\\log.txt";
                        //System.IO.File.AppendAllText(path, logEntry.ToString());
                        //System.IO.File.AppendAllText(path, DateTime.Now.ToString());
                    }
                    */
                }
            }
            //Console.WriteLine("end");
        }

        static event Func<object?, EventArgs, Task> NetEvent;
        static async System.Threading.Tasks.Task WriteEvent(object? sender, EventArgs e)
        {
            Console.WriteLine("start of write");
            String path = "C:\\Users\\aguthr01\\OneDrive - AHS\\Desktop\\NotesAndTraining\\Scripts\\ag_script\\ReceivingScript\\DatabaseUpdates\\log.txt";
            foreach (IReadOnlyCollection<OpenQA.Selenium.LogEntry> entry in entryCollection)
            {
                foreach (OpenQA.Selenium.LogEntry logEntry in entry)
                {
                    System.IO.File.AppendAllText(path, logEntry.ToString());
                    System.IO.File.AppendAllText(path, "GuthrieIterator");
                    //System.IO.File.AppendAllText(path, DateTime.Now.ToString());
                }
            }
            System.IO.File.AppendAllText(path, "GuthrieIterator StartOfJson");
            foreach (JObject logEntry in eventCollection)
            {
                System.IO.File.AppendAllText(path, logEntry.ToString());
                System.IO.File.AppendAllText(path, "GuthrieIterator");
            }
            Console.WriteLine("end of write");
            /*
            HttpRequestData requestData = new HttpRequestData();
            requestData.Url = "https://mb.verizonwireless.com/mbt/secure/pendingordersvc/mbt/orders";
            requestData.Headers = new Dictionary<String, String>() { { "Content-type", "application/json" } };
            requestData.Method = "POST";
            requestData.PostData = "{\"columnNames\":[\"\"],\"ecpdId\":\"72816\",\"facetEnabled\":true,\"searchFilter\":{\"days\":\"30\",\"filters\":[{\"serachBy\":\" \",\"serachValue\":[\" \"]}],\"fromDate\":\"\",\"toDate\":\"\"},\"sortResults\":{},\"userId\":\"ANDRGU3\",\"loggedInEmailId\":\"Andrew.Guthrie@atlantichealth.org\",\"csr\":false,\"quoteType\":\"\"}";
            JObject j = JObject.Parse(e.EventData.ToString());
            if (j["method"] != null)
            {
                if (j["method"].ToString() == requestData.Method)
                {
                    Console.WriteLine("POST");
                    if (j["url"] != null)
                    {
                        if (j["url"].ToString() == requestData.Url)
                        {
                            Console.WriteLine("URL");
                            if (j["postdata"] != null)
                            {
                                if(j["postdata"].ToString() == requestData.PostData)
                                {
                                    Console.WriteLine(e.EventData.ToString());
                                }
                            }
                        }
                    }
                }
            }
            NetworkResponseHandler networkResponseHandler = new NetworkResponseHandler();
            Console.WriteLine(e.EventData.ToString());
            Console.WriteLine("#######################");
            */
        }


    }
}
