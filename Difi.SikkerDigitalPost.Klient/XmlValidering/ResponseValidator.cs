﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Difi.Felles.Utility;
using Difi.Felles.Utility.Security;
using Difi.SikkerDigitalPost.Klient.Domene.Exceptions;
using Difi.SikkerDigitalPost.Klient.Domene.Extensions;
using Difi.SikkerDigitalPost.Klient.Utilities;

namespace Difi.SikkerDigitalPost.Klient.XmlValidering
{
    /// <summary>
    ///     Inneholder funksjonalitet for å validere motatte svar fra meldingsformidleren.
    /// </summary>
    internal class ResponseValidator
    {
        private readonly XmlNamespaceManager _nsMgr;
        private XmlElement _signatureNode;
        private SignedXmlWithAgnosticId _signedXmlWithAgnosticId;

        /// <summary>
        ///     Oppretter en ny instanse av responsvalidatoren.
        /// </summary>
        /// <param name="sentMessage">Soap meldingen som har blitt sendt til meldingsformidleren.</param>
        /// <param name="responseMessage">
        ///     Et soap dokument i tekstform. Dette er svaret som har blitt motatt fra meldingsformidleren ved en
        ///     forsendelse av brev eller kvittering.
        /// </param>
        /// <param name="certificateChainValidator"></param>
        public ResponseValidator(XmlDocument sentMessage, XmlDocument responseMessage, CertificateChainValidator certificateChainValidator)
        {
            ResponseMessage = responseMessage;
            SentMessage = sentMessage;
            CertificateChainValidator = certificateChainValidator;

            _nsMgr = new XmlNamespaceManager(ResponseMessage.NameTable);
            _nsMgr.AddNamespace("env", NavneromUtility.SoapEnvelopeEnv12);
            _nsMgr.AddNamespace("wsse", NavneromUtility.WssecuritySecext10);
            _nsMgr.AddNamespace("ds", NavneromUtility.XmlDsig);
            _nsMgr.AddNamespace("eb", NavneromUtility.EbXmlCore);
            _nsMgr.AddNamespace("wsu", NavneromUtility.WssecurityUtility10);
            _nsMgr.AddNamespace("ebbp", NavneromUtility.EbppSignals);
            _nsMgr.AddNamespace("sbd", NavneromUtility.StandardBusinessDocumentHeader);
            _nsMgr.AddNamespace("difi", NavneromUtility.DifiSdpSchema10);
        }

        public XmlDocument ResponseMessage { get; internal set; }

        public XmlDocument SentMessage { get; internal set; }

        public CertificateChainValidator CertificateChainValidator { get; internal set; }

        public void ValidateMessageReceipt()
        {
            ValidateHeaderSignature();
            ValidateReceiptSignature();
        }

        public void ValidateTransportReceipt(GuidUtility guidUtility)
        {
            ValidateHeaderSignature();
            ValidateDigest(guidUtility);
        }

        public void ValidateEmptyQueueReceipt()
        {
            ValidateHeaderSignature();
        }

        private void ValidateHeaderSignature()
        {
            XmlNode responseRoot = ResponseMessage.DocumentElement;
            _signatureNode = (XmlElement) responseRoot.SelectSingleNode("/env:Envelope/env:Header/wsse:Security/ds:Signature", _nsMgr);
            _signedXmlWithAgnosticId = new SignedXmlWithAgnosticId(ResponseMessage);

            ValidateSignatureElements();
            ValidateSignatureAndCertificate("/env:Envelope/env:Header/wsse:Security/wsse:BinarySecurityToken");
        }

        private void ValidateReceiptSignature()
        {
            var standardBusinessDocumentNode =
                ResponseMessage.SelectSingleNode("/env:Envelope/env:Body/sbd:StandardBusinessDocument", _nsMgr);

            if (standardBusinessDocumentNode != null)
            {
                var standardBusinessDocument = XmlNodeToXmlDocument(standardBusinessDocumentNode);

                _signedXmlWithAgnosticId = new SignedXmlWithAgnosticId(standardBusinessDocument);
                _signatureNode = (XmlElement) standardBusinessDocument.SelectSingleNode("//ds:Signature", _nsMgr);

                ValidateSignatureAndCertificate("./ds:KeyInfo/ds:X509Data/ds:X509Certificate");
            }
            else
            {
                throw new SecurityException("Fant ikke StandardBusinessDocument-node. Prøvde du å validere en transportkvittering?");
            }
        }

        private static XmlDocument XmlNodeToXmlDocument(XmlNode standardBusinessDocument)
        {
            var sbd = new XmlDocument();
            sbd.LoadXml(standardBusinessDocument.OuterXml);
            return sbd;
        }

        private void ValidateSignatureAndCertificate(string path)
        {
            var certificate = new X509Certificate2(Convert.FromBase64String(_signatureNode.SelectSingleNode(path, _nsMgr).InnerText));
            ValidateResponseCertificateAndThrowIfInvalid(certificate);

            _signedXmlWithAgnosticId.LoadXml(_signatureNode);

            AsymmetricAlgorithm asymmetricAlgorithm;
            if (!_signedXmlWithAgnosticId.CheckSignatureReturningKey(out asymmetricAlgorithm))
                throw new SecurityException("Signaturen i motatt svar er ikke gyldig.");

            if (asymmetricAlgorithm.ToXmlString(false) != certificate.PublicKey.Key.ToXmlString(false))
                throw new SecurityException(
                    $"Sertifikatet som er benyttet for å validere signaturen er ikke det samme som er spesifisert i {path} elementet.");
        }

        private void ValidateResponseCertificateAndThrowIfInvalid(X509Certificate2 certificate)
        {
            var certificateValidationResult = CertificateChainValidator.Validate(certificate);

            if (certificateValidationResult.Type != CertificateValidationType.Valid)
            {
                throw new SecurityException($"Sertifikatet som ble mottatt i responsen er ikke gyldig. Grunnen er '{certificateValidationResult.Type.ToNorwegianString()}', med melding '{certificateValidationResult.Message}'");
            }
        }

        /// <summary>
        ///     Sjekker at motatt soap dokument har samme digest verdier for body og dokumentpakke i avsendt brev vha motatt
        ///     NonRepudiationInformation element.
        /// </summary>
        /// <param name="guidHandler">Samme guid handler som ble benyttet for å generere det avsendte brevet.</param>
        private void ValidateDigest(GuidUtility guidHandler)
        {
            var sentMessageDigestPath = "/env:Envelope/env:Header/wsse:Security/ds:Signature/ds:SignedInfo/ds:Reference[@URI='{0}']/ds:DigestValue";
            var receivedMessageDigestPath = "/env:Envelope/env:Header/eb:Messaging/eb:SignalMessage/eb:Receipt/ebbp:NonRepudiationInformation/ebbp:MessagePartNRInformation/ds:Reference[@URI='{0}']/ds:DigestValue";

            var ids = new List<string>
            {
                $"#{guidHandler.BodyId}",
                $"cid:{guidHandler.DokumentpakkeId}"
            };

            foreach (var id in ids)
            {
                string sentMessageDigest;
                string reveivedMessageDigest;

                var isValidDigest = ValidateDigestElement(sentMessageDigestPath, receivedMessageDigestPath, id, out sentMessageDigest, out reveivedMessageDigest);
                if (!isValidDigest)
                {
                    throw new SecurityException($"Digest verdien av uri {id} for sendt melding ({sentMessageDigest}) matcher ikke motatt digest ({reveivedMessageDigest}).");
                }
            }
        }

        private bool ValidateDigestElement(string sendtMeldingDigestSti, string mottattSvarDigestSti, string id, out string sentMessageDigest, out string receivedMessageDigest)
        {
            sentMessageDigest = null;
            receivedMessageDigest = null;

            var sentMessageSelectedNode = SentMessage.SelectSingleNode(string.Format(sendtMeldingDigestSti, id), _nsMgr);
            if (sentMessageSelectedNode != null)
                sentMessageDigest = sentMessageSelectedNode.InnerText;

            var responseMessageSelectedNode = ResponseMessage.SelectSingleNode(string.Format(mottattSvarDigestSti, id), _nsMgr);
            if (responseMessageSelectedNode != null)
                receivedMessageDigest = responseMessageSelectedNode.InnerText;

            return sentMessageDigest != null && responseMessageSelectedNode != null && sentMessageDigest == receivedMessageDigest;
        }

        /// <summary>
        ///     Sjekker at soap envelopen inneholder timestamp, body og messaging element med korrekt id og referanser i security
        ///     signaturen.
        /// </summary>
        private void ValidateSignatureElements()
        {
            string[] requiredSignatureElements = {"/env:Envelope/env:Header/wsse:Security/wsu:Timestamp", "/env:Envelope/env:Body", "/env:Envelope/env:Header/eb:Messaging"};

            foreach (var elementXPath in requiredSignatureElements)
            {
                XmlNodeList nodes;
                ResponseContainsRequiredSignatureNodes(elementXPath, out nodes);

                var elementId = ElementId(nodes);
                FindReferenceToNodeInSignatureElement(elementId, elementXPath);

                var targetNode = GetTargetNode(elementId);
                if (targetNode != nodes[0])
                    throw new SecurityException(
                        $"Signaturreferansen med id '{elementId}' må refererer til node med sti '{elementXPath}'");
            }
        }

        private XmlElement GetTargetNode(string elementId)
        {
            return _signedXmlWithAgnosticId.GetIdElement(ResponseMessage, elementId);
        }

        private void FindReferenceToNodeInSignatureElement(string elementId, string elementXPath)
        {
            var references = _signatureNode.SelectNodes($"./ds:SignedInfo/ds:Reference[@URI='#{elementId}']",
                _nsMgr);
            if (references == null || references.Count == 0)
                throw new SecurityException($"Kan ikke finne påkrevet refereanse til element '{elementXPath}' i signatur fra meldingsformidler.");
            if (references.Count > 1)
                throw new SecurityException($"Påkrevd refereanse til element '{elementXPath}' kan kun forekomme én gang i signatur. Ble funnet {references.Count} ganger.");
        }

        private static string ElementId(XmlNodeList nodes)
        {
            return nodes[0].Attributes["wsu:Id"].Value;
        }

        private void ResponseContainsRequiredSignatureNodes(string elementXPath, out XmlNodeList nodes)
        {
            nodes = ResponseMessage.SelectNodes(elementXPath, _nsMgr);
            if (nodes == null || nodes.Count == 0)
                throw new SecurityException($"Kan ikke finne påkrevet element '{elementXPath}' i svar fra meldingsformidler.");
            if (nodes.Count > 1)
                throw new SecurityException($"Påkrevet element '{elementXPath}' kan kun forekomme én gang i svar fra meldingsformidler. Ble funnet {nodes.Count} ganger.");
        }
    }
}