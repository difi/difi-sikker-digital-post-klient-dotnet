﻿namespace Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Kvitteringer.Transport
{
    public abstract class Transportkvittering : Kvittering
    {
        public Transportkvittering():base(meldingsId: string.Empty)
        {
            
        }
    }
}
