﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Interface;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Post;
using Difi.SikkerDigitalPost.Klient.Utilities;

namespace Difi.SikkerDigitalPost.Klient.Internal.AsicE
{
    internal class AsiceArchive : ISoapVedlegg
    {
        public AsiceArchive(X509Certificate2 cryptographicCertificate, GuidUtility guidUtility,
            IEnumerable<AsiceAttachableProcessor> asiceAttachableProcessors, params IAsiceAttachable[] asiceAttachables)
        {
            CryptographicCertificate = cryptographicCertificate;
            GuidUtility = guidUtility;
            AsiceAttachableProcessors = asiceAttachableProcessors;
            AsiceAttachables = asiceAttachables;
            UnencryptedBytes = CreateZipFile();
        }

        internal byte[] UnencryptedBytes { get; }

        public long UnzippedContentBytesCount
        {
            get
            {
                return AsiceAttachables.Aggregate(0L,
                    (current, asiceAttachable) => current + asiceAttachable.Bytes.Length);
            }
        }

        public IAsiceAttachable[] AsiceAttachables { get; }

        private GuidUtility GuidUtility { get; }

        public IEnumerable<AsiceAttachableProcessor> AsiceAttachableProcessors { get; }

        private X509Certificate2 CryptographicCertificate { get; }

        public string Filnavn => "post.asice.zip";

        public string Innholdstype => "application/cms";

        public string ContentId => GuidUtility.DokumentpakkeId;

        public string TransferEncoding => "binary";

        public byte[] Bytes => EncryptedBytes(UnencryptedBytes);

        private byte[] EncryptedBytes(byte[] bytes)
        {
            var contentInfo = new ContentInfo(bytes);
            var encryptAlgoOid = new Oid("2.16.840.1.101.3.4.1.42"); // AES-256-CBC            
            var envelopedCms = new EnvelopedCms(contentInfo, new AlgorithmIdentifier(encryptAlgoOid));
            var recipient = new CmsRecipient(CryptographicCertificate);
            envelopedCms.Encrypt(recipient);
            return envelopedCms.Encode();
        }

        private byte[] CreateZipFile()
        {
            var stream = new MemoryStream();
            using (stream)
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
                {
                    foreach (var asiceAttachable in AsiceAttachables)
                    {
                        AddFilesToArchive(archive, asiceAttachable is Dokument
                            ? ((Dokument) asiceAttachable).FilnavnRådata
                            : asiceAttachable.Filnavn, asiceAttachable.Bytes);
                    }
                }
            }
            
            var zipFile = stream.ToArray();
            SendArchiveThroughBundleProcessors(zipFile);
            return zipFile;
        }

        private void SendArchiveThroughBundleProcessors(byte[] archiveBytes)
        {
            foreach (var documentBundleProcessor in AsiceAttachableProcessors)
            {
                try
                {
                    documentBundleProcessor.Process(new MemoryStream(archiveBytes));
                }
                catch (Exception exception)
                {
                    throw new IOException("Could not run stream through document bundle processor.", exception);
                }
            }
        }

        private static void AddFilesToArchive(ZipArchive archive, string filename, byte[] data)
        {
            var entry = archive.CreateEntry(filename, CompressionLevel.Optimal);
            using (var stream = entry.Open())
            {
                stream.Write(data, 0, data.Length);
            }
        }

        public void SaveToFile(params string[] filsti)
        {
            FileUtility.WriteToBasePath(UnencryptedBytes, filsti);
        }

        public static byte[] Decrypt(byte[] kryptertData)
        {
            var envelopedCms = new EnvelopedCms();
            envelopedCms.Decode(kryptertData);
            envelopedCms.Decrypt(envelopedCms.RecipientInfos[0]);
            return envelopedCms.ContentInfo.Content;
        }
    }
}
