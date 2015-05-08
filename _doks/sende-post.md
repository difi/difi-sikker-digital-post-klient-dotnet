---
title: Sende post
id: sendepost
layout: default
description: Hvordan sende fysisk og digital post
isHome: false
---

I disse eksempler er det Posten som er den som produserer informasjon/brev/post som skal formidles (Behandlingsansvarlig) og Posten som er teknisk avsender.

<blockquote>Det anbefales å bruke dokumentasjon i klassene for mer detaljert beskrivelse av inputparametere</blockquote>

<h3 id="postinfodigital">PostInfo for digital post</h3>

Først, lag en motaker av type `DigitalPostMottaker`:

{% highlight csharp %}
var mottaker = new DigitalPostMottaker(personnummer, postkasseadresse, mottakersertifikat, orgnummerPostkasse)
{% endhighlight%}

Organisasjonsnummer til postkassen vil være 984661185 hvis det er Posten som har postkassen, og 996460320 hvis det er EBoks.

Opprett så en `DigitalPostInfo`:
{% highlight csharp %}
var postInfo = new DigitalPostInfo(mottaker, ikkeSensitivTittel, sikkerhetsnivå, åpningskvittering)
{% endhighlight%}

<h3 id="postinfofysisk">PostInfo for fysisk post</h3>

Skal du sende fysisk post må du først lage en `FysiskPostMottaker`:
{% highlight csharp %}
var mottaker = new FysiskPostMottaker(navn, adresse, mottakersertifikat, orgnummerPostkasse)
{% endhighlight%}

Her er adressen av type `NorskAdresse` eller `UtenlandskAdresse`.

Ved sending av fysisk post må man oppgi en returadresse, uavhengig av om brevet er satt til `Posthåndtering.MakuleringMedMelding`. Oppretting av en FysiskPostInfo vil da se slik ut:

{% highlight csharp %}
var postInfo = new FysiskPostInfo(mottaker, Posttype.A, Utskriftsfarge.SortHvitt, Posthåndtering.MakuleringMedMelding, returMottaker);
{% endhighlight%}

<h3 id="oppsettfoersending">Oppsett før sending</h3>

Lag en behandlingsansvarlig og en teknisk avsender:
{% highlight csharp %}
var behandlingsansvarlig = new Behandlingsansvarlig(orgnummerBehandlingsansvarlig);
behandlingsansvarlig.Avsenderidentifikator = "Digipost";

tekniskAvsender = new Databehandler(orgnummerDatabehandler, avsendersertifikat);
{% endhighlight%}

<h3 id="oppretteforsendelse">Opprette forsendelse</h3>
Deretter, opprett forsendelse. Forsendelsen inneholder de dokumentene som skal til mottakeren:

{% highlight csharp %}
var hoveddokumentSti = "/Dokumenter/Hoveddokument.pdf";
var hoveddokument = new Dokument(tittel, hoveddokumentsti, "application/pdf", "NO", "filnavn");

var dokumentpakke = new Dokumentpakke(hoveddokument);

var vedleggssti = "/Dokumenter/Vedlegg.pdf";
var vedlegg = new Dokument("tittel", vedleggsti, "application/pdf", "NO", "filnavn");

dokumentpakke.LeggTilVedlegg(vedlegg);
{% endhighlight %}

<blockquote>
	<b>Viktig å huske på</b>: <code>Dokument.Tittel</code> brukes bare for hoveddokument ved sending digitalt og vises bare i innboksen om du har satt sikkerhetsnivå til <code>Sikekrhetsnivå.Nivå3</code>. Ved <code>Sikkerhetsnivå.Nivå4</code> vil tittelen være <i>Konfidensielt brev</i>.
	<code>Dokument.Filnavn</code> er visningsnavn for filen i digital postkasse. Merk at filnavn trenger ikke ha filendelse. Ved nedlasting av fil vil filnavn være en kombinasjon av dato og <code>Dokument.Filnavn</code>. 
</blockquote>


Deretter er det bare å opprette en forsendelse med `PostInfo` (`DigitalPostInfo` eller `FysiskPostInfo`). 

{% highlight csharp %}
var forsendelse = new Forsendelse(behandlingsansvarlig, postInfo, dokumentpakke, Prioritet.Normal, mpcId, "NO");
{% endhighlight %}

<h3 id="opprettKlient">Opprette klient og sende post </h3>
Siste steg er å opprette en `SikkerDigitalPostKlient`:

{% highlight csharp %}
var sdpKlient = new SikkerDigitalPostKlient(tekniskAvsender, klientkonfigurasjon)

var transportkvittering = sdpKlient.Send(forsendelse)
{% endhighlight %}

Transportkvitteringen kan enten være av type `TransportOkKvittering` eller `TransportFeiletKvittering`. For sistnevnte er `Alvorlighetsgrad` og `Beskrivelse` nyttige felter når det går galt.

{% highlight csharp %}
if(transportkvittering.GetType() == typeof(TransportOkKvittering))
{
	//Gjør logikk når alt går fint	
}

if(transportkvittering.GetType() == typeof(TransportFeiletKvittering))
{
	//Gjør logikk når det går galt	
	 var feil = ((TransportFeiletKvittering)kvittering).Beskrivelse;
}
{% endhighlight %}

Transportkvitteringen får du tilbake umiddelbart; den trenger du ikke å polle for å få. 

<h3 id="henteKvitteringer"> Hente kvitteringer</h3>
For å hente kvitteringer må du sende en kvitteringsforespørsel:

{% highlight csharp %}
var kvitteringsforespørsel = new Kvitteringsforespørsel(Prioritet, MpcId)

var kvittering = sdpKlient.HentKvittering(kvitteringsforespørsel);
{% endhighlight %}

<blockquote>
Husk at det ikke er mulig å hente nye kvitteringer før du har bekreftet mottak av nåværende.
</blockquote>

{%highlight csharp%}
sdpKlient.Bekreft((Forretningskvittering)kvittering);
{% endhighlight%}

Kvitteringer du mottar når du gjør en kvitteringsforespørsel kan være av følgende typer: `Leveringskvittering`,`Åpningskvittering`, `Returpostkvittering`, `Mottakskvittering` eller `Feilmelding`. Kvittering kan også være av typen`TransportFeiletKvittering`. Dette kan skje når selve kvitteringsforespørselen er feilformatert.

<blockquote>
Husk at hvis kvitteringen er <code>null</code> så er køen tom. Du henter bare kvitteringer fra kø gitt av <code>MpcId</code>.
</blockquote>

Et eksempel på sjekk om kvittering er `Åpningskvittering`:
{%highlight csharp%}
if(kvittering is Åpningskvittering)
{
	Console.WriteLine("Åpningskvittering mottatt!")
}
{% endhighlight%}

