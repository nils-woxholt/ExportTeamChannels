using CommandLine;
using HtmlAgilityPack;
using Newtonsoft.Json;
using QuickType;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ExportTeamChannels
{
    class Program
    {
        // figure out how to do this programmatically - for now get token from MS graph explorer
        static string bearerToken = "";
        // which team to you want to save? - get team id from graph explorer
        static string teamID = "";
        static string url = "https://graph.microsoft.com/beta/teams/{0}/channels/{1}/messages/?top=50";
        static string outputFolderMessages = Path.Combine(AppContext.BaseDirectory, "_chat_messages");
        static string outputFolderChannels = Path.Combine(AppContext.BaseDirectory, "_channels");
        static readonly HttpClient client = new HttpClient();
        static int pageNo = 0;

        static async Task Main(string[] args)
        {
            // process args
            var result = Parser.Default.ParseArguments<CommandLineOptions>(args);
            var retCode = await result.MapResult(async options => await ExportTeamAsync(options), _ => Task.FromResult(1));
            Console.WriteLine($"retCode={retCode}");
        }

        static async Task<int> ExportTeamAsync(CommandLineOptions opts)
        {
            teamID = opts.TeamID;
            bearerToken = opts.BearerToken;

            // set auth header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            CleanFolder(outputFolderMessages);
            CleanFolder(outputFolderChannels);

            // get channels - currently the beta has more fields
            string channelsJson = await client.GetStringAsync($"https://graph.microsoft.com/beta/teams/{teamID}/channels");
            var channels = JsonConvert.DeserializeObject<Channels>(channelsJson);

            int channelCount = 0;

            foreach (var channel in channels.Value)
            {
                if (channel.MembershipType != "private")
                {
                    channelCount++;
                    // write channel markdown
                    StringBuilder txtOut = new StringBuilder();
                    // Front Matter
                    txtOut.AppendLine("---");
                    txtOut.AppendLine("layout: channel");
                    txtOut.AppendLine($"channelid: {channel.Id}");
                    txtOut.AppendLine($"displayname: {channel.DisplayName}");
                    txtOut.AppendLine($"channelname: {CleanupString(channel.DisplayName)}");
                    // save the default ranking - there's no field for this
                    txtOut.AppendLine($"rank: {channelCount}");
                    txtOut.AppendLine("---");
                    // save markdown file to output folder
                    File.WriteAllText(Path.Combine(outputFolderChannels, $"{CleanupString(channel.DisplayName)}.md"), txtOut.ToString());

                    // tokenize url 
                    string fullUrl = string.Format(url, teamID, channel.Id);
                    await GetMessages(fullUrl, CleanupString(channel.DisplayName), channel.Id);
                }
            }
            return 0;
        }


        private static void CleanFolder(string folder)
        {
            //initialize output folder for frontmatter files
            Directory.CreateDirectory(folder);
            // remove any existing files in the folder
            foreach (FileInfo file in new DirectoryInfo(folder).GetFiles()) file.Delete();
        }

        private static string CleanupString(string displayName)
        {
            // basic helper func to get cleaner filenames
            return displayName.Replace(" ", "-")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("!", "")
                .Replace("?", "")
                .Replace("/", "-")
                .Replace(@"\", "-")
                .Replace(".", "-")
                .ToLower();
        }

        static async Task GetMessages(string fullUrl, string channelName, string channelID)
        {

            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                pageNo++;
                Console.WriteLine("calling page: " + pageNo);
                string responseBody = await client.GetStringAsync(fullUrl);

                // load string as json
                var result = JsonConvert.DeserializeObject<TeamsMessages>(responseBody);

                Console.WriteLine("messages: " + result.Value.Count);

                foreach (var item in result.Value)
                {
                    // check for images, get the binary code and convert to inline base64
                    item.Body.Content = processInlineImages(item.Body.Content);

                    // get a json 
                    string jsonItem = JsonConvert.SerializeObject(item);
                    // save json file to output folder
                    File.WriteAllText(Path.Combine(outputFolderMessages, $"{item.Id}.json"), jsonItem);

                    // check for replies (get top 50 for now)
                    string replyBody = await client.GetStringAsync($"https://graph.microsoft.com/beta/teams/{teamID}/channels/{channelID}/messages/{item.Id}/replies?top=50");
                    var replies = JsonConvert.DeserializeObject<TeamsMessages>(replyBody);
                    Console.WriteLine($"Message: {item.Id} on {item.CreatedDateTime}  with {replies.Value.Count} replies");

                    StringBuilder txtOut = new StringBuilder();
                    // Front Matter
                    txtOut.AppendLine("---");
                    txtOut.AppendLine($"channelid: {channelID}");
                    txtOut.AppendLine($"channelname: {channelName}");
                    // other data about messages
                    txtOut.AppendLine($"messageid: {item.Id}" );
                    txtOut.AppendLine($"created: {item.CreatedDateTime.ToString("dd MMM yyyy HH:mm:ss")}");
                    txtOut.AppendLine($"replies: {replies.Value.Count}");
                    txtOut.AppendLine($"user: {item.From.User.DisplayName}");
                    txtOut.AppendLine("---");
                    // append body html as-is
                    txtOut.AppendLine(item.Body.Content);

                    // if there were any replies, also append those
                    if (replies.Value.Count > 0)
                    {
                        // write replies json
                        string jsonReplies = JsonConvert.SerializeObject(replies.Value);
                        File.WriteAllText(Path.Combine(outputFolderMessages, $"{item.Id}-replies.json"), jsonReplies);

                        replies.Value.Sort((x, y) => x.CreatedDateTime.CompareTo(y.CreatedDateTime));
                        foreach (var reply in replies.Value)
                        {
                            // some custom html to wrap the replies
                            txtOut.AppendLine($"<div class=\"reply\" id=\"{reply.Id}\">");
                            txtOut.AppendLine($"<span class=\"reply-user\">{reply.From.User.DisplayName}</span> ");
                            txtOut.AppendLine($"<span class=\"reply-date\">{reply.CreatedDateTime.ToString("dd MMM yyyy HH:mm:ss")}</span>");

                            // check for images, get the binary code and convert to inline base64
                            reply.Body.Content = processInlineImages(reply.Body.Content);
                            txtOut.AppendLine($"<div class=\"reply-body\">");
                            txtOut.AppendLine(reply.Body.Content);
                            txtOut.AppendLine("</div>");
                            txtOut.AppendLine("</div>");
                        }
                    }
                    // save markdown file to output folder
                    File.WriteAllText(Path.Combine(outputFolderMessages, $"{item.Id}.md"), txtOut.ToString());
                }

                // look for odata next link
                if (!string.IsNullOrEmpty(result.OdataNextLink))
                {
                    // if there are more pages call this func again with the nextlink
                    await GetMessages(result.OdataNextLink.ToString(), channelName, channelID);
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
        }

        private static string processInlineImages(string bodyHTML)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(bodyHTML);

            HtmlNodeCollection nodes = htmlDoc.DocumentNode.SelectNodes("//img");

            if (nodes is null)
            {
                // htmlagilitypack returns null if the tag isn't found
                return bodyHTML;
            }

            try
            {
                foreach (var htmlNode in nodes)
                {
                    string src = htmlNode.Attributes["src"].Value;

                    // download file
                    using (WebClient webClient = new WebClient())
                    {
                        // set auth header
                        webClient.Headers[HttpRequestHeader.Authorization] = "Bearer " + bearerToken;
                        // get file
                        byte[] filebytes = webClient.DownloadData(src);
                        // convert to base64
                        string b64 = Convert.ToBase64String(filebytes, Base64FormattingOptions.None);
                        // assign img src as base64 - use generic image mime type
                        htmlNode.SetAttributeValue("src", @"data:image;base64," + b64);
                    }
                }

                using (StringWriter writer = new StringWriter())
                {
                    // weird htmlagilitypack behaviour - must save for it to update its internal html
                    htmlDoc.Save(writer);
                    bodyHTML = writer.ToString();
                }

            }
            catch (Exception)
            {
                // if e.g. I don;t have permission to get the image, just return the original body 
                // without raising an exception
                return bodyHTML;
            }

            return bodyHTML;
        }
    }
}
