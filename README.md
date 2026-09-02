# E-Commerce API

C# ve .NET öğrenirken adım adım geliştirdiğimiz bir e-ticaret Web API projesidir.

Bu projenin amacı yalnızca çalışan bir uygulama oluşturmak değildir. Kullandığımız her kavramın hangi problemi çözdüğünü anlamak, gerçek proje pratiği kazanmak ve teknik mülakatlara hazırlanmak da hedeflenmektedir.

> Proje basitten karmaşığa ilerler. Her öğrenme günü küçük, anlaşılır ve çalışan bir commit ile tamamlanır.

## Mevcut durum — Day 01

İlk gün çalışan en küçük ASP.NET Core Web API iskeleti oluşturuldu.

Tamamlananlar:

- .NET solution oluşturuldu.
- ASP.NET Core Web API projesi solution'a eklendi.
- E-ticaret dışı örnek kod kaldırıldı.
- İlk `GET /` endpoint'i eklendi.
- JSON response üretildi.
- Proje derlendi ve gerçek bir HTTP isteğiyle doğrulandı.

Henüz özellikle eklemediklerimiz:

- Entity modelleri
- CRUD işlemleri
- Veritabanı bağlantısı
- Entity Framework Core
- Authentication ve authorization
- Test projeleri

Bir sonraki adımda C# veri tiplerini, değişkenleri ve ilk `Product` sınıfını öğreneceğiz.

## Kullanılan teknolojiler

| Teknoloji | Görevi |
| --- | --- |
| C# | Uygulamayı yazdığımız programlama dili |
| .NET 10 | Uygulamanın geliştirme ve çalışma platformu |
| ASP.NET Core | Web API geliştirme çatısı |
| Minimal API | Endpoint'leri az kodla tanımlayan ASP.NET Core yaklaşımı |
| Kestrel | HTTP isteklerini karşılayan web sunucusu |
| HTTP ve JSON | İstemci ile API arasındaki iletişim biçimleri |

İlerleyen günlerde ORM olarak **Entity Framework Core** kullanacağız.

**ORM (Object-Relational Mapping)**, C# nesneleriyle ilişkisel veritabanı tabloları arasında eşleme kurar. Böylece veritabanı işlemlerinin önemli bir bölümünü C# koduyla ifade edebiliriz. ORM öğrenirken SQL'in ve ilişkisel veritabanının temel mantığını da ayrıca ele alacağız.

## Gereksinimler

- .NET 10 SDK
- Git
- İsteğe bağlı olarak Visual Studio Code, Visual Studio veya JetBrains Rider

Kurulu .NET sürümünü kontrol etmek için:

```bash
dotnet --version
```

## Projeyi çalıştırma

Bağımlılıkları hazırla:

```bash
dotnet restore
```

Solution içindeki projeleri derle:

```bash
dotnet build ECommerce.slnx
```

API'yi çalıştır:

```bash
dotnet run --project src/ECommerce.Api/ECommerce.Api.csproj
```

Uygulama geliştirme ortamında şu adreste çalışır:

```text
http://localhost:5080
```

Başka bir terminalden isteği gönder:

```bash
curl http://localhost:5080/
```

Beklenen cevap:

```json
{
  "message": "ECommerce API is running."
}
```

## Mevcut endpoint

| HTTP metodu | Adres | Açıklama | Başarılı cevap |
| --- | --- | --- | --- |
| `GET` | `/` | API'nin çalıştığını kontrol eder | `200 OK` |

Bir **endpoint**, HTTP metodu ile URL yolunun birleşimidir. Örneğin `GET /`, uygulamanın kök yoluna gönderilen bir okuma isteğidir.

## Proje yapısı

```text
e-ticaretAPI/
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

- `ECommerce.slnx`: Bir veya daha fazla projeyi aynı solution altında toplar.
- `ECommerce.Api.csproj`: Projenin türünü, hedef framework'ünü ve bağımlılıklarını tanımlar.
- `Program.cs`: Uygulamanın başlangıç noktasıdır.
- `appsettings.json`: Uygulamanın yapılandırma ayarlarını tutar.
- `launchSettings.json`: Yerel geliştirme profillerini ve adreslerini tanımlar.
- `ECommerce.Api.http`: Editör üzerinden örnek HTTP istekleri göndermeyi sağlar.

## Öğrenme yol haritası

- [x] Day 01 — Solution, Web API iskeleti ve ilk endpoint
- [ ] C# değişkenleri, veri tipleri ve operatörler
- [ ] Class, object ve ilk `Product` modeli
- [ ] Koleksiyonlar ve bellekte ürün yönetimi
- [ ] HTTP metotları, REST ve durum kodları
- [ ] Ürünler için CRUD işlemleri
- [ ] Encapsulation, interface ve Dependency Injection
- [ ] Entity Framework Core ve ORM
- [ ] Veritabanı, migration ve ilişkiler
- [ ] `Product` ve `Category` ilişkisi
- [ ] DTO, mapping ve validation
- [ ] Async programlama, LINQ, filtering ve pagination
- [ ] Sepet, sipariş, stok ve transaction yönetimi
- [ ] Global hata yönetimi, logging ve caching
- [ ] JWT authentication ve role-based authorization
- [ ] Unit ve integration testleri
- [ ] Docker, CI/CD ve deployment

Konular takvim uğruna hızlandırılmayacaktır. Bir başlık gerektiğinde birden fazla güne bölünebilir.

## Günlük ilerleme

| Gün | Konu | Durum |
| --- | --- | --- |
| Day 01 | Proje kurulumu, Minimal API, ilk endpoint ve JSON response | Tamamlandı |
| Day 02 | C# veri tipleri, değişkenler ve ilk `Product` sınıfı | Sırada |

Bu tablo her öğrenme gününün sonunda güncellenecektir.

## Commit standardı

Commit mesajlarında şu biçimi kullanacağız:

```text
<type>(day-XX): <kısa ve açıklayıcı mesaj>
```

| Tür | Ne zaman kullanılır? |
| --- | --- |
| `feat` | Yeni bir özellik eklendiğinde |
| `fix` | Bir hata düzeltildiğinde |
| `docs` | Yalnızca dokümantasyon değiştiğinde |
| `refactor` | Davranış değişmeden kod iyileştirildiğinde |
| `test` | Test eklendiğinde veya güncellendiğinde |
| `chore` | Kurulum ve bakım işlemlerinde |

Örnekler:

```text
chore(day-01): initialize ASP.NET Core Web API
feat(day-02): add Product model
feat(day-04): implement product CRUD endpoints
feat(day-06): configure Entity Framework Core
refactor(day-08): move product logic into service layer
test(day-12): add product service unit tests
```

Günlük çalışma kuralımız:

1. O günün tek ve küçük öğrenme hedefini belirleriz.
2. Kodu yazıp ne yaptığını açıklarız.
3. Derleme ve ilgili testleri çalıştırırız.
4. `git status` ve staged diff'i kontrol ederiz.
5. Anlamlı commit mesajıyla commit oluştururuz.
6. README içindeki günlük ilerleme tablosunu güncelleriz.

## Öğrenme yaklaşımı

Her yeni konuyu şu sırayla ele alacağız:

1. Kavramın sade tanımı
2. Neden kullanıldığının açıklanması
3. Küçük bir kod örneği
4. Kodun satır satır incelenmesi
5. E-ticaret projesine uygulanması
6. Mini alıştırma
7. Teknik mülakat soruları
8. Çalışan kodun commit edilmesi

## Proje durumu

Bu bir öğrenme projesidir ve henüz production kullanımı için hazır değildir. Yapı, her yeni kavramın çözdüğü problem görülebilecek şekilde kademeli olarak geliştirilecektir.
