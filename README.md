# E-Commerce API

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4)
![Status](https://img.shields.io/badge/status-active_development-orange)

ASP.NET Core ve C# ile geliştirilen modüler e-ticaret backend servisidir. Sistem; katalog, stok, sepet, sipariş ve kimlik yönetimi gibi temel e-ticaret iş alanlarını güvenli, test edilebilir ve sürdürülebilir bir API altında birleştirmek üzere tasarlanmıştır.

## Proje kapsamı

| Modül | Sorumluluk |
| --- | --- |
| Catalog | Ürün ve kategori yönetimi |
| Inventory | Stok miktarı ve stok hareketleri |
| Cart | Kullanıcı sepeti ve sepet kalemleri |
| Ordering | Sipariş oluşturma, sipariş kalemleri ve durum yönetimi |
| Identity | Kullanıcı, rol, authentication ve authorization |
| Platform | Hata yönetimi, logging, caching ve health checks |

Ödeme ve kargo sağlayıcısı entegrasyonları çekirdek kapsam tamamlandıktan sonra ayrı dış servis adaptörleri olarak ele alınacaktır.

## Hedef mimari

Solution, iş alanı büyüdükçe aşağıdaki sorumluluklara ayrılacaktır:

```text
src/
├── ECommerce.Api
├── ECommerce.Application
├── ECommerce.Domain
└── ECommerce.Infrastructure

tests/
├── ECommerce.UnitTests
└── ECommerce.IntegrationTests
```

| Proje | Sorumluluk |
| --- | --- |
| `ECommerce.Api` | HTTP endpoint'leri, middleware ve uygulama başlangıcı |
| `ECommerce.Application` | Use-case'ler, DTO'lar, validation ve servis sözleşmeleri |
| `ECommerce.Domain` | Entity'ler, value object'ler ve iş kuralları |
| `ECommerce.Infrastructure` | Entity Framework Core, veritabanı ve dış servis adaptörleri |
| `ECommerce.UnitTests` | Domain ve application davranışlarının izole testleri |
| `ECommerce.IntegrationTests` | API ve veri erişim akışlarının bütünleşik testleri |

Bağımlılık yönü iç katmanlara doğrudur. Domain katmanı framework ve veri erişim detaylarından bağımsız tutulur. API projesi composition root görevi görür ve bağımlılıkları Dependency Injection aracılığıyla bir araya getirir.

Katmanlar başlangıçta yapay olarak oluşturulmaz; ilgili sorumluluk ortaya çıktığında solution'a eklenir.

## Teknoloji yığını

| Alan | Teknoloji / yaklaşım |
| --- | --- |
| Dil | C# |
| Platform | .NET 10 |
| Web | ASP.NET Core Web API |
| Sunucu | Kestrel |
| Veri erişimi | Entity Framework Core |
| Veritabanı | İlişkisel veritabanı |
| API dokümantasyonu | OpenAPI / Swagger |
| Authentication | ASP.NET Core Identity ve JWT |
| Authorization | Role ve policy tabanlı yetkilendirme |
| Test | xUnit ve ASP.NET Core integration testing |
| Container | Docker |
| Veri formatı | JSON |

## Tasarım ilkeleri

- Domain kuralları HTTP ve veritabanı detaylarından ayrılır.
- API sınırlarında entity yerine request/response DTO'ları kullanılır.
- Bağımlılıklar doğrudan oluşturulmak yerine Dependency Injection ile sağlanır.
- I/O işlemleri asenkron yürütülür ve uygun noktalarda `CancellationToken` desteklenir.
- Girdiler işleme alınmadan önce doğrulanır.
- Liste endpoint'lerinde filtering, sorting ve pagination uygulanır.
- Birden fazla veri değişikliğinin tutarlılığı transaction ile korunur.
- Gizli bilgiler kaynak kodda veya repository'de tutulmaz.
- Yeni abstraction ve dependency yalnızca somut bir gereksinim olduğunda eklenir.

## API standartları

- Kaynak odaklı REST endpoint'leri kullanılır.
- JSON property adları `camelCase` biçimindedir.
- HTTP durum kodları işlem sonucunu doğru şekilde ifade eder.
- Validation ve iş kuralı hataları tutarlı bir hata sözleşmesiyle döndürülür.
- Beklenmeyen hatalar merkezi exception handling üzerinden RFC 7807 Problem Details formatına çevrilir.
- Kimlik doğrulama ve yetkilendirme birbirinden ayrı ele alınır.
- API sözleşmesinin kaynak noktası OpenAPI dokümanıdır.

Değişken endpoint kataloğu README içinde tekrar edilmez. OpenAPI entegrasyonu tamamlandığında request/response şemaları ve endpoint'ler Swagger arayüzünden yayınlanacaktır. Repository içindeki [ECommerce.Api.http](src/ECommerce.Api/ECommerce.Api.http) dosyası geliştirme sırasında hızlı HTTP kontrolleri için kullanılabilir.

## Başlangıç

### Gereksinimler

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git

SDK kurulumunu doğrulayın:

```bash
dotnet --version
```

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

Varsayılan HTTP adresi:

```text
http://localhost:5080
```

Servisin çalıştığını doğrulayın:

```bash
curl http://localhost:5080/
```

## Yapılandırma

ASP.NET Core yapılandırması aşağıdaki kaynaklardan sağlanır:

1. `appsettings.json`
2. Ortama özel `appsettings.{Environment}.json`
3. Environment variable'lar
4. Komut satırı parametreleri

Aktif ortam `ASPNETCORE_ENVIRONMENT` değişkeniyle belirlenir. Hassas değerler configuration dosyalarına yazılmaz; environment variable veya güvenli bir secret store üzerinden sağlanır.

Yerel çalışma profilleri [launchSettings.json](src/ECommerce.Api/Properties/launchSettings.json) dosyasında tanımlıdır.

HTTPS profilini çalıştırmak için:

```bash
dotnet run \
  --project src/ECommerce.Api/ECommerce.Api.csproj \
  --launch-profile https
```

## Veri erişimi ve ORM

Entity Framework Core, uygulamanın ORM katmanı olarak kullanılacaktır.

- Model değişiklikleri migration dosyalarıyla sürümlenir.
- Migration dosyaları kaynak kontrolüne dahil edilir.
- Okuma sorgularında gereksiz tracking önlenir.
- İlişkiler ve constraint'ler hem domain kurallarıyla hem veritabanı seviyesinde korunur.
- Veritabanı erişimi application katmanına interface'ler üzerinden sunulur.
- Transaction sınırları iş akışına göre belirlenir.
- Production veritabanı güncellemeleri kontrollü deployment adımı olarak uygulanır.

## Güvenlik

- Parolalar düz metin olarak saklanmaz.
- Authentication için kısa ömürlü access token ve kontrollü refresh token akışı kullanılır.
- Endpoint erişimleri role veya policy ile sınırlandırılır.
- Kullanıcıdan gelen bütün girdiler güvenilmeyen veri olarak kabul edilir.
- Hassas veriler log mesajlarına yazılmaz.
- HTTPS production ortamında zorunludur.
- Yetkilendirme yalnızca istemci tarafı kontrollere bırakılmaz.

## Test stratejisi

- Domain iş kuralları unit testlerle doğrulanır.
- Application use-case'leri bağımlılıkları izole edilerek test edilir.
- HTTP sözleşmeleri ve veri erişimi integration testlerle doğrulanır.
- Kritik sipariş ve stok senaryoları başarı, hata ve eş zamanlılık durumlarını kapsar.
- Her hata düzeltmesi mümkün olduğunda regression testiyle korunur.

Temel kalite kontrolleri:

```bash
dotnet build ECommerce.slnx --no-restore
dotnet test ECommerce.slnx --no-build
```

## Geliştirme ve sürümleme

Commit mesajlarında Conventional Commits biçimi kullanılır:

```text
feat: add product creation endpoint
fix: prevent negative stock values
docs: update project documentation
refactor: extract product service
test: cover product validation
chore: update project configuration
```

Değişiklik geçmişi README içinde günlük olarak çoğaltılmaz. Gelişim; Git commit geçmişi, pull request'ler ve gerektiğinde GitHub Releases üzerinden takip edilir.

## Yol haritası

1. Ürün ve kategori kataloğu
2. Entity Framework Core ve kalıcı veri erişimi
3. DTO, validation ve standart hata sözleşmesi
4. Filtering, sorting ve pagination
5. Sepet ve stok yönetimi
6. Sipariş ve transaction akışları
7. Identity, JWT ve yetkilendirme
8. Logging, caching ve health checks
9. Unit ve integration test kapsamı
10. Docker, CI/CD ve production deployment

## Maintainer

[@TyLnZgr](https://github.com/TyLnZgr)
