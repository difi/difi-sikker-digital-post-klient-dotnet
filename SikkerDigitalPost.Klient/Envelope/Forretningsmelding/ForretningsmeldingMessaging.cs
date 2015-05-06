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
using Difi.SikkerDigitalPost.Klient.Domene.Enums;
using Difi.SikkerDigitalPost.Klient.Domene.Extensions;
using Difi.SikkerDigitalPost.Klient.Envelope.Abstract;
using Difi.SikkerDigitalPost.Klient.Utilities;

namespace Difi.SikkerDigitalPost.Klient.Envelope.Forretningsmelding
{
    internal class ForretningsmeldingMessaging : EnvelopeXmlPart
    {
        public ForretningsmeldingMessaging(EnvelopeSettings settings, XmlDocument context) : base(settings, context)
        {
        }

        public override XmlNode Xml()
        {
            XmlElement messaging = Context.CreateElement("eb", "Messaging", Navnerom.EbXmlCore);
            messaging.SetAttribute("xmlns:wsu", Navnerom.WssecurityUtility10);
            XmlAttribute mustUnderstand = Context.CreateAttribute("env", "mustUnderstand", Navnerom.SoapEnvelopeEnv12);
            mustUnderstand.InnerText = "true";
            messaging.Attributes.Append(mustUnderstand);

            messaging.SetAttribute("Id", Navnerom.WssecurityUtility10, Settings.GuidHandler.EbMessagingId);

            messaging.AppendChild(UserMessageElement());
            
            return messaging;
        }

        private XmlElement UserMessageElement()
        {
            XmlElement userMessage = Context.CreateElement("eb", "UserMessage", Navnerom.EbXmlCore);
            userMessage.SetAttribute("mpc", Settings.Forsendelse.Mpc);

            userMessage.AppendChild(MessageInfoElement());
            userMessage.AppendChild(PartyInfoElement());
            userMessage.AppendChild(CollaborationInfoElement());
            userMessage.AppendChild(PayloadInfoElement());

            return userMessage;
        }

        private XmlElement MessageInfoElement()
        {
            XmlElement messageInfo = Context.CreateElement("eb", "MessageInfo", Navnerom.EbXmlCore);
            {
                XmlElement timestamp =  messageInfo.AppendChildElement("Timestamp", "eb", Navnerom.EbXmlCore, Context);
                timestamp.InnerText = DateTime.UtcNow.ToString(DateUtility.DateFormat);

                // http://begrep.difi.no/SikkerDigitalPost/1.0.2/transportlag/UserMessage/MessageInfo
                // Unik identifikator, satt av MSH. Kan med fordel benytte SBDH.InstanceIdentifier 
                XmlElement messageId = messageInfo.AppendChildElement("MessageId", "eb", Navnerom.EbXmlCore, Context);
                messageId.InnerText = Settings.GuidHandler.StandardBusinessDocumentHeaderId;
            }
            return messageInfo;
        }

        private XmlElement PartyInfoElement()
        {
            XmlElement partyInfo = Context.CreateElement("eb", "PartyInfo", Navnerom.EbXmlCore);
            {
                XmlElement from = partyInfo.AppendChildElement("From", "eb", Navnerom.EbXmlCore, Context);
                {
                    XmlElement partyId = from.AppendChildElement("PartyId", "eb", Navnerom.EbXmlCore, Context);
                    partyId.SetAttribute("type", "urn:oasis:names:tc:ebcore:partyid-type:iso6523:9908");
                    partyId.InnerText = Settings.Databehandler.Organisasjonsnummer.Iso6523();

                    XmlElement role = from.AppendChildElement("Role", "eb", Navnerom.EbXmlCore, Context);
                    role.InnerText = "urn:sdp:avsender";
                }

                XmlElement to = partyInfo.AppendChildElement("To", "eb", Navnerom.EbXmlCore, Context);
                {
                    XmlElement partyId = to.AppendChildElement("PartyId", "eb", Navnerom.EbXmlCore, Context);
                    partyId.SetAttribute("type", "urn:oasis:names:tc:ebcore:partyid-type:iso6523:9908");
                    partyId.InnerText = Settings.Konfigurasjon.MeldingsformidlerOrganisasjon.Iso6523();

                    XmlElement role = to.AppendChildElement("Role", "eb", Navnerom.EbXmlCore, Context);
                    role.InnerText = "urn:sdp:meldingsformidler";
                }
            }
            return partyInfo;
        }

        private XmlElement CollaborationInfoElement()
        {
            XmlElement collaborationInfo = Context.CreateElement("eb", "CollaborationInfo", Navnerom.EbXmlCore);
            {
                PMode currPmode = Settings.Forsendelse.PostInfo.PMode();
                var currPmodeRef = PModeHelper.EnumToRef(currPmode);

                XmlElement agreementRef = collaborationInfo.AppendChildElement("AgreementRef","eb",Navnerom.EbXmlCore,Context);
                agreementRef.InnerText = currPmodeRef;

                XmlElement service = collaborationInfo.AppendChildElement("Service", "eb", Navnerom.EbXmlCore, Context);
                service.InnerText = "SDP";

                XmlElement action = collaborationInfo.AppendChildElement("Action", "eb", Navnerom.EbXmlCore, Context);
                action.InnerText = currPmode.ToString();

                XmlElement conversationId = collaborationInfo.AppendChildElement("ConversationId", "eb", Navnerom.EbXmlCore, Context);
                conversationId.InnerText = Settings.Forsendelse.KonversasjonsId.ToString();
            }
            return collaborationInfo;
        }

        private XmlElement PayloadInfoElement()
        {
            // Mer info på http://begrep.difi.no/SikkerDigitalPost/1.0.2/transportlag/UserMessage/PayloadInfo

            XmlElement payloadInfo = Context.CreateElement("eb", "PayloadInfo", Navnerom.EbXmlCore);
            {
                XmlElement partInfoBody = payloadInfo.AppendChildElement("PartInfo", "eb", Navnerom.EbXmlCore, Context);
                partInfoBody.SetAttribute("href", "#"+Settings.GuidHandler.BodyId);

                XmlElement partInfoDokumentpakke = payloadInfo.AppendChildElement("PartInfo", "eb", Navnerom.EbXmlCore, Context);
                partInfoDokumentpakke.SetAttribute("href", "cid:"+Settings.GuidHandler.DokumentpakkeId);
                {
                    XmlElement partProperties = partInfoDokumentpakke.AppendChildElement("PartProperties", "eb", Navnerom.EbXmlCore, Context);
                    {
                        XmlElement propertyMimeType = partProperties.AppendChildElement("Property", "eb", Navnerom.EbXmlCore, Context);
                        propertyMimeType.SetAttribute("name", "MimeType");
                        propertyMimeType.InnerText = "application/cms";

                        XmlElement propertyContent = partProperties.AppendChildElement("Property", "eb", Navnerom.EbXmlCore, Context);
                        propertyContent.SetAttribute("name", "Content");
                        propertyContent.InnerText = "sdp:Dokumentpakke";
                    }
                }
            }
            return payloadInfo;
        }
    }
}
