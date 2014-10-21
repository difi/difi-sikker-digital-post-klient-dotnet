﻿using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SikkerDigitalPost.Klient;
using SikkerDigitalPost.Klient.XmlValidering;

namespace SikkerDigitalPost.Tester
{
    [TestClass]
    public class ManifestTester : TestBase
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            Initialiser();
        }

        [TestMethod]
        public void UgyldigNavnPåHoveddokumentValidererIkke()
        {
            var manifestXml = Arkiv.Manifest.Xml();
            var manifestValidering = new ManifestValidering();
            var validert = manifestValidering.ValiderDokumentMotXsd(manifestXml.OuterXml);

            //Endre navn på hoveddokument til å være for kort
            var namespaceManager = new XmlNamespaceManager(manifestXml.NameTable);
            namespaceManager.AddNamespace("ns9", Navnerom.Ns9);
            namespaceManager.AddNamespace("ds", Navnerom.ds);

            var hoveddokumentNode = manifestXml.DocumentElement.SelectSingleNode("//ns9:hoveddokument", namespaceManager);
            var gammelVerdi = hoveddokumentNode.Attributes["href"].Value;
            hoveddokumentNode.Attributes["href"].Value = "abc";

            validert = manifestValidering.ValiderDokumentMotXsd(manifestXml.OuterXml);
            Assert.IsFalse(validert, manifestValidering.ValideringsVarsler);

            hoveddokumentNode.Attributes["href"].Value = gammelVerdi;
        }

        [TestMethod]
        public void ValidereManifestMotXsdValiderer()
        {
            var manifestXml = Arkiv.Manifest.Xml();

            var manifestValidering = new ManifestValidering();
            var validert = manifestValidering.ValiderDokumentMotXsd(manifestXml.OuterXml);
            Assert.IsTrue(validert, manifestValidering.ValideringsVarsler);
        }
    }
}
