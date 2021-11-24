using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;

namespace wifish
{
    internal class CommandDisconnect : ICommand
    {
        private List<string> _args;
        bool _will_forget = false;

        public string Description { get; } = @"
Usage:  wifish10 disconnect
        wifish10 disconnect [-f]

Commands in this context:
[-f | -forget]              - forgets the network as well.";

        public void Execute()
        {
            if (Program.FindAdapter())
            {
                if (_will_forget)
                    _forget().Wait();
                else
                    Program.MainAdapter.Disconnect();
                return;
            }
            Console.WriteLine("Failed disconnecting...");
        }

        public bool AreRequirementsFulfilled()
        {
            _args = Program.Arguments.ToList();
            _args.RemoveAt(0); //Remove the command
            string result;

            // Forget
            if (Program.TryGetSubCommandAndValue(_args, "-forget", "-f", out result, false))
                _will_forget = result != null;
            else
                return false;

            if (_args.Count > 0)
            {
                Console.WriteLine("Too many arguments:");
                foreach (string arg in _args)
                    Console.WriteLine($"  Could not parse: {arg}");
                return false;
            }
            return true;
        }

        static async Task<bool> _forget(string ssid = null)
        {
            ConnectionProfile profile = await Program.MainAdapter.NetworkAdapter.GetConnectedProfileAsync(); //Gets currently connected wlan profile
            //Don't know how to find ConnectionProfile when not connected to them.
            if (profile == null)
            {
                return false;
            }
            else if (profile.CanDelete)
            {
                ConnectionProfileDeleteStatus deleteStatus = await profile.TryDeleteAsync();
                if (deleteStatus == ConnectionProfileDeleteStatus.Success)
                    return true;
                Console.WriteLine(deleteStatus);
                return false;
            }
            Console.WriteLine("Profile can't be deleted");
            return false;
        }
    }
}
