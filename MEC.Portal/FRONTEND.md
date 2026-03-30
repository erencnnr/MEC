# MEC.Portal Frontend Guide

## Tasarim Yonelimi
`MEC.Portal` icin hedef gorsel dil sakin, kurumsal ve uzun omurlu olmali. Aradigimiz hissiyat:

- mat yuzeyler
- kontrollu kontrast
- okunakli tipografi
- sade bilesenler
- is odakli ekranlar

Kacinilacak dil:

- parlak gradient ve glow efektleri
- glassmorphism ve isikli kutular
- dikkat cekmek icin kullanilan neon veya fazla doygun renkler
- emoji
- dekoratif ikonlar
- AI usulu sloganimsi copy
- baslik veya kart metninde dekoratif dash ayiraclari

## Ana Renk Sistemi
Temel renk karari artik sabit:

- Arka plan: `#ffffff`
- Birincil renk: `#18285c`
- Ikincil renk: `#000000`
- Ucuncul renk: `#06c6f7`

Bu dortlu, portalin ana kimligini belirler. Nötr destek tonlari sadece kenarlik, yardimci metin ve yuzey ayrimi icin kullanilir.

## Ana Renk Paleti
Bu palette `wwwroot/css/site.css` icindeki aktif tema degiskenleri baz alinmistir.

| Token | Kod | Kullanim |
| --- | --- | --- |
| `--portal-bg` | `#ffffff` | Sayfa zemini |
| `--surface-1` | `#ffffff` | Ana kart ve panel yuzeyi |
| `--surface-2` | `#f8faff` | Ikinci seviye yuzey |
| `--surface-3` | `#eaf7fd` | Hafif ucuncul vurgu zemini |
| `--border-1` | `#d8e0ee` | Standart kenarlik |
| `--border-2` | `#06c6f7` | Aktif ve hover kenarligi |
| `--text-1` | `#000000` | Ana metin |
| `--text-2` | `#202020` | Ikincil metin |
| `--text-3` | `#5a6481` | Yardimci metin |
| `--brand-1` | `#18285c` | Birincil renk |
| `--brand-2` | `#0f1b42` | Birincil hover tonu |
| `--brand-soft` | `#e8edf8` | Birincil rengin soft zemini |
| `--accent-1` | `#06c6f7` | Ucuncul vurgu rengi |
| `--accent-2` | `#05abd3` | Ucuncul hover tonu |
| `--accent-soft` | `#e3f9fe` | Ucuncul rengin soft zemini |
| `--sidebar-1` | `#18285c` | Admin sidebar zemini |
| `--sidebar-2` | `#22356e` | Sidebar ikinci ton |
| `--secondary-fill` | `#000000` | Ikincil koyu dolu buton |
| `--success-fill` | `#06c6f7` | Form gonderim ve olumlu aksiyon |
| `--danger-fill` | `#000000` | Kritik veya silme aksiyonu |
| `--info-soft` | `#e3f9fe` | Bilgi zemini |
| `--info-text` | `#18285c` | Bilgi metni |

## Uygulama Kurallari
1. Yeni bilesen yazarken once mevcut CSS degiskenlerini kullan.
2. Yeni kart veya panelde varsayilan yuzey `--surface-1`, kenarlik `--border-1` olmali.
3. Arka plan ana seviyede beyaz kalmali. Buyuk alanlarda kirik beyaz veya griye donme.
4. Ana eylem butonlari icin `--brand-1`, koyu ikincil aksiyonlar icin `--secondary-fill`, vurgu ve olumlu aksiyonlar icin `--accent-1` kullan.
5. Durum renkleri sadece anlam tasimali. Bu projede yesil, kirmizi ve sari yerine once ana paletteki tonlarla cozum ara.
6. Hover durumlari sakin olmali. Renk degisimi ve hafif kenarlik degisimi yeterli; buyuyen, ziplayan veya parlayan hover istemiyoruz.
7. Kutularin veya baslik bantlarinin icine gradient koyma.
8. `text-shadow`, kuvvetli `box-shadow`, `backdrop-filter` ve cam efekti kullanma.

## Yazi ve Icerik Kurallari
1. Basliklar net ve duz olmali.
2. Buton, kart ve bos durum metinlerinde reklam dili kullanma.
3. Emoji kullanma.
4. Dekoratif ikon kullanma.
5. Dekoratif dash kullanma.
6. Tarih araligi gibi teknik durumlarda dash yerine slash veya dogrudan cumle tercih et.
7. Gereksiz unlem, buyuk harf ve dikkat cekme dili kullanma.

## Kontrol Listesi
- Bu ekran parlak veya gosterisli mi duruyor?
- Renkler paletteki tonlarla sinirli mi?
- Bir renk anlamsal gorev tasiyor mu?
- Emoji veya dekoratif ikon var mi?
- Baslik ya da kart metninde yapay duran dash ayraci var mi?
- Hover hali dikkat dagitiyor mu?

Bu sorulardan biri bile sorunluysa tasarimi sadeleştir.
