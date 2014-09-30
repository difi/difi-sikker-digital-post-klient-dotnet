﻿using System.Xml;
using SikkerDigitalPost.Net.KlientApi.Envelope.Body;

namespace SikkerDigitalPost.Net.KlientApi.Envelope.EnvelopeBody
{
    public class BodyElement : XmlPart
    {
        private const string NsXmlnsEnv = "http://www.w3.org/2003/05/soap-envelope";
        private const string NsWsu = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        private const string NsWsuId = "";

        public BodyElement(XmlDocument dokument) : base(dokument)
        {
            
        }

        public override XmlElement Xml()
        {
            var bodyElement = XmlDocument.CreateElement("env", "body", NsXmlnsEnv);
            bodyElement.SetAttribute("xmlns:wsu", NsWsu);
            bodyElement.SetAttribute("id", NsWsu, NsWsuId);

            var sbdElement = new StandardBusinessDocument(XmlDocument);
            bodyElement.AppendChild(sbdElement.Xml());

            return bodyElement;
        }
    }
}
