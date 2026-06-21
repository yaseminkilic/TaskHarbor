# TaskTracker — Devam Promptu

> Bu dosya yarıda kesilmiş bir Claude oturumunu yeniden başlatmak için tasarlandı.
> Yeni bir konuşmada Claude'a bu dosyayı **referans olarak** ver.
> Tüm mimari kararlar **kilitli** (bkz. §5) — yeniden sormaya gerek yok.

> **Son güncelleme:** 2026-06-02
> **Tamamlanan adımlar:** 1–10 (test piramidi dahil tüm planlanan adımlar tamamlandı: Solution → Domain → Infrastructure → Migration → Application → Repos → API + Serilog + Exception Handler → JsonNode extensions → Domain unit tests + Builder pattern → Application service tests + SQLite in-memory fixture → Integration tests `WebApplicationFactory`).
> **Bir sonraki adım:** Plan boş — yeni bir özellik veya milestone gelmesi bekleniyor (öneriler: auth, frontend, CI/CD pipeline, OpenAPI/Swagger, yeni domain capabilities).

---

## 1. Proje Bağlamı

- **Amaç:** Modern test senaryolarını uygulayabileceğimiz, gerçekçi bir use case üzerinden adım adım bir
  .NET 8 + EF Core + SQLite projesi inşa etmek.
- **Use case:** **TaskTracker** — Görev takip API'si.
  - Entity'ler: `User` (1-N) `Project` (1-N) `TaskItem` (N-N) `Tag`
  - İş kuralları: status geçişleri (Open → InProgress → Done), Done olan task yeniden açılamaz/düzenlenemez vb.
- **API stili:** Minimal API (kullanıcı tercihi — Controller değil)
- **Veritabanı:** SQLite (bilgisayarda MSSQL kurulu değil)
- **Hedef framework:** `net8.0`, SDK `8.0.418` (`global.json` ile pinli; sistemde ayrıca 5.0, 9.0, 10.0 SDK'ları mevcut)
- **Çalışma dizini:** `c:\Users\Admin\Desktop\claude\netUI`
- **Platform:** Windows 10, PowerShell 5.1 (PS 7 değil — `SkipHttpErrorCheck` gibi parametreler yok)
- **GitHub remote (private):** https://github.com/peachnektari/tasktracker-net8
  - Branch: `main` (origin/main ile senkron, başka açık branch yok)
  - Son commit: `b235086 test(integration): WebApplicationFactory endpoint tests over SQLite in-memory (#7)`
  - Merge edilmiş PR'lar: **#1** (Application), **#2** (Infrastructure repos), **#3** (API + Serilog), **#4** (JsonNode extensions), **#5** (Domain tests + Builders + User email trim fix), **#6** (Application service tests + SQLite in-memory fixture), **#7** (Integration tests + CustomWebApplicationFactory)
  - Local git config (yalnız bu repoda): `user.name=peachnektari`, `user.email=peachnektari@gmail.com`
- **Git akışı:** Her adım için **feature branch + PR + squash merge** (`gh pr merge <n> --squash --delete-branch`)

---

## 2. Mimari — Clean Architecture

```
Domain         ← Application ← Infrastructure ← Api
                            ↖___________________↙
UnitTests → Application/Domain
IntegrationTests → Api
```

| Katman | Sorumluluk | Bağımlılıklar |
|---|---|---|
| `TaskTracker.Domain` | Entity'ler + iş kuralları + `DomainException` | Yok |
| `TaskTracker.Application` | DTO, servis, validation, repo interface'leri, `IUnitOfWork` | Domain |
| `TaskTracker.Infrastructure` | EF Core, `AppDbContext`, repo implementasyonları, `UnitOfWork`, migrations | Domain, Application |
| `TaskTracker.Api` | Minimal API endpoint'leri, Serilog, `GlobalExceptionHandler`, DI wiring, `Program.cs` | tümü |
| `TaskTracker.UnitTests` | Domain & Application unit testleri (Adım 8-9) | Domain, Application |
| `TaskTracker.IntegrationTests` | API + DB end-to-end testler (WebApplicationFactory) (Adım 10) | Api, Infrastructure |

**Tasarım kararları (sebepleriyle):**
- Domain'de **private setter + private parameterless ctor** → EF materialization'a izin verir ama dış kod
  state'i sadece davranış metodlarıyla değiştirir (encapsulation, anemic-model anti-pattern'inden kaçınma).
- Public ctor'lar parametre kontrolü yapar → Entity'ler her zaman valid state'te yaratılır.
- `TaskItem`'in ctor'u `internal` → sadece `Project.AddTask()` üzerinden yaratılabilir (aggregate root).
- EF konfigürasyonları **Fluent API** ile (DataAnnotations yerine) → Domain'in EF'e bağımlılığı yok.
- `IUnitOfWork.SaveChangesAsync()` ayrı — repository'ler `Add`/`Remove` yapar, kaydetmez. Servis koordinator.
- Application'da `ValidationException` collision'ı için her servis dosyasında `using ValidationException = TaskTracker.Application.Common.Exceptions.ValidationException;` alias var.
- API'de `UseSerilogRequestLogging` **`UseExceptionHandler`'dan ÖNCE** çağrılır — exception handler status'u 500'den 4xx'e değiştirdikten sonra request logging final code'u görsün.
- TaskService `_tasks.AddAsync(task, ct)` EXPLICIT olarak çağrılır (project.AddTask sonrası); EF Core ctor'la set edilen Guid'i gördüğü için entity'i "Modified" sayıp UPDATE atıyor → bu yüzden explicit `Add` gerekli (§3.7'deki bug açıklamasına bak).

---

## 3. Tamamlanan Adımlar (Şu Anki State)

### ✅ Adım 1 — Solution & Projeler
- `global.json` ile SDK 8.0.418 pinlendi.
- `TaskTracker.sln` ve 6 proje oluşturuldu (`src/` ve `tests/`).
- Proje referansları bağlandı.

### ✅ Adım 2 — NuGet Paketleri
| Proje | Paketler |
|---|---|
| Application | `FluentValidation` 11.10.0, `FluentValidation.DependencyInjectionExtensions` 11.10.0 |
| Infrastructure | `Microsoft.EntityFrameworkCore.Sqlite` 8.0.10, `Microsoft.EntityFrameworkCore.Design` 8.0.10 |
| Api | `Microsoft.EntityFrameworkCore.Design` 8.0.10, `Serilog.AspNetCore` 8.0.3, `Serilog.Sinks.Console` 6.0.0, `Serilog.Sinks.File` 6.0.0 |
| UnitTests | `FluentAssertions` 6.12.1 |
| IntegrationTests | `FluentAssertions` 6.12.1, `Microsoft.AspNetCore.Mvc.Testing` 8.0.10, `Microsoft.EntityFrameworkCore.Sqlite` 8.0.10 |
| Global tool | `dotnet-ef` 8.0.10 |

### ✅ Adım 3 — Domain Entities
- `Common/DomainException.cs`
- `Entities/TaskStatus.cs` (enum: Open=0, InProgress=1, Done=2)
- `Entities/User.cs` (email regex, normalization)
- `Entities/Project.cs` (rename, `AddTask`)
- `Entities/Tag.cs` (lowercase normalization)
- `Entities/TaskItem.cs` (Start/Complete/UpdateDetails/Add-RemoveTag)

### ✅ Adım 4 — Infrastructure + Migration
- `Persistence/AppDbContext.cs` — `ApplyConfigurationsFromAssembly` ile
- `Persistence/Configurations/` (User/Project/TaskItem/Tag) — Fluent API
- `appsettings.json` → `ConnectionStrings:Default = "Data Source=tasktracker.db"`
- `Program.cs` → `public partial class Program {}` (integration test için)
- Migration: `20260518131803_Initial` üretildi → `tasktracker.db` oluştu (Users / Projects / Tasks / Tags / TaskTags / __EFMigrationsHistory)

### ✅ Adım 5 — Application Layer (PR #1)
- `Common/Exceptions/`: `NotFoundException`, `ConflictException`, `ValidationException` (FluentValidation `ValidationFailure` ile)
- `Common/DependencyInjection.cs` — `AddApplication()` (`AddValidatorsFromAssembly` + scoped servisler)
- `Abstractions/IUnitOfWork.cs` + `Abstractions/Repositories/` (`IUserRepository`, `IProjectRepository`, `ITaskRepository`)
- `Users/` / `Projects/` / `Tasks/` feature slice'ları (DTO + Validator + Service interface/impl)
- `TaskDto.Status` string olarak taşınıyor; servis sınırında `Enum.TryParse` ile parse

### ✅ Adım 6 — Infrastructure Repository Impls + UnitOfWork (PR #2)
- `Persistence/UnitOfWork.cs` — `AppDbContext.SaveChangesAsync` proxy; tüm repolar aynı `AppDbContext`'i DI scope'tan paylaşır → tek transaction
- `Persistence/Repositories/UserRepository.cs` — email normalize edilerek sorgu (`.Trim().ToLowerInvariant()`)
- `Persistence/Repositories/ProjectRepository.cs` — `GetByIdWithTasksAsync` için `Include(p => p.Tasks)`; listeleme `AsNoTracking`
- `Persistence/Repositories/TaskRepository.cs` — `GetByIdWithTagsAsync` için `Include(t => t.Tags)`; `ListByProjectAsync` opsiyonel status filtresi; `AddAsync` + tag helper'ları
- `Infrastructure/DependencyInjection.cs` → 4 scoped registration eklendi (`IUnitOfWork`, 3 repo)
- `Api/Program.cs` → `AddApplication()` çağrısı (composition root)
- Smoke test: `GET /` 200 — DI container resolve hatasız

### ✅ Adım 7 — API Endpoints + Serilog + Global Exception Handler (PR #3)
**Yeni dosyalar:**
- `Api/Common/GlobalExceptionHandler.cs` (`IExceptionHandler`):
  - `NotFoundException` → 404
  - `ConflictException` → 409
  - `ValidationException` → 400 (`ValidationProblemDetails` + `Errors` dict)
  - `DomainException` → 400
  - fallback → 500
- `Api/Endpoints/UserEndpoints.cs` — `POST/GET /users`, `GET /users/{id}` (MapGroup ile)
- `Api/Endpoints/ProjectEndpoints.cs` — `POST/GET /projects`, `GET /projects/{id}`, `GET /users/{ownerId}/projects`
- `Api/Endpoints/TaskEndpoints.cs` — 9 endpoint (CRUD + start/complete + tag attach/detach + status filter)

**Program.cs güncellemesi:**
- Serilog Console + rolling File (`logs/log-.txt`, daily, 14-day retention)
- `UseSerilogRequestLogging()` **`UseExceptionHandler()`'dan ÖNCE** (final status kodu loglansın)
- `AddProblemDetails()` + `AddExceptionHandler<GlobalExceptionHandler>()`
- 12 endpoint map edildi

**Smoke test:**
- `smoke-test.ps1` — 12 senaryo (happy path + 5 negatif: 409, 400×3, 404)
- Tüm cevaplar doğru status code'la döndü (server log'larıyla doğrulandı)

**Bu adımda yakalanıp düzeltilen 2 bug:**
1. **EF Core "0 rows affected"** task create'de. Sebep: `TaskItem` constructor'da `Guid.NewGuid()` ile Id atadığı için EF, entity'yi Added değil **Modified** sayıp UPDATE çalıştırıyordu. Fix: `ITaskRepository.AddAsync` eklendi, `TaskService.CreateAsync` explicit çağırıyor. **User ve Project'te bu sorun yoktu çünkü zaten `_users.AddAsync` / `_projects.AddAsync` çağrılıyordu.** Aynı bug ilerideki "yeni entity oluşturma" senaryolarında tekrar çıkabilir — entity'in ctor'unda non-default key atanıyorsa repo'da explicit `AddAsync` şart.
2. **Tag listesi response'larda eksik** Start/Complete/Update sonrası. Sebep: bu metodlar `GetByIdAsync` kullanıyordu (Include yok). Fix: `GetByIdWithTagsAsync`'e geçirildi.

### ✅ Adım 7.5 — JsonNode Read Extensions (PR #4)
Plan dışı küçük bir helper turu (kullanıcı isteğiyle):
- `src/TaskTracker.Api/Common/Json/JsonNodeExtensions.cs` — `JsonNode?` üzerinde safe read helper'ları: `string`, `Guid`, `int`, `bool` için `TryGet*` / `Get*OrNull` / `GetRequired*` varyantları.
- `Required*` `InvalidOperationException` fırlatır (GlobalExceptionHandler'da fallback 500 — istenirse ileride `ValidationException`'a sarılabilir).
- `int`/`bool` için `JsonNode.GetValue<T>` non-throw alternatif sunmadığı için try/catch kullanıldı; testler bunu pinler.
- `tests/TaskTracker.UnitTests/Api/Common/Json/JsonNodeExtensionsTests.cs` — 37 test (missing key, JSON null, non-object node, type mismatch edge case'leri).
- UnitTests artık `TaskTracker.Api` project reference taşıyor (IntegrationTests'te zaten vardı).

### ✅ Adım 9 — Application Service Tests + SQLite Fixture (PR #6)
- `tests/TaskTracker.UnitTests/Application/Fixtures/SqliteInMemoryFixture.cs` — `SqliteConnection("DataSource=:memory:")` + `Open()` (keep-alive) + `DbContextOptionsBuilder.UseSqlite` + `EnsureCreated()` ile şema; gerçek `UnitOfWork` + 3 repo. xUnit `IDisposable` pattern'ı ile her test fresh fixture (xUnit her `[Fact]` için yeni test class instance yaratır).
- 39 servis testi: `UserServiceTests` (6), `ProjectServiceTests` (8), `TaskServiceTests` (25).
- Validator'lar `new CreateXRequestValidator()` ile gerçek instance (mock yok — KARAR-6).
- **Adım 7 bug regression case'leri yeşil ve sabit:**
  - `TaskServiceTests.CreateAsync_PersistsTask_AndDtoHasOpenStatus` — ctor-set Guid'le `Modified` sayan EF davranışı; gerçekten DB'ye INSERT olduğunu doğrular.
  - `TaskServiceTests.CompleteAsync_PreservesTags_InDto` — Start/Complete sonrası `Tags` DTO'da kaldığını (Include path'i) doğrular.
- UnitTests artık `TaskTracker.Infrastructure` project reference de taşıyor (Domain + Application + Api + Infrastructure).
- **Test toplamı:** 124 (49 Domain + 37 JsonNode − 1 deprecated + 39 Application).

### ✅ Adım 8 — Domain Unit Tests + Builders (PR #5)
- `tests/TaskTracker.UnitTests/Common/Builders/` — manuel Builder pattern (KARAR-6):
  - `UserBuilder` — default `alice@example.com` / `Alice`
  - `ProjectBuilder` — owner verilmezse `UserBuilder` ile fallback
  - `TaskItemBuilder` — `Project.AddTask()` üzerinden internal ctor'a erişim
- `tests/TaskTracker.UnitTests/Domain/` — **49 test** (UserTests / ProjectTests / TagTests / TaskItemTests), xUnit + FluentAssertions, mock yok.
- Pinlenen davranışlar: email/displayName validation + normalization, Project naming + AddTask, Tag lowercase trim, TaskItem state machine (Start/Complete idempotent, UpdateDetails-after-Done, AddTag dedupe by Id, AddTag null throws, RemoveTag no-op).
- **Domain düzeltmesi:** `User..ctor` email validation sırası — eskiden regex raw input üzerinde çalıştığı için `"  alice@example.com  "` reddediliyordu, sonraki `.Trim()` iş yapmıyordu. Fix: önce IsNullOrWhiteSpace check, sonra `.Trim().ToLowerInvariant()`, sonra regex. Plan'ın `NormalizesEmail (lowercase, trim)` niyetiyle hizalandı.
- **Test toplamı:** 85 (49 Domain + 37 JsonNode − 1 deprecated case).

---

## 4. ⚠️ Bilinen Problemler

### ✅ Zombie testhost (ÇÖZÜLDÜ — 2026-06-02)
- Bilgisayar yeniden başlatıldı; 18.05.2026 başlangıçlı stuck dotnet/testhost handle'ları gitti.
- Doğrulama: `dotnet test` Adım 8'de temiz çalıştı (85/85).
- İleride benzeri kilit görülürse: önce `dotnet build-server shutdown`, sonra `Get-Process dotnet,testhost | Format-Table Id,StartTime` ile eski PID'leri tespit edip `Stop-Process -Force`; OS-level stuck handle ise reboot.

---

## 5. ✅ Verilen Kararlar (kilitli — yeniden sorulmasın)

| # | Karar | Seçim |
|---|---|---|
| KARAR-1 | Veri erişim | **Repository Pattern (saf Clean)** — `IUserRepository`, `IProjectRepository`, `ITaskRepository`, `IUnitOfWork` |
| KARAR-2 | Validation | **FluentValidation** |
| KARAR-3 | Auth | **Yok** (gerekirse ileride ayrı milestone) |
| KARAR-4 | Hata modeli | **Exception-based** — `NotFoundException`, `ConflictException`, `ValidationException` + `IExceptionHandler` → `ProblemDetails` |
| KARAR-5 | TaskStatus enum | **DTO'da string** olarak taşı, servis sınırında parse et |
| KARAR-6 | Test data | **Manuel Builder pattern** (`UserBuilder`, `ProjectBuilder`, `TaskItemBuilder`) |
| KARAR-7 | Logging | **Serilog** (`Serilog.AspNetCore` + Console + File rolling sink) |
| Workflow | Git akışı | **Feature branch + PR + squash merge** her adım için |

---

## 6. Sonraki Adımlar (Plan)

### ⏭️ Adım 10 — Integration Tests (`WebApplicationFactory`)
**Branch:** `feature/integration-tests`

- `CustomWebApplicationFactory<Program>` — `ConfigureWebHost` ile:
  - Mevcut `AppDbContext` registration'ını kaldır (`services.RemoveAll<DbContextOptions<AppDbContext>>()`)
  - SQLite `:memory:` connection manuel oluştur ve `OpenAsync()` çağır
  - `AddDbContext` bu connection'ı kullansın
  - `db.Database.EnsureCreated()` (migration kullanmadan şemayı uygula)
- `Endpoints/UserEndpointsTests.cs` — `POST 201` + `GET 200` + `GET not-found 404` + `POST duplicate 409`
- `Endpoints/ProjectEndpointsTests.cs` — Create/Get/List + validation 400
- `Endpoints/TaskEndpointsTests.cs`:
  - Happy path (create → start → complete)
  - State machine: `Done` sonrası `Start` → 400 (DomainException → ProblemDetails)
  - Status filter query string parse hatası → 400
  - Tag attach/detach
  - Get non-existent → 404
- Mvc.Testing client ile HTTP, `HttpClient.PostAsJsonAsync`/`GetFromJsonAsync<TaskDto>`

### 🔁 Adım 7'deki bug'lar için ekstra regression test'leri
Adım 9 ve 10'da unutmamak için işaretle:
- TaskItem oluşturma (yeni TaskItem persist olmalı, EF Modified saymamalı)
- Start/Complete/Update sonrası response'taki `tags` koleksiyonu doğru
- Status query string `Bogus` gibi geçersiz değerle → 400

---

## 7. Yardımcı Komut Referansı

```powershell
# Tam build
dotnet build TaskTracker.sln

# Sadece src (test projeleri kilitliyse — reboot öncesi)
dotnet build src/TaskTracker.Api/TaskTracker.Api.csproj

# API'yi çalıştır
dotnet run --project src/TaskTracker.Api
# veya özel port
dotnet run --project src/TaskTracker.Api --urls http://localhost:5099

# End-to-end smoke test (12 senaryo)
./smoke-test.ps1

# Yeni migration
dotnet ef migrations add <Name> `
  --project src/TaskTracker.Infrastructure `
  --startup-project src/TaskTracker.Api `
  --output-dir Persistence/Migrations

# DB güncelle
dotnet ef database update `
  --project src/TaskTracker.Infrastructure `
  --startup-project src/TaskTracker.Api

# DB'yi sıfırla (dosya sil + tekrar update)
Remove-Item src/TaskTracker.Api/tasktracker.db -Force
dotnet ef database update --project src/TaskTracker.Infrastructure --startup-project src/TaskTracker.Api

# Zombie süreçleri görmek
Get-Process | Where-Object {$_.ProcessName -match 'dotnet|testhost'} | Format-Table Id,ProcessName,StartTime

# Build server'larını kapat (lock sorunlarında ilk dene)
dotnet build-server shutdown

# Logları izlemek
Get-Content logs/log-*.txt -Tail 20 -Wait
```

### Git / PR akışı

```powershell
# Yeni adım için branch
git checkout -b feature/<step-name>
# ... değişiklikler ...
git add -A
git commit -m "feat(<area>): ..."
git push -u origin feature/<step-name>

# PR aç + squash merge + branch sil
gh pr create --title "..." --body-file .pr-body.tmp   # --fill de var, ya da inline --body
gh pr merge <n> --squash --delete-branch

# Main'e dön + temizle
git checkout main
git pull
git fetch --prune
```

---

## 8. Yeniden Başladığında Claude'a Söylenecekler

```
TaskTracker projesinde devam ediyoruz. Lütfen önce doc/DEVAM_PROMPT.md dosyasını oku.

Tüm mimari kararlar §5'te kilitli — yeniden sormana gerek yok.

Adım 1–9 tamamlandı (en son: Application service testleri + SQLite
in-memory fixture, PR #6). Şimdi Adım 10 — Integration tests ile devam.
Feature branch + PR + squash merge akışıyla. Her dosyayı yazdıktan sonra
neden öyle yazdığını kısaca açıkla (en fazla 2-3 cümle).

Adım 9 ve 10'da §6'daki "Adım 7'deki bug'lar için regression test'leri"
listesini unutma — bunlar test piramidinin yakalaması gereken hata türleri.
```

---

## 9. Git İş Akışı Notları

- **Main her zaman yeşil derlenir** kuralı (`dotnet build TaskTracker.sln` zero error)
- Her adım kendi feature branch + PR + squash merge
- PR açıklamasında: özet + test plan + ilgili KARAR referansları + (varsa) bulunan bug'lar
- `doc/` klasörü `.gitignore`'da → bu dosya repoya gitmez, yerel kalır
- `*.log` ve `logs/` da gitignored
- Uzun PR body için `.pr-body.tmp` kullan (gh CLI body file ile geçir, sonra sil) — inline body bazen PowerShell escape sorunu yapıyor

---

## 10. Dosya Envanteri (2026-06-02 itibarıyla)

```
netUI/
├─ .gitignore                                          (doc/, .claude/, *.db, *.log, logs/ ignored)
├─ global.json                                         (SDK 8.0.418 pin)
├─ README.md                                           (project status, endpoint tablosu, getting started)
├─ TaskTracker.sln
├─ smoke-test.ps1                                      (12 senaryolu uçtan uca smoke test)
├─ doc/                                                (gitignored)
│  └─ DEVAM_PROMPT.md                                  ← bu dosya
├─ src/
│  ├─ TaskTracker.Domain/
│  │  ├─ Common/DomainException.cs
│  │  └─ Entities/ (TaskStatus, User, Project, Tag, TaskItem)
│  ├─ TaskTracker.Application/
│  │  ├─ Abstractions/
│  │  │  ├─ IUnitOfWork.cs
│  │  │  └─ Repositories/ (IUserRepository, IProjectRepository, ITaskRepository)
│  │  ├─ Common/
│  │  │  ├─ DependencyInjection.cs                     (AddApplication)
│  │  │  └─ Exceptions/ (NotFoundException, ConflictException, ValidationException)
│  │  ├─ Users/   (Dtos/, Validators/, Services/)
│  │  ├─ Projects/ (Dtos/, Validators/, Services/)
│  │  └─ Tasks/    (Dtos/, Validators/, Services/)
│  ├─ TaskTracker.Infrastructure/
│  │  ├─ DependencyInjection.cs                        (DbContext + 3 repo + UoW scoped)
│  │  └─ Persistence/
│  │     ├─ AppDbContext.cs
│  │     ├─ UnitOfWork.cs
│  │     ├─ Configurations/ (UserConfiguration, ProjectConfiguration, TaskItemConfiguration, TagConfiguration)
│  │     ├─ Repositories/ (UserRepository, ProjectRepository, TaskRepository)
│  │     └─ Migrations/ (20260518131803_Initial.cs + Designer + ModelSnapshot)
│  └─ TaskTracker.Api/
│     ├─ Program.cs                                    (Serilog + AddApplication + AddInfrastructure + handler + endpoint map)
│     ├─ Common/
│     │  ├─ GlobalExceptionHandler.cs                  (IExceptionHandler -> ProblemDetails)
│     │  └─ Json/JsonNodeExtensions.cs                 (Adım 7.5 — safe JsonNode read helpers)
│     ├─ Endpoints/ (UserEndpoints, ProjectEndpoints, TaskEndpoints)
│     ├─ appsettings.json
│     ├─ logs/                                         (runtime, gitignored)
│     └─ tasktracker.db                                (gitignored)
└─ tests/
   ├─ TaskTracker.UnitTests/                           ← 124 test (Domain + Application + JsonNode)
   │  ├─ Common/Builders/ (UserBuilder, ProjectBuilder, TaskItemBuilder)
   │  ├─ Domain/ (UserTests, ProjectTests, TagTests, TaskItemTests — 49 test)
   │  ├─ Application/
   │  │  ├─ Fixtures/SqliteInMemoryFixture.cs          (SQLite in-memory + EnsureCreated)
   │  │  ├─ Users/UserServiceTests.cs                  (6 test)
   │  │  ├─ Projects/ProjectServiceTests.cs            (8 test)
   │  │  └─ Tasks/TaskServiceTests.cs                  (25 test, Adım 7 bug regression'larıyla)
   │  └─ Api/Common/Json/JsonNodeExtensionsTests.cs    (37 test)
   └─ TaskTracker.IntegrationTests/                    ← henüz test yok (Adım 10)
```
