/** 
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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Difi.SikkerDigitalPost.Klient.AsicE;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Akt�rer;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Kvitteringer;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Kvitteringer.Forretning;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Kvitteringer.Transport;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Post;
using Difi.SikkerDigitalPost.Klient.Domene.Exceptions;
using Difi.SikkerDigitalPost.Klient.Envelope;
using Difi.SikkerDigitalPost.Klient.Envelope.Forretningsmelding;
using Difi.SikkerDigitalPost.Klient.Envelope.Kvitteringsbekreftelse;
using Difi.SikkerDigitalPost.Klient.Envelope.Kvitteringsforesp�rsel;
using Difi.SikkerDigitalPost.Klient.Utilities;
using Difi.SikkerDigitalPost.Klient.XmlValidering;

namespace Difi.SikkerDigitalPost.Klient.Api
{
    public class SikkerDigitalPostKlient : ISikkerDigitalPostKlient
    {
        private readonly Databehandler _databehandler;
        private readonly Klientkonfigurasjon _klientkonfigurasjon;

        /// <param name="databehandler">
        /// Virksomhet (offentlig eller privat) som har en kontraktfestet avtale med Avsender med 
        /// form�l � dekke hele eller deler av prosessen med � formidle en digital postmelding fra 
        /// Behandlingsansvarlig til Meldingsformidler. Det kan v�re flere databehandlere som har 
        /// ansvar for forskjellige steg i prosessen med � formidle en digital postmelding.
        /// </param>
        /// <remarks>
        /// Se <a href="http://begrep.difi.no/SikkerDigitalPost/forretningslag/Aktorer">oversikt over akt�rer</a>
        /// </remarks>
        public SikkerDigitalPostKlient(Databehandler databehandler)
            : this(databehandler, new Klientkonfigurasjon())
        {
        }

        /// <param name="databehandler">
        /// Virksomhet (offentlig eller privat) som har en kontraktfestet avtale med Avsender med 
        /// form�l � dekke hele eller deler av prosessen med � formidle en digital postmelding fra 
        /// Behandlingsansvarlig til Meldingsformidler. Det kan v�re flere databehandlere som har 
        /// ansvar for forskjellige steg i prosessen med � formidle en digital postmelding.
        /// </param>
        /// <param name="klientkonfigurasjon">Klientkonfigurasjon for klienten. Brukes for � sette parametere
        /// som proxy, timeout og URI til meldingsformidler. For � bruke standardkonfigurasjon, lag
        /// SikkerDigitalPostKlient uten Klientkonfigurasjon som parameter.</param>
        /// <remarks>
        /// Se <a href="http://begrep.difi.no/SikkerDigitalPost/forretningslag/Aktorer">oversikt over akt�rer</a>
        /// </remarks>
        public SikkerDigitalPostKlient(Databehandler databehandler, Klientkonfigurasjon klientkonfigurasjon)
        {
            _databehandler = databehandler;
            _klientkonfigurasjon = klientkonfigurasjon;
            Logging.Initialize(klientkonfigurasjon);
            FileUtility.BasePath = klientkonfigurasjon.StandardLoggSti;
        }

        /// <summary>
        /// Sender en forsendelse til meldingsformidler. Dersom noe feilet i sendingen til meldingsformidler, vil det kastes en exception.
        /// </summary>
        /// <param name="forsendelse">Et objekt som har all informasjon klar til � kunne sendes (mottakerinformasjon, sertifikater, vedlegg mm), enten digitalt eller fysisk.</param>
        /// <param name="lagreDokumentpakke">Hvis satt til true, s� lagres dokumentpakken p� Klientkonfigurasjon.StandardLoggSti.</param>
        public Transportkvittering Send(Forsendelse forsendelse, bool lagreDokumentpakke = false)
        {
            return SendAsync(forsendelse, lagreDokumentpakke).Result;
        }

        /// <summary>
        /// Sender en forsendelse til meldingsformidler. Dersom noe feilet i sendingen til meldingsformidler, vil det kastes en exception.
        /// </summary>
        /// <param name="forsendelse">Et objekt som har all informasjon klar til � kunne sendes (mottakerinformasjon, sertifikater, vedlegg mm), enten digitalt eller fysisk.</param>
        /// <param name="lagreDokumentpakke">Hvis satt til true, s� lagres dokumentpakken p� Klientkonfigurasjon.StandardLoggSti.</param>
        public async Task<Transportkvittering> SendAsync(Forsendelse forsendelse, bool lagreDokumentpakke = false)
        {
            Logging.Log(TraceEventType.Information, forsendelse.KonversasjonsId, "Sender ny forsendelse til meldingsformidler.");

            var guidHandler = new GuidUtility();
            
            var arkiv = LagAsicEArkiv(forsendelse, lagreDokumentpakke, guidHandler);

            var forretningsmeldingEnvelope = LagForretningsmeldingEnvelope(forsendelse, arkiv, guidHandler);

            Logg(TraceEventType.Verbose, forsendelse.KonversasjonsId, arkiv.Signatur.Xml().OuterXml, true, true, "Sendt - Signatur.xml");
            Logg(TraceEventType.Verbose, forsendelse.KonversasjonsId, arkiv.Manifest.Xml().OuterXml, true, true, "Sendt - Manifest.xml");
            Logg(TraceEventType.Verbose, forsendelse.KonversasjonsId, forretningsmeldingEnvelope.Xml().OuterXml, true, true, "Sendt - Envelope.xml");
            
            try
            {
                ValiderForretningsmeldingEnvelope(forretningsmeldingEnvelope.Xml());
                ValiderArkivManifest(arkiv.Manifest.Xml());
                ValiderArkivSignatur(arkiv.Signatur.Xml());
            }
            catch (Exception e)
            {
                throw new Exception("Sending av forsendelse feilet under validering. Feilmelding: " + e.GetBaseException(), e.InnerException);
            }

            var soapContainer = LagSoapContainer(forretningsmeldingEnvelope, arkiv);
            var meldingsformidlerRespons = await SendSoapContainer(soapContainer);

            Logg(TraceEventType.Verbose, forsendelse.KonversasjonsId, meldingsformidlerRespons, true, true, "Mottatt - Meldingsformidlerespons.txt");
            Logg(TraceEventType.Verbose, forsendelse.KonversasjonsId, new byte[1], true,false, "Sendt - SOAPContainer.txt");

            Logging.Log(TraceEventType.Information, forsendelse.KonversasjonsId, "Kvittering for forsendelse" + Environment.NewLine + meldingsformidlerRespons);
            
            return ValiderTransportkvittering(meldingsformidlerRespons, forretningsmeldingEnvelope.Xml(), guidHandler);
        }

        private static SoapContainer LagSoapContainer(ForretningsmeldingEnvelope forretningsmeldingEnvelope, AsicEArkiv arkiv)
        {
            var soapContainer = new SoapContainer(forretningsmeldingEnvelope);
            soapContainer.Vedlegg.Add(arkiv);
            return soapContainer;
        }

        private ForretningsmeldingEnvelope LagForretningsmeldingEnvelope(Forsendelse forsendelse, AsicEArkiv arkiv,
            GuidUtility guidHandler)
        {
            var forretningsmeldingEnvelope =
                new ForretningsmeldingEnvelope(new EnvelopeSettings(forsendelse, arkiv, _databehandler, guidHandler,
                    _klientkonfigurasjon));
            return forretningsmeldingEnvelope;
        }


        private AsicEArkiv LagAsicEArkiv(Forsendelse forsendelse, bool lagreDokumentpakke, GuidUtility guidHandler)
        {
            var arkiv = new AsicEArkiv(forsendelse, guidHandler, _databehandler.Sertifikat);
            if (lagreDokumentpakke)
            {
                arkiv.LagreTilDisk(_klientkonfigurasjon.StandardLoggSti, "dokumentpakke",
                    DateUtility.DateForFile() + " - Dokumentpakke.zip");
            }
            return arkiv;
        }

        private static Transportkvittering ValiderTransportkvittering(string meldingsformidlerRespons,
            XmlDocument forretningsmeldingEnvelope, GuidUtility guidHandler)
        {
            try
            {
                var valideringAvRespons = new Responsvalidator(meldingsformidlerRespons, forretningsmeldingEnvelope);
                valideringAvRespons.ValiderHeaderSignatur();
                valideringAvRespons.ValiderDigest(guidHandler);
            }
            catch (Exception e)
            {
                var transportFeiletKvittering = KvitteringFactory.GetTransportkvittering(meldingsformidlerRespons);
                if (transportFeiletKvittering is TransportOkKvittering)
                {
                    throw new SdpSecurityException("Validering av signatur og digest p� respons feilet.", e);
                }
                return transportFeiletKvittering;
            }

            return KvitteringFactory.GetTransportkvittering(meldingsformidlerRespons);
        }

        /// <summary>
        /// Foresp�r kvittering for forsendelser. Kvitteringer blir tilgjengeliggjort etterhvert som de er klare i meldingsformidler.
        /// Det er ikke mulig � ettersp�rre kvittering for en spesifikk forsendelse.
        /// </summary>
        /// <param name="kvitteringsforesp�rsel"></param>
        /// <returns></returns>
        /// <remarks>
        /// <list type="table">
        /// <listheader><description>Dersom det ikke er tilgjengelige kvitteringer skal det ventes f�lgende tidsintervaller f�r en ny foresp�rsel gj�res</description></listheader>
        /// <item><term>normal</term><description>Minimum 10 minutter</description></item>
        /// <item><term>prioritert</term><description>Minimum 1 minutt</description></item>
        /// </list>
        /// </remarks>
        public Kvittering HentKvittering(Kvitteringsforesp�rsel kvitteringsforesp�rsel)
        {
            return HentKvitteringOgBekreftForrige(kvitteringsforesp�rsel, null);
        }

        /// <summary>
        /// Foresp�r kvittering for forsendelser. Kvitteringer blir tilgjengeliggjort etterhvert som de er klare i meldingsformidler.
        /// Det er ikke mulig � ettersp�rre kvittering for en spesifikk forsendelse.
        /// </summary>
        /// <param name="kvitteringsforesp�rsel"></param>
        /// <returns></returns>
        /// <remarks>
        /// <list type="table">
        /// <listheader><description>Dersom det ikke er tilgjengelige kvitteringer skal det ventes f�lgende tidsintervaller f�r en ny foresp�rsel gj�res</description></listheader>
        /// <item><term>normal</term><description>Minimum 10 minutter</description></item>
        /// <item><term>prioritert</term><description>Minimum 1 minutt</description></item>
        /// </list>
        /// </remarks>
        public async Task<Kvittering> HentKvitteringAsync(Kvitteringsforesp�rsel kvitteringsforesp�rsel)
        {
            return await HentKvitteringOgBekreftForrigeAsync(kvitteringsforesp�rsel, null);
        }

        /// <summary>
        /// Foresp�r kvittering for forsendelser med mulighet til � samtidig bekrefte p� forrige kvittering for � slippe � kj�re eget kall for bekreft. 
        /// Kvitteringer blir tilgjengeliggjort etterhvert som de er klare i meldingsformidler. Det er ikke mulig � ettersp�rre kvittering for en 
        /// spesifikk forsendelse. 
        /// </summary>
        /// <param name="kvitteringsforesp�rsel"></param>
        /// <param name="forrigeKvittering"></param>
        /// <returns></returns>
        /// <remarks>
        /// <list type="table">
        /// <listheader><description>Dersom det ikke er tilgjengelige kvitteringer skal det ventes f�lgende tidsintervaller f�r en ny foresp�rsel gj�res</description></listheader>
        /// <item><term>normal</term><description>Minimum 10 minutter</description></item>
        /// <item><term>prioritert</term><description>Minimum 1 minutt</description></item>
        /// </list>
        /// </remarks>
        public Kvittering HentKvitteringOgBekreftForrige(Kvitteringsforesp�rsel kvitteringsforesp�rsel,
            Forretningskvittering forrigeKvittering)
        {
            return HentKvitteringOgBekreftForrigeAsync(kvitteringsforesp�rsel, forrigeKvittering).Result;
        }
        
        /// <summary>
        /// Foresp�r kvittering for forsendelser med mulighet til � samtidig bekrefte p� forrige kvittering for � slippe � kj�re eget kall for bekreft. 
        /// Kvitteringer blir tilgjengeliggjort etterhvert som de er klare i meldingsformidler. Det er ikke mulig � ettersp�rre kvittering for en 
        /// spesifikk forsendelse. 
        /// </summary>
        /// <param name="kvitteringsforesp�rsel"></param>
        /// <param name="forrigeKvittering"></param>
        /// <returns></returns>
        /// <remarks>
        /// <list type="table">
        /// <listheader><description>Dersom det ikke er tilgjengelige kvitteringer skal det ventes f�lgende tidsintervaller f�r en ny foresp�rsel gj�res</description></listheader>
        /// <item><term>normal</term><description>Minimum 10 minutter</description></item>
        /// <item><term>prioritert</term><description>Minimum 1 minutt</description></item>
        /// </list>
        /// </remarks>
        public async Task<Kvittering> HentKvitteringOgBekreftForrigeAsync(Kvitteringsforesp�rsel kvitteringsforesp�rsel, Forretningskvittering forrigeKvittering)
        {
            if (forrigeKvittering != null)
            {
                Bekreft(forrigeKvittering);
            }

            Logging.Log(TraceEventType.Information, "Henter kvittering for " + kvitteringsforesp�rsel.Mpc);

            var guidHandler = new GuidUtility();
            var envelopeSettings = new EnvelopeSettings(kvitteringsforesp�rsel, _databehandler, guidHandler);
            var kvitteringsenvelope = new Kvitteringsforesp�rselEnvelope(envelopeSettings);

            Logging.Log(TraceEventType.Verbose, "Envelope for kvitteringsforesp�rsel" + Environment.NewLine + kvitteringsenvelope.Xml().OuterXml);

            ValiderKvitteringsEnvelope(kvitteringsenvelope);

            var soapContainer = new SoapContainer(kvitteringsenvelope);
            var kvittering = await SendSoapContainer(soapContainer);

            Logg(TraceEventType.Verbose, Guid.Empty , kvitteringsenvelope.Xml().OuterXml, true, true, "Sendt - Kvitteringsenvelope.xml");

            try
            {
                var valideringAvResponsSignatur = new Responsvalidator(kvittering, kvitteringsenvelope.Xml());
                valideringAvResponsSignatur.ValiderHeaderSignatur();
                valideringAvResponsSignatur.ValiderKvitteringSignatur();
            }
            catch (Exception e)
            {
                return ValiderTransportkvittering(kvittering,kvitteringsenvelope.Xml(),guidHandler);
            }

            return KvitteringFactory.GetForretningskvittering(kvittering);
        }

        private static void ValiderKvitteringsEnvelope(Kvitteringsforesp�rselEnvelope kvitteringsenvelope)
        {
            try
            {
                var kvitteringForesp�rselEnvelopeValidering = new Kvitteringsforesp�rselEnvelopeValidator();
                var kvitteringForesp�rselEnvelopeValidert =
                    kvitteringForesp�rselEnvelopeValidering.ValiderDokumentMotXsd(kvitteringsenvelope.Xml().OuterXml);
                if (!kvitteringForesp�rselEnvelopeValidert)
                    throw new Exception(kvitteringForesp�rselEnvelopeValidering.ValideringsVarsler);
            }
            catch (Exception e)
            {
                throw new XmlValidationException("Kvitteringsforesp�rsel validerer ikke mot xsd:" + e.Message);
            }

        }

        /// <summary>
        /// Bekreft mottak av forretningskvittering gjennom <see cref="HentKvittering(Kvitteringsforesp�rsel)"/>.
        /// <list type="bullet">
        /// <listheader><description><para>Dette legger opp til f�lgende arbeidsflyt</para></description></listheader>
        /// <item><description><para><see cref="HentKvittering(Kvitteringsforesp�rsel)"/></para></description></item>
        /// <item><description><para>Gj�r intern prosessering av kvitteringen (lagre til database, og s� videre)</para></description></item>
        /// <item><description><para>Bekreft mottak av kvittering</para></description></item>
        /// </list>
        /// </summary>
        /// <param name="forrigeKvittering"></param>
        /// <remarks>
        /// <see cref="HentKvittering(Kvitteringsforesp�rsel)"/> kommer ikke til � returnere en ny kvittering f�r mottak av den forrige er bekreftet.
        /// </remarks>
        public void Bekreft(Forretningskvittering forrigeKvittering)
        {
            BekreftAsync(forrigeKvittering);
        }

        /// <summary>
        /// Bekreft mottak av forretningskvittering gjennom <see cref="HentKvittering(Kvitteringsforesp�rsel)"/>.
        /// <list type="bullet">
        /// <listheader><description><para>Dette legger opp til f�lgende arbeidsflyt</para></description></listheader>
        /// <item><description><para><see cref="HentKvittering(Kvitteringsforesp�rsel)"/></para></description></item>
        /// <item><description><para>Gj�r intern prosessering av kvitteringen (lagre til database, og s� videre)</para></description></item>
        /// <item><description><para>Bekreft mottak av kvittering</para></description></item>
        /// </list>
        /// </summary>
        /// <param name="forrigeKvittering"></param>
        /// <remarks>
        /// <see cref="HentKvittering(Kvitteringsforesp�rsel)"/> kommer ikke til � returnere en ny kvittering f�r mottak av den forrige er bekreftet.
        /// </remarks>
        public async Task BekreftAsync(Forretningskvittering forrigeKvittering)
        {
            Logging.Log(TraceEventType.Information, forrigeKvittering.KonversasjonsId, "Bekrefter forrige kvittering.");

            var envelopeSettings = new EnvelopeSettings(forrigeKvittering, _databehandler, new GuidUtility());
            var kvitteringMottattEnvelope = new KvitteringsbekreftelseEnvelope(envelopeSettings);

            string filnavn = forrigeKvittering.GetType().Name + ".xml";
            Logg(TraceEventType.Verbose, forrigeKvittering.KonversasjonsId, forrigeKvittering, true, true, filnavn);
            
            try
            {
                var kvitteringMottattEnvelopeValidering = new KvitteringMottattEnvelopeValidator();
                var kvitteringMottattEnvelopeValidert = kvitteringMottattEnvelopeValidering.ValiderDokumentMotXsd(kvitteringMottattEnvelope.Xml().OuterXml);
                if (!kvitteringMottattEnvelopeValidert)
                    throw new Exception(kvitteringMottattEnvelopeValidering.ValideringsVarsler);
            }
            catch (Exception e)
            {
                throw new XmlValidationException("Kvitteringsbekreftelse validerer ikke:" + e.Message);
            }


            var soapContainer = new SoapContainer(kvitteringMottattEnvelope);
            await SendSoapContainer(soapContainer);
        }

        private async Task<string> SendSoapContainer(SoapContainer soapContainer)
        {
            string data;

            var sender = new Meldingshandling(_klientkonfigurasjon);
            var responseMessage = await sender.Send(soapContainer);

            try
            {               
                data = await responseMessage.Content.ReadAsStringAsync();
            }
            catch (WebException we)
            {
                using (var response = we.Response as HttpWebResponse)
                {
                    if (response == null)
                    {
                        throw new SendException("F�r ikke kontakt med meldingsformidleren. Sjekk tilkoblingsdetaljer og pr�v p� nytt.");
                    }


                    using (Stream errorStream = response.GetResponseStream())
                    {
                        XDocument soap = XDocument.Load(errorStream);
                        data = soap.ToString();
               
                        Logg(TraceEventType.Critical, Guid.Empty, data, true, true, "Sendt - SoapException.xml");
                    }
                }
            }
            return data;
        }

        private static void ValiderForretningsmeldingEnvelope(XmlDocument forretningsmeldingEnvelopeXml)
        {
            const string preMessage = "Envelope validerer ikke: ";

            var envelopeValidering = new ForretningsmeldingEnvelopeValidator();
            var envelopeValidert = envelopeValidering.ValiderDokumentMotXsd(forretningsmeldingEnvelopeXml.OuterXml);
            if (!envelopeValidert)
                throw new XmlValidationException(preMessage + envelopeValidering.ValideringsVarsler);
        }

        private static void ValiderArkivSignatur(XmlDocument signaturXml)
        {
            const string preMessage = "Envelope validerer ikke: ";

            var signaturValidering = new Signaturvalidator();
            var signaturValidert = signaturValidering.ValiderDokumentMotXsd(signaturXml.OuterXml);
            if (!signaturValidert)
                throw new XmlValidationException(preMessage + signaturValidering.ValideringsVarsler);
        }

        private static void ValiderArkivManifest(XmlDocument manifestXml)
        {
            const string preMessage = "Envelope validerer ikke: ";

            var manifestValidering = new ManifestValidator();
            var manifestValidert = manifestValidering.ValiderDokumentMotXsd(manifestXml.OuterXml);
            if (!manifestValidert)
                throw new XmlValidationException(preMessage + manifestValidering.ValideringsVarsler);
        }

        private void Logg(TraceEventType viktighet, Guid konversasjonsId, string melding, bool datoPrefiks, bool isXml, string filnavn, params string[] filsti)
        {
            string[] fullFilsti = new string[filsti.Length + 1];
            for (int i = 0; i < filsti.Length; i ++ )
            {
                var sti = filsti[i];
                fullFilsti[i] = sti;
            }

            filnavn = datoPrefiks ? String.Format("{0} - {1}", DateUtility.DateForFile(), filnavn) : filnavn;
            fullFilsti[filsti.Length] = filnavn;

            if (_klientkonfigurasjon.LoggXmlTilFil && filnavn!= null)
            {
                if (isXml)
                    FileUtility.WriteXmlToBasePath(melding, "logg", filnavn);
                else
                    FileUtility.WriteToBasePath(melding, "logg", filnavn);
            }

            Logging.Log(viktighet, konversasjonsId, melding);
        }

        private void Logg(TraceEventType viktighet, Guid konversasjonsId, byte[] melding, bool datoPrefiks, bool isXml, string filnavn, params string[] filsti)
        {
            string data = System.Text.Encoding.UTF8.GetString(melding);
            Logg(viktighet, konversasjonsId, data,datoPrefiks,isXml,filnavn,filsti);
        }

        private void Logg(TraceEventType viktighet, Guid konversasjonsId, Forretningskvittering kvittering, bool datoPrefiks, bool isXml, params string[] filsti)
        {
            var fileSuffix = isXml ? ".xml" : ".txt";
            Logg(viktighet,konversasjonsId,kvittering.R�data, datoPrefiks, isXml,"Mottatt - " + kvittering.GetType().Name + fileSuffix);
        }
    }
}
