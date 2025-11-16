using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string sonosIp = "192.168.0.9"; 
        string trackExample = "x-file-cifs://192.168.0.31/media/FoolsParadise/fp090530.mp4";
        await ReadTrack(sonosIp);

        //await PlayTrack(sonosIp, trackExample);

        await ReadTrack(sonosIp);

        Console.ReadLine();
    }

    static async Task SendSoapRequest(string sonosIp, string action, string soapBody)   
    {
        string url = $"http://{sonosIp}:1400/MediaRenderer/AVTransport/Control";
        string soapAction = $"urn:schemas-upnp-org:service:AVTransport:1#{action}";

        using HttpClient client = new HttpClient();
        var content = new StringContent(soapBody, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPACTION", soapAction);

        try
        {
            HttpResponseMessage response = await client.PostAsync(url, content);
            string responseXml = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"{action} Response:");
            Console.WriteLine(responseXml);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task PlayTrack(string sonosIp, string trackUri)
    {
        string setUriSoap = $@"<?xml version=""1.0""?>
        <s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
          <s:Body>
            <u:SetAVTransportURI xmlns:u=""urn:schemas-upnp-org:service:AVTransport:1"">
              <InstanceID>0</InstanceID>
              <CurrentURI>{trackUri}</CurrentURI>
              <CurrentURIMetaData></CurrentURIMetaData>
            </u:SetAVTransportURI>
          </s:Body>
        </s:Envelope>";

        await SendSoapRequest(sonosIp, "SetAVTransportURI", setUriSoap);

        string playSoap = @"<?xml version=""1.0""?>
        <s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
          <s:Body>
            <u:Play xmlns:u=""urn:schemas-upnp-org:service:AVTransport:1"">
              <InstanceID>0</InstanceID>
              <Speed>1</Speed>
            </u:Play>
          </s:Body>
        </s:Envelope>";

        await SendSoapRequest(sonosIp, "Play", playSoap);
    }

    private static async Task ReadTrack(string sonosIp)
    {
        string soapAction = "GetPositionInfo";

        string soapBody = @"<?xml version=""1.0""?>
        <s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
          <s:Body>
            <u:GetPositionInfo xmlns:u=""urn:schemas-upnp-org:service:AVTransport:1"">
              <InstanceID>0</InstanceID>
            </u:GetPositionInfo>
          </s:Body>
        </s:Envelope>";

        await SendSoapRequest(sonosIp, soapAction, soapBody);
    }
}
