# EventManager API

REST API для управления событиями и бронированиями. Данные хранятся в PostgreSQL (Entity Framework Core).

## Стек

- .NET 8
- ASP.NET Core Web API
- Swagger / OpenAPI
- PostgreSQL + EF Core (Npgsql)
- InMemory-провайдер EF Core в юнит-тестах
- Testcontainers + PostgreSQL в интеграционных тестах

## Требования

Для запуска приложения нужен **PostgreSQL** (локально или в Docker). Юнит-тесты базу не требуют (InMemory). **Интеграционные тесты** (`EventApi.IntegrationTests`) поднимают PostgreSQL через Testcontainers — для них должен быть запущен **Docker**.

## Строка подключения

Задаётся в `EventManager/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=postgres"
  }
}
```

Подставьте хост, порт, имя БД и учётные данные своей установки. Через Docker: поднимите сервис `events-db` из `docker-compose.yml` (порт `5432`, БД `eventapi`).

Схема БД управляется **миграциями EF Core** (`users`, `events`, `bookings`, уникальный `login`, FK `bookings.user_id` → `users.id`, FK `event_id`). При старте приложения вызывается `Database.Migrate()` — недостающие миграции применяются сами, руками SQL писать не нужно.

```bash
# создать новую миграцию
dotnet ef migrations add <Name> --project EventManager --startup-project EventManager

# применить миграции (то же самое делает запуск приложения)
dotnet ef database update --project EventManager --startup-project EventManager
```

## Запуск

```bash
git clone - url
cd EventManager
dotnet build
dotnet run --project EventManager --launch-profile http
dotnet test
```

После запуска Swagger: http://localhost:5164/swagger/index.html

- Юнит-тесты (`EventService.Test`): `UseInMemoryDatabase`, PostgreSQL не нужен.
- Интеграционные тесты (`EventApi.IntegrationTests`): один контейнер Testcontainers, перед каждым тестом `EnsureDeleted()` + `Migrate()`. Нужен Docker.

## Роли и права

| Роль    | Кто получает | Что можно |
|---------|--------------|-----------|
| `User`  | регистрация без `role` или `"role": "User"` | `POST /events/{id}/book`, `GET /bookings/{id}`, `DELETE /bookings/{id}` — только свои брони |
| `Admin` | регистрация с `"role": "Admin"` (для тестов) | всё, что есть у `User`, плюс `POST` / `PUT` / `DELETE /events`; может отменить любую бронь |

`GET /events` и `GET /events/{id}` доступны без токена. `POST /auth/register` и `POST /auth/login` тоже без токена.

## JWT: конфигурация

Секция `Jwt` в `EventManager/appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "EventManager-Jwt-Signing-Key-32ch!",
    "Issuer": "EventManager",
    "Audience": "EventManager",
    "ExpirationMinutes": 60
  }
}
```

`Secret` должен быть не короче 32 символов (HMAC-SHA256). В продакшне не храните ключ в репозитории: задайте его через переменную окружения `Jwt__Secret` или секреты хоста и используйте длинное случайное значение.

## JWT: как получить токен в Swagger

1. Запустите API (`dotnet run --project EventManager --launch-profile http`) и откройте http://localhost:5164/swagger.
2. `POST /auth/register` — логин, пароль, для админа добавьте `"role": "Admin"`.
3. `POST /auth/login` — те же логин и пароль. В `data` придёт JWT.
4. Нажмите **Authorize**, вставьте токен (без слова `Bearer`) и подтвердите.
5. Дальше защищённые методы уходят с заголовком `Authorization: Bearer …`.

Проверка прав: создать событие без токена → **401**; с токеном `User` → **403**; с токеном `Admin` → **201**.

## Эндпоинты аутентификации

| Метод | Путь              | Доступ        | Описание |
|-------|-------------------|---------------|----------|
| POST  | /auth/register    | без токена    | Регистрация. Тело: `login`, `password`, необязательно `role` (`User` по умолчанию, `Admin` допустим для тестов). |
| POST  | /auth/login       | без токена    | Вход. Возвращает JWT. |

## Эндпоинты событий

| Метод  | Путь              | Описание                                                              |
|--------|-------------------|------------------------------------------------------------------------|
| GET    | /events           | Получить все события (фильтрация по `title`, `from`, `to` и пагинация `page`, `pageSize`) |
| GET    | /events/{id}      | Получить событие по ID                                                |
| POST   | /events           | Создать событие. Только `Admin`                                       |
| PUT    | /events/{id}      | Изменить событие. Только `Admin`                                      |
| DELETE | /events/{id}      | Удалить событие. Только `Admin`                                       |

## Эндпоинты бронирований

| Метод | Путь                    | Описание |
|-------|-------------------------|----------|
| POST  | /events/{id}/book       | Создать бронь (нужен JWT). **202 Accepted**, `Location: /bookings/{bookingId}`. Прошедшее событие — **400**. Лимит 10 активных броней пользователя — **409**. Нет мест — **409**. |
| GET   | /bookings/{id}          | Своя бронь или любая для `Admin`. **200** / **404** / **403**. |
| DELETE| /bookings/{id}          | Отменить бронь: свою или любую для `Admin`. **403**, если чужая. |

### Формат ответа `POST /events/{id}/book`

```json
{
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "eventId": 1,
    "status": 0,
    "createdAt": "2026-08-25T06:00:00Z",
    "processedAt": null
  },
  "success": true,
  "statusCode": 202,
  "message": "Номер брони ..."
}
```

`status`: `0` — Pending, `1` — Confirmed, `2` — Rejected, `3` — Cancelled.

## Модель Event

| Поле             | Тип     | Обязательное | Описание |
|------------------|---------|--------------|----------|
| `Id`             | `int`   | да           | Уникальный идентификатор события |
| `Title`          | `string`| да           | Название |
| `Description`    | `string?` | нет        | Описание |
| `StartAt`        | `DateTime` | да        | Дата и время начала |
| `EndAt`          | `DateTime` | да        | Дата и время окончания |
| `TotalSeats`     | `int`   | да           | Общее количество мест. При создании должно быть больше нуля |
| `AvailableSeats` | `int`   | нет          | Текущее число свободных мест. При создании равно `TotalSeats` |

`TryReserveSeats(count)` уменьшает `AvailableSeats`, если мест достаточно, иначе возвращает `false`. `ReleaseSeats(count)` возвращает места в пул (при отклонении брони).

## Модель Booking

| Поле          | Тип            | Обязательное | Описание |
|---------------|----------------|--------------|----------|
| `Id`          | `Guid`         | да           | Уникальный идентификатор брони |
| `EventId`     | `int`          | да           | Идентификатор события |
| `Status`      | `BookingStatus`| да           | Текущий статус |
| `CreatedAt`   | `DateTime`     | да           | Дата и время создания |
| `ProcessedAt` | `DateTime?`    | нет          | Дата и время обработки фоновым сервисом |

### Статусы `BookingStatus`

| Статус      | Значение | Описание |
|-------------|----------|----------|
| `Pending`   | 0        | Бронь создана, ожидает обработки |
| `Confirmed` | 1        | Бронь подтверждена |
| `Rejected`  | 2        | Бронь отклонена |
| `Cancelled` | 3        | Бронь отменена |

При создании бронь всегда получает статус `Pending`, уникальный `Id` (`Guid.NewGuid()`) и текущую дату в `CreatedAt`.

## Синхронизация

| Примитив | Где | Зачем |
|----------|-----|--------|
| `static SemaphoreSlim` | `BookingService.CreateBookingAsync` | Критическая секция «проверка мест + резерв + запись брони». `lock` нельзя использовать с `await`; семафор static, потому что сервис scoped. |

`GetBookingByIdAsync` не блокируется — это только чтение.

## Фоновая обработка

`BookingProcessor` (`BackgroundService`) — синглтон: `DbContext` берёт через `IServiceScopeFactory` (отдельный scope на список Pending и на каждую бронь). Заявки обрабатываются **параллельно** (`Task.WhenAll`):

1. выбирает id броней со статусом `Pending`;
2. для каждой запускает `ProcessBookingAsync` со своим `DbContext` (`Task.Delay` 2 с — имитация внешней системы);
3. если событие есть — `Confirm()` и сохранение; если событие удалено или произошла ошибка — `Reject()`, место возвращается через `ReleaseSeats()`.

Эндпоинт создания отвечает сразу (**202 Accepted**), обработка идёт асинхронно. Через несколько секунд `GET /bookings/{id}` возвращает уже изменённый статус.

## Пример сценария

1. Создать событие:

```
POST /events
```

```json
{
  "id": 1,
  "title": "Конференция",
  "description": "Описание",
  "startAt": "2026-09-01T10:00:00",
  "endAt": "2026-09-01T18:00:00",
  "totalSeats": 5
}
```

2. Создать бронь:

```
POST /events/1/book
```

Ожидается **202 Accepted**, заголовок `Location: /bookings/{bookingId}`, в теле `status: Pending`.

3. Сразу запросить бронь:

```
GET /bookings/{bookingId}
```

Статус — `Pending`.

4. Подождать несколько секунд и повторить `GET /bookings/{bookingId}`.

Статус изменится на `Confirmed`, появится `processedAt`.

## Пример: овербукинг

Событие на **5** мест, **20** одновременных `POST /events/1/book`:

1. `CreateBookingAsync` берёт `SemaphoreSlim` на пару «проверить `AvailableSeats` + `TryReserveSeats` + `SaveChangesAsync`».
2. Ровно **5** запросов получают **202 Accepted** (уникальные `Id`, статус `Pending`).
3. Остальные **15** получают **409 Conflict** (`NoAvailableSeatsException`, сообщение `No available seats for this event`).
4. У события `AvailableSeats = 0`.

Без семафора несколько потоков могли бы прочитать одно и то же значение свободных мест и создать больше пяти броней.

## Формат запроса для добавления события

```json
{
  "id": 1,
  "title": "Название события",
  "description": "Описание (необязательно)",
  "startAt": "2025-01-01T10:00:00",
  "endAt": "2025-01-01T12:00:00",
  "totalSeats": 10
}
```

`totalSeats` обязателен и должен быть больше нуля. `availableSeats` выставляет сервер (`= totalSeats`).

## Пагинация

Эндпоинт `GET /events` поддерживает пагинацию через query-параметры:

| Параметр   | Тип | По умолчанию | Описание                          |
|------------|-----|---------------|------------------------------------|
| `page`     | int | 1             | Номер страницы (начиная с 1)      |
| `pageSize` | int | 10            | Количество элементов на странице  |

Пример запроса:

```
GET /events?page=2&pageSize=20&title=Конференция
```

## Формат ответа при ошибках

В случае ошибки API возвращает JSON-объект следующего вида:

```json
{
  "statusCode": 404,
  "message": "Не удалось найти событие"
}
```

Примеры кодов состояния:

| StatusCode | Когда возникает                                      |
|------------|-------------------------------------------------------|
| 400        | Некорректные данные или событие уже началось          |
| 401        | Нет или невалидный JWT                                |
| 403        | Нет прав (не админ / чужая бронь)                     |
| 404        | Событие или бронь не найдены                          |
| 409        | Нет мест или превышен лимит активных броней (10)      |
| 202        | Бронь принята в обработку (`POST /events/{id}/book`)  |
| 500        | Внутренняя ошибка сервера                              |

## Важно

Данные хранятся в PostgreSQL и остаются после перезапуска приложения.

## Архитектура


Приложение реорганизовано по принципам чистой архитектуры и разделено на четыре отдельных проекта:

```
EventManager.sln
├── EventManager.Domain          # доменные сущности (Event, Booking), доменные исключения — ни от чего не зависит
├── EventManager.Application     # use cases (EventService, BookingService), интерфейсы портов, DTO — зависит только от Domain
├── EventManager.Infrastructure  # DbContext, репозитории, BookingProcessor — зависит от Application и Domain
└── EventManager.Presentation    # Web API, контроллеры, composition root, точка входа — зависит от Application и Infrastructure
```
