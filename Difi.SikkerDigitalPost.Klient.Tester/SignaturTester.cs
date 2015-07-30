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
using Difi.SikkerDigitalPost.Klient.Tester.Utilities;
using Difi.SikkerDigitalPost.Klient.Utilities;
using Difi.SikkerDigitalPost.Klient.XmlValidering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Difi.SikkerDigitalPost.Klient.Tester
{
    [TestClass]
    public class SignaturTester
    {
        [TestClass]
        public class XsdValidering
        {
            [TestMethod]
            public void HoveddokumentStarterMedEtTallXsdValidererIkke()
            {
                var arkiv = DomeneUtility.GetAsicEArkivEnkel();

                var signaturXml = arkiv.Signatur.Xml();
                var signaturvalidator = new Signaturvalidator();

                //Endre id på hoveddokument til å starte på et tall
                var namespaceManager = new XmlNamespaceManager(signaturXml.NameTable);
                namespaceManager.AddNamespace("ds", NavneromUtility.XmlDsig);
                namespaceManager.AddNamespace("ns10", NavneromUtility.UriEtsi121);
                namespaceManager.AddNamespace("ns11", NavneromUtility.UriEtsi132);

                var hoveddokumentReferanseNode = signaturXml.DocumentElement
                    .SelectSingleNode("//ds:Reference[@Id = '" + DomeneUtility.GetHoveddokumentEnkel().Id + "']",
                        namespaceManager);

                var gammelVerdi = hoveddokumentReferanseNode.Attributes["Id"].Value;
                hoveddokumentReferanseNode.Attributes["Id"].Value = "0_Id_Som_Skal_Feile";

                var validerer = signaturvalidator.ValiderDokumentMotXsd(signaturXml.OuterXml);
                Assert.IsFalse(validerer, signaturvalidator.ValideringsVarsler);

                hoveddokumentReferanseNode.Attributes["Id"].Value = gammelVerdi;
            }

            [TestMethod]
            public void ValidereSignaturMotXsdValiderer()
            {
                var arkiv = DomeneUtility.GetAsicEArkivEnkel();

                var signaturXml = arkiv.Signatur.Xml();
                var signaturValidering = new Signaturvalidator();
                var validerer = signaturValidering.ValiderDokumentMotXsd(signaturXml.OuterXml);

                Assert.IsTrue(validerer, signaturValidering.ValideringsVarsler);
            }
        }
    }
}
