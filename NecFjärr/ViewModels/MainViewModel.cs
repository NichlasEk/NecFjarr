using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia.Threading;
using ReactiveUI;

namespace NecFjärr.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private static readonly string IpSavePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "nec_last_ip.txt");

        private string _ipAddress = "192.168.32.1";
        public string IpAddress
        {
            get => _ipAddress;
            set => this.RaiseAndSetIfChanged(ref _ipAddress, value);
        }

        public ObservableCollection<string> Log { get; } = new();

        public ReactiveCommand<string, Unit> SendSetCommand { get; }
        public ReactiveCommand<Unit, Unit> SendPowerOnCommand { get; }
        public ReactiveCommand<Unit, Unit> SendPowerOffCommand { get; }
        public ReactiveCommand<Unit, Unit> ScanForNecTvCommand { get; }

        public MainViewModel()
        {
            // Läs in senast sparade IP-adress vid uppstart
            if (File.Exists(IpSavePath))
            {
                try
                {
                    string savedIp = File.ReadAllText(IpSavePath).Trim();
                    if (!string.IsNullOrWhiteSpace(savedIp))
                        IpAddress = savedIp;
                }
                catch (Exception ex)
                {
                    Log.Add($"⚠️ Kunde inte läsa sparad IP: {ex.Message}");
                }
            }

            // Skapa kommandon
            SendSetCommand = ReactiveCommand.CreateFromTask<string>(SendSetPayload);
            SendPowerOnCommand = ReactiveCommand.CreateFromTask(() =>
            SendRawHex("01304130413043024332303344363030303103730D"));
            SendPowerOffCommand = ReactiveCommand.CreateFromTask(() =>
            SendRawHex("01304130413043024332303344363030303403760D"));
            ScanForNecTvCommand = ReactiveCommand.CreateFromTask(ScanForNecTv);
        }

        private async Task SendSetPayload(string payload)
        {
            try
            {
                byte[] packet = BuildSetPacket(payload);
                Log.Add($"⏩ Skickar: {BitConverter.ToString(packet).Replace("-", "")}");

                using TcpClient client = new();
                await client.ConnectAsync(IpAddress, 7142);
                using NetworkStream stream = client.GetStream();

                await stream.WriteAsync(packet, 0, packet.Length);

                byte[] response = new byte[1024];
                int bytesRead = await stream.ReadAsync(response, 0, response.Length);
                string hexResponse = BitConverter.ToString(response, 0, bytesRead).Replace("-", "");

                Log.Add($"✅ Svar: {hexResponse}");
            }
            catch (Exception ex)
            {
                Log.Add($"❌ Fel: {ex.Message}");
            }
        }

        private async Task SendRawHex(string hexString)
        {
            try
            {
                byte[] packet = ConvertHexStringToBytes(hexString);
                Log.Add($"⏩ Skickar (rå): {hexString}");

                using TcpClient client = new();
                await client.ConnectAsync(IpAddress, 7142);
                using NetworkStream stream = client.GetStream();

                await stream.WriteAsync(packet, 0, packet.Length);

                byte[] response = new byte[1024];
                int bytesRead = await stream.ReadAsync(response, 0, response.Length);
                string hexResponse = BitConverter.ToString(response, 0, bytesRead).Replace("-", "");

                Log.Add($"✅ Svar: {hexResponse}");
            }
            catch (Exception ex)
            {
                Log.Add($"❌ Fel (rå): {ex.Message}");
            }
        }

        private byte[] ConvertHexStringToBytes(string hex)
        {
            int len = hex.Length;
            byte[] bytes = new byte[len / 2];
            for (int i = 0; i < len; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        private byte[] BuildSetPacket(string payload)
        {
            byte soh = 0x01;
            byte[] header = Encoding.ASCII.GetBytes("0A0E0A");
            byte stx = 0x02;
            byte[] command = Encoding.ASCII.GetBytes("0060");
            byte[] payloadBytes = Encoding.ASCII.GetBytes(payload);
            byte etx = 0x03;

            byte[] raw = Combine(
                new byte[] { soh },
                header,
                new byte[] { stx },
                command,
                payloadBytes,
                new byte[] { etx }
            );

            byte bcc = CalculateBCC(raw, 1); // utan SOH
            byte cr = 0x0D;

            return Combine(raw, new byte[] { bcc, cr });
        }

        private byte CalculateBCC(byte[] data, int startIndex)
        {
            byte bcc = data[startIndex];
            for (int i = startIndex + 1; i < data.Length; i++)
                bcc ^= data[i];
            return bcc;
        }

        private byte[] Combine(params byte[][] arrays)
        {
            int totalLength = 0;
            foreach (var a in arrays)
                totalLength += a.Length;

            byte[] result = new byte[totalLength];
            int offset = 0;

            foreach (var a in arrays)
            {
                Buffer.BlockCopy(a, 0, result, offset, a.Length);
                offset += a.Length;
            }

            return result;
        }

        private IPAddress? GetLocalIp()
        {
            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                try
                {
                    if (netInterface.OperationalStatus != OperationalStatus.Up)
                        continue;

                    var properties = netInterface.GetIPProperties();
                    foreach (var addr in properties.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(addr.Address))
                        {
                            AddLog($"🌐 Lokal IP hittad: {addr.Address}");
                            return addr.Address;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"⚠️ Undantag i GetLocalIp: {ex.Message}");
                }
            }

            return null;
        }


        private async Task ScanForNecTv()
        {
            var localIp = GetLocalIp();
            if (localIp == null)
            {
                AddLog("❌ Kunde inte bestämma lokal IP-adress.");
                return;
            }

            string subnet = string.Join(".", localIp.GetAddressBytes()[..3]);
            AddLog($"🔍 Söker i subnät: {subnet}.x");

            for (int i = 1; i <= 254; i++)
            {
                string targetIp = $"{subnet}.{i}";

                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(targetIp, 200);

                    if (reply.Status == IPStatus.Success)
                    {
                        using TcpClient client = new();
                        var connectTask = client.ConnectAsync(targetIp, 7142);
                        var timeoutTask = Task.Delay(200);

                        var completed = await Task.WhenAny(connectTask, timeoutTask);
                        if (completed == connectTask && client.Connected)
                        {
                            IpAddress = targetIp;
                            AddLog($"✅ Hittade NEC-TV på: {targetIp}");

                            try
                            {
                                File.WriteAllText(IpSavePath, targetIp);
                            }
                            catch (Exception ex)
                            {
                                AddLog($"⚠️ Kunde inte spara IP: {ex.Message}");
                            }

                            return;
                        }
                    }
                }
                catch
                {
                    // Ignorera IP som inte svarar
                }
            }

            AddLog("❌ Ingen NEC-TV hittades.");
        }



        private void AddLog(string message)
        {
            Dispatcher.UIThread.Post(() => Log.Add(message));
        }

        private async Task PingHost(string ip)
        {
            try
            {
                using Ping ping = new();
                await ping.SendPingAsync(ip, 300);
            }
            catch { /* Ignorera ping-fel */ }
        }

        private string? GetLocalIPv4()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        return ip.Address.ToString();
                }
            }
            return null;
        }
    }
}
