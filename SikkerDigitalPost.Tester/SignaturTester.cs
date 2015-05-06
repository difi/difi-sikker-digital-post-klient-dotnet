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

using System.Xml;
using Difi.SikkerDigitalPost.Klient.XmlValidering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Difi.SikkerDigitalPost.Klient.Tester
{
    [TestClass]
    public class SignaturTester : TestBase
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            Initialiser();
        }

        [TestMethod]
        public void HoveddokumentStarterMedEtTallXsdValidererIkke()
        {
            var signaturXml = Arkiv.Signatur.Xml();
            var signaturvalidator = new Signaturvalidator();
            
            //Endre id på hoveddokument til å starte på et tall
            var namespaceManager = new XmlNamespaceManager(signaturXml.NameTable);
            namespaceManager.AddNamespace("ds", Navnerom.XmlDsig);
            namespaceManager.AddNamespace("ns10", Navnerom.UriEtsi121);
            namespaceManager.AddNamespace("ns11", Navnerom.UriEtsi132);

            var hoveddokumentReferanseNode = signaturXml.DocumentElement
                .SelectSingleNode("//ds:Reference[@Id = '" + Hoveddokument.Id + "']", namespaceManager);

            var gammelVerdi = hoveddokumentReferanseNode.Attributes["Id"].Value;
            hoveddokumentReferanseNode.Attributes["Id"].Value = "0_Id_Som_Skal_Feile";

            var validerer = signaturvalidator.ValiderDokumentMotXsd(signaturXml.OuterXml);
            Assert.IsFalse(validerer, signaturvalidator.ValideringsVarsler);

            hoveddokumentReferanseNode.Attributes["Id"].Value = gammelVerdi;
        }

        [TestMethod]
        public void ValidereSignaturMotXsdValiderer()
        {
            var signaturXml = Arkiv.Signatur.Xml();
            var signaturValidering = new Signaturvalidator();
            var validerer = signaturValidering.ValiderDokumentMotXsd(signaturXml.OuterXml);

            Assert.IsTrue(validerer, signaturValidering.ValideringsVarsler);
        }
    }
}
