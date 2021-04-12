using System;
using System.Collections.Generic;
using CMSlib.Extensions;
using CMSlib.Tables;
using System.Linq;
using System.IO;
namespace CMSrequester
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.Title = "CMSrequester";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Thank you for using CMSrequester! Please use the command \"allcommands\" to see all available commands.");
            RequesterConfig cfg = new();
            System.Net.Http.HttpClient client = new();
            System.Text.StringBuilder currentJson = new();
            Dictionary<string, Command> commands = new();
            List<ExceptionRecord> exceptionRecords = new();
            Table excs = new(
                new TableSection(
                    typeof(int),
                        new TableColumn(null, 4, "Num", LeftPipe:true, RightPipe:true)
                ),
                new TableSection(
                    typeof(ExceptionRecord),
                        new TableColumn("ThrownAt", 21, "Timestamp", RightPipe:true, CustomFormatter:(object item)=> { return ((DateTime)item).ToString("yyyy-MM-dd - HH:mm:ss"); }),
                        new TableColumn("CommandName", 20, "Command", RightPipe:true),
                        new TableColumn("Exception", 25, null, RightPipe: true, CustomFormatter: (object item) => { return ((Exception)item).GetType().Name; })
                )
            );

       
            commands.Add("ADDHEADER", new("Adds a header to all outgoing requests.", async () => {
                Console.Write("Header name: ");
                string headerName = Console.ReadLine();
                Console.Write("Header value(s): ");
                List<string> values = new();
                string value = Console.ReadLine();
                do
                {
                    values.Add(value);
                    value = Console.ReadLine();
                } while (value != "");
                if (values.Count == 1) {
                    client.DefaultRequestHeaders.Add(headerName, values[0]);
                    Console.WriteLine($"Added header \"{headerName}\": \"{values[0]}\"");
                }
                else
                {
                    client.DefaultRequestHeaders.Add(headerName, values);
                    Console.WriteLine($"Added header \"{headerName}\": {values.ToArray().ToReadableString()}");
                }
            }));
            commands.Add("CLEARHEADERS", new("Clears all headers set using ADDHEADER", async () => {
                client.DefaultRequestHeaders.Clear();
                Console.WriteLine("Successfully cleared headers");
            }));
            commands.Add("SETBASEADDRESS", new("Sets the base address for all HTTP verb command urls.", async () =>
            {
                Console.Write("Address: ");
                client.BaseAddress = new Uri(Console.ReadLine());
                Console.WriteLine("Set address successfully");
            }));
            commands.Add("SETJSON", new("Sets the json used in POST, PUT, and PATCH. Press enter on an empty line to end entry.", async () => {
                currentJson.Clear();
                string input = Console.ReadLine();
                do
                {
                    currentJson.Append(input.Trim());
                    input = Console.ReadLine();

                } while (input != "");
                Console.WriteLine("Successfully set json");
            }));
            commands.Add("ALLCOMMANDS", new("Shows this list.", async () =>
            {
                foreach (KeyValuePair<string, Command> pair in from KeyValuePair<string, Command> pair in commands select pair)
                {
                    Console.ForegroundColor = cfg.CommandColor;
                    Console.Write(pair.Key);
                    Console.ForegroundColor = cfg.DefaultText;
                    Console.WriteLine(": " + pair.Value.Description);
                }
            }));
            commands.Add("WINDOWTITLE", new("Sets the title of this console window", async static () =>
            {
                Console.Write("Title: ");
                Console.Title = Console.ReadLine();
            }));
            commands.Add("CLEAR", new("Clears all text in this console window.", static async () =>
            {
                Console.Clear();
            }));
            commands.Add("OPTIONS", new("Makes an OPTIONS request to the provided url", async () => {
                Console.Write("URL: ");
                string url = Console.ReadLine();
                if (url.ToUpper() == "CANCEL") return;
                Console.WriteLine("Running...");
                await DisplayResponse(await client.SendAsync(new(System.Net.Http.HttpMethod.Options, url)), cfg);
            }));

            commands.Add("POST", new("Makes a POST request to the provided url, using the json provided in SETJSON.", async () =>
            {
                if (currentJson.Length != 0)
                {
                    Console.Write("URL: ");
                    string url = Console.ReadLine();
                    if (url.ToUpper() == "CANCEL") return;
                    Console.WriteLine("Running...");
                    await DisplayResponse(
                        await client.PostAsync(url,
                            new System.Net.Http.StringContent(
                                currentJson.ToString(), System.Text.Encoding.UTF8, "application/json"
                            )
                        ), cfg
                    );
                }
                else
                {
                    Console.WriteLine("Run SETJSON first");
                }
            }));
            commands.Add("HEAD", new("Makes a HEAD request to the provided url.", async () =>
            {
               Console.Write("URL: ");
               string url = Console.ReadLine();
               if (url.ToUpper() == "CANCEL") return;
               Console.WriteLine("Running...");
               await DisplayResponse(await client.SendAsync(new(System.Net.Http.HttpMethod.Head, url)), cfg);
            }));
            commands.Add("PUT", new("Makes a PUT request to the provided url, using the json provided in SETJSON.", async () => {
                if (currentJson.Length != 0)
                {
                    Console.Write("URL: ");
                    string url = Console.ReadLine();
                    if (url.ToUpper() == "CANCEL") return;
                    Console.WriteLine("Running...");
                    await DisplayResponse(
                    await client.PutAsync(url,
                        new System.Net.Http.StringContent(
                            currentJson.ToString(), System.Text.Encoding.UTF8, "application/json"
                        )
                    ), cfg);
                }
                else
                {
                    Console.WriteLine("Run SETJSON first");
                }
            }));
            commands.Add("PATCH", new("Makes a PATCH request to the provided url, using the json provided in SETJSON.", async () => {
                if (currentJson.Length != 0)
                {
                    Console.Write("URL: ");
                    string url = Console.ReadLine();
                    if (url.ToUpper() == "CANCEL") return;
                    Console.WriteLine("Running...");
                    await DisplayResponse(
                    await client.PatchAsync(url,
                        new System.Net.Http.StringContent(
                            currentJson.ToString(), System.Text.Encoding.UTF8, "application/json"
                        )
                    ), cfg);
                }
                else
                {
                    Console.WriteLine("Run SETJSON first");
                }
            }));
            commands.Add("DELETE", new("Makes a DELETE request to the provided url.", async () => {

                Console.Write("URL: ");
                string url = Console.ReadLine();
                if (url.ToUpper() == "CANCEL") return;
                Console.WriteLine("Running...");
                await DisplayResponse(
                    await client.DeleteAsync(url), cfg
                );

            }));
            commands.Add("GET", new("Makes a GET request to the provided url.", async () =>
            {
               Console.Write("URL: ");
               string url = Console.ReadLine();
               if (url.ToUpper() == "CANCEL") return;
               Console.WriteLine("Running...");
               await DisplayResponse(await client.GetAsync(url), cfg);
            }));
            commands.Add("GETBYTES", new("Makes a GET request to the provided url, but gets a byte array. You can leave \"Output file title\" blank if you don't want an output file, or give the file a title to save the bytes to your disk.", async () =>
            {
                if (!Directory.Exists(cfg.StorageFolder))
                {
                    Console.WriteLine("Invalid path provided - please correct the StorageFolder in the config");
                    return;
                }
                Console.Write("Output file title: ");
                string outputTitle = Console.ReadLine();
                Console.Write("URL: ");
                string url = Console.ReadLine();
                if (url.ToUpper() == "CANCEL") return;
                Console.WriteLine("Running...");
                System.Net.Http.HttpResponseMessage message = await client.SendAsync(new(System.Net.Http.HttpMethod.Head, url));
                if (!message.IsSuccessStatusCode)
                {
                    await DisplayResponse(message, cfg);
                    return;
                }
                byte[] bytes = await client.GetByteArrayAsync(url);
                if (outputTitle.Trim() == "")
                    Console.WriteLine(bytes.ToReadableString());
                else
                {
                    Console.WriteLine("Writing to file...");
                    using FileStream stream = File.Create($@"{cfg.StorageFolder}{(outputTitle.StartsWith("NOEXT") ? outputTitle[5..] : outputTitle + url[url.LastIndexOf('.')..])}");
                    await stream.WriteAsync(bytes);
                    Console.WriteLine("Successfully wrote to file");
                }
            }));
            commands.Add("GETBASE64", new("Makes a GET request to the provided url, but gets a base64 encoded string. You can leave \"Output file title\" blank if you don't want an output file, or give the file a title to save the txt to your disk.", async () =>
            {
                if (!Directory.Exists(cfg.StorageFolder))
                {
                    Console.WriteLine("Invalid path provided - please correct the StorageFolder in the config");
                    return;
                }
                Console.Write("Output file title: ");
                string outputTitle = Console.ReadLine();
                Console.Write("URL: ");
                string url = Console.ReadLine();
                if (url.ToUpper() == "CANCEL") return;
                Console.WriteLine("Running...");
                System.Net.Http.HttpResponseMessage message = await client.SendAsync(new(System.Net.Http.HttpMethod.Head, url));
                if (!message.IsSuccessStatusCode)
                {
                    await DisplayResponse(message, cfg);
                    return;
                }
                string encoded = Convert.ToBase64String(await client.GetByteArrayAsync(url));
                if (outputTitle.Trim() == "")
                    Console.WriteLine(encoded);
                else
                {
                    Console.WriteLine("Writing to file...");
                    using StreamWriter stream = File.CreateText($@"{cfg.StorageFolder}{Path.DirectorySeparatorChar}{outputTitle}.txt");
                    await stream.WriteAsync(encoded);
                    Console.WriteLine("Successfully wrote to file");
                }
            }));
            commands.Add("DOWNLOADGETJSON", new("Makes a GET request to the provided url, but gets a string. You can leave \"Output file title\" blank if you don't want an output file, or give the file a title to save the txt to your disk.", async () =>
            {
                if (!Directory.Exists(cfg.StorageFolder))
                {
                    Console.WriteLine("Invalid path provided - please correct the StorageFolder in the config");
                    return;
                }
                Console.Write("Output file title: ");
                string outputTitle = Console.ReadLine();
                Console.Write("URL: ");
                string url = Console.ReadLine();
                if (url.ToUpper() == "CANCEL") return;
                Console.WriteLine("Running...");
                System.Net.Http.HttpResponseMessage message = await client.SendAsync(new(System.Net.Http.HttpMethod.Head, url));
                if (!message.IsSuccessStatusCode)
                {
                    await DisplayResponse(message, cfg);
                    return;
                }

                if (outputTitle.Trim() == "")
                    Console.WriteLine((await client.GetAsync(url)));
                else
                {
                    Console.WriteLine("Writing to file...");
                    using StreamWriter stream = File.CreateText($@"{cfg.StorageFolder}{Path.DirectorySeparatorChar}{outputTitle}.txt");
                    (await client.GetAsync(url)).Content.CopyTo(stream.BaseStream, null, System.Threading.CancellationToken.None);
                    Console.WriteLine("Successfully wrote to file");
                }
            }));

            commands.Add("EXCEPTIONS", new("Shows the most recent exception", async () =>
            {
                if(exceptionRecords.Count == 0)
                {
                    Console.WriteLine("No exceptions currently stored.");
                    return;
                }
                Console.WriteLine(excs);
            }));
            
            commands.Add("CONFIG", new("Shows the current config's properties", async () =>
            {
                cfg.DisplayInspect();
            }));

            foreach (var prop in typeof(RequesterConfig).GetProperties())
            {
                commands.Add("CONFIG." + prop.Name.ToUpper(), new($"Sets the \"{prop.Name}\" property of the config", async () =>
                {
                    Console.Write("Value: ");
                    Console.ForegroundColor = cfg.ConfigOptionsDisplayColor;
                    string val = Console.ReadLine();
                    Console.ForegroundColor = cfg.DefaultText;
                    cfg.SetProp(prop.Name, val);
                }));
            }


            Console.ForegroundColor = cfg.DefaultText;
            while (true)
            {
                Console.ForegroundColor = cfg.DefaultText;
                Console.Write("Enter a command: ");
                Console.ForegroundColor = cfg.CommandColor;
                string input = Console.ReadLine();
                Console.ForegroundColor = cfg.DefaultText;
                if (commands.ContainsKey(input.ToUpper()))
                {
                    try
                    {
                        await commands[input.ToUpper()].Func.Invoke();
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            ExceptionRecord record = new(DateTime.Now, e, input.ToUpper());
                            exceptionRecords.Add(record);
                            excs.AddRow(exceptionRecords.Count, record);
                            Console.WriteLine("Exception encountered, use EXCEPTIONS command to view");
                        }catch(Exception ee)
                        {
                            Console.WriteLine(ee);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("\"" + input + "\" is an invalid command, run ALLCOMMANDS for a full list of commands");
                }
            }
        }
        public static async System.Threading.Tasks.Task DisplayResponse(System.Net.Http.HttpResponseMessage message, RequesterConfig config)
        {
            Console.WriteLine("Status code: " + message.StatusCode + " - " + (int)message.StatusCode + ' ' + message.StatusCode.ToString().ToUpper());
            Console.WriteLine("Reason: " + message.ReasonPhrase);
            Console.WriteLine("Headers: ");
            foreach (KeyValuePair<string, IEnumerable<string>> pair in message.Headers)
            {
                Console.WriteLine("\"" + pair.Key + "\": " + (pair.Value.Count() == 1 ? pair.Value.First() : pair.Value.ToArray().ToReadableString()));
            }
            Console.WriteLine("Content: ");
            PrettyPrint(await message.Content.ReadAsStringAsync(), config);
        }
        public static void PrettyPrint(string json, RequesterConfig config)
        {
            int tabs = 0;
            ConsoleColor nonquote = Console.ForegroundColor;
            bool inquote = false;
            foreach (char c in json)
            {
                switch (c)
                {
                    case '{': case '[':
                        tabs++;
                        Console.Write(c);
                        if (config.Indent)
                            Console.Write('\n' + new string(' ', tabs * 2));
                        break;
                    case '}': case ']':
                        tabs--;
                        if (config.Indent)
                            Console.Write('\n' + new string(' ', tabs * 2));
                        Console.Write(c);
                        break;
                    case ',':
                        Console.Write(c);
                        if (config.Indent)
                            Console.Write('\n' + new string(' ', tabs * 2));
                        break;
                    case '"':
                        Console.ForegroundColor = config.QuoteColor;
                        Console.Write(c);
                        inquote = !inquote;
                        if (!inquote)
                            Console.ForegroundColor = nonquote;
                        break;
                    default:
                        Console.Write(c.ToString());
                        break;
                }
            }
            Console.WriteLine();
        }

    }

    public class RequesterConfig
    {
        //all props should be bool, enum, or string.
        public bool Indent { get; private set; } = true;
        public ConsoleColor QuoteColor { get; private set; } = ConsoleColor.Green;
        public ConsoleColor ConfigOptionsDisplayColor { get; private set; } = ConsoleColor.Blue;
        public ConsoleColor CommandColor { get; private set; } = ConsoleColor.Red;
        public ConsoleColor DefaultText { get; private set; } = ConsoleColor.White;
        private string _storageFolder = "./";
        public string StorageFolder { get { return _storageFolder; } set { value = value.Replace('\\', '/'); _storageFolder = value + (value[^1] == '/' ? '\0' : '/'); } }
        public void DisplayInspect()
        {
            var props = typeof(RequesterConfig).GetProperties();
            foreach (var prop in props)
            {
                Console.Write(prop.Name + ": ");
                ConsoleColor prev = Console.ForegroundColor;
                Console.ForegroundColor = ConfigOptionsDisplayColor;
                Console.Write(prop.GetValue(this).ToString());
                Console.Write('\n');
                Console.ForegroundColor = prev;
            }
        }
        public void SetProp(string name, string value)
        {
            System.Reflection.PropertyInfo info = typeof(RequesterConfig).GetProperty(name);
            if (info is null)
                Console.WriteLine($"The property {name} was not found.");
            if (info?.CanWrite ?? false)
            {
                if (info.PropertyType.IsEnum && Enum.TryParse(info.PropertyType, value, true, out object result))
                    info.GetSetMethod().Invoke(this, new object[] { result });
                else
                    try {
                        info.GetSetMethod().Invoke(this, new object[]{
                        info.PropertyType == typeof(bool) ?
                          bool.Parse(value)
                        : info.PropertyType == typeof(int) ?
                          int.Parse(value)
                        :
                          value
                        });
                    }
                    catch (Exception) {
                        Console.WriteLine("There was an error setting the value, due to an invalid value type being provided.");
                    }
            }
        }
    } 
    
    public record Command(string Description, Func<System.Threading.Tasks.Task> Func);

    public record ExceptionRecord(DateTime ThrownAt, Exception Exception, string CommandName)
    {
        public override string ToString()
        {
            return ThrownAt.ToString("dd-MM-yyyy - HH:mm:ss");
        }
    }

}
