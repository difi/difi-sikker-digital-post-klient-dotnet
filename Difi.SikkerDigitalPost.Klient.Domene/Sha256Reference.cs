﻿using System.IO;

namespace Difi.SikkerDigitalPost.Klient.Domene
{
    /// <summary>
    /// Represents the &lt;reference&gt; element of an Xml Signature with http://www.w3.org/2001/04/xmlenc#sha256 as the digest method.
    /// </summary>
    internal class Sha256Reference : System.Security.Cryptography.Xml.Reference
    {
        public Sha256Reference()
        {
            SetDigest();
        }

        public Sha256Reference(Stream stream): base(stream)
        {
            SetDigest();
        }

        public Sha256Reference(string uri): base(uri)
        {
            SetDigest();
        }

        public Sha256Reference(byte[] bytes): base(new MemoryStream(bytes))
        {
           SetDigest();
        }

        private void SetDigest()
        {
            DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
        }
    }
}
