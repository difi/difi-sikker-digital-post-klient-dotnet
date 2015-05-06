﻿/** 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *         http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Xml;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Kvitteringer.Forretning;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Kvitteringer.Transport;
using Difi.SikkerDigitalPost.Klient.Domene.Exceptions;

namespace Difi.SikkerDigitalPost.Klient
{
    internal class KvitteringFactory
    {
        public static Forretningskvittering GetForretningskvittering(string xml)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            if (IsLeveringskvittering(xmlDocument))
                return new Leveringskvittering(xmlDocument, NamespaceManager(xmlDocument));
            
            if (IsVarslingFeiletkvittering(xmlDocument))
                return new VarslingFeiletKvittering(xmlDocument, NamespaceManager(xmlDocument));
               
            if (IsFeilmelding(xmlDocument))
                return new Feilmelding(xmlDocument, NamespaceManager(xmlDocument));
            
            if (IsÅpningskvittering(xmlDocument))
                return new Åpningskvittering(xmlDocument, NamespaceManager(xmlDocument));

            if (IsMottaksKvittering(xmlDocument))
                return new Mottakskvittering(xmlDocument, NamespaceManager(xmlDocument));

            if (IsReturpost(xmlDocument))
                return new Returpostkvittering(xmlDocument, NamespaceManager(xmlDocument));

            if (IsTomKøKvittering(xmlDocument))
                return null;

            var ingenKvitteringstypeFunnetException = new XmlParseException(
                "Klarte ikke å finne ut hvilken type Forretningskvittering som ble tatt inn. Sjekk rådata for mer informasjon.")
            {
                Rådata = xml
            };

            throw ingenKvitteringstypeFunnetException;
        }

        public static Transportkvittering GetTransportkvittering(string xml)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            if (IsTransportOkKvittering(xmlDocument))
                return new TransportOkKvittering(xmlDocument, NamespaceManager(xmlDocument));

            if (IsTransportFeiletKvittering(xmlDocument))
                return new TransportFeiletKvittering(xmlDocument, NamespaceManager(xmlDocument));

            var exception = new XmlParseException(
                "Klarte ikke å finne ut hvilken type Transportkvittering som ble tatt inn. Sjekk rådata for mer informasjon.")
            {
                Rådata = xml
            };

            throw exception;
        }

        private static bool IsLeveringskvittering(XmlDocument document)
        {
            return DocumentHasNode(document, "ns9:levering");
        }

        private static bool IsVarslingFeiletkvittering(XmlDocument document)
        {
            return DocumentHasNode(document, "ns9:varslingfeilet");
        }

        private static bool IsFeilmelding(XmlDocument document)
        {
            return DocumentHasNode(document, "ns9:feil");
        }

        private static bool IsÅpningskvittering(XmlDocument document)
        {
            return DocumentHasNode(document, "ns9:aapning");
        }

        private static bool IsTomKøKvittering(XmlDocument document)
        {
            return DocumentHasNode(document, "ns6:Error[@shortDescription = 'EmptyMessagePartitionChannel']");
        }
        
        private static bool IsTransportOkKvittering(XmlDocument document)
        {
            return DocumentHasNode(document, "ns6:Receipt");
        }

        private static bool IsTransportFeiletKvittering(XmlDocument document)
        {
            return DocumentHasNode(document, "env:Fault");
        }

        private static bool IsMottaksKvittering(XmlDocument document)
        {
            return DocumentHasNode(document, "ns9:mottak");
        }

        private static bool IsReturpost(XmlDocument document)
        {
            return DocumentHasNode(document, "ns9:returpost");
        }

        private static bool DocumentHasNode(XmlDocument document, string node)
        {
            return DocumentNode(document, node) != null;
        }

        private static XmlNode DocumentNode(XmlDocument document, string node)
        {
            var rot = document.DocumentElement;
            string nodeString = String.Format("//{0}", node);
            var targetNode = rot.SelectSingleNode(nodeString, NamespaceManager(document));

            return targetNode;
        }

        private static XmlNamespaceManager NamespaceManager(XmlDocument document)
        {
            XmlNamespaceManager manager = new XmlNamespaceManager(document.NameTable);
            manager.AddNamespace("env", Navnerom.SoapEnvelopeEnv12);
            manager.AddNamespace("eb", Navnerom.EbXmlCore);
            manager.AddNamespace("ns3", Navnerom.StandardBusinessDocumentHeader);
            manager.AddNamespace("ns5", Navnerom.XmlDsig);
            manager.AddNamespace("ns6", Navnerom.EbXmlCore);
            manager.AddNamespace("ns9", Navnerom.DifiSdpSchema10);
            return manager;
        }
    }
}
