﻿using System;
using System.Xml;

namespace Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Kvitteringer.Forretning
{
    /// <summary>
    /// Dette er Kvittering på at posten har kommet i retur og har blitt makulert.
    /// Les mer på http://begrep.difi.no/SikkerDigitalPost/1.2.0/meldinger/ReturpostKvittering
    /// </summary>
    public class Returpostkvittering : Forretningskvittering
    {
        public Returpostkvittering(Guid konversasjonsId, string bodyReferenceUri, string digestValue) : base(konversasjonsId, bodyReferenceUri, digestValue)
        {
        }

        public DateTime Returnert
        {
            get { return Generert; }
        }

        public override string ToString()
        {
            return String.Format("{0} med meldingsId {1}: \nReturnert: {2}. \nKonversasjonsId: {3}. \nRefererer til melding med id: {4}",
                GetType().Name, MeldingsId, Returnert, KonversasjonsId, ReferanseTilMeldingId);
        }
    }

}

