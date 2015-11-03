﻿using System.Security.Cryptography.X509Certificates;

namespace Difi.SikkerDigitalPost.Klient.XmlValidering
{
    internal class SertifikatkjedevalidatorFunksjoneltTestmiljø : Sertifikatkjedevalidator
    {
        public SertifikatkjedevalidatorFunksjoneltTestmiljø(X509Certificate2Collection sertifikatLager) : base(sertifikatLager)
        {
        }

        public override X509ChainPolicy ChainPolicy()
        {
            var policy = new X509ChainPolicy()
            {
                RevocationMode = X509RevocationMode.NoCheck

            };
            policy.ExtraStore.AddRange(SertifikatLager);
                                
            return policy;
        }
    }
}
