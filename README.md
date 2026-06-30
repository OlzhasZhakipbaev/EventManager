# EventManager API

REST API для управления событиями. Данные хранятся в памяти приложения.

## Стек

- .NET 8
- ASP.NET Core Web API
- Swagger / OpenAPI

## Запуск

```bash
git clone - url
cd EventManager
dotnet build
dotnet run

После запуска Swagger доступен по адресу: (http://localhost:{port}/swagger/index.html)

## Эндпоинты

| Метод | Путь | Описание |
|-------|------| ---------|
| GET | /api/event | Получить все события |
| GET | /api/event/{id} | Получить событие по ID |
| POST | /api/event | Создать событие |
| PUT | /api/event | Изменить событие |
| DELETE | /api/event/{id} | Удалить событие |

## Формат запроса для добавления события

json
{
  "title": "Название события",
  "description": "Описание (необязательно)",
  "startAt": "2025-01-01T10:00:00",
  "endAt": "2025-01-01T12:00:00"
}

## Важно

Данные хранятся в памяти — при перезапуске приложения все события удаляются.
