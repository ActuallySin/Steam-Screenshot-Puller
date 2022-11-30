using System.Net;
using System.Text.RegularExpressions;

namespace Steam_ScreenShot_Puller
{
    internal static class Program
    {
        // Collection of often used readonly items, containing regex expressions and link parts
        private const string ProfileUrl = "https://steamcommunity.com/id/";
        private const string ProfileAltUrl = "https://steamcommunity.com/profiles/";
        private const string GridFilter = "/screenshots/?p=";
        private const string GridFilterEnd = "&sort=newestfirst&browsefilter=myfiles&view=grid";
        private const string ScreenshotUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=";
        private const string ImageWallExpr = "imgWallItem_\\d+";
        private const string IdFilterExpr = "\\d+";
        private const string ImgLinkExpr = "https:\\/\\/steamuserimages-a\\.akamaihd\\.net\\/ugc\\/\\d+\\/(\\w*)\\/\\?imw=5000&imh=5000&ima=fit&impolicy=Letterbox&imcolor=%23000000&letterbox=false";
        private static readonly List<string> EmptyList = new List<string>();

        // Enums for exit codes
        private enum ExitCode
        {
            Success = 0,
            Failure = 10,
            //InvalidUrl = -1,
            //UnknownError = -2,
        }

        //Function to grab image IDs from screenshot grid
        private static List<string> GetScreenshotList(string expr, HttpClient client)
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
            var idType = 0;
            var vanityId = "" + Console.ReadLine();

            if (Regex.IsMatch(vanityId, "\\d{17}") || (Regex.IsMatch(vanityId, "\\[U:\\d:\\d{8}\\]")))
            {
                idType = 1;
            }

            var pagination = 1;
            string urlAddress;

            if (idType == 1)
            {
                urlAddress = ProfileAltUrl + vanityId + GridFilter + pagination + GridFilterEnd;
            }
            else
            {
                urlAddress = ProfileUrl + vanityId + GridFilter + pagination + GridFilterEnd;
            }
            if (urlAddress == "")
            {
                Console.WriteLine("The input was empty. Cancelling operation");
                return EmptyList;
            }
            
            try
            {
                var found = true;
                var imageSuperString = string.Empty;
                var linkList = new List<string>();
                while (found)
                {
                    var response = client.GetAsync(urlAddress).Result;
                    var content = response.Content;
                    var htmlCode = content.ReadAsStringAsync().Result;

                    if (Regex.IsMatch(htmlCode, expr))
                    {
                        Console.WriteLine($"Checking page {pagination}");

                        var mc = Regex.Matches(htmlCode, expr);

                        foreach (Match match in mc)
                        {
                            imageSuperString = imageSuperString + match;
                        }

                        var ids = Regex.Matches(imageSuperString, IdFilterExpr);

                        foreach (Match id in ids)
                        {
                            var screenshotLink = ScreenshotUrl + id.Value;
                            linkList.Add(screenshotLink);
                        }
                        pagination++;
                        if (idType == 1)
                        {
                            urlAddress = ProfileAltUrl + vanityId + GridFilter + pagination + GridFilterEnd;
                        }
                        else
                        {
                            urlAddress = ProfileUrl + vanityId + GridFilter + pagination + GridFilterEnd;
                        }
                    }
                    else
                    {
                        found = false;
                    }
                }
                var sanitizedLinkList = linkList.Distinct().ToList();
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

        //Function to get the actual screenshots
        private static async Task GetScreenshots(IReadOnlyList<string> links, HttpClient client)
        {
            Console.WriteLine("Please enter a download location:");
            var downloadLocation = "" + Console.ReadLine();

            //enforced directory formatting
            if (downloadLocation.Contains('\\'))
            {
                downloadLocation = downloadLocation.Replace("\\", "\\\\");
            }
            if (!downloadLocation.EndsWith("\\") || !downloadLocation.EndsWith("/"))
            {
                downloadLocation += "\\";
            }
            
            for (var i = 0; i < links.Count; i++)
            {
                try
                {
                    Console.WriteLine($"Downloading image {i + 1} of {links.Count + 1}");

                    var imageName = Regex.Match(links[i], IdFilterExpr).Value;
                    var response = client.GetAsync(links[i]).Result;
                    var content = response.Content;
                    var html = content.ReadAsStringAsync().Result;
                    var realImageLink = Regex.Match(html, ImgLinkExpr).Value;
                    var fileUrl = await client.GetAsync(realImageLink);
                    var resultStream = await fileUrl.Content.ReadAsStreamAsync();
                    var fileStream = File.Create($"{downloadLocation}{imageName}.jpg");
                    await resultStream.CopyToAsync(fileStream);
                    Thread.Sleep(1000);
                }
                catch (WebException exp)
                {
                    Console.WriteLine(exp.Message);
                }
            }
        }

        private static async Task<int> Main()
        {
            var client = new HttpClient();
            var exit = GetScreenshotList(ImageWallExpr, client);
            if (exit.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The Operation failed. Retry ? Y / N");
                var answer = string.Empty + Console.ReadLine();
                if (answer.ToLower() == "y")
                {
                    Console.Clear();
                    await Main();
                    return (int)ExitCode.Failure;
                }
                else
                    Console.WriteLine("Closing application...");
                Thread.Sleep(1000);
                return (int)ExitCode.Failure;
            }
            else
            {
                await GetScreenshots(exit, client);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("The Operation was successful. Closing in 10 seconds.");
                Thread.Sleep(10000);
                return (int)ExitCode.Success;
            }
        }
    }
}


