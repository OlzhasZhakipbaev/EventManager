# EventManager

Система из трёх независимых сервисов. У каждого своя база PostgreSQL. Подтверждение брони передаётся через Kafka: сервис броней публикует событие, сервис событий по нему уменьшает число свободных мест.

## Состав системы

| Сервис | Что делает | База в Docker | Порт |
|--------|------------|---------------|------|
| Users | Регистрация, вход, выдача JWT | `users` | http://localhost:5101 |
| Events | CRUD событий и учёт мест | `events` | http://localhost:5102 |
| Bookings | Создание и отмена броней, подтверждение | `bookings` | http://localhost:5103 |

Kafka доступна с хоста на `localhost:9092`. Внутри Docker сервисы подключаются к `kafka:29092`. Топик — `booking-confirmed`.

JWT выдаёт только Users. Events и Bookings проверяют тот же секрет, издателя и аудиторию (`EventManager`). Управление событиями (`POST`, `PUT`, `DELETE /events`) доступно роли `Admin`. Эндпоинты `/bookings` требуют аутентификации; идентификатор пользователя читается из claims.

Сервисы не вызывают друг друга по HTTP. Изменение мест происходит только при обработке сообщения из Kafka.

## Поток BookingConfirmed

1. Клиент создаёт бронь в Bookings (`POST /bookings`). Бронь сохраняется со статусом `Pending`.
2. Фоновый обработчик Bookings подтверждает бронь и сначала записывает статус `Confirmed` в базу броней.
3. После сохранения Bookings публикует `BookingConfirmed` в топик `booking-confirmed`. Ключ сообщения — идентификатор события, чтобы брони одного события обрабатывались по порядку.
4. Events подписан на этот топик (группа `events-service`). При получении сообщения он находит событие и уменьшает `AvailableSeats` на `SeatCount`.
5. Если события нет или свободных мест не хватает, сообщение пропускается с записью в лог. Подписчик из-за этого не останавливается.

В сообщении только то, что нужно подписчику: идентификаторы брони, события и пользователя, количество мест и момент подтверждения.

## Запуск

Нужен Docker.

```bash
docker compose up -d --build
```

Команда читает `docker-compose.yml`, который подключает `docker-compose_14.yml`: Zookeeper, Kafka, три базы PostgreSQL и три API. При старте каждый сервис применяет свою миграцию.

Swagger:

- http://localhost:5101/swagger
- http://localhost:5102/swagger
- http://localhost:5103/swagger

Остановка:

```bash
docker compose down
```

### Проверка сценария

1. `POST /auth/register` на Users с `"role": "Admin"`, затем `POST /auth/login`. В `data` придёт JWT.
2. В Swagger Events нажмите **Authorize** и вставьте токен без слова `Bearer`. `POST /events` создаёт событие. Запомните `totalSeats`: сразу после создания столько же свободных мест.
3. `POST /bookings` с `eventId` этого события. Ответ **202**, статус `Pending`.
4. Через несколько секунд `GET /bookings/{id}` показывает `Confirmed`.
5. `GET /events/{id}` показывает `availableSeats` на единицу меньше. Это результат обработки `BookingConfirmed` из Kafka.
