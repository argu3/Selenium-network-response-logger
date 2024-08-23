using Newtonsoft.Json;
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
using System.Linq;

namespace SeleniumTesting
{
    class ResponseLogger
    {

        static int lockCounter = 0;
        private static Mutex counterLock = new Mutex(false);
        private static Mutex logLock = new Mutex(false);
        static ChromeDriver chromeDriver = InitializeDriver();
        //static IReadOnlyCollection<OpenQA.Selenium.LogEntry> logEntries = new List<OpenQA.Selenium.LogEntry>();
        //static Logs perfLogs = new Logs(chromeDriver);
        //static List<IReadOnlyCollection<OpenQA.Selenium.LogEntry>> entryCollection = new List<IReadOnlyCollection<OpenQA.Selenium.LogEntry>>();
        //static List<JToken> eventCollection = new List<JToken>();
        static bool oneTime = true;

        static string[] exactImportantHeaderURLs = new string[0];
        static string[] fuzzyImportantHeaderURLs = new string[0];
        static string importantLogPath = "";
        static string otherLogPath = "";
        static string url = "";
        static void Main(string[] args)
        {
            
            bool checkArgs = true;
            if (args.Length == 0)
            {
                //args = mockArgs;
            }
            for(int i = 0; i < args.Length; i++)
            {
                if (args[i] == "help" || args[i] == "?")
                {
                    Console.WriteLine("Parameters:" +
                        "\n1: The path to the text file which holds the filtered network responses." +
                        "\n2: The path to the text file which holds all other responses." +
                        "\n3: Comma-separated list of partially-matching response header URLs used to filter the responses" +
                        "\n4: Comma-separated list of exactly matching response header URLs used to filter the responses" +
                        "\n5: URL for the initial navigation. *using this may break the program*");

                    checkArgs = false;
                    return;
                }
                else if(checkArgs)
                {
                    switch (i)
                    {
                        case 0:
                            importantLogPath = args[i]; break;
                        case 1:
                            otherLogPath = args[i]; break;
                        case 2:
                            if (fuzzyImportantHeaderURLs.Contains(","))
                            {
                                fuzzyImportantHeaderURLs = args[i].Split(','); 
                            }
                            else
                            {
                                fuzzyImportantHeaderURLs = [args[i]];
                            }
                            break;
                        case 3:
                            if (exactImportantHeaderURLs.Contains(","))
                            {
                                exactImportantHeaderURLs = args[i].Split(',');
                            }
                            else
                            {
                                exactImportantHeaderURLs = [args[i]];
                            }
                            break;
                        case 4:
                            url = args[i];
                            break;
                    }
                }
                if(fuzzyImportantHeaderURLs == null)
                {
                    fuzzyImportantHeaderURLs = new string[0];
                }
            }
            NetworkManager networkManager = new NetworkManager(chromeDriver);
            NetworkResponseHandler networkResponseHandler = new NetworkResponseHandler();
            networkResponseHandler.ResponseMatcher = ResponseReader;
            networkResponseHandler.ResponseTransformer = ResponseTransformer;
            networkManager.AddResponseHandler(networkResponseHandler);
            networkManager.NetworkResponseReceived += ResponseReceivedAsync; //this.ResponseReceivedAsync;
            networkManager.StartMonitoring();
            if (url.Length != 0) { chromeDriver.Navigate().GoToUrl(url); }
            //https://codoid.com/selenium-testing/how-to-use-selenium-webdriver-event-listener/#:~:text=How%20to%20use%20Selenium%20WebDriver%20Event%20Listener%3F%201,afterFindBy%20...%205%20beforeClickOn%20...%206%20Conclusion%20
            while (true)
            {
                //do nothing
            }
        }

        static void ResponseReceivedAsync(object? sender, NetworkResponseReceivedEventArgs e)
        {
            //debugging step, this task could be in the main body here
            //this.LogNetworkResponse(e);
            LogNetworkResponse(e);
        }

        //public virtual async Task LogNetworkResponse(NetworkResponseReceivedEventArgs e)
        public static async Task LogNetworkResponse(NetworkResponseReceivedEventArgs e)
        {
            counterLock.WaitOne();
            lockCounter++;
            int localCounter = lockCounter;
            //Console.WriteLine($"NetworkresponseReceived, waiting {localCounter}");
            counterLock.ReleaseMutex();
            logLock.WaitOne();
            //Console.WriteLine("start of write");
            String path = otherLogPath;
            if (e.ResponseUrl != null)
            {
                var query = from url in fuzzyImportantHeaderURLs
                            where url.Contains(e.ResponseUrl) || e.ResponseUrl.Contains(url)
                            select url;
                if (exactImportantHeaderURLs.Contains(e.ResponseUrl) ||query.Any())
                {
                    path = importantLogPath;
                }
                else
                {
                    System.IO.File.AppendAllText(path, e.ResponseUrl);
                    System.IO.File.AppendAllText(path, "\n");
                }
            }
            else if (path == otherLogPath)
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
            //Console.WriteLine("end of write");
            logLock.ReleaseMutex();
            //Console.BackgroundColor = ConsoleColor.Red;
            //Console.WriteLine($"NetworkresponseReceived, releasing {localCounter}");
            //Console.BackgroundColor = ConsoleColor.Black;
        }
        static event Func<HttpResponseData, bool> ResponseReader = response => true;
        //hangs if I don't have it, just a passthrough
        static event Func<HttpResponseData, HttpResponseData> ResponseTransformer = response => response;
        static ChromeDriver InitializeDriver()
        {
            ChromeOptions options = new ChromeOptions();
            String appData = $"user-data-dir={Path.GetPathRoot(Environment.SystemDirectory)}users\\{Environment.UserName}\\AppData\\Local\\Google\\Chrome\\User Data";
            options.AddArgument(appData);
            options.AddExcludedArgument("enable-automation");
            //ChromiumPerformanceLoggingPreferences pref = new ChromiumPerformanceLoggingPreferences();
            //pref.IsCollectingNetworkEvents = true;
            //options.PerformanceLoggingPreferences = pref;
            //options.SetLoggingPreference(LogType.Performance, LogLevel.All);
            //service.LogPath = "logpath";
            //service.EnableVerboseLogging = true;
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            ChromeDriver driver = new ChromeDriver(service, options);
            return driver;
        }
    }
}
