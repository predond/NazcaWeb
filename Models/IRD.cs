using Modbus.Device;
using System.Net.Sockets;

namespace NazcaWeb.Models
{
    public class IRD
    {
        public string DeviceIP { get; private set; }
        public int DevicePort { get; private set; }
        private Dictionary<string, ushort> Commands { get; set; }

        public IRD(string DeviceIP, int DevicePort = 502)
        {
            this.DeviceIP = DeviceIP;
            this.DevicePort = DevicePort;
            Commands = new Dictionary<string, ushort>();
            InitializeCommandsAsync().Wait();
        }

        private Task InitializeCommandsAsync()
        {
            try
            {
                using (var writer = new StreamWriter("errors.txt", true))
                using (var client = new TcpClient(DeviceIP, DevicePort))
                {
                    client.SendTimeout = 5000;
                    var master = ModbusIpMaster.CreateIp(client);
                    master.WriteSingleRegister(1, 1103, 1);

                    var commandCount = master.ReadHoldingRegisters(1, 1265, 1);
                    for (ushort command = 1; command <= commandCount[0]; command++)
                    {
                        var commandNumber = command;//master.ReadHoldingRegisters(1, 1103, 1);
                        var commandNameLength = master.ReadHoldingRegisters(1, 1213, 1);
                        var commandName = ReadCommandName(master, commandNameLength.Length > 0 ? commandNameLength[0] : (ushort)0);
                        var transmitterName = ReadTransmitterName(master);

                        var fullCommandName = $"{transmitterName}.{commandName}";
                        Commands[fullCommandName] = commandNumber;

                        master.WriteSingleRegister(1, 1103, (ushort)(command + 1));
                        Thread.Sleep(170);
                    }
                }
            }
            catch (Exception e)
            {
                using (var writer = new StreamWriter("errors.txt", true))
                {
                    writer.WriteLine(e.Message + "\n" + e.StackTrace);
                }
            }

            return Task.CompletedTask;
        }

        private string ReadCommandName(ModbusIpMaster master, ushort commandNameLength)
        {
            var commandNameRegisters = new List<ushort>();
            var numRegisters = commandNameLength;

            for (ushort i = 0; i < numRegisters; i++)
            {
                var registerValues = master.ReadHoldingRegisters(1, (ushort)(1214 + i), 1);
                commandNameRegisters.AddRange(registerValues);
            }

            var commandNameBytes = new byte[commandNameLength * 2];
            Buffer.BlockCopy(commandNameRegisters.ToArray(), 0, commandNameBytes, 0, commandNameBytes.Length);

            // Odwrócenie kolejności bajtów w każdej parze
            for (int i = 0; i < commandNameBytes.Length; i += 2)
            {
                byte temp = commandNameBytes[i];
                commandNameBytes[i] = commandNameBytes[i + 1];
                commandNameBytes[i + 1] = temp;
            }

            var commandName = System.Text.Encoding.ASCII.GetString(commandNameBytes).TrimEnd('\0');
            return commandName;
        }

        private string ReadTransmitterName(ModbusIpMaster master)
        {
            var transmitterNameLength = master.ReadHoldingRegisters(1, 1162, 1);
            var transmitterNameRegisters = new List<ushort>();
            var numRegisters = transmitterNameLength[0];

            for (ushort i = 0; i < numRegisters; i++)
            {
                var registerValues = master.ReadHoldingRegisters(1, (ushort)(1163 + i), 1);
                transmitterNameRegisters.AddRange(registerValues);
            }

            var transmitterNameBytes = new byte[transmitterNameLength[0] * 2];
            Buffer.BlockCopy(transmitterNameRegisters.ToArray(), 0, transmitterNameBytes, 0, transmitterNameBytes.Length);

            // Odwrócenie kolejności bajtów w każdej parze
            for (int i = 0; i < transmitterNameBytes.Length; i += 2)
            {
                byte temp = transmitterNameBytes[i];
                transmitterNameBytes[i] = transmitterNameBytes[i + 1];
                transmitterNameBytes[i + 1] = temp;
            }

            var transmitterName = System.Text.Encoding.ASCII.GetString(transmitterNameBytes).TrimEnd('\0');
            return transmitterName;
        }

        public Dictionary<string, List<string>> GetCommands()
        {
            var output = new Dictionary<string, List<string>>();
            foreach (var command in Commands.Keys)
            {
                var device = command.Split('.')[0];
                var commandName = command.Split('.')[1];
                if (!output.ContainsKey(device))
                    output.Add(device, new List<string>());
                output[device].Add(commandName);
            }

            return output;
        }

        public bool SendQuery(string command)
        {
            if (Commands.ContainsKey(command))
                return SendQuery(1, 1103, Commands[command]) && SendQuery(1, 1104, 11);
            else
                return false;
        }

        public bool SendQuery(byte slaveAddress, ushort registerAddress, ushort value)
        {
            using (var client = new TcpClient(DeviceIP, DevicePort))
            {
                client.SendTimeout = 5000;
                var master = ModbusIpMaster.CreateIp(client);

                try
                {
                    master.WriteSingleRegister(slaveAddress, registerAddress, value);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }

}
