﻿using System.Security.Policy;
using Difi.SikkerDigitalPost.Klient.Utilities;
using Difi.SikkerDigitalPost.Klient.XmlValidering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Difi.SikkerDigitalPost.Klient.Tester.XmlValidering
{
    [TestClass]
    public class MiljøTester
    {
        [TestClass]
        public class GetMiljøMethod : MiljøTester
        {
            [TestMethod]
            public void ReturnererInitialisertFunksjoneltTestmiljø()
            {
                //Arrange
                var url = "https://qaoffentlig.meldingsformidler.digipost.no/api/ebms";
                var miljø = Miljø.FunksjoneltTestmiljø;
                var sertifikater = SertifikatkjedeUtility.FunksjoneltTestmiljøSertifikater();

                //Act

                //Assert
                Assert.IsNotNull(miljø.Sertifikatkjedevalidator);
                Assert.AreEqual(url, miljø.Url.AbsoluteUri);
                CollectionAssert.AreEqual(sertifikater, miljø.Sertifikatkjedevalidator.SertifikatLager);
            }

            [TestMethod]
            public void ReturnererInitialisertProduksjonsmiljø()
            {
                //Arrange
                var url = "https://meldingsformidler.digipost.no/api/ebms";
                var miljø = Miljø.Produksjonsmiljø;
                var sertifikater = SertifikatkjedeUtility.ProduksjonsSertifikater();

                //Act

                //Assert
                Assert.IsNotNull(miljø.Sertifikatkjedevalidator);
                Assert.AreEqual(url, miljø.Url.ToString());
                CollectionAssert.AreEqual(sertifikater, miljø.Sertifikatkjedevalidator.SertifikatLager);
            }

            [TestMethod]
            public void ReturnererInitialisertFunksjoneltTestmiljøNorskHelsenett()
            {
                //Arrange
                var url = "https://qaoffentlig.meldingsformidler.nhn.digipost.no:4445/api/";
                var miljø = Miljø.FunksjoneltTestmiljøNorskHelsenett;
                var sertifikater = SertifikatkjedeUtility.FunksjoneltTestmiljøSertifikater();

                //Act

                //Assert
                Assert.IsNotNull(miljø.Sertifikatkjedevalidator);
                Assert.AreEqual(url, miljø.Url.AbsoluteUri);
                CollectionAssert.AreEqual(sertifikater, miljø.Sertifikatkjedevalidator.SertifikatLager);
            }

            [TestMethod]
            public void ReturnererInitialisertProduksjonsmiljøNorskHelsenett()
            {
                //Arrange
                var url = "https://meldingsformidler.nhn.digipost.no:4444/api/";
                var miljø = Miljø.ProduksjonsmiljøNorskHelsenett;
                var sertifikater = SertifikatkjedeUtility.ProduksjonsSertifikater();

                //Act

                //Assert
                Assert.IsNotNull(miljø.Sertifikatkjedevalidator);
                Assert.AreEqual(url, miljø.Url.ToString());
                CollectionAssert.AreEqual(sertifikater, miljø.Sertifikatkjedevalidator.SertifikatLager);
            }
        }
    }
}
