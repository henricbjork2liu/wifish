using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Security.Credentials;
using DeviceInformationCollection = Windows.Devices.Enumeration.DeviceInformationCollection;


/*
Preramble:
This is a console application. Regular console application will not work (and this application should only be used for Windows)
Console application (dotnet framework) did not work for me. I received "UnsupportedProtocol...." when connecting to the network
 
What must be done to get Windows.* references to work:
1: Add <TargetPlatformVersion>10.0</TargetPlatformVersion> to PropertyGroup tag in csproj.
   Right click the wifish.csproj and unload it first. Now you should be able to edit it.

- I think this step is only if 2 does not work. -
3. Add refence to Windows.winmd. Same as above, but choose "browse" and find it under C:\Program Files(x86)/Windows Kits/10/UnionMetaData

4. Add reference to C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.netcore\v4.5\System.Runtime.WindowsRuntime.dll 
   I had to install Visual Studio 2019 for .net core 4.5 to be installed. Was not automatically installed when installing Visual Studio 2022.
*/

namespace wifish
{
    internal class Program
    {
        static public WiFiAdapter MainAdapter;
        static public Dictionary<string, WiFiAvailableNetwork> AvailableNetworks;
        static public string[] Arguments;
        static public PasswordCredential Credentials;
        static public string Ssid;

        static private string commandName;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                // Print available commands and exit the application
                Console.WriteLine();
                Console.WriteLine("Available commands:");
                Console.WriteLine("  connect     -  Connects to a wireless network using ssid. Username and password are optional");
                Console.WriteLine("  disconnect  -  Disconnects from currently connected network");
                return;
            }
            else
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].StartsWith("-"))
                        args[i] = args[i].ToLower();
                }
                Arguments = args;
                commandName = args[0];
            }

            Credentials = new PasswordCredential();
            ICommand command;
            switch (commandName)
            {
                case "connect":
                    command = new CommandConnect();

                    if (args.Length == 2 && args[1] == "?")
                        Console.WriteLine(command.Description);
                    else if (command.AreRequirementsFulfilled())
                        command.Execute();
                    break;

                case "disconnect":
                    command = new CommandDisconnect();

                    if (args.Length == 2 && args[1] == "?")
                        Console.WriteLine(command.Description);
                    else if (command.AreRequirementsFulfilled())
                        command.Execute();
                    break;

                case "save":
                    command = new CommandSaveCredentials();

                    if (args.Length == 2 && args[1] == "?")
                        Console.WriteLine(command.Description);
                    else if (command.AreRequirementsFulfilled())
                        command.Execute();
                    break;

                case "?":
                    Console.WriteLine("Usage: wifish10 [Command]");
                    Console.WriteLine("       wifish10 [Command] [-subcommand]");
                    Console.WriteLine("       wifish10 [Command] ?");
                    Console.WriteLine("");
                    Console.WriteLine("The following commands are available:");
                    Console.WriteLine("connect [ssid]              - connects to network with ssid.");
                    Console.WriteLine("disconnect                  - Disconnects from currently connected network.");
                    break;

                default:
                    Console.WriteLine($"Error: Could not parse {string.Join(" ", args)}");
                    break;
            }

        }

        static public bool SubCommandExists(List<string> args, out int index, string command, string optionalCommand)
        {
            index = args.IndexOf(command);
            index = index == -1 ? args.IndexOf(optionalCommand) : index;
            if (index != -1)
                return true;
            return false;
        }

        static public bool ValueExists(List<string> args, int index)
        {
            int value_index = index + 1;
            if (value_index == -1)
                return false;

            if (args.Count > value_index && !args[value_index].StartsWith("-"))
                return true;

            return false;
        }

        static public string GetValueOfSubcommand(List<string> _args, int index)
        {
            _args.RemoveAt(index); //Removes command
            string value = _args[index];
            _args.RemoveAt(index); //Removes the value
            return value;
        }

        ///<summary>
        ///Returns true if:
        ///    - Subcommand and value exists and are valid
        ///    - Subcommand does not exist.
        ///</summary>
        static public bool TryGetSubCommandAndValue(List<string> _args, string command, string shortCommand, out string result, bool has_value = true)
        {
            result = null;
            if (SubCommandExists(_args, out int index, command, shortCommand))
            {
                if (has_value && ValueExists(_args, index))
                    result = GetValueOfSubcommand(_args, index);
                else if (!has_value)
                {
                    _args.RemoveAt(index); //removes the command
                    result = "";
                }
                else
                {
                    Console.WriteLine($"Error: No valid value for {command}");
                    return false;
                }
            }
            return true;
        }

        static public async Task<bool> ScanForNetworks()
        {
            if (MainAdapter == null)
            {
                Task<bool> t = _find_adapter();
                t.Wait();
                if (!t.Result)
                {
                    return false;
                }
            }

            try
            {
                await MainAdapter.ScanAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error scanning WiFi adapter: 0x{0:X}: {1}", ex.HResult, ex.Message);
                return false;
            }

            AvailableNetworks = new Dictionary<string, WiFiAvailableNetwork>();
            foreach (WiFiAvailableNetwork network in MainAdapter.NetworkReport.AvailableNetworks)
            {
                string ssid = network.Ssid?.ToLower();
                if (!string.IsNullOrEmpty(ssid) && !AvailableNetworks.ContainsKey(ssid))
                    AvailableNetworks.Add(ssid, network);
            }
            return true;
        }

        static private async Task<bool> _find_adapter()
        {
            WiFiAccessStatus access = await WiFiAdapter.RequestAccessAsync();
            if (access != WiFiAccessStatus.Allowed)
            {
                Console.WriteLine("Not able to access wireless network adapters: Access denied");
                return false;
            }
            DeviceInformationCollection result = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
            if (result.Count > 0)
            {
                MainAdapter = await WiFiAdapter.FromIdAsync(result[0].Id);
                return true;
            }
            Console.WriteLine("No wireless network adapter found");
            return false;
        }

        static public bool FindAdapter()
        {
            using (Task<bool> result = _find_adapter())
            {
                result.Wait();
                return result.Result;
            }
        }
    }
}
