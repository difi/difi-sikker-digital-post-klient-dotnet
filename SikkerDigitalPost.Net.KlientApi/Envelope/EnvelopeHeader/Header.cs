﻿using System.Xml;
using SikkerDigitalPost.Net.Domene.Entiteter;

namespace SikkerDigitalPost.Net.KlientApi.Envelope.EnvelopeHeader
{
    public class Header : XmlPart
    {
        public Header(XmlDocument dokument, Forsendelse forsendelse) : base(dokument, forsendelse)
        {
        }

        public override XmlElement Xml()
        {
            var header = XmlDocument.CreateElement("Header");
            header.AppendChild(SecurityElement());
            return header;
        }

        public XmlElement SecurityElement()
        {
            var securityElement = new Security(XmlDocument);
            return securityElement.Xml();
        }

        public XmlElement MessagingElement()
        {
            var messaging = new Messaging(XmlDocument, Forsendelse);
            return messaging.Xml();
        }
    }
}
