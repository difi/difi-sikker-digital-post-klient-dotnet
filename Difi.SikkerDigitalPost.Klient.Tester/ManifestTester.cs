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
    public class ManifestTester
    {
        [TestClass]
        public class Hoveddokument
        {
            [TestMethod]
            public void UgyldigNavnPåHoveddokumentValidererIkke()
            {
                var arkiv = DomeneUtility.GetAsicEArkivEnkel();

                var manifestXml = arkiv.Manifest.Xml();
                var manifestValidering = new ManifestValidator();

                //Endre navn på hoveddokument til å være for kort
                var namespaceManager = new XmlNamespaceManager(manifestXml.NameTable);
                namespaceManager.AddNamespace("ns9", NavneromUtility.DifiSdpSchema10);
                namespaceManager.AddNamespace("ds", NavneromUtility.XmlDsig);

                var hoveddokumentNode = manifestXml.DocumentElement.SelectSingleNode("//ns9:hoveddokument",
                    namespaceManager);
                var gammelVerdi = hoveddokumentNode.Attributes["href"].Value;
                hoveddokumentNode.Attributes["href"].Value = "abc";

                var validert = manifestValidering.ValiderDokumentMotXsd(manifestXml.OuterXml);
                Assert.IsFalse(validert, manifestValidering.ValideringsVarsler);

                hoveddokumentNode.Attributes["href"].Value = gammelVerdi;
            }
        }

        [TestClass]
        public class XsdValidering
        {
            [TestMethod]
            public void ValidereManifestMotXsdValiderer()
            {
                var arkiv = DomeneUtility.GetAsicEArkivEnkel();

                var manifestXml = arkiv.Manifest.Xml();

                var manifestValidering = new ManifestValidator();
                var validert = manifestValidering.ValiderDokumentMotXsd(manifestXml.OuterXml);
                Assert.IsTrue(validert, manifestValidering.ValideringsVarsler);
            }
        }
    }
}
