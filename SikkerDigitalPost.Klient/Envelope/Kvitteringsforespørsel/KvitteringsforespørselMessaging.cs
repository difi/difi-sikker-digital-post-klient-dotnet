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
using Difi.SikkerDigitalPost.Klient.Domene.Extensions;
using Difi.SikkerDigitalPost.Klient.Envelope.Abstract;
using Difi.SikkerDigitalPost.Klient.Utilities;

namespace Difi.SikkerDigitalPost.Klient.Envelope.Kvitteringsforespørsel
{
    internal class KvitteringsforespørselMessaging : EnvelopeXmlPart
    {
        public KvitteringsforespørselMessaging(EnvelopeSettings settings, XmlDocument context) : base(settings, context)
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

            messaging.AppendChild(SignalMessageElement());
            
            return messaging;
        }

        private XmlElement SignalMessageElement()
        {
            XmlElement signalMessage = Context.CreateElement("eb", "SignalMessage", Navnerom.EbXmlCore);

            signalMessage.AppendChild(MessageInfoElement());
            signalMessage.AppendChild(PullRequestElement());
            
            return signalMessage;
        }

        private XmlElement MessageInfoElement()
        {
            XmlElement messageInfo = Context.CreateElement("eb", "MessageInfo", Navnerom.EbXmlCore);
            {
                XmlElement timestamp = messageInfo.AppendChildElement("Timestamp", "eb", Navnerom.EbXmlCore, Context);
                timestamp.InnerText = DateTime.UtcNow.ToString(DateUtility.DateFormat);

                XmlElement messageId = messageInfo.AppendChildElement("MessageId", "eb", Navnerom.EbXmlCore, Context);
                messageId.InnerText = Settings.GuidHandler.StandardBusinessDocumentHeaderId;
            }
            return messageInfo;
        }

        private XmlElement PullRequestElement()
        {
            XmlElement pullRequest = Context.CreateElement("eb", "PullRequest", Navnerom.EbXmlCore);
            pullRequest.SetAttribute("mpc", Settings.Kvitteringsforespørsel.Mpc);
            return pullRequest;
        }
    }
}
