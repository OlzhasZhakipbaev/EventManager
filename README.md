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

Схема БД управляется **миграциями EF Core** (`events`, `bookings`, FK `event_id`). При старте приложения вызывается `Database.Migrate()` — недостающие миграции применяются сами, руками SQL писать не нужно.

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

## Эндпоинты событий

| Метод  | Путь              | Описание                                                              |
|--------|-------------------|------------------------------------------------------------------------|
| GET    | /events           | Получить все события (фильтрация по `title`, `from`, `to` и пагинация `page`, `pageSize`) |
| GET    | /events/{id}      | Получить событие по ID                                                |
| POST   | /events           | Создать событие                                                       |
| PUT    | /events/{id}      | Изменить событие                                                      |
| DELETE | /events/{id}      | Удалить событие                                                       |

## Эндпоинты бронирований

| Метод | Путь                    | Описание |
|-------|-------------------------|----------|
| POST  | /events/{id}/book       | Создать бронь для события. Возвращает **202 Accepted**, в заголовке `Location` — ссылка на бронь (`/bookings/{bookingId}`). Если событие не найдено — **404**. Если свободных мест нет — **409 Conflict**. |
| GET   | /bookings/{id}          | Получить текущее состояние брони по её идентификатору. **200 OK** или **404**, если бронь не найдена. |

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

`status`: `0` — Pending, `1` — Confirmed, `2` — Rejected.

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
| 400        | Некорректные данные запроса (например, `endAt` раньше `startAt`, `totalSeats <= 0`) |
| 404        | Событие или бронь не найдены                          |
| 409        | Нет свободных мест (`POST /events/{id}/book`)         |
| 202        | Бронь принята в обработку (`POST /events/{id}/book`)  |
| 500        | Внутренняя ошибка сервера                              |

## Важно

Данные хранятся в PostgreSQL и остаются после перезапуска приложения.
