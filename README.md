# RhineWaterApi 🌊
API за мониторинг на нивата на река Рейн и изчисляване на плавателните дълбочини в реално време.

## 🚀 Основни функции
- **Автоматична синхронизация:** Бекграунд сървис (`BackgroundService`), който изтегля данни от официалното API на [PegelOnline](https://www.pegelonline.wsv.de/) на всеки 15 минути.
- **Интелигентни изчисления:** Изчислява "Fahrrinnentiefe" (дълбочина на плавателния път) и "Recommended Draft" (препоръчително газене) на базата на референтни стойности (GLW).
- **Оптимизирана база данни:** Използва PostgreSQL за съхранение на исторически данни с автоматично почистване на записи, по-стари от 48 часа.
- **Minimal API:** Модерна и лека архитектура на ендпоинтите с .NET 10.

## 🛠 Технологичен стек
- **Backend:** .NET 10 (C#)
- **ORM:** Entity Framework Core
- **Database:** PostgreSQL (Hosted on neon.tech)
- **Data Source:** PegelOnline REST API v2
- **IDE:** JetBrains Rider (macOS)

## 📊 Ендпоинти
- `GET /api/rhine/latest` - Връща текущото състояние и изчисления за всички станции.
- `GET /api/rhine/history` - Списък с последните измервания.
- `GET /api/rhine/depth-history?station=Kaub&days=7` - Исторически данни за графики.
- `GET /api/rhine/stations` - Списък с конфигурираните станции и техните референтни стойности.
