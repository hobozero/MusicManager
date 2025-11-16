using SonosManagerApi.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace SonosManagerApi.Application
{
    public class SonosDiscovery
    {
        private static IEnumerable<Device>? DEVICES;
        private static readonly object _lock = new();
        private const string MulticastAddress = "239.255.255.250";
        private const int MulticastPort = 1900;
        private const string MSearchRequest = "M-SEARCH * HTTP/1.1\r\n" +
                                              "HOST: 239.255.255.250:1900\r\n" +
                                              "MAN: \"ssdp:discover\"\r\n" +
                                              "MX: 1\r\n" +
                                              "ST: urn:schemas-upnp-org:device:ZonePlayer:1\r\n\r\n";

        public async Task<IEnumerable<Device>> DiscoverSonosDevicesAsync(bool reset=false)
        {
            lock (_lock) { 
                if (reset)
                {
                    DEVICES = null;
                }
                if (DEVICES != null) return DEVICES;
            }

            var locations = new List<string>();

            NetworkInterface correctAdapter = GetNetworkAdapterForNetwork("192.168.0.0", "255.255.255.0");
            if (correctAdapter == null)
            {
                Console.WriteLine("Correct network adapter not found.");
                return new List<Device>();
            }

            // Get the adapter index
            int adapterIndex = correctAdapter.GetIPProperties().GetIPv4Properties().Index;

            using (UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0)))
            {
                udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(adapterIndex));

                // Join the multicast group
                udpClient.JoinMulticastGroup(IPAddress.Parse(MulticastAddress));

                IPEndPoint multicastEndPoint = new IPEndPoint(IPAddress.Parse(MulticastAddress), MulticastPort);
                byte[] requestBytes = Encoding.UTF8.GetBytes(MSearchRequest);

                // Send the M-SEARCH request
                Console.WriteLine("Sending M-SEARCH request...");
                await udpClient.SendAsync(requestBytes, requestBytes.Length, multicastEndPoint);

                // Set a timeout for receiving responses
                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    try
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            Console.WriteLine("Waiting for response...");
                            UdpReceiveResult result = await udpClient.ReceiveAsync().WithCancellation(cts.Token);
                            string response = Encoding.UTF8.GetString(result.Buffer);

                            Console.WriteLine("Received response: " + response);

                            // Check if the response contains the Sonos device information
                            if (response.Contains("Sonos"))
                            {
                                string location = ExtractLocation(response);
                                if (!string.IsNullOrEmpty(location) && !locations.Exists(d => d == location))
                                {
                                    locations.Add(location);
                                    Console.WriteLine("Found Sonos device at location: " + location);
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Timeout reached, stop receiving
                        Console.WriteLine("Timeout reached, stopping reception.");
                    }
                }
            }

            var zoneTest = new List<Zone>();

            var firstLoacation = locations.FirstOrDefault();
            if (firstLoacation != null)
            {
                var ip = new Uri(firstLoacation).Host;
                zoneTest.AddRange(await GetZones(ip));
            }
            lock (_lock)
            {
                DEVICES = zoneTest.SelectMany(z => z.Devices.Where(d => d.ZoneLeader));
            }

            return DEVICES;
        }


        private static NetworkInterface GetNetworkAdapterForNetwork(string networkAddress, string subnetMask)
        {
            IPAddress network = IPAddress.Parse(networkAddress);
            IPAddress mask = IPAddress.Parse(subnetMask);

            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation ip in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        IPAddress ipAddress = ip.Address;
                        IPAddress ipMask = ip.IPv4Mask;

                        if (ipMask != null && AreInSameSubnet(ipAddress, network, mask))
                        {
                            return adapter;
                        }
                    }
                }
            }

            return null;
        }

        private static bool AreInSameSubnet(IPAddress address, IPAddress network, IPAddress mask)
        {
            byte[] addressBytes = address.GetAddressBytes();
            byte[] networkBytes = network.GetAddressBytes();
            byte[] maskBytes = mask.GetAddressBytes();

            if (addressBytes.Length != networkBytes.Length || networkBytes.Length != maskBytes.Length)
            {
                throw new ArgumentException("Lengths of IP address and/or subnet mask do not match.");
            }

            for (int i = 0; i < addressBytes.Length; i++)
            {
                if ((addressBytes[i] & maskBytes[i]) != (networkBytes[i] & maskBytes[i]))
                {
                    return false;
                }
            }

            return true;
        }


        private string ExtractLocation(string response)
        {
            string locationHeader = "LOCATION: ";
            int locationIndex = response.IndexOf(locationHeader, StringComparison.OrdinalIgnoreCase);
            if (locationIndex >= 0)
            {
                int startIndex = locationIndex + locationHeader.Length;
                int endIndex = response.IndexOf("\r\n", startIndex);
                if (endIndex > startIndex)
                {
                    return response.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
            return null;
        }

        private async Task<Device> GetDeviceDetails(string location)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string response = await client.GetStringAsync(location);
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(response);

                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                    nsmgr.AddNamespace("d", "urn:schemas-upnp-org:device-1-0");

                    XmlNode roomNameNode = doc.SelectSingleNode("//d:roomName", nsmgr);

                    var device = new Device()
                    {
                        IPAddress = new Uri(location).Host,
                        Location = location
                    };

                    if (roomNameNode != null)
                    {
                        device.RoomName = roomNameNode.InnerText;
                    }

                    return device;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching room name from {location}: {ex.Message}");
                }
            }
            return null;
        }

        private async Task<List<Zone>> GetZones(string ipAddress)
        {
            var zones = new List<Zone>();
         
            using (HttpClient client = new HttpClient())
            {
                // Construct the URL for the GetZoneGroupState action
                string zoneGroupTopologyUrl = $"http://{ipAddress}:1400/ZoneGroupTopology/Control";

                // Construct the SOAP request for GetZoneGroupState
                string soapRequest = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/""
                            s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
                  <s:Body>
                    <u:GetZoneGroupState xmlns:u=""urn:schemas-upnp-org:service:ZoneGroupTopology:1"" />
                  </s:Body>
                </s:Envelope>";

                // Set up the HTTP request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, zoneGroupTopologyUrl);
                request.Headers.Add("SOAPACTION", "\"urn:schemas-upnp-org:service:ZoneGroupTopology:1#GetZoneGroupState\"");
                request.Content = new StringContent(soapRequest);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml");

                // Send the request and get the response
                HttpResponseMessage response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                // Load the response XML
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(responseContent);

                // Parse the ZoneGroupState XML
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("s", "http://schemas.xmlsoap.org/soap/envelope/");
                nsmgr.AddNamespace("u", "urn:schemas-upnp-org:service:ZoneGroupTopology:1");

                XmlNode zoneGroupStateNode = doc.SelectSingleNode("//u:GetZoneGroupStateResponse/ZoneGroupState", nsmgr);
                if (zoneGroupStateNode != null)
                {
                    XmlDocument zoneGroupStateDoc = new XmlDocument();
                    zoneGroupStateDoc.LoadXml(zoneGroupStateNode.InnerText);

                    XmlNamespaceManager zoneGroupNsmgr = new XmlNamespaceManager(zoneGroupStateDoc.NameTable);
                    //zoneGroupNsmgr.AddNamespace("g", "urn:schemas-upnp-org:group:1-0");

                    XmlNodeList zoneGroups = zoneGroupStateDoc.SelectNodes("//ZoneGroup", zoneGroupNsmgr);

                    foreach (XmlNode zoneGroup in zoneGroups)
                    {
                        XmlNode coordinatorNode = zoneGroup.Attributes["Coordinator"];
                        var coordinatorId = coordinatorNode.Value;

                        var zone = new Zone();
                        zones.Add(zone);

                        var memberNodes = zoneGroup.SelectNodes("ZoneGroupMember");
                        foreach (XmlNode memberNode in memberNodes)
                        {
                            var location = memberNode.Attributes["Location"].Value; ;
                            var memberId = memberNode.Attributes["UUID"].Value;
                            var roomName = memberNode.Attributes["ZoneName"].Value;

                            var device = new Device()
                            {
                                IPAddress = new Uri(location).Host,
                                Location = location,
                                 ZoneLeader = memberId == coordinatorId,
                                 RoomName = roomName
                            };

                            zone.Devices.Add(device);
                        }
                    }
                }

                return zones;
            }
        }
    }

    public static class TaskExtensions
    {
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();

            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task;
        }
    }
}
