using System.Net;
using System.Text.RegularExpressions;

namespace SteamScreenShotPuller
{
    class Program
    {
        // Collection of often used readonly items, containting regex expressions and link parts
        static readonly string profileUrl = "https://steamcommunity.com/id/";
        static readonly string gridFilter = "/screenshots/?appid=0&sort=newestfirst&browsefilter=myfiles&view=grid";
        static readonly string screenshotUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=";
        static readonly string imageWallExpr = "imgWallItem_\\d{10}";
        static readonly string idFilterExpr = "\\d{10}";
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
            Console.WriteLine(@"Please enter the vanity ID of the profile.
The Vanity ID can be found on the profile link.
For example: https://steamcommunity.com/id/ActuallySin/ , here ActuallySin would be the ID.
Vanity ID:");
            string urlAddress = profileUrl + Console.ReadLine() + gridFilter;
            if (urlAddress == null)
            {
                Console.WriteLine("The input was empty. Cancelling operation");
                return EmptyList;
            }

            using (WebClient client = new WebClient())
            {
                try
                {
                    string htmlCode = client.DownloadString(urlAddress);

                    MatchCollection mc = Regex.Matches(htmlCode, expr);

                    string ImageSuperString = string.Empty;

                    if(mc.Count > 0)
                    {
                        foreach (Match match in mc)
                        {
                            ImageSuperString = ImageSuperString + match;
                        }

                        MatchCollection ids = Regex.Matches(ImageSuperString, idFilterExpr);
                        List<string> linkList = new List<string>();

                        foreach (Match id in ids)
                        {
                            string screenshotLink = screenshotUrl + id.Value;
                            linkList.Add(screenshotLink);
                        }

                        return linkList;
                    }

                    else
                    {
                        Console.WriteLine("This profile is either invalid, has no screenshots or is private");
                    }

                }
                catch (WebException exp)
                {
                    Console.WriteLine("Error processing the URL, is it the correct format ?");
                    Console.WriteLine(exp.Message);
                    return EmptyList;
                }
                
            }

            return EmptyList;
            
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
            if (!downloadLocation.EndsWith("\\"))
            {
                downloadLocation = downloadLocation + "\\";
            }
            using (WebClient client = new WebClient())
            {
                for (int i = 0; i < links.Count; i++)
                {
                    try
                    {
                        //Generates a random string to assign as image name
                        Random random = new Random();
                        int length = 16;
                        string rString = "";
                        for (int y = 0; y < length; y++)
                        {
                            rString += ((char)(random.Next(1, 26) + 64)).ToString().ToLower();
                        }

                        Console.WriteLine($"Downloading image {i+1} of {links.Count+1}");

                        // Downloads the image with a random name
                        string imageName = rString + i.ToString();
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
        {;
            List<string> exit = getScreenshotList(imageWallExpr);
            if (exit.Count == 0)
            {
                Console.WriteLine("The Operation was not completes successfully.");
                return (int)ExitCode.Failure;
            }
            else
            {
                getScreenshots(exit);
                Console.WriteLine("The Operation was successful");
                return (int)ExitCode.Success;
            }
        }
    }
}


