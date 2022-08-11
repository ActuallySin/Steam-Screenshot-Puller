using System.Net;
using System.Text.RegularExpressions;

namespace SteamScreenShotPuller
{
    class Program
    {
        // Collection of often used readonly items, containting regex expressions and link parts
        static readonly string profileUrl = "https://steamcommunity.com/id/";
        static readonly string profileAltUrl = "https://steamcommunity.com/profiles/";
        static readonly string gridFilter = "/screenshots/?p=";
        static readonly string gridFilterEnd = "&sort=newestfirst&browsefilter=myfiles&view=grid";
        static readonly string screenshotUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=";
        static readonly string imageWallExpr = "imgWallItem_\\d+";
        static readonly string idFilterExpr = "\\d+";
        static readonly string imgLinkExpr = "https:\\/\\/steamuserimages-a\\.akamaihd\\.net\\/ugc\\/\\d+\\/(\\w*)\\/\\?imw=5000&imh=5000&ima=fit&impolicy=Letterbox&imcolor=%23000000&letterbox=false";
        static readonly List<string> EmptyList = new List<string>();

        // Enums for exit codes
        enum ExitCode : int
        {
            Success = 0,
            Failure = 10,
            InvalidUrl = -1,
            UnknownError = -2,
        }

        //Function to grab image IDs from screenshot grid
        private static List<string> getScreenshotList(string expr)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Welcome to the Steam Screenshot Puller!");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(@"Please enter the VanityID, SteamID or Steam3 ID of the profile.
Possible Formats:
VanityID: ActuallySin
SteamID: 76561198039137626
Steam3 ID: [U:1:78871898]

ID: ");
            int idType = 0;
            string vanityId = "" + Console.ReadLine();
            if (Regex.IsMatch(vanityId, "\\d{17}") is true || (Regex.IsMatch(vanityId, "\\[U:\\d:\\d{8}\\]") is true))
            {
                idType = 1;
            }

            int pagination = 1;
            string urlAddress = string.Empty;

            if (idType == 1)
            {
                urlAddress = profileAltUrl + vanityId + gridFilter + pagination + gridFilterEnd;
            }
            else
            {
                urlAddress = profileUrl + vanityId + gridFilter + pagination + gridFilterEnd;
            }
            if (urlAddress == "")
            {
                Console.WriteLine("The input was empty. Cancelling operation");
                return EmptyList;
            }

            using (WebClient client = new WebClient())
            {
                try
                {
                    bool found = true;
                    string ImageSuperString = string.Empty;
                    List<string> linkList = new List<string>();
                    while (found is true)
                    {
                        string htmlCode = client.DownloadString(urlAddress);

                        if (Regex.IsMatch(htmlCode, expr) is true)
                        {
                            Console.WriteLine($"Checking page {pagination}");

                            MatchCollection mc = Regex.Matches(htmlCode, expr);

                            foreach (Match match in mc)
                            {
                                ImageSuperString = ImageSuperString + match;
                            }

                            MatchCollection ids = Regex.Matches(ImageSuperString, idFilterExpr);

                            foreach (Match id in ids)
                            {
                                string screenshotLink = screenshotUrl + id.Value;
                                linkList.Add(screenshotLink);
                            }
                            pagination++;
                            if (idType == 1)
                            {
                                urlAddress = profileAltUrl + vanityId + gridFilter + pagination + gridFilterEnd;
                            }
                            else
                            {
                                urlAddress = profileUrl + vanityId + gridFilter + pagination + gridFilterEnd;
                            }
                        }
                        else
                        {
                            found = false;
                        }
                    }
                    List<string> sanitizedLinkList = linkList.Distinct().ToList();
                    if (sanitizedLinkList.Count == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("This profile is either invalid, has no screenshots or is private");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        return EmptyList;

                    }
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Found a total of {sanitizedLinkList.Count} images.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return sanitizedLinkList;

                }
                catch (WebException exp)
                {
                    Console.WriteLine("Error processing the URL, is it the correct format ?");
                    Console.WriteLine(exp.Message);
                    return EmptyList;
                }

            }
        }

        //Function to get the actual screenshots
        private static void getScreenshots(List<string> links)
        {
            Console.WriteLine("Please enter a download location:");
            string downloadLocation = "" + Console.ReadLine();

            //enforced directory formatting
            if (downloadLocation.Contains("\\"))
            {
                downloadLocation.Replace("\\", "\\\\");
            }
            if (!downloadLocation.EndsWith("\\") || !downloadLocation.EndsWith("/"))
            {
                downloadLocation = downloadLocation + "\\";
            }
            using (WebClient client = new WebClient())
            {
                for (int i = 0; i < links.Count; i++)
                {
                    try
                    {
                        Console.WriteLine($"Downloading image {i + 1} of {links.Count + 1}");

                        string imageName = Regex.Match(links[i], idFilterExpr).Value;
                        string html = client.DownloadString(links[i]);
                        string realImageLink = Regex.Match(html, imgLinkExpr).Value;
                        client.DownloadFile(new Uri(realImageLink), $"{downloadLocation}{imageName}.jpg");
                        Thread.Sleep(1000);
                    }
                    catch (WebException exp)
                    {
                        Console.WriteLine(exp.Message);
                    }
                }
            }
        }

        static int Main(string[] args)
        {
            ;
            List<string> exit = getScreenshotList(imageWallExpr);
            if (exit.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The Operation failed. Rety ? Y / N");
                string answer = String.Empty + Console.ReadLine();
                if (answer.ToLower() == "y")
                {
                    Console.Clear();
                    Main(args);
                    return (int)ExitCode.Failure;
                }
                else
                    Console.WriteLine("Closing application...");
                Thread.Sleep(3000);
                return (int)ExitCode.Failure;
            }
            else
            {
                getScreenshots(exit);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("The Operation was successful. Closing in 10 seconds.");
                Thread.Sleep(10000);
                return (int)ExitCode.Success;
            }
        }
    }
}


