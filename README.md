# E-Commerce API

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4)
![Status](https://img.shields.io/badge/status-active_development-orange)

ASP.NET Core üzerinde geliştirilen e-ticaret backend servisidir. Proje; ürün kataloğu, kategori, stok, sepet, sipariş ve kullanıcı yönetimi gibi temel e-ticaret alanlarını güvenli ve sürdürülebilir bir API altında toplamayı hedefler.

Repository şu anda başlangıç aşamasındadır. Çalışan API host'u ve servis durumunu döndüren ilk endpoint hazırdır; iş alanları ve veri erişim katmanı sonraki sürümlerde eklenecektir.

## İçindekiler

- [Proje durumu](#proje-durumu)
- [Teknoloji yığını](#teknoloji-yığını)
- [Başlangıç](#başlangıç)
- [API](#api)
- [Yapılandırma](#yapılandırma)
- [Proje yapısı](#proje-yapısı)
- [Teknik yol haritası](#teknik-yol-haritası)
- [Geliştirme standartları](#geliştirme-standartları)

## Proje durumu

| Bileşen | Durum |
| --- | --- |
| ASP.NET Core API host | Hazır |
| Temel servis endpoint'i | Hazır |
| Ürün ve kategori kataloğu | Planlandı |
| Kalıcı veri erişimi | Planlandı |
| Entity Framework Core | Planlandı |
| Sepet, stok ve sipariş yönetimi | Planlandı |
| Kimlik doğrulama ve yetkilendirme | Planlandı |
| Otomatik testler | Planlandı |
| Container ve deployment | Planlandı |

Mevcut sürüm production kullanımı için hazır değildir.

## Teknoloji yığını

### Mevcut

| Teknoloji | Kullanım |
| --- | --- |
| C# | Uygulama dili |
| .NET 10 | Hedef framework ve çalışma platformu |
| ASP.NET Core Minimal API | HTTP endpoint tanımları ve uygulama host'u |
| Kestrel | Web sunucusu |
| JSON | API veri alışveriş formatı |

Projede nullable reference types ve implicit global usings etkindir.

### Planlanan

- Entity Framework Core
- İlişkisel veritabanı
- OpenAPI/Swagger
- Validation
- JWT tabanlı authentication
- Role ve policy tabanlı authorization
- Unit ve integration testleri
- Docker
- CI/CD

Planlanan teknolojiler, ilgili iş gereksinimi uygulanırken kesinleştirilecektir.

## Başlangıç

### Gereksinimler

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git

Kurulu SDK sürümünü kontrol edin:

```bash
dotnet --version
```

### Kurulum

Repository'yi klonlayın:

```bash
git clone https://github.com/TyLnZgr/eTicaretAPI.git
cd eTicaretAPI
```

Bağımlılıkları geri yükleyin:

```bash
dotnet restore
```

Solution'ı derleyin:

```bash
dotnet build ECommerce.slnx
```

API'yi çalıştırın:

```bash
dotnet run --project src/ECommerce.Api/ECommerce.Api.csproj
```

HTTP profili varsayılan olarak aşağıdaki adresi kullanır:

```text
http://localhost:5080
```

## API

### Servis durumu

```http
GET /
```

Başarılı cevap:

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "message": "ECommerce API is running."
}
```

Terminal üzerinden örnek istek:

```bash
curl http://localhost:5080/
```

IDE HTTP istemcileri için hazır istek [ECommerce.Api.http](src/ECommerce.Api/ECommerce.Api.http) dosyasında bulunur.

## Yapılandırma

Uygulama ASP.NET Core'un standart yapılandırma sistemini kullanır.

| Dosya | Amaç |
| --- | --- |
| `appsettings.json` | Ortak uygulama ayarları |
| `appsettings.Development.json` | Development ortamına özel ayarlar |
| `Properties/launchSettings.json` | Yerel çalışma profilleri ve URL'ler |

Aktif ortam `ASPNETCORE_ENVIRONMENT` değişkeniyle belirlenir. Parolalar, bağlantı bilgileri, token'lar ve diğer gizli değerler repository'ye eklenmemelidir. Yerel geliştirmede environment variable kullanılmalıdır.

HTTPS profilini çalıştırmak için:

```bash
dotnet run \
  --project src/ECommerce.Api/ECommerce.Api.csproj \
  --launch-profile https
```

HTTPS profili `https://localhost:7080` adresini kullanır.

## Proje yapısı

```text
eTicaretAPI/
├── .gitignore
├── ECommerce.slnx
├── README.md
└── src/
    └── ECommerce.Api/
        ├── Properties/
        │   └── launchSettings.json
        ├── appsettings.Development.json
        ├── appsettings.json
        ├── ECommerce.Api.csproj
        ├── ECommerce.Api.http
        └── Program.cs
```

| Yol | Sorumluluk |
| --- | --- |
| `ECommerce.slnx` | Solution içindeki projeleri organize eder |
| `src/ECommerce.Api` | HTTP host'u, endpoint'ler ve API yapılandırması |
| `Program.cs` | Uygulama başlangıcı ve request pipeline |
| `ECommerce.Api.csproj` | Hedef framework ve proje bağımlılıkları |

Yeni katmanlar ve projeler yalnızca domain karmaşıklığı gerektirdiğinde eklenecektir.

## Teknik yol haritası

### 1. Ürün kataloğu

- Product ve Category modelleri
- CRUD endpoint'leri
- DTO ve request/response sözleşmeleri
- Girdi doğrulama
- Filtering, sorting ve pagination

### 2. Veri erişimi

- Entity Framework Core entegrasyonu
- Veritabanı provider yapılandırması
- Migration yönetimi
- Entity ilişkileri ve veri bütünlüğü
- Asenkron sorgular ve transaction yönetimi

### 3. Ticaret akışları

- Sepet yönetimi
- Stok kontrolü
- Sipariş ve sipariş kalemleri
- Fiyat hesaplama
- Eş zamanlı güncelleme senaryoları

### 4. Güvenlik

- Kullanıcı ve rol yönetimi
- Authentication
- Role/policy tabanlı authorization
- JWT access ve refresh token akışı
- Güvenli secret yönetimi

### 5. Production hazırlığı

- Merkezi hata yönetimi ve Problem Details
- Structured logging ve health checks
- Unit ve integration testleri
- Docker
- CI/CD pipeline
- İzlenebilirlik ve performans iyileştirmeleri

## Geliştirme standartları

- Kod ve domain isimleri İngilizce yazılır.
- Değişiklikler küçük, odaklı ve derlenebilir tutulur.
- API sözleşmeleri geriye uyumluluk dikkate alınarak değiştirilir.
- Gizli bilgiler kaynak kodda veya configuration dosyalarında tutulmaz.
- Yeni bağımlılıklar yalnızca açık bir gereksinim olduğunda eklenir.
- Her değişiklikte en azından solution build'i doğrulanır.

Temel doğrulama komutu:

```bash
dotnet build ECommerce.slnx --no-restore
```

Commit mesajlarında Conventional Commits türleri tercih edilir:

```text
feat: add product creation endpoint
fix: prevent negative stock values
docs: update API usage
refactor: extract product service
test: cover product validation
chore: update project configuration
```

## Maintainer

[@TyLnZgr](https://github.com/TyLnZgr)
