using SonosManagerApi.Models;
using SonosManagerAPI.Models;
using System.Text;
using System.Xml;

public class SonosAdapter
{
    public async Task<XmlDocument> SendSoapRequest(string sonosIp, string action, string soapBody)
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

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(responseXml);

            return doc;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        return null;
    }

    public async Task<TransportInfo> GetTransportInfo(string sonosIp)
    {
        string action = "GetTransportInfo";
        string soapBody = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               soap:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
  <soap:Body>
    <u:GetTransportInfo xmlns:u=""urn:schemas-upnp-org:service:AVTransport:1"">
      <InstanceID>0</InstanceID>
    </u:GetTransportInfo>
  </soap:Body>
</soap:Envelope>";

        XmlDocument responseDoc = await SendSoapRequest(sonosIp, action, soapBody);

        if (responseDoc != null)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(responseDoc.NameTable);
            nsmgr.AddNamespace("u", "urn:schemas-upnp-org:service:AVTransport:1");

            string currentTransportState = responseDoc.SelectSingleNode("//CurrentTransportState", nsmgr)?.InnerText;
            string currentTransportStatus = responseDoc.SelectSingleNode("//CurrentTransportStatus", nsmgr)?.InnerText;
            string currentSpeed = responseDoc.SelectSingleNode("//CurrentSpeed", nsmgr)?.InnerText;

            return new TransportInfo
            {
                CurrentTransportState = currentTransportState,
                CurrentTransportStatus = currentTransportStatus,
                CurrentSpeed = currentSpeed
            };
        }

        return null;
    }


    public async Task PlayTrack(string sonosIp, string trackUri)
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

    public async Task<Track> ReadTrack(string sonosIp)
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

        var doc = await SendSoapRequest(sonosIp, soapAction, soapBody);
        var nsmgr = MgrFactory(doc);
        var trackDto = new Track();

        XmlNode trackMetaDataNode = doc.SelectSingleNode("//u:GetPositionInfoResponse/TrackMetaData", nsmgr);
        if (trackMetaDataNode != null)
        {
            XmlDocument metaDataDoc = new XmlDocument();
            if (trackMetaDataNode.InnerText == "NOT_IMPLEMENTED" || trackMetaDataNode.InnerText == string.Empty)
            {
                return Track.CreateEmpty();
            }
            metaDataDoc.LoadXml(trackMetaDataNode.InnerText);

            XmlNode titleNode = metaDataDoc.SelectSingleNode("//dc:title", nsmgr);
            XmlNode artistNode = metaDataDoc.SelectSingleNode("//dc:creator", nsmgr);
            XmlNode albumNode = metaDataDoc.SelectSingleNode("//r:albumArtist", nsmgr);
            XmlNode pathNode = metaDataDoc.SelectSingleNode("//didl:res", nsmgr);
            XmlNode fileNameNode = metaDataDoc.SelectSingleNode("//didl:res", nsmgr);

            if (titleNode != null)
            {
                trackDto.Title = titleNode.InnerText;
            }

            if (artistNode != null)
            {
                trackDto.Artist = artistNode.InnerText;
            }

            if (albumNode != null)
            {
                trackDto.Album = albumNode.InnerText;
            }

            if (pathNode != null)
            {
                trackDto.Path = pathNode.InnerText;
            }

            if (fileNameNode != null)
            {
                trackDto.FileName = System.IO.Path.GetFileName(fileNameNode.InnerText);
            }
            if (pathNode.Attributes["duration"] != null && TimeSpan.TryParse(pathNode.Attributes["duration"].Value, out TimeSpan totalPlaytime))
            {
                trackDto.TotalPlayTime = totalPlaytime;
            }
        }

        XmlNode currentPlaytimeNode = doc.SelectSingleNode("//u:GetPositionInfoResponse/RelTime", nsmgr);
        if (currentPlaytimeNode != null && TimeSpan.TryParse(currentPlaytimeNode.InnerText, out TimeSpan currentPlaytime))
        {
            trackDto.CurrentPlayTime = currentPlaytime;
        }

        return trackDto;
    }

    public async Task PlayTrack(string sonosIp)
    {
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

    public async Task PauseTrack(string sonosIp)
    {
        string pauseSoap = @"<?xml version=""1.0""?>
        <s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
          <s:Body>
            <u:Pause xmlns:u=""urn:schemas-upnp-org:service:AVTransport:1"">
              <InstanceID>0</InstanceID>
              <Speed>1</Speed>
            </u:Pause>
          </s:Body>
        </s:Envelope>";

        await SendSoapRequest(sonosIp, "Pause", pauseSoap);
    }

    public async Task Seek(string sonosIp, TimeSpan time)
    {
        string formattedTime = $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";

        string seekSoap = $@"<?xml version=""1.0""?>
        <s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
          <s:Body>
            <u:Seek xmlns:u=""urn:schemas-upnp-org:service:AVTransport:1"">
              <InstanceID>0</InstanceID>
              <Unit>REL_TIME</Unit>
              <Target>{formattedTime}</Target>
            </u:Seek>
          </s:Body>
        </s:Envelope>";

        await SendSoapRequest(sonosIp, "Seek", seekSoap);
    }

    public async Task Skip(string sonosIp)
    {
        string nextTrackSoap = $@"<?xml version=""1.0""?>
    <s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
      <s:Body>
        <u:Next xmlns:u=""urn:schemas-upnp-org:service:AVTransport:1"">
          <InstanceID>0</InstanceID>
        </u:Next>
      </s:Body>
    </s:Envelope>";

        await SendSoapRequest(sonosIp, "Next", nextTrackSoap);
    }

    private XmlNamespaceManager MgrFactory(XmlDocument doc)
    {
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
        nsmgr.AddNamespace("s", "http://schemas.xmlsoap.org/soap/envelope/");
        nsmgr.AddNamespace("u", "urn:schemas-upnp-org:service:AVTransport:1");
        nsmgr.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
        nsmgr.AddNamespace("upnp", "urn:schemas-upnp-org:metadata-1-0/upnp/");
        nsmgr.AddNamespace("r", "urn:schemas-rinconnetworks-com:metadata-1-0/");
        nsmgr.AddNamespace("didl", "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/");

        return nsmgr;
    }
}